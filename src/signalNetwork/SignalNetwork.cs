using signals.src.signalNetwork;
using System;
using System.Collections.Generic;
using System.Linq;

namespace signals.src.transmission
{
    public interface ISignalNetwork
    {
        long networkId {get;}
        bool isValid {get; set;}
        void AddNode(ISignalNode node);
        void RemoveNode(ISignalNode node);
        IEnumerable<NodePos> GetNodePositions();
        IEnumerable<ISignalNode> GetNodes();
    }

    public class SignalNetwork : ISignalNetwork
    {
        //The list of all the nodes containing connections etc
        public Dictionary<NodePos, ISignalNode> nodes = new Dictionary<NodePos, ISignalNode>();

        internal SignalNetworkMod mod;

        public long networkId;

        /// <summary>
        /// Set to false when the nodes in the network needs upate
        /// </summary>
        public bool isValid = true;

        long ISignalNetwork.networkId => networkId;

        bool ISignalNetwork.isValid { get => isValid; set => isValid = value; }

        public SignalNetwork(SignalNetworkMod mod, long networkId)
        {
            this.networkId = networkId;
            this.mod = mod;
        }

        /// <summary>
        /// Compute values at nodes and notify the devices
        /// </summary>
        public void Simulate()
        {
            List<ISignalNode> sources = nodes.Values.Where(v => v.isSource).ToList();
            HashSet<ISignalNode> updated = new HashSet<ISignalNode>();
            HashSet<ISignalNode> changed = new HashSet<ISignalNode>();

            foreach (ISignalNode source in sources){
                List<ISignalNode> openList = new List<ISignalNode>{source};
                byte startValue;
                if (updated.Contains(source)){
                    startValue = Math.Max(source.output,source.value);
                }else{
                    startValue = source.output;
                }
                source.value = startValue;
                updated.Add(source);
                while (openList.Count > 0) {
                    ISignalNode node = openList.Last();
                    openList.Remove(node);
                    foreach (Connection con in node.Connections) {
                        ISignalNode otherNode;
                        byte att;
                        if (con.node1 == node) {
                            otherNode = con.node2;
                            att = con.Att;}
                        else {
                            otherNode = con.node1;
                            att = con.revAtt;
                        }
                        int tempValue = node.value - att;
                        byte newValue = tempValue >= 0 ? Convert.ToByte(tempValue): (byte)0;
                        if (updated.Contains(otherNode)) {
                            if (otherNode.value < newValue) {
                                otherNode.value = newValue;
                                changed.Add(otherNode);
                                if (newValue > 0) {openList.Add(otherNode);}
                            }
                        }
                        else{
                            if (otherNode.value != newValue) {
                                changed.Add(otherNode);
                            }
                            otherNode.value = newValue;
                            updated.Add(otherNode);
                            if (newValue > 0) {openList.Add(otherNode);}
                        }
                    }
                }
            }

            isValid = true;//Needs to be here as following
            foreach(ISignalNode node in nodes.Values){
                if(!updated.Contains(node)){
                    if(node.value != 0){changed.Add(node);};
                    node.value = 0;
                }
            }
            foreach(ISignalNode node in changed){
                mod.GetDeviceAt(node.Pos.blockPos)?.OnNodeUpdate(node.Pos);
            }
        }

        public void AddNode(ISignalNode node){
            if (nodes.ContainsKey(node.Pos)) mod.Api.Logger.Error("Network {0} already contains a node at pos {1}", this.networkId, node.Pos);
            nodes[node.Pos] = node;
            isValid = false;
        }

        /// <summary>
        /// remove a node within the network
        /// </summary>
        public void RemoveNode(ISignalNode node)
        {
            if (!nodes.ContainsKey(node.Pos)) { mod.Api.Logger.Error("removing node in network, no node to remove at {0}", node.Pos); return; }
            if (!nodes.Remove(node.Pos)) { mod.Api.Logger.Error("removing node in network, failed to remove at {0}", node.Pos); return; }
            isValid = false;
        }

        public IEnumerable<NodePos> GetNodePositions()
        {
            return nodes.Keys;
        }

        public IEnumerable<ISignalNode> GetNodes()
        {
            return nodes.Values;
        }
    }
}
