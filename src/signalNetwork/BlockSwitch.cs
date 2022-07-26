using signals.src.transmission;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BlockSwitch : BlockConnection
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BESwitch be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BESwitch;
            if(be is null) return false;
            be.OnInteract(byPlayer);
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

    }
}
