using signals.src.circuits.components;
using signals.src.hangingwires;
using signals.src.signalNetwork;
using signals.src.transmission;
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



        #region Block placement
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            BlockEntity be = world.BlockAccessor.GetBlockEntity(blockPos);
            if (be != null && byItemStack != null)
            {
                byItemStack.Attributes.SetInt("posx", blockPos.X);
                byItemStack.Attributes.SetInt("posy", blockPos.Y);
                byItemStack.Attributes.SetInt("posz", blockPos.Z);

                be.FromTreeAttributes(byItemStack.Attributes, world);
                be.MarkDirty(true);

            }
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            base.OnBlockRemoved(world, pos);
            HangingWiresMod mod = api.ModLoader.GetModSystem<HangingWiresMod>();
            mod.RemoveAllNodesAtBlockPos(pos);
        }

        #endregion


        #region Block drops and items
        //when the player uses the middle button to select a block
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            Block block = world.BlockAccessor.GetBlock(CodeWithParts("north","down"));
            BlockEntity bec = world.BlockAccessor.GetBlockEntity(pos);
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

            HangingWiresMod mod = api.ModLoader.GetModSystem<HangingWiresMod>();
            if (mod != null && api.Side == EnumAppSide.Client)
            {
                NodePos pos = GetNodePosForWire(world, blockSel);
                if (pos != null) mod.SetPendingNode(GetNodePosForWire(world, blockSel));
            }

            base.OnBlockInteractStart(world, byPlayer, blockSel);
            if (api.Side == EnumAppSide.Client)
            {
                BlockEntity entity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
                entity?.GetBehavior<BEBehaviorCircuitHolder>()?.OnUseOver(byPlayer, blockSel, false);
            }

            return true;

        }

        //Detects when the player interacts with left click, usually to remove a component
        public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            BlockEntity entity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            entity?.GetBehavior<BEBehaviorCircuitHolder>()?.OnUseOver(player, blockSel, true);
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
            BlockEntity bec = blockAccessor.GetBlockEntity(pos);

            Cuboidf[] entitySB = bec?.GetBehavior<BEBehaviorCircuitHolder>()?.GetSelectionBoxes(blockAccessor, pos, holdingItemStack);
            if (entitySB == null || entitySB.Length == 0)
            {

                return base.GetSelectionBoxes(blockAccessor, pos);
            }

            return entitySB;
        }


        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BlockEntity bec = blockAccessor.GetBlockEntity(pos);

            if (bec != null)
            {
                Cuboidf[] selectionBoxes = bec?.GetBehavior<BEBehaviorCircuitHolder>()?.GetCollisionBoxes(blockAccessor, pos);

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


        #region WireAnchor
        public Vec3f GetAnchorPosInBlock(IWorldAccessor world, NodePos pos)
        {
            BlockEntity entity = world?.BlockAccessor?.GetBlockEntity(pos.blockPos);
            Vec3f posOut = null;// entity?.GetBehavior<BEBehaviorCircuitHolder>()?.GetNodePosInBlock(pos);
            return posOut != null ? posOut : new Vec3f(0, 0, 0);
        }

        public NodePos GetNodePosForWire(IWorldAccessor world, BlockSelection blockSel, NodePos posInit = null)
        {
            BlockEntity entity = world?.BlockAccessor?.GetBlockEntity(blockSel.Position);
            return null;// entity?.GetNodePos(blockSel);
        }

        public bool CanAttachWire(IWorldAccessor world, NodePos pos, NodePos posInit = null)
        {

            if (posInit != null && posInit == pos) return false;
            return pos != null;
        }

        public NodePos[] GetWireAnchors(IWorldAccessor world, BlockPos pos)
        {
            throw new System.NotImplementedException();
        }
        #endregion

    }
}
