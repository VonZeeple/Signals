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
                .SetMessageHandler<HangingWiresData>(onDataFromServer);
            }
            else
            {
                serverChannel = ((ICoreServerAPI)api).Network.RegisterChannel("hangingwires")
               .RegisterMessageType(typeof(HangingWiresData));
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

        public void CutWire(EntityAgent byEntity, NodePos pos1, NodePos pos2)
        {
            bool removed = TryToRemoveConnection(pos1, pos2);
            if (removed)
            {
                Item item = api.World.GetItem(new AssetLocation("signals:el_wire"));
                if(item != null)
                {
                    var itemStack = new ItemStack(item);
                    byEntity.TryGiveItemStack(itemStack);
                }

            }

        }

        public bool TryToRemoveConnection(NodePos pos1, NodePos pos2)
        {
            List<WireConnection> toRemove = data.connections.Where((WireConnection con) =>
            {
                return (con.pos1 == pos1 && con.pos2 == pos2) || (con.pos1 == pos2 && con.pos2 == pos1);
            }).ToList();
            if (toRemove.Count > 0)
            {
                foreach (WireConnection con in toRemove)
                {
                    data.connections.Remove(con);
                    api.ModLoader.GetModSystem<SignalNetworkMod>()?.OnWireRemoved(con);
                }

                serverChannel.BroadcastPacket(data);
            }
            return toRemove.Count > 0;
        }

        public bool TryToAddConnection(WireConnection connection){
            bool added = data.connections.Add(connection);
            if (added)
            {
                api.ModLoader.GetModSystem<SignalNetworkMod>()?.OnWireAdded(connection);
                serverChannel.BroadcastPacket(data);
            }
            return added;
        }

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
        #endregion
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class HangingWiresData
    {
        //public Dictionary<long, HangingWiresNetwork> HangingWiresNetworks = new Dictionary<long, HangingWiresNetwork>();
        //public long nextNetworkId = 1;
        public HashSet<WireConnection> connections = new HashSet<WireConnection>();

    }
}
