using signals.src.transmission;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    class BlockSwitch : BlockConnection
    {
        public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs = null)
        {
            BESwitch be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BESwitch;
            if(!(be is null)){
                be.OnInteract();
            }
            base.Activate(world, caller, blockSel, activationArgs);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if(!base.OnBlockInteractStart(world, byPlayer, blockSel)) return false;
            BESwitch be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BESwitch;
            if (!(be is null))
            {
                be.OnInteract();
            }
            return true;
        }

    }
}
