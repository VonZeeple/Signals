using ProtoBuf;
using signals.src.hangingwires;
using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace signals.src.signalNetwork
{
    [ProtoContract()]
    [ProtoInclude(10, typeof(WireConnection))]
    public class Connection : IEquatable<Connection>
    {
        [ProtoMember(1)]
        public NodePos pos1;
        [ProtoMember(2)]
        public NodePos pos2;


        public Connection() { }
        public Connection(NodePos pos1, NodePos pos2)
        {
            this.pos1 = pos1;
            this.pos2 = pos2;
        }

        public Connection GetReversed()
        {
            return new Connection(pos2, pos1);
        }
        public bool Equals(Connection otherPos)
        {
            if (otherPos == null) return false;
            return (pos1 == otherPos.pos1 && pos2 == otherPos.pos2);
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as Connection);
        }

        public override int GetHashCode()
        {
            return this.pos1.GetHashCode()*128 + this.pos2.GetHashCode();
        }

        public static bool operator ==(Connection left, Connection right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Connection left, Connection right)
        {
            return !(left == right);
        }
    }
}
