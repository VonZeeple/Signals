using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace signals.src
{
    //https://github.com/anegostudios/vsessentialsmod/blob/master/BlockBehavior/BehaviorMultiblock.cs


    public class BlockBehaviorSparseMultiblock(Block block) : BlockBehavior(block)
    {
        /// The type of the multiblock. Usually monolithic.
        /// </summary>
        string type;

        Vec3i offset;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            type = properties["type"].AsString("monolithic");
            offset = properties["offset"].AsObject<Vec3i>(new Vec3i(-1, 0, 0));
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
        {
            return true;
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
        {
            bool blocked = false;
            IterateOverEach(blockSel.Position, (mpos) =>
            {
                if (mpos == blockSel.Position) return true;

                Block mblock = world.BlockAccessor.GetBlock(mpos);
                if (!mblock.IsReplacableBy(block))
                {
                    blocked = true;
                    return false;
                }

                return true;
            });

            if (blocked)
            {
                handling = EnumHandling.PreventDefault;
                return false;
            }

            return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handling);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;

            IterateOverEach(pos, (mpos) =>
            {
                if (mpos == pos) return true;

                int dx = mpos.X - pos.X;
                int dy = mpos.Y - pos.Y;
                int dz = mpos.Z - pos.Z;

                string sdx = (dx < 0 ? "n" : (dx > 0 ? "p" : "")) + Math.Abs(dx);
                string sdy = (dy < 0 ? "n" : (dy > 0 ? "p" : "")) + Math.Abs(dy);
                string sdz = (dz < 0 ? "n" : (dz > 0 ? "p" : "")) + Math.Abs(dz);

                AssetLocation loc = new AssetLocation("multiblock-" + type + "-" + sdx + "-" + sdy + "-" + sdz);
                Block block = world.GetBlock(loc) ?? throw new IndexOutOfRangeException("Multiblocks are currently limited to 5x5x5 with the controller being in the middle of it, yours likely exceeds the limit because I could not find block with code " + loc.Path);
                world.BlockAccessor.SetBlock(block.Id, mpos);
                return true;
            });
        }


        public void IterateOverEach(BlockPos controllerPos, ActionConsumable<BlockPos> onBlock)
        {
            int x = controllerPos.X;
            int y = controllerPos.Y;
            int z = controllerPos.Z;
            BlockPos tmpPos = new BlockPos(controllerPos.dimension);

            tmpPos.Set(x, y, z);
            if (!onBlock(tmpPos)) return;
            tmpPos.Set(x+offset.X, y+offset.Y, z+offset.Z);
            if (!onBlock(tmpPos)) return;
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            IterateOverEach(pos, (mpos) =>
            {
                if (mpos == pos) return true;

                Block mblock = world.BlockAccessor.GetBlock(mpos);
                if (mblock is BlockMultiblock)
                {
                    world.BlockAccessor.SetBlock(0, mpos);
                }

                return true;
            });
        }
    }
}