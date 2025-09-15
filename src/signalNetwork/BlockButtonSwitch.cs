using signals.src.transmission;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    class BlockButtonSwitch : BlockConnection
    {

        public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs = null)
        {
            BESwitch be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BESwitch;
            if(be is not null)
            {
                be.OnInteract();
                be.ReleaseInteract();
            }
            base.Activate(world, caller, blockSel, activationArgs);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            bool result = base.OnBlockInteractStart(world, byPlayer, blockSel);
            if ( blockSel.SelectionBoxIndex == GetSelectionBoxes(world.BlockAccessor, blockSel.Position).Length - 1 ){
                BEButtonSwitch be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEButtonSwitch;
                if(!(be is null)){
                    be.OnInteract();
                }
                return true;
            }
            return result;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            //return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
            return true;
        }
        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
            BEButtonSwitch be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEButtonSwitch;
            if(!(be is null)){
                be.ReleaseInteract();
            }
        }

    }
}
