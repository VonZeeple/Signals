using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{
    public interface ISignalNetworkBlock
    {

        SignalNetwork GetNetwork(IWorldAccessor world, SignalNodePos pos);
        bool HasSignalNetworkConnectorAt(IWorldAccessor world, SignalNodePos pos);
        void DidConnectAt(IWorldAccessor world, SignalNodePos pos);
    }
}
