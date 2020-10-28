using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{

    [ProtoContract()]
    public class Connection:IEquatable<Connection>
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
        public bool Equals(Connection otherPos)
        {
            if (otherPos == null) return false;
            return (pos1 == otherPos.pos1 &&  pos2 == otherPos.pos2) || (pos1 == otherPos.pos2 && pos2 == otherPos.pos1);
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as Connection);
        }

        public override int GetHashCode()
        {
            return this.pos1.GetHashCode() ^ this.pos2.GetHashCode();
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
            this.index = index;
            this.blockPos = pos;
        }

        public override int GetHashCode()
        {
            return blockPos.GetHashCode()*23+index;
        }

        public override bool Equals(object other)
        {
            NodePos otherPos = other as NodePos;
            return otherPos == null? false:Equals(otherPos);
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



    [ProtoContract]
    public class SignalNetwork
    {
        public Dictionary<NodePos, ISignalNode> nodes = new Dictionary<NodePos, ISignalNode>(); 

        internal SignalNetworkMod mod;

        [ProtoMember(1)]
        public long networkId;
        [ProtoMember(2)]
        protected short level;
        [ProtoMember(3)]
        public Dictionary<Vec3i, int> inChunks = new Dictionary<Vec3i, int>();


        int chunksize;
        public bool fullyLoaded;
        private bool firstTick = true;
        public bool isValid = true;


        /// <summary>
        /// Set to false when a block with more than one connection in the network has been broken
        /// </summary>
        public bool Valid
        {
            get; set;
        } = true;

        public SignalNetwork()
        {

        }


        public SignalNetwork(SignalNetworkMod mod, long networkId)
        {
            this.networkId = networkId;
            Init(mod);
        }

        public void Init(SignalNetworkMod mod)
        {
            this.mod = mod;
            chunksize = mod.Api.World.BlockAccessor.ChunkSize;
        }

        public void Join(ISignalNode node)
        {
            NodePos pos = node.Pos;
            nodes[pos] = node;

            Vec3i chunkpos = new Vec3i(pos.blockPos.X / chunksize, pos.blockPos.Y / chunksize, pos.blockPos.Z / chunksize);
            int q;
            inChunks.TryGetValue(chunkpos, out q);
            inChunks[chunkpos] = q + 1;
        }
        public void Leave(ISignalNode node)
        {
            NodePos pos = node.Pos;
            nodes.Remove(pos);

            Vec3i chunkpos = new Vec3i(pos.blockPos.X / chunksize, pos.blockPos.Y / chunksize, pos.blockPos.Z / chunksize);
            int q;
            inChunks.TryGetValue(chunkpos, out q);
            if (q <= 1)
            {
                inChunks.Remove(chunkpos);
            }
            else
            {
                inChunks[chunkpos] = q - 1;
            }
        }

        internal void broadcastData()
        {
            throw new NotImplementedException();
        }

        public void updateNetwork(long tick)
        { 
        
            foreach(ISignalNode node in nodes.Values)
            {

            }
        
        }

        public void ReadFromTreeAttribute(ITreeAttribute tree)
        {
            networkId = tree.GetLong("networkId");
        }
        public void WriteToTreeAttribute(ITreeAttribute tree)
        {
            tree.SetLong("networkId", networkId);

        }

        }
}
