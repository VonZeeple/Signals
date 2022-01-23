using System;
using signals.src.transmission;

namespace signals.src.signalNetwork
{
    public class Connection
    {
        public ISignalNode node1;
        public ISignalNode node2;

        public byte Att; //attenuation from node1 to node2
        public byte revAtt; //attenuation from node2 to node1

        public ISignalNode GetOther(ISignalNode node)
        {
            if(node == node1) return node2;
            else if (node == node2) return node1;
            else throw new ArgumentException("node not in connection.");
        }

        public Connection(ISignalNode node1, ISignalNode node2, byte att = 0, byte? revAtt = null)
        {
            this.node1 = node1;
            this.node2 = node2;
            this.Att = att;
            this.revAtt = revAtt.HasValue? revAtt.Value : att;
        }
    }
}
