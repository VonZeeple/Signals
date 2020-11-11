using ProtoBuf;
using signals.src.signalNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace signals.src.hangingwires
{

    [ProtoContract()]
    public class WireConnection : Connection
    {
        public WireConnection() : base() { }
        public WireConnection(NodePos pos1, NodePos pos2 ): base(pos1,pos2)
            {
            }

    }
}
