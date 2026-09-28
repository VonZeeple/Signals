using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    // The signal source used to be one block that always stands up. Now it turns to the
    // face it is placed on, like the connectors do.
    //
    // The old "blocksource" block must stay exactly as it is: in old worlds it stands in the
    // air, hangs on walls or sits on chiseled stands, and we do not want any remap or any
    // broken block. So "blocksource" stays the item (trader, inventory, handbook) and the
    // block that is already placed. When a player places it, this class puts the new
    // "blocksource-<side>" variant into the world instead. Those variants are never an item,
    // they drop the old block again.
    public class BlockSignalSource : BlockConnection, IPlacementPreview
    {
        const string LegacyCode = "signals:blocksource";

        Block Legacy(IWorldAccessor world) => world.GetBlock(new AssetLocation(LegacyCode)) ?? this;

        public Block GetPlacedBlock(IWorldAccessor world, BlockSelection blockSel)
        {
            if (blockSel?.Face == null) return null;
            return world.GetBlock(new AssetLocation(LegacyCode + "-" + blockSel.Face.Code));
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            Block oriented = GetPlacedBlock(world, blockSel);
            if (oriented == null || oriented == this)
            {
                return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            }

            // Same rules as before: no support needed, so stands and mid-air placement still work.
            if (!oriented.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) return false;
            oriented.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            return true;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return new ItemStack(Legacy(world));
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new[] { new ItemStack(Legacy(world)) };
        }
    }
}
