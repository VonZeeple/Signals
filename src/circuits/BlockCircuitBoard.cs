using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace signals.src
{
    class BlockCircuitBoard : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            //Client side stuff
            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            //init of interactions
        }


        #region Block orientation and placement

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }

            // Prefer selected block face
            if (CanAttachTo(world, blockSel.Position, blockSel.Face,itemstack)) return PlaceWithOrientation(world, blockSel.Position, blockSel.Face, blockSel.HitPosition, itemstack) ;
            

            failureCode = "requireattachable";

            return false;
        }

        private bool PlaceWithOrientation(IWorldAccessor world, BlockPos blockpos, BlockFacing onBlockFace, Vec3d hitPosition , ItemStack itemstack)
        {

            //hitPosition projected on the onBlockFace to determine block orientation
            Vec3d normal = onBlockFace.Normalf.NormalizedCopy().ToVec3d();
            normal.Mul((float)hitPosition.SubCopy(0.5,0.5,0.5).Dot(normal));
            Vec3d projVector = hitPosition.SubCopy(normal).Sub(0.5,0.5,0.5);

            BlockFacing orientation = BlockFacing.FromVector(projVector.X,projVector.Y,projVector.Z);
            BlockFacing oppositeFace = onBlockFace.GetOpposite();
            int blockId = world.BlockAccessor.GetBlock(CodeWithParts(orientation.Code,oppositeFace.Code)).BlockId;
            world.BlockAccessor.SetBlock(blockId, blockpos, itemstack);
            return true;
        }

        bool CanAttachTo(IWorldAccessor world, BlockPos blockpos, BlockFacing onBlockFace, ItemStack itemstack)
        {
            BlockFacing oppositeFace = onBlockFace.GetOpposite();

            BlockPos attachingBlockPos = blockpos.AddCopy(oppositeFace);
            Block block = world.BlockAccessor.GetBlock(world.BlockAccessor.GetBlockId(attachingBlockPos));

            if (block.CanAttachBlockAt(world.BlockAccessor, this, attachingBlockPos, onBlockFace))
            {
                //int blockId = world.BlockAccessor.GetBlock(CodeWithParts(oppositeFace.Code)).BlockId;
                //PlaceWithOrientation(world, blockpos, onBlockFace, itemstack);
                return true;
            }

            return false;
        }



        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            BECircuitBoard be = world.BlockAccessor.GetBlockEntity(blockPos) as BECircuitBoard;
            if (be != null && byItemStack != null)
            {
                byItemStack.Attributes.SetInt("posx", blockPos.X);
                byItemStack.Attributes.SetInt("posy", blockPos.Y);
                byItemStack.Attributes.SetInt("posz", blockPos.Z);

                be.FromTreeAtributes(byItemStack.Attributes, world);
                be.MarkDirty(true);

            }
        }

        #endregion


        #region Block drops and items
        //when the player uses the middle button to select a block
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            Block block = world.BlockAccessor.GetBlock(CodeWithParts("north","down"));
            BECircuitBoard bec = world.BlockAccessor.GetBlockEntity(pos) as BECircuitBoard;
            if (bec == null)
            {
                return null;
            }

            TreeAttribute tree = new TreeAttribute();

            bec.ToTreeAttributes(tree);
            tree.RemoveAttribute("posx");
            tree.RemoveAttribute("posy");
            tree.RemoveAttribute("posz");

            return new ItemStack(block.Id, EnumItemClass.Block, 1, tree, world);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new ItemStack[] { OnPickBlock(world, pos) };
        }

        #endregion


        #region Interactions
        //Detects when the player interacts with right click, usually to place a component
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            base.OnBlockInteractStart(world, byPlayer, blockSel);
            if (api.Side == EnumAppSide.Client)
            {
                BECircuitBoard entity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECircuitBoard;
                entity?.OnUseOver(byPlayer, blockSel, false);
            }
            return true;

        }

        //Detects when the player interacts with left click, usually to remove a component
        public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            BECircuitBoard entity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BECircuitBoard;
            entity?.OnUseOver(player, blockSel, true);
            return base.OnGettingBroken(player, blockSel, itemslot, remainingResistance, dt, counter);
        }

        //Allows the selection of individual selectionBoxes
        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {

            ItemStack holdingItemStack = (api as ICoreClientAPI)?.World?.Player.Entity.RightHandItemSlot.Itemstack?.Clone();
            BECircuitBoard bec = blockAccessor.GetBlockEntity(pos) as BECircuitBoard;
            Cuboidf[] entitySB = bec?.GetSelectionBoxes(blockAccessor, pos, holdingItemStack);
            if (entitySB == null || entitySB.Length == 0)
            {

                return base.GetSelectionBoxes(blockAccessor, pos);
            }

            return entitySB;
        }


        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BECircuitBoard bec = blockAccessor.GetBlockEntity(pos) as BECircuitBoard;

            if (bec != null)
            {
                Cuboidf[] selectionBoxes = bec.GetCollisionBoxes(blockAccessor, pos);

                return selectionBoxes;
            }
            return base.GetSelectionBoxes(blockAccessor, pos);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            ITreeAttribute tree = inSlot.Itemstack.Attributes;
            if (tree.HasAttribute("circuit")) VoxelCircuit.GetCircuitInfo(dsc, tree.GetTreeAttribute("circuit"));
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return new WorldInteraction[] {
                new WorldInteraction()
            {
                MouseButton = EnumMouseButton.Right,
                ActionLangCode = "Add element"
            },
            new WorldInteraction()
            {
                HotKeyCode = "sprint",
                MouseButton = EnumMouseButton.Right,
                ActionLangCode = "rotate element"
            },
            new WorldInteraction()
            {
                MouseButton = EnumMouseButton.Left,
                ActionLangCode = "remove element"
            }
            }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
        #endregion

        #region Rendering
        //this is where the rendering of the itemstack is handled
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            CircuitBlockModelCache cache = capi.ModLoader.GetModSystem<CircuitBlockModelCache>();
            renderinfo.ModelRef = cache.GetOrCreateMeshRef(itemstack);
        }
        #endregion
    }
}
