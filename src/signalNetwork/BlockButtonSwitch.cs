using signals.src.transmission;
using Vintagestory.API.Common;

namespace signals.src.signalNetwork
{
    class BlockButtonSwitch : BlockConnection
    {
        string sound = "effect/woodswitch";
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            bool result = base.OnBlockInteractStart(world, byPlayer, blockSel);
            if ( blockSel.SelectionBoxIndex == GetSelectionBoxes(world.BlockAccessor, blockSel.Position).Length - 1 ){
                BEButtonSwitch be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEButtonSwitch;
                if(!(be is null)){
                    be.OnInteract();
                }
                world.PlaySoundAt(new AssetLocation("sounds/" + sound), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
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
                world.PlaySoundAt(new AssetLocation("sounds/" + sound), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
            }
        }

    }
}
