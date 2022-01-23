using signals.src.transmission;
using System.Collections.Generic;
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
