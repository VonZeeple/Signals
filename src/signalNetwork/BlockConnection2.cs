using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    // The double connector on a wall can lie flat or stand upright. Where the player clicks
    // decides, like the switch does with its direction: near the top or bottom edge of the
    // wall block gives the upright one, near the sides the flat one.
    //
    // "connection2" itself stays untouched so old worlds are safe. The upright one is a
    // separate block "connection2v" that is never an item and drops the normal connector.
    public class BlockConnection2 : BlockConnection, IPlacementPreview
    {
        public Block GetPlacedBlock(IWorldAccessor world, BlockSelection blockSel)
        {
            BlockFacing face = blockSel?.Face;
            if (face == null) return null;
            string code = face.IsHorizontal && WantsUpright(blockSel) ? "signals:connection2v-" : "signals:connection2-";
            return world.GetBlock(new AssetLocation(code + face.Code));
        }

        // Hit position projected on the clicked face; up or down means upright.
        static bool WantsUpright(BlockSelection blockSel)
        {
            Vec3d hit = blockSel.HitPosition;
            if (hit == null) return false;
            Vec3d normal = blockSel.Face.Normalf.NormalizedCopy().ToVec3d();
            normal.Mul((float)hit.SubCopy(0.5, 0.5, 0.5).Dot(normal));
            Vec3d proj = hit.SubCopy(normal).Sub(0.5, 0.5, 0.5);
            BlockFacing dir = BlockFacing.FromVector(proj.X, proj.Y, proj.Z);
            return dir == BlockFacing.UP || dir == BlockFacing.DOWN;
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            Block wanted = GetPlacedBlock(world, blockSel);
            if (wanted is BlockConnection2Vertical && wanted.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode))
            {
                return true;
            }
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }
    }

    // Upright double connector. Only placed by BlockConnection2, never an item: it drops and
    // picks as the normal connector, so no new item ever shows up.
    public class BlockConnection2Vertical : BlockConnection
    {
        ItemStack Flat(IWorldAccessor world)
        {
            return new ItemStack(world.GetBlock(new AssetLocation("signals:connection2-north")) ?? this);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return Flat(world);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new[] { Flat(world) };
        }
    }
}
