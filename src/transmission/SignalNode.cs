using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace signals.src.transmission
{
    public interface ISignalNode
    {
        NodePos GetNodePos();
        List<NodePos> GetConnectedNodes();
        List<int> GetAttenuations();

        int GetNetwork();

        bool IsSource();
    }

    public class SignalNode : ISignalNode
    {
        public NodePos pos;
        public int netId;
        public List<int> attenuations;
        public List<NodePos> connectedNodes;
        public bool? isSource;

        public SignalNode(NodePos pos)
        {
            this.pos = pos;
        }
        public List<int> GetAttenuations() { return attenuations; }

        public List<NodePos> GetConnectedNodes() { return connectedNodes; }

        public int GetNetwork() { return netId; }

        public NodePos GetNodePos() { return pos; }

        public bool IsSource() { return isSource==null?false:isSource.Value; }
    }

}
