using System.Collections.Generic;
using System.Linq;
using signals.src;
using signals.src.hangingwires;
using signals.src.signalNetwork;
using signals.src.transmission;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;


namespace Vintagestory.GameContent
{
    public class BlockPassThroughConnector : BlockConnection, IMultiBlockColSelBoxes, IMultiBlockInteract
    {

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            //Add the second wire anchor
            if (wireAnchors.Length > 0)
            {
                WireAnchor newAnchor = new WireAnchor(1, "toto", wireAnchors[0].MinX,
                -1 - wireAnchors[0].MinY,
                wireAnchors[0].MinZ,
                wireAnchors[0].MaxX,
                -1 - wireAnchors[0].MaxY,
                wireAnchors[0].MaxZ);
            }
        }

        private static EnumAxis GetAxis(Vec3i vector)
        {
            if (vector.X != 0)
            {
                return EnumAxis.X;
            }
            else if (vector.Y != 0)
            {
                return EnumAxis.Y;
            }
            else
            {
                return EnumAxis.Z;
            }
        }

        public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        {

            return [.. from box in GetCollisionBoxes(blockAccessor, pos) select SignalsUtils.MirrorCuboidf(box, GetAxis(offset))];
        }

        public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        {
            return [.. from box in GetSelectionBoxes(blockAccessor, pos) select SignalsUtils.MirrorCuboidf(box, GetAxis(offset))];
        }

        public bool MBDoParticalSelection(IWorldAccessor world, BlockPos pos, Vec3i offset)
        {
            return base.DoParticalSelection(world, pos);
        }

        public bool MBOnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset)
        {
            var master_blockSel = blockSel.Clone();
            master_blockSel.Position.Add(offset);
            PlacingWiresMod mod = api.ModLoader.GetModSystem<PlacingWiresMod>();
            if (mod == null)
            {
                api.Logger.Error("PlacingWiresMod mod system not found");
            }
            else
            {
                NodePos pos = GetNodePosForWire(world, master_blockSel, mod.GetPendingNode());
                if (pos is not null)
                {
                    pos.index = (pos.index + 1) % 2;

                    if (CanAttachWire(world, pos, mod.GetPendingNode()))
                    {
                        mod.ConnectWire(pos, byPlayer, this);
                        return false;
                    }
                }
            }

            return true;
        }

        public bool MBOnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset)
        {
            return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
        }

        public void MBOnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset)
        {
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public bool MBOnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason, Vec3i offset)
        {
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
        }

        public ItemStack MBOnPickBlock(IWorldAccessor world, BlockPos pos, Vec3i offset)
        {
            return base.OnPickBlock(world, pos);
        }

        public WorldInteraction[] MBGetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer, Vec3i offset)
        {
            return base.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer);
        }

        public BlockSounds MBGetSounds(IBlockAccessor blockAccessor, BlockSelection blockSel, ItemStack stack, Vec3i offset)
        {
            return base.GetSounds(blockAccessor, blockSel, stack);
        }
    }
}