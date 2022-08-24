using signals.src.transmission;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BlockBuzzer : BlockConnection
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if ( blockSel.SelectionBoxIndex == GetSelectionBoxes(world.BlockAccessor, blockSel.Position).Length - 1 ){
                BEBuzzer be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBuzzer;
                if(!(be is null)){
                    be.OnInteract(byPlayer);
                }
                return true;
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            string info = base.GetPlacedBlockInfo(world, pos, forPlayer);
            BEBuzzer be = world.BlockAccessor.GetBlockEntity(pos) as BEBuzzer;
            if(!(be is null)){
                int value = be.GetPitchInfo();
                info += "Pitch: " + (value).ToString() + "\r\n";
            }
            return info;
        }
    }
}
