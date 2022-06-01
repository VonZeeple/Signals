using ProtoBuf;
using signals.src.signalNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace signals.src.hangingwires
{
    public class HangingWiresMod : ModSystem
    {

        public HangingWiresRenderer Renderer;

        private PendingWireRenderer WireRenderer;

        IServerNetworkChannel serverChannel;
        IClientNetworkChannel clientChannel;

        HangingWiresData data = new HangingWiresData();
        ICoreAPI api;
        ICoreServerAPI sapi;
        ICoreClientAPI capi;

        #region ModSystem

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.api = api;

            if (api.World is IClientWorldAccessor)
            {
                clientChannel = ((ICoreClientAPI)api).Network.RegisterChannel("hangingwires")
                .RegisterMessageType(typeof(HangingWiresData))
                .RegisterMessageType(typeof(AddConnectionPacket))
                .SetMessageHandler<HangingWiresData>(onDataFromServer);

            }
            else
            {
                serverChannel = ((ICoreServerAPI)api).Network.RegisterChannel("hangingwires")
               .RegisterMessageType(typeof(HangingWiresData))
               .RegisterMessageType(typeof(AddConnectionPacket))
               .SetMessageHandler<AddConnectionPacket>(OnAddConnectionFromClient);
            }
        }

        internal List<WireConnection> GetWireConnectionsFrom(NodePos pos)
        {
            List<WireConnection> toProcess = data.connections.Where(c => c.pos1 == pos || c.pos2 == pos).ToList();
            List<WireConnection> output = new List<WireConnection>();
            foreach(WireConnection con in toProcess)
            {
                if(con.pos1 == pos)
                {
                    output.Add(new WireConnection(con.pos1, con.pos2));
                }
                else
                {
                    output.Add(new WireConnection(con.pos2, con.pos1));
                }
            }
            return output;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;
            capi.Event.ChunkDirty += OnChunkDirty;
            capi.Event.AfterActiveSlotChanged += OnActiveSlotChanged;
            api.Event.BlockTexturesLoaded += onLoaded;
            api.Event.LeaveWorld += () =>
            {
                Renderer?.Dispose();
            };

        }

        private void OnChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason){
            if( reason == EnumChunkDirtyReason.NewlyLoaded){
                Renderer.UpdateWiresMesh(data);
            }       
        }

        private void onDataFromServer(HangingWiresData data)
        {
            this.data = data;
            Renderer.UpdateWiresMesh(data);
        }


        private void onLoaded()
        {
            Renderer = new HangingWiresRenderer(capi, this);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;

            api.Event.GameWorldSave += Event_GameWorldSave;
            api.Event.SaveGameLoaded += Event_SaveGameLoaded;
            sapi.Event.PlayerNowPlaying += Event_OnPlayerJoin;
            sapi.Event.ChunkColumnLoaded += Event_ChunksLoaded;
        }

        private void Event_ChunksLoaded(Vec2i chunkCoord, IWorldChunk[] chunks){
            foreach(IPlayer player in sapi.World.AllOnlinePlayers){
                foreach(IWorldChunk chunk in chunks){
                    if (chunk.Entities.Contains(player.Entity)){
                        //serverChannel.SendPacket<HangingWiresData>(data, player as IServerPlayer);
                    }
                }
            }
        }

        private void Event_GameWorldSave()
        {
            sapi.WorldManager.SaveGame.StoreData("hangingWiresData", SerializerUtil.Serialize(data));
        }

        private void Event_OnPlayerJoin(IServerPlayer player)
        {
            serverChannel.SendPacket<HangingWiresData>(data, player);
        }

        private void Event_SaveGameLoaded()
        {
            
            byte[] data = sapi.WorldManager.SaveGame.GetData("hangingWiresData");

                try
                {
                    this.data = SerializerUtil.Deserialize<HangingWiresData>(data);
                }
                catch(Exception)
                {
                    this.data = new HangingWiresData();
                }

        }

        #endregion

        #region interactions

        public void RemoveAllNodesAtBlockPos(BlockPos pos)
        {
            if (api.Side == EnumAppSide.Client) return;

            List<WireConnection> toRemove = data.connections.Where((WireConnection con) => {
                return con.pos1.blockPos == pos || con.pos2.blockPos == pos;
            }).ToList();
            if(toRemove.Count > 0)
            {
                foreach(WireConnection con in toRemove)
                {
                    data.connections.Remove(con);
                }
                

                serverChannel.BroadcastPacket(data);
            }
            
        }

        public bool IsHoldingWire(IPlayer player){
            Item item = player?.Entity.RightHandItemSlot.Itemstack?.Item;
            return item?.Code?.ToString() == "signals:el_wire";
        }

        public bool UseWire(IPlayer player){
            ItemStack itemStack = player?.InventoryManager.ActiveHotbarSlot.Itemstack;
            if ( itemStack?.Item?.Code?.ToString() != "signals:el_wire") return false;
            player?.InventoryManager.ActiveHotbarSlot.TakeOut(1);
            player?.InventoryManager.ActiveHotbarSlot.MarkDirty();
            return true;
        }

        public void OnActiveSlotChanged(ActiveSlotChangeEventArgs slotChange){
            pendingNode = null;
            WireRenderer?.Dispose();
        }

        public void OnAddConnectionFromClient(IServerPlayer fromPlayer, AddConnectionPacket networkMessage)
        {
            var connection = networkMessage.connection as WireConnection;
            if (connection == null) return;

            //TODO: add checks to be sure that their is a node provider at the position (never trust the client)

            bool added = data.connections.Add(connection);
            if (added)
            {
                if (UseWire(fromPlayer)){
                    api.ModLoader.GetModSystem<SignalNetworkMod>()?.OnWireAdded(connection);
                    serverChannel.BroadcastPacket(data);
                }
            }
        }

        NodePos pendingNode = null;
        public NodePos GetPendingNode()
        {
            return this.pendingNode;
        }

        public void ConnectWire(NodePos pos, IPlayer byPlayer, IHangingWireAnchor anchor)
        {
            if (api.Side == EnumAppSide.Server) return;

            if (!IsHoldingWire(byPlayer)) return;

            if(pendingNode == null)
            {
                pendingNode = pos;
                Vec3f offset = anchor.GetAnchorPosInBlock(pos);
                capi?.ShowChatMessage(String.Format("Pending {0}:{1}",pos.blockPos,pos.index));
                WireRenderer = new PendingWireRenderer(capi, this,pos.blockPos, offset);
            }
            else
            {
                capi?.ShowChatMessage(String.Format("trying to attach {0}:{1}", pos.blockPos, pos.index));
                WireConnection connection = new WireConnection(pendingNode, pos);
                clientChannel.SendPacket(new AddConnectionPacket() { connection = connection, byPlayer = byPlayer.PlayerUID});
                pendingNode = null;
                WireRenderer?.Dispose();
            }
        }
        #endregion
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class AddConnectionPacket
    {
        public WireConnection connection;
        public string byPlayer;

        public AddConnectionPacket()
        {
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class HangingWiresData
    {
        //public Dictionary<long, HangingWiresNetwork> HangingWiresNetworks = new Dictionary<long, HangingWiresNetwork>();
        //public long nextNetworkId = 1;
        public HashSet<WireConnection> connections = new HashSet<WireConnection>();

    }
}
