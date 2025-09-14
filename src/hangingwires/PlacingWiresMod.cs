using System;
using ProtoBuf;
using signals.src.signalNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace signals.src.hangingwires
{
    public class PlacingWiresMod : ModSystem
    {
        ICoreClientAPI capi;
        ICoreAPI api;
        IServerNetworkChannel serverChannel;
        IClientNetworkChannel clientChannel;
        private PendingWireRenderer WireRenderer;
        HangingWiresMod wireMod;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;
            capi.Event.AfterActiveSlotChanged += OnActiveSlotChanged;
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.api = api;
            this.wireMod = api.ModLoader.GetModSystem<HangingWiresMod>();

            if (api.World is IClientWorldAccessor)
            {
                clientChannel = ((ICoreClientAPI)api).Network.RegisterChannel("placingwires")
                .RegisterMessageType(typeof(AddConnectionPacket));
            }
            else
            {
                serverChannel = ((ICoreServerAPI)api).Network.RegisterChannel("placingwires")
               .RegisterMessageType(typeof(AddConnectionPacket))
               .SetMessageHandler<AddConnectionPacket>(OnAddConnectionFromClient);
            }
        }


        public void OnAddConnectionFromClient(IServerPlayer fromPlayer, AddConnectionPacket networkMessage)
        {
            var connection = networkMessage.connection as WireConnection;
            if (connection == null) return;

            //TODO: add checks to be sure that their is a node provider at the position (never trust the client)

            if (!UseWire(fromPlayer, false)){return;}
            bool added = wireMod.TryToAddConnection(connection);
            if (added){UseWire(fromPlayer, true);}
        }

        public bool IsHoldingWire(IPlayer player){
            Item item = player?.Entity.RightHandItemSlot.Itemstack?.Item;
            return item?.Code?.ToString() == "signals:el_wire";
        }
        public bool UseWire(IPlayer player, bool doUse = false){
            ItemStack itemStack = player?.InventoryManager.ActiveHotbarSlot.Itemstack;
            if ( itemStack?.Item?.Code?.ToString() != "signals:el_wire") return false;
            if ( player.WorldData.CurrentGameMode == EnumGameMode.Creative ){return true;}
            if(doUse){
                player?.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                player?.InventoryManager.ActiveHotbarSlot.MarkDirty();
            }
            return true;
        }

        public void OnActiveSlotChanged(ActiveSlotChangeEventArgs slotChange){
            pendingNode = null;
            WireRenderer?.Dispose();
        }

        NodePos pendingNode = null;
        public NodePos GetPendingNode()
        {
            return this.pendingNode;
        }

        public bool ConnectWire(NodePos pos, IPlayer byPlayer, IHangingWireAnchor anchor)
        {
            if (api.Side == EnumAppSide.Server) return false;

            if (!IsHoldingWire(byPlayer)) return false;

            if(pendingNode == null)
            {
                pendingNode = pos;
                Vec3f offset = anchor.GetAnchorPosInBlock(pos);
                capi?.Logger.Debug(string.Format("Pending {0}:{1}", pos.blockPos, pos.index));
                WireRenderer = new PendingWireRenderer(capi, this, pos.blockPos, offset);
            }
            else
            {
                capi?.Logger.Debug(string.Format("trying to attach {0}:{1}", pos.blockPos, pos.index));
                WireConnection connection = new WireConnection(pendingNode, pos);
                clientChannel.SendPacket(new AddConnectionPacket() { connection = connection, byPlayer = byPlayer.PlayerUID});
                pendingNode = null;
                WireRenderer?.Dispose();
            }
            return true;
        }
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
}
