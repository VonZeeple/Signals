using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    public class SNDeviceProxy : ISignalNodeProvider
    {

        Dictionary<NodePos, ISignalNode> nodes;

        public SNDeviceProxy(ISignalNodeProvider device)
        {
            nodes = device.GetNodes();
        }

        public ISignalNode GetNodeAt(NodePos pos)
        {
            ISignalNode node;
            nodes.TryGetValue(pos, out node);
            return node;
        }

        public Vec3f GetNodePosinBlock(NodePos pos)
        {
            return null;
        }

        public Dictionary<NodePos, ISignalNode> GetNodes()
        {
            return nodes;
        }

        public void OnNodeUpdate(NodePos pos)
        {
        }
    }
}
