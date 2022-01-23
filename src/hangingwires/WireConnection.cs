using System;
using ProtoBuf;
using signals.src.signalNetwork;

namespace signals.src.hangingwires
{
    [ProtoContract()]
    public class WireConnection : IEquatable<WireConnection>
    {
        [ProtoMember(1)]
        public NodePos pos1;
        [ProtoMember(2)]
        public NodePos pos2;
        public WireConnection() { }
        public WireConnection(NodePos pos1, NodePos pos2 )
        {
            this.pos1 = pos1;
            this.pos2 = pos2;
        }


        public bool Equals(WireConnection otherPos)
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

        public static bool operator == (WireConnection left, WireConnection right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator != (WireConnection left, WireConnection right)
        {
            return !(left == right);
        }
    }
}
