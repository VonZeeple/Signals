using System.Collections.Generic;
using signals.src.transmission;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace signals.src.signalNetwork
{
    class BlockSignalMeter : BlockConnection
    {
        Dictionary<string, Cuboidi> attachmentAreas;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            var areas = Attributes?["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>(null);
            if (areas != null)
            {
                attachmentAreas = new Dictionary<string, Cuboidi>();
                foreach (var val in areas)
                {
                    val.Value.Origin.Set(8, 8, 8);
                    attachmentAreas[val.Key] = val.Value.RotatedCopy().ConvertToCuboidi();
                }
            }
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (byPlayer.Entity.Controls.Sneak)
            {
                failureCode = "__ignore__";
                return false;
            }

            //if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            //{
            //    return false;
            //}

            // Prefer selected block face
            if (blockSel.Face.IsHorizontal)
            {
                if (TryAttachTo(world, blockSel.Position, blockSel.Face)) return true;
            }else if (blockSel.Face == BlockFacing.UP)
            {
                if (PlaceOnFloor(world, blockSel)) return true;
            }

            failureCode = "requireattachable";
            return false;
        }

        bool PlaceOnFloor(IWorldAccessor world, BlockSelection blockSel)
        {
            //hitPosition projected on the onBlockFace to determine block orientation
            BlockFacing onBlockFace = blockSel.Face;
            Vec3d hitPosition = blockSel.HitPosition;
            Vec3d normal = onBlockFace.Normalf.NormalizedCopy().ToVec3d();
            normal.Mul((float)hitPosition.SubCopy(0.5, 0.5, 0.5).Dot(normal));
            Vec3d projVector = hitPosition.SubCopy(normal).Sub(0.5, 0.5, 0.5);

            BlockFacing orientation = BlockFacing.FromVector(projVector.X, projVector.Y, projVector.Z);
            Dictionary<string, string> dict = new Dictionary<string, string>()
                        {{"type", "floor"},
                        {"orientation", orientation.Code}};
            int blockId = world.BlockAccessor.GetBlock(CodeWithVariants(dict)).BlockId;
            world.BlockAccessor.SetBlock(blockId, blockSel.Position);
            return true;
        }

        bool TryAttachTo(IWorldAccessor world, BlockPos blockpos, BlockFacing onBlockFace)
        {
            BlockFacing onFace = onBlockFace;

            BlockPos attachingBlockPos = blockpos.AddCopy(onBlockFace.Opposite);
            Block block = world.BlockAccessor.GetBlock(attachingBlockPos);

            Cuboidi attachmentArea = null;
            attachmentAreas?.TryGetValue(onBlockFace.Opposite.Code, out attachmentArea);

            if (block.CanAttachBlockAt(world.BlockAccessor, this, attachingBlockPos, onFace, attachmentArea))
            {
                int blockId = world.BlockAccessor.GetBlock(CodeWithVariant("orientation", onBlockFace.Code)).BlockId;
                world.BlockAccessor.SetBlock(blockId, blockpos);
                return true;
            }

            return false;
        }
    }
}