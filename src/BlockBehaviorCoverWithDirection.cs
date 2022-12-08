using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src
{
    class BlockBehaviorCoverWithDirection : BlockBehavior
    {
        public string orientationCode => "orientation";
        public string sideCode => "side";

        public BlockBehaviorCoverWithDirection(Block block) : base(block)
        {

        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            AssetLocation baseBlock = block.CodeWithVariants(new string[]{orientationCode, sideCode}, new string[]{"north","down"});
            return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(baseBlock)) };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            AssetLocation baseBlock = block.CodeWithVariants(new string[]{orientationCode, sideCode}, new string[]{"north","down"});
            return new ItemStack(world.BlockAccessor.GetBlock(baseBlock));
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
        {
            handling = EnumHandling.PreventDefault;
            Block orientedBlock = GetOrientedBlock(world, blockSel);

            if (orientedBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                world.BlockAccessor.SetBlock(orientedBlock.BlockId, blockSel.Position, itemstack);
                return true;
            }
            return false;
        }


        public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
        {
            BlockFacing oppositeFace = blockSel.Face.Opposite;

            BlockPos attachingBlockPos = blockSel.Position.AddCopy(oppositeFace);
            Block block = world.BlockAccessor.GetBlock(attachingBlockPos);

            if (block.CanAttachBlockAt(world.BlockAccessor, this.block, attachingBlockPos, blockSel.Face))
            {
                //int blockId = world.BlockAccessor.GetBlock(CodeWithParts(oppositeFace.Code)).BlockId;
                //PlaceWithOrientation(world, blockpos, onBlockFace, itemstack);

                return true;
            }

            failureCode = "requireattachable";
            return false;
        }


        private Block GetOrientedBlock(IWorldAccessor world, BlockSelection blockSel)
        {
            BlockFacing onBlockFace = blockSel.Face;
            Vec3d hitPosition = blockSel.HitPosition;

            //hitPosition projected on the onBlockFace to determine block orientation
            Vec3d normal = onBlockFace.Normalf.NormalizedCopy().ToVec3d();
            normal.Mul((float)hitPosition.SubCopy(0.5, 0.5, 0.5).Dot(normal));
            Vec3d projVector = hitPosition.SubCopy(normal).Sub(0.5, 0.5, 0.5);

            BlockFacing orientation = BlockFacing.FromVector(projVector.X, projVector.Y, projVector.Z);
            BlockFacing oppositeFace = onBlockFace.Opposite;
            AssetLocation oBlock = block.CodeWithVariants(new Dictionary<string, string>(){{orientationCode, orientation.Code}, {sideCode, oppositeFace.Code}});
            return world.BlockAccessor.GetBlock(oBlock);
        }



    }
}
