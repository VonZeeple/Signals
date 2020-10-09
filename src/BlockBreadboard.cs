using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace signals.src
{
    class BlockBreadboard : Block
    {

        

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            //Client side stuff
            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            //init of interactions

        }


        //Detects when the player interacts with right click, usually to place a component
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            base.OnBlockInteractStart(world, byPlayer, blockSel);
            if (api.Side == EnumAppSide.Client)
            {
                BEBreadboard entity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBreadboard;
                entity?.OnUseOver(byPlayer, blockSel, false);
            }
            return true;

        }

        //when the player uses the middle button to select a block
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            BEBreadboard bec = world.BlockAccessor.GetBlockEntity(pos) as BEBreadboard;
            if (bec == null)
            {
                return null;
            }

            TreeAttribute tree = new TreeAttribute();

            bec.ToTreeAttributes(tree);
            tree.RemoveAttribute("posx");
            tree.RemoveAttribute("posy");
            tree.RemoveAttribute("posz");

            return new ItemStack(this.Id, EnumItemClass.Block, 1, tree, world);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new ItemStack[] { OnPickBlock(world, pos) };
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            BEBreadboard be = world.BlockAccessor.GetBlockEntity(blockPos) as BEBreadboard;
            if (be != null && byItemStack != null)
            {
                byItemStack.Attributes.SetInt("posx", blockPos.X);
                byItemStack.Attributes.SetInt("posy", blockPos.Y);
                byItemStack.Attributes.SetInt("posz", blockPos.Z);

                be.FromTreeAtributes(byItemStack.Attributes, world);
                be.MarkDirty(true);
                
                if (world.Side == EnumAppSide.Client)
                {
                    //be.RegenMesh();
                }

                //be.RegenSelectionBoxes(null);
            }
        }

        //this is where the rendering of the itemstack is handled
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            CircuitBlockModelCache cache = capi.ModLoader.GetModSystem<CircuitBlockModelCache>();
            renderinfo.ModelRef = cache.GetOrCreateMeshRef(itemstack);
        }

        //Detects when the player interacts with left click, usually to remove a component
        public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            BEBreadboard entity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBreadboard;
            entity?.OnUseOver(player, blockSel, true);
            return base.OnGettingBroken(player, blockSel,itemslot,remainingResistance,dt,counter);
        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            
            

            BEBreadboard bec = blockAccessor.GetBlockEntity(pos) as BEBreadboard;
            Cuboidf[] entitySB = bec?.GetSelectionBoxes(blockAccessor, pos);
            if (entitySB == null || entitySB.Length == 0)
            {

                return base.GetSelectionBoxes(blockAccessor, pos);
            }

            return entitySB;
        }


        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BEBreadboard bec = blockAccessor.GetBlockEntity(pos) as BEBreadboard;

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
            if(tree.HasAttribute("circuit")) VoxelCircuit.GetCircuitInfo(dsc, tree.GetTreeAttribute("circuit"));
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
    }
}
