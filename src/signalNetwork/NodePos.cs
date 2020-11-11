using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    [ProtoContract()]
    public class NodePos : IEquatable<NodePos>
    {
        [ProtoMember(1)]
        public BlockPos blockPos;
        [ProtoMember(2)]
        public int index;

        public NodePos() { }
        public NodePos(BlockPos pos, int index)
        {
            if (null == pos)
                throw new ArgumentNullException("blockPos");
            this.index = index;
            this.blockPos = pos;
        }

        public override int GetHashCode()
        {
            return blockPos.GetHashCode() * 23 + index;
        }

        public override string ToString()
        {
            return blockPos.X + ", " + blockPos.Y + ", " + blockPos.Z + ": " + index;
        }
        public override bool Equals(object other)
        {
            NodePos otherPos = other as NodePos;
            return otherPos == null ? false : Equals(otherPos);
        }

        public bool Equals(NodePos otherPos)
        {
            if (otherPos == null) return false;
            return (blockPos.Equals(otherPos.blockPos) && index == otherPos.index);
        }

        public static bool operator ==(NodePos left, NodePos right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(NodePos left, NodePos right)
        {
            return !(left == right);
        }
    }
}
