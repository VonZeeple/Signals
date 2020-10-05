﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
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
                BEbreadboard entity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEbreadboard;
                entity?.OnUseOver(byPlayer, blockSel, false);
            }
            return true;

        }

        //Detects when the player interacts with left click, usually to remove a component
        public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            //base.OnGettingBroken(player, blockSel, itemslot, remainingResistance, dt, counter);
            BEbreadboard entity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BEbreadboard;
            entity?.OnUseOver(player, blockSel, true);

            //TODO: test if any action (return a bool), if false, return the time from base
            return 100f;
        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            
            

            BEbreadboard bec = blockAccessor.GetBlockEntity(pos) as BEbreadboard;
            Cuboidf[] entitySB = bec?.GetSelectionBoxes(blockAccessor, pos);
            if (entitySB == null || entitySB.Length == 0)
            {

                return base.GetSelectionBoxes(blockAccessor, pos);
            }

            return entitySB;
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BEbreadboard bec = blockAccessor.GetBlockEntity(pos) as BEbreadboard;

            if (bec != null)
            {
                Cuboidf[] selectionBoxes = bec.GetCollisionBoxes(blockAccessor, pos);

                return selectionBoxes;
            }

            return base.GetSelectionBoxes(blockAccessor, pos);
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