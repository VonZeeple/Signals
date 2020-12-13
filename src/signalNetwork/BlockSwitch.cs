using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BlockSwitch : BlockConnection
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            api.Logger.Debug("switch block loaded");
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            this.api.Logger.Debug("switch block placed");
        }

    }
}
