using signals.src.signalNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.hangingwires
{
    class WireCutterBehavior : CollectibleBehavior
    {
        NodePos pendingNode;
        HangingWiresMod wireMod;
        ICoreAPI api;

        public WireCutterBehavior(CollectibleObject collObj) : base(collObj){}

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            wireMod = api.ModLoader.GetModSystem<HangingWiresMod>();
        }

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            //api.Logger.Error("AppSide: "+api.Side);
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
            //if (handHandling == EnumHandHandling.PreventDefault) return;
            if(blockSel == null){return;}//Happens when clicking on an entiry for example
            IHangingWireAnchor anchor = byEntity.World.BlockAccessor.GetBlock(blockSel.Position) as IHangingWireAnchor;
            NodePos pos = anchor?.GetNodePosForWire(byEntity.World, blockSel, pendingNode);
            if (pos == null) return;

            if (pendingNode == null){
                pendingNode = pos;
            }
            else {
                if(api.Side == EnumAppSide.Server){
                        wireMod.CutWire(byEntity, pos, pendingNode);
                        api.Logger.Debug("Cutting wire between "+pendingNode+" and "+pos);
                }
                pendingNode = null;
            }
            handHandling = EnumHandHandling.PreventDefault;
        }
    }
}