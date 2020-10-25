using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace signals.src.transmission
{
    class BESignalSource : BlockEntity, INodeProvider
    {
        int netId = -1;


        public int GetSignalNetworkId(IWorldAccessor world, NodePos pos)
        {
            if (this.Pos != pos.blockPos || pos.index != 0) return -1;

            return this.netId;
        }

        public ISignalNode GetSignalNodeAt(IWorldAccessor world, NodePos pos)
        {
            if (this.Pos != pos.blockPos || pos.index != 0) return null;

            return new SignalNode(pos) { 
                netId = this.netId,
                isSource = true
                };
        }
    }
}
