using signals.src.transmission;
using Vintagestory.API.Common;

namespace signals.src.signalNetwork
{
    class BlockSwitch : BlockConnection
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            bool result = base.OnBlockInteractStart(world, byPlayer, blockSel);
            if ( blockSel.SelectionBoxIndex == GetSelectionBoxes(world.BlockAccessor, blockSel.Position).Length - 1 ){
                BESwitch be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BESwitch;
                if(!(be is null)){
                    be.OnInteract(byPlayer);
                }
                return true;
            }
            return result;
        }

    }
}
