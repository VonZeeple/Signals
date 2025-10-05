using signals.src.transmission;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BlockPressurePlate : BlockConnection
    {
        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
            if (world.Side == EnumAppSide.Client) return;
            BEPressurePlate be = world.BlockAccessor.GetBlockEntity(pos) as BEPressurePlate;
            if (be == null) return;
            be.OnEntityCollide(entity);
        }
    }
}
