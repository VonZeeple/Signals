using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{

    [ProtoContract()]
    public class SignalNodePos : IEquatable<SignalNodePos>
    {
        [ProtoMember(1)]
        public BlockPos blockPos;
        [ProtoMember(2)]
        public int index;

        public bool Equals(SignalNodePos other)
        {
            return (blockPos == other.blockPos && index == other.index);
        }
    }

    public interface ISignalNode
    {

    }

    [ProtoContract]
    public class SignalNetwork
    {
        public Dictionary<SignalNodePos, ISignalNode> nodes = new Dictionary<SignalNodePos, ISignalNode>(); 

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

        /// <summary>
        /// Set to false when a block with more than one connection in the network has been broken
        /// </summary>
        public bool Valid
        {
            get; set;
        } = true;


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
            //BlockPos pos = node.GetPosition();
            //nodes[pos] = node;

            //Vec3i chunkpos = new Vec3i(pos.X / chunksize, pos.Y / chunksize, pos.Z / chunksize);
            //int q;
            //inChunks.TryGetValue(chunkpos, out q);
            //inChunks[chunkpos] = q + 1;
        }

        public void DidUnload(ISignalNode node)
        {
            fullyLoaded = false;
        }

        public void Leave(ISignalNode node)
        {
            //BlockPos pos = node.GetPosition();
            //nodes.Remove(pos);

            //Vec3i chunkpos = new Vec3i(pos.X / chunksize, pos.Y / chunksize, pos.Z / chunksize);
            //int q;
            //inChunks.TryGetValue(chunkpos, out q);
            //if (q <= 1)
            //{
            //    inChunks.Remove(chunkpos);
            //}
            //else
            //{
            //    inChunks[chunkpos] = q - 1;
           //}
        }



        public void ServerTick(float dt, long tickNumber)
        {
            if (tickNumber % 5 == 0)
            {
                updateNetwork(tickNumber);
            }

            if (tickNumber % 40 == 0)
            {
                //broadcastData();
            }
        }

        public void updateNetwork(long tick)
        { 
            foreach (ISignalNode node in nodes.Values)
            {
                //update
            }

        }
    
    
    }
}
