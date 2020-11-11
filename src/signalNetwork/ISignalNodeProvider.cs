using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    public interface ISignalNodeProvider
    {
        ISignalNode GetNodeAt(NodePos pos);

        Dictionary<NodePos, ISignalNode> GetNodes();
        Vec3f GetNodePosinBlock(NodePos pos);
        void OnNodeUpdate(NodePos pos);
    }
}
