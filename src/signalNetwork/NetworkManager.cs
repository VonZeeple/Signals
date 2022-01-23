using System.Collections.Generic;
using signals.src.transmission;
using Vintagestory.API.Server;

namespace signals.src.signalNetwork
{
    public class NetworkManager
    {
        public Dictionary<long, ISignalNetwork> networks = new Dictionary<long, ISignalNetwork>();
        public long nextNetworkId = 1;

        ICoreServerAPI Api;
        SignalNetworkMod Mod;

        public NetworkManager(ICoreServerAPI api, SignalNetworkMod mod)
        {
            Api = api;
            Mod = mod;
        }

        /// <summary>
        /// Creates a new SignalNetwork and adds it to the network list.
        /// </summary>
        /// <returns>The new SignalNetwork</returns>
        public SignalNetwork CreateNetwork(ISignalNode firstNode)
        {
            Api.Logger.Debug("Creating signal network with Id: {0}", nextNetworkId);
            SignalNetwork net = new SignalNetwork(Mod, nextNetworkId);

            net.AddNode(firstNode);
            networks[nextNetworkId] = net;

            firstNode.netId = nextNetworkId;
            nextNetworkId ++;
            return net;
        }

        public void AddNodes(ISignalNode[] nodes)
        {
            foreach(ISignalNode node in nodes)
            {
                if(node.netId.HasValue){continue;}
                if(node.isSource && node.Connections.Count > 0)
                {
                    CreateNetwork(node);
                    BuildNetFrom(node);
                }
            }
        }

        public List<ISignalNode> GetConnectedNodes(ISignalNode node){
            List<ISignalNode> output = new List<ISignalNode>();
            foreach(Connection con in node.Connections){
                if(con.node1 == node){output.Add(con.node2);}
                else if(con.node2 == node){output.Add(con.node1);}
            }
            return output;
        }

        public void BuildNetFrom(ISignalNode node0)
        {
            List<ISignalNode> openList = GetConnectedNodes(node0);
            if(!node0.netId.HasValue){return;}
            long netId = node0.netId.Value;
            ISignalNetwork net = networks[netId];
            while(openList.Count>0)
            {
                ISignalNode node = openList[0];
                // if(node.netId == netId){} do nothing
                if(node.netId.HasValue && node.netId != netId)
                {
                    MergeNetworks(netId,node.netId.Value);
                }
                else if(!node.netId.HasValue)
                {
                    node.netId = netId;
                    net.AddNode(node);
                    openList.AddRange(GetConnectedNodes(node));
                }
                openList.Remove(node);
            }

        }

        public void MergeNetworks(long id1, long id2)
        {
            MergeNetworks(networks[id1], networks[id2]);
        }

        /// <summary>
        /// Merges network 2 into network 1.
        /// </summary>
        public void MergeNetworks(ISignalNetwork net1, ISignalNetwork net2)
        {
            foreach(ISignalNode node in net2.GetNodes())
            {
                net1.AddNode(node);
                node.netId = net1.networkId;
            }
            networks.Remove(net2.networkId);
        }

        /// <summary>
        /// Connects two nodes, for example when a wire is created.
        /// </summary>
        public void AddConnection(Connection con)
        {
            con.node1.Connections.Add(con);
            con.node2.Connections.Add(con);

            if(con.node1.netId == con.node2.netId && con.node1.netId.HasValue)
            {
                networks[con.node1.netId.Value].isValid = false;
            }
            else if(con.node1.netId != null)
            {
                BuildNetFrom(con.node1);
            }
            else if(con.node2.netId != null)
            {
                BuildNetFrom(con.node2);
            }
            else if(con.node1.isSource)
            {
                CreateNetwork(con.node1);
                BuildNetFrom(con.node1);
            }
            else if(con.node2.isSource)
            {
                CreateNetwork(con.node2);
                BuildNetFrom(con.node2);
            }
        }

        public void UpdateConnection(Connection con, byte newValue, byte newRevValue)
        {
            con.Att = newValue;
            con.revAtt = newRevValue;

            long? netId = con.node1.netId;
            if(netId.HasValue)
            {
                SignalNetwork net = networks[netId.Value] as SignalNetwork;
                net.isValid = false;
            }
        }

        /// <summary>
        /// Remove a connection between two nodes.
        /// </summary>
        public void RemoveConnection(Connection con)
        {
            ISignalNode node1 = con.node1;
            ISignalNode node2 = con.node2;

            List<Connection> conToRemove = new List<Connection>();
            node1.Connections.Remove(con);
            node2.Connections.Remove(con);
        }
    }
}