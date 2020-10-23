using signals.src.hangingwires;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace signals.src.transmission
{
    class BlockSource : BlockConnection
    {
        public bool CanAttachWire(IWorldAccessor world, BlockSelection blockSel)
        {
            return false;
        }

        public bool CanAttachWire(IWorldAccessor world, NodePos pos)
        {
            return false;
        }

        public NodePos GetNodePos(IWorldAccessor world, BlockSelection blockSel)
        {
            return new NodePos(blockSel.Position, 0);
        }

        //Called on client and server
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            HangingWiresMod mod = api.ModLoader.GetModSystem<HangingWiresMod>();
            if (mod == null)
            {
                api.Logger.Error("HangingWiresMod mod system not found");
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }

            mod.SetPendingNode(GetNodePos(world, blockSel));
            return true;
        }

    }
}
