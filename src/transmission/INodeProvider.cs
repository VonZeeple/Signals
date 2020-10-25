using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{
    public interface INodeProvider
    {

        ISignalNode GetSignalNodeAt(IWorldAccessor world, NodePos pos);
        int GetSignalNetworkId(IWorldAccessor world, NodePos pos);
       
    }
}
