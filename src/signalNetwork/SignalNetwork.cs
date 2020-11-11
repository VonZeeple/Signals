using ProtoBuf;
using signals.src.signalNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{



    [ProtoContract]
    public class SignalNetwork
    {
        //The list of all the nodes containing connections etc
        public Dictionary<NodePos, ISignalNode> nodes = new Dictionary<NodePos, ISignalNode>();
        //The list of all the devices (should be BlockEntities) that are connected to the network
        public Dictionary<BlockPos, ISignalNodeProvider> devices = new Dictionary<BlockPos, ISignalNodeProvider>();

        internal SignalNetworkMod mod;

        [ProtoMember(1)]
        public long networkId;
        [ProtoMember(3)]
        public Dictionary<Vec3i, int> inChunks = new Dictionary<Vec3i, int>();


        int chunksize;
        public bool fullyLoaded;




        /// <summary>
        /// Set to false when the nodes in the network needs upate
        /// </summary>
        public bool isValid = true;

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

        public List<BlockPos> GetDevices()
        {
            return devices.Keys.ToList();
        }

        public ISignalNode GetNodeAt(NodePos pos)
        {
            ISignalNode node;
            nodes.TryGetValue(pos, out node);
            return node;
        }


        /// <summary>
        /// Compute values at nodes and notify the devices
        /// </summary>
        public void Simulate()
        {
            List<ISignalNode> sources = nodes.Values.Where(v => v.isSource).ToList();
            foreach (ISignalNode source in sources)
            {
                List<NodePos> openList = new List<NodePos> { source.Pos };
                List<NodePos> closedList = new List<NodePos>();
                
                mod.Api.Logger.Debug("Network {0}: Simulation from source at {1}", this.networkId, source.Pos);
                byte startValue = (byte)15;




                while(openList.Count > 0 && closedList.Count <= nodes.Count+1)
                {
                    if (closedList.Count == nodes.Count + 1) { mod.Api.Logger.Error("Network simulation: closed list larger than number of nodes in net!"); break; }

                    NodePos pos = openList.Last();
                    ISignalNode currentNode = nodes[pos];
                    
                    currentNode.value = startValue;
                    ISignalNodeProvider device = mod.GetDeviceAt(pos.blockPos);
                    device.OnNodeUpdate(pos);

                    mod.Api.Logger.Debug("Network {0}: Asigning value {0} at node {1}",this.networkId, startValue, pos);

                    openList.RemoveAt(openList.Count - 1);
                    currentNode.Connections.ForEach(c => {
                        if (!closedList.Contains(c.pos2))
                        {
                            openList.Add(c.pos2);
                        }
                    });
                    closedList.Add(pos);

                }

            }

            isValid = true;
        }

        public void AddNode(NodePos pos, ISignalNode node)
        {
            if (nodes.ContainsKey(pos)) mod.Api.Logger.Error("Network {0} already contains a node at pos {1}", this.networkId, pos);
            nodes[pos] = node;
            isValid = false;
        }

        internal void AddNodesFoundFrom(NodePos pos, ISignalNode node)
        {
            List<NodePos> openList = new List<NodePos> { pos };
            List<NodePos> closedList = new List<NodePos>();

            while(openList.Count > 0)
            {
                NodePos currentPos = openList.Last();
                ISignalNode currentNode = mod.GetDeviceAt(currentPos.blockPos)?.GetNodeAt(currentPos);

                openList.RemoveAt(openList.Count - 1);
                if (currentNode != null)
                {
                    AddNode(currentPos, currentNode);
                    currentNode.Connections.ForEach(c => {
                        if (!closedList.Contains(c.pos2))
                        {
                            openList.Add(c.pos2);
                        }
                    });
                }
                closedList.Add(currentPos);
                
            }

        }

        /// <summary>
        /// Add a connection between two nodes within the same network
        /// </summary>
        public void AddConnection(Connection con)
        {
            if (!nodes.ContainsKey(con.pos1)) { mod.Api.Logger.Error("Adding intra-network connection, no node in network at pos {0}", con.pos1); return; }
            if (!nodes.ContainsKey(con.pos2)){ mod.Api.Logger.Error("Adding intra-network connection, no node in network at pos {0}", con.pos2); return; }

            if (nodes[con.pos1].Connections.Any(c => c.pos2 == con.pos2)) { mod.Api.Logger.Error("Adding intra-network connection, adding duplicate", con.pos2); return; }

            mod.Api.Logger.Debug("Network {0}: Adding intra-network connection", this.networkId);
            nodes[con.pos1].Connections.Add(con);

            isValid = false;
            //TODO: notify some handlers here
        }

        /// <summary>
        /// remove connection between two nodes within the same network
        /// </summary>
        public void RemoveConnection(Connection con)
        {

            if (!nodes.ContainsKey(con.pos1)) { mod.Api.Logger.Error("removing intra-network connection, no node in network at pos {0}", con.pos1); return; }

            bool didRemove = nodes[con.pos1].Connections.Remove(con);
            if (!didRemove) mod.Api.Logger.Error("removing intra-network connection, failed to remove connection at pos {0}", con.pos1);

            if (didRemove) isValid = false;
            //TODO: notify some handlers here
        }

        /// <summary>
        /// remove a node within the network
        /// </summary>
        public void RemoveNode(NodePos pos)
        {
            if (!nodes.ContainsKey(pos)) { mod.Api.Logger.Error("removing node in network, no node to remove at {0}", pos); return; }
            if (!nodes.Remove(pos)) { mod.Api.Logger.Error("removing node in network, failed to remove at {0}", pos); return; }
            isValid = false;
        }

        public void RemoveAllNodesAt(BlockPos pos)
        {
           List<NodePos> nodesToRemove = nodes.Keys.Where(k => k.blockPos == pos).ToList();
            foreach(NodePos nodePos in nodesToRemove)
            {
                RemoveNode(nodePos);
            }

        }

        /// <summary>
        /// remove a device within the network
        /// </summary>
        public void RemoveDevice(BlockPos pos)
        {
            ISignalNodeProvider device;
            devices.TryGetValue(pos, out device);
            if(device == null)
            {
                mod.Api.Logger.Error("removing device in network, cant find device at {0}, removing nodes...", pos);
                RemoveAllNodesAt(pos);
            }
            devices.Remove(pos);

        }



        internal IEnumerable<NodePos> GetNodePositions()
        {
            return nodes.Keys;
        }

        internal SignalNetwork Merge(SignalNetwork net2)
        {
            SignalNetwork net = new SignalNetwork(this.mod, this.networkId);
            foreach(KeyValuePair<NodePos,ISignalNode> kv in this.nodes)
            {
                net.nodes.Add(kv.Key,kv.Value);
            }
            foreach (KeyValuePair<NodePos, ISignalNode> kv in net2.nodes)
            {
                net.nodes.Add(kv.Key, kv.Value);
            }
            foreach(KeyValuePair<BlockPos, ISignalNodeProvider> kv in net.devices)
            {
                net.devices.Add(kv.Key, kv.Value);
            }
            foreach (KeyValuePair<BlockPos, ISignalNodeProvider> kv in net2.devices)
            {
                net.devices.Add(kv.Key, kv.Value);
            }

            isValid = false;
            return net;
        }

        private void AddChunkPos(BlockPos pos)
        {
            Vec3i chunkpos = new Vec3i(pos.X / chunksize, pos.Y / chunksize, pos.Z / chunksize);
            int q;
            inChunks.TryGetValue(chunkpos, out q);
            inChunks[chunkpos] = q + 1;
        }

        private void RemoveChunkPos(BlockPos pos)
        {
            Vec3i chunkpos = new Vec3i(pos.X / chunksize, pos.Y / chunksize, pos.Z / chunksize);
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
