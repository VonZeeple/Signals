using ProtoBuf;
using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace signals.src
{

    //Concept
    // A Network is a set of connected nodes, all nodes in the same network have the same state
    // A node is indexed by a NodePos, wich is a BlockPos + an integer index. A block can contain several nodes
   //Because diodes and vacuum tubes are implemented, node A being connected with node B doesn't mean that node B is connected with node A
   //Connections can be lossy, the conveyed signal can be decreased by an interger number until 0.
   
   //BlockEntities forget their associated network ids upon unloading
   //Whenever a block entity got unloaded, it is removed from the network. network is updated according to the following
   //-Get signal sources from the original network
   //-Delete the network
   //Get/create new network for any producer


    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SignalNetworksData
    {
        public Dictionary<long, SignalNetwork> networksById = new Dictionary<long, SignalNetwork>();
        public long nextNetworkId = 1;
    }

    
    public class SignalNetworkMod : ModSystem
    {
        //public MechNetworkRenderer Renderer;

        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        public ICoreAPI Api;
        IClientNetworkChannel clientNetworkChannel;
        IServerNetworkChannel serverNetworkChannel;

        SignalNetworksData data = new SignalNetworksData();

        public override bool ShouldLoad(EnumAppSide side)
        {
            return true;
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.Api = api;

            if (api.World is IClientWorldAccessor)
            {
                //(api as ICoreClientAPI).Event.RegisterRenderer(this, EnumRenderStage.Before, "signalnetworktick");
                clientNetworkChannel =
                ((ICoreClientAPI)api).Network.RegisterChannel("signalnetwork");
                //.RegisterMessageType(typeof(MechNetworkPacket))
                //.RegisterMessageType(typeof(NetworkRemovedPacket))
                //.RegisterMessageType(typeof(MechClientRequestPacket))
                //.SetMessageHandler<MechNetworkPacket>(OnPacket)
                //.SetMessageHandler<NetworkRemovedPacket>(OnNetworkRemovePacket);
            }
            else
            {
                //api.World.RegisterGameTickListener(OnServerGameTick, 20);
                serverNetworkChannel =
                ((ICoreServerAPI)api).Network.RegisterChannel("signalnetwork");
                //.RegisterMessageType(typeof(MechNetworkPacket))
                //.RegisterMessageType(typeof(NetworkRemovedPacket))
                //.RegisterMessageType(typeof(MechClientRequestPacket))
                //.SetMessageHandler<MechClientRequestPacket>(OnClientRequestPacket);
            }
        }

        internal void OnNodeRemoved(ISignalDevice signalNode)
        {
            if(signalNode.Network != null)
            {
                RebuildNetwork(signalNode.Network, signalNode);
            }
        }

        //Rebuilds networks upon supression of a node
        private void RebuildNetwork(SignalNetwork network, ISignalDevice removedNode)
        {
           network.isValid = false;

            if (Api.Side == EnumAppSide.Server) DeleteNetwork(network);

            if (network.nodes.Values.Count == 0)
            {
                if (Api.Side == EnumAppSide.Server) Api.Logger.Notification("Signal Network with id " + network.networkId + " had zero nodes?");
                return;
            }

            var nnodes = network.nodes.Values.ToArray();

            foreach (var nnode in nnodes)
            {
                nnode.LeaveNetwork();
            }

            foreach(var nnode in nnodes)
            {
                if (!(nnode is ISignalDevice)) continue;
                //TODO
                ISignalDevice newNode = GetSignalNodeAt(nnode.Pos);
                if (newNode == null) continue;
                if (newNode.canStartsNetworkDiscovery && (removedNode != null || newNode.Pos != removedNode.Pos))
                {
                    SignalNetwork newNetwork = newNode.CreateJoinAndDiscoverNetwork();
                    //Maybe se network states here?
                    if (Api.Side == EnumAppSide.Server) newNetwork.broadcastData();
                }
            }


        }

        private ISignalDevice GetSignalNodeAt(NodePos pos)
        {
            throw new NotImplementedException();
        }

        public void DeleteNetwork(SignalNetwork network)
        {
            data.networksById.Remove(network.networkId);
            //serverNetworkChannel.BroadcastPacket<NetworkRemovedPacket>(new NetworkRemovedPacket() { networkId = network.networkId });
        }

        internal SignalNetwork GetOrCreateNetwork(long networkId)
        {
            SignalNetwork mw;

            if (!data.networksById.TryGetValue(networkId, out mw))
            {
                data.networksById[networkId] = mw = new SignalNetwork(this, networkId);
            }

            return mw;
        }

        public SignalNetwork CreateNetwork(ISignalDevice deviceNode)
        {
            SignalNetwork nw = new SignalNetwork(this, data.nextNetworkId);
            data.networksById[data.nextNetworkId] = nw;
            data.nextNetworkId++;
            return nw;
        }

        internal SignalNetwork GetNetworkAt(NodePos pos)
        {
            throw new NotImplementedException();
        }
    }
}
