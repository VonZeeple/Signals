using ProtoBuf;
using signals.src.hangingwires;
using signals.src.signalNetwork;
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
using Vintagestory.GameContent.Mechanics;


namespace signals.src
{

    //Concept
    // A Network is a set of connected nodes
    // A node is indexed by a NodePos, wich is a BlockPos + an integer index. A block can contain several nodes
    //A connection is a link between two nodes
   //Because diodes and vacuum tubes are implemented, node A being connected with node B doesn't mean that node B is connected with node A
   //Connections can be lossy, the conveyed signal can be decreased by an interger number until 0.
   
    //Networks are created when a signal source is placed/loaded/activated


    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SignalNetworksData
    {
        public Dictionary<long, SignalNetwork> networksById = new Dictionary<long, SignalNetwork>();
        public long nextNetworkId = 1;
    }

    
    public class SignalNetworkMod : ModSystem
    {

        public SignalNetworkDebugRenderer Renderer;

        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        public ICoreAPI Api;
        IClientNetworkChannel clientNetworkChannel;
        IServerNetworkChannel serverNetworkChannel;

        SignalNetworksData data = new SignalNetworksData();
        HangingWiresMod wireMod;

        #region Mod stuff
        public override bool ShouldLoad(EnumAppSide side)
        {
            return true;
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.Api = api;
            this.wireMod = api.ModLoader.GetModSystem<HangingWiresMod>();

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
                api.World.RegisterGameTickListener(OnServerGameTick, 20);
                serverNetworkChannel =
                ((ICoreServerAPI)api).Network.RegisterChannel("signalnetwork");
                //.RegisterMessageType(typeof(MechNetworkPacket))
                //.RegisterMessageType(typeof(NetworkRemovedPacket))
                //.RegisterMessageType(typeof(MechClientRequestPacket))
                //.SetMessageHandler<MechClientRequestPacket>(OnClientRequestPacket);
            }

            
            
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            api.Event.ChunkColumnLoaded += Event_ChunkColumnLoaded;
        }
        
        private void Event_ChunkColumnLoaded( Vec2i chunkCoord, IWorldChunk[] chuncks)
        {

        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;

            Renderer = new SignalNetworkDebugRenderer(capi, this);
            api.Event.LeaveWorld += () =>
            {
                Renderer?.Dispose();
            };

        }

        public void broadcastNetwork(SignalNetworkPacket packet)
        {
            serverNetworkChannel.BroadcastPacket(packet);
        }

        #endregion

        #region network stuff


        
        List<ISignalNodeProvider> devicesToLoad = new List<ISignalNodeProvider>();



        //TODO change this to be loaded with savegame
        Dictionary<BlockPos, SNDeviceProxy> proxies = new Dictionary<BlockPos, SNDeviceProxy>();



        public void OnServerGameTick(float dt)
        {
            LoadDevices();

            foreach(SignalNetwork net in data.networksById.Values)
            {
                if (!net.isValid)
                {
                    net.Simulate();
                }
            }
        }
        //devices that have been initialized, can be after block placement, or chunk loading
        public void LoadDevices()
        {
            if (devicesToLoad.Count == 0) return;
            foreach (ISignalNodeProvider device in devicesToLoad)
            {
                Api.Logger.Debug("Loading device at pos");


                //If the device is not in proxies, create a network if the device contains a source node
                foreach (ISignalNode node in device.GetNodes().Values)
                {
                    
                    //ISignalNode node = kv.Value;
                    NodePos pos = node.Pos;
                    if (pos == null) continue;
                    Api.Logger.Debug("node found, at {0}, looking at connections", pos);
                    List<Connection> connections = node.Connections;
                    SignalNetwork net;
                    if (node.isSource)
                    {
                        Api.Logger.Debug("node is source, creating a network and lauching network discovery");
                        net = GetNetworkAt(pos, true);
                        net.AddNodesFoundFrom(node.Pos, node);
                        continue;
                    }
                    
                    foreach(Connection con in connections)
                    {
                        TryToAddConnection(con);
                    }

                    
                }
             
            }
            devicesToLoad.Clear();
            
        }


        public void OnWireAdded(WireConnection con)
        {
            ISignalNodeProvider dev1 = GetDeviceAt(con.pos1.blockPos);
            ISignalNodeProvider dev2 = GetDeviceAt(con.pos2.blockPos);
            if (dev1 == null || dev2 == null) return;
            //wireConnections.Add(con);
            GetDeviceAt(con.pos1.blockPos)?.GetNodeAt(con.pos1)?.Connections.Add(con);
            GetDeviceAt(con.pos2.blockPos)?.GetNodeAt(con.pos2)?.Connections.Add(con.GetReversed());
            TryToAddConnection(con);
            TryToAddConnection(con.GetReversed());
        }

        public void TryToAddConnection(Connection con)
        {
            NodePos pos1 = con.pos1;
            NodePos pos2 = con.pos2;            
            SignalNetwork net1 = GetNetworkAt(pos1, false);
            SignalNetwork net2 = GetNetworkAt(pos2, false);
            if (net1 == null && net2 == null) return;
            if(net1 == null)
            {
                ISignalNode node = GetDeviceAt(pos1.blockPos)?.GetNodeAt(pos1);
                if (node == null) return;
                net2.AddNodesFoundFrom(pos1, node);
            }
            else if(net2 == null)
            {
                ISignalNode node = GetDeviceAt(pos2.blockPos)?.GetNodeAt(pos2);
                if (node == null) return;
                net1.AddNodesFoundFrom(pos2, node);
            }
            AddConnection(con);
        }



        public void AddConnection(Connection con)
        {
            NodePos pos1 = con.pos1;
            NodePos pos2 = con.pos2;
            SignalNetwork net1 = GetNetworkAt(pos1, false);
            SignalNetwork net2 = GetNetworkAt(pos2, false);
            if (net1 == null || net2 == null) return;
            SignalNetwork mergedNetwork;
            if(net1 != net2)
            {
                mergedNetwork = net1.Merge(net2);
                Api.Logger.Debug("Merging signal networks {0} and {1}, into net {2}",net1.networkId, net2.networkId, mergedNetwork.networkId);
                foreach (ISignalNode node in mergedNetwork.nodes.Values)
                {
                    SetNodeNetwork(node, mergedNetwork);
                }
            }
            else
            {
                mergedNetwork = net1;
            }

            Api.Logger.Debug("Adding connection into net {0}",mergedNetwork.networkId);
            mergedNetwork.AddConnection(con);

            //Notify handler and sync ect...
        }


        private void AddConnectionToNode(ISignalNode node, Connection con)
        {
            if (node.Pos != con.pos1) return;
            node.Connections.Add(con);
        }

        private void SetNodeNetwork(ISignalNode node, SignalNetwork net)
        {

        }

        private SignalNetwork CreateNetwork()
        {
            Api.Logger.Debug("Creating signal network with Id: {0}", data.nextNetworkId);
            SignalNetwork net = new SignalNetwork(this, data.nextNetworkId);
            data.networksById[data.nextNetworkId] = net;
            data.nextNetworkId++;
            return net;
        }


        public ISignalNodeProvider GetDeviceAt(BlockPos pos)
        {
            BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(pos);
            if (be == null) return null;
            ISignalNodeProvider device = be as ISignalNodeProvider;
            if (device != null) return device;
            return be.GetBehavior<BEBehaviorSignalNodeProvider>() as ISignalNodeProvider;
        }


        private SignalNetwork GetNetworkAt(NodePos pos, bool createIfnull)
        {
            foreach(SignalNetwork net in data.networksById.Values)
            {
                if (net.nodes.ContainsKey(pos))
                {
                    return net;
                }
            }

            if (!createIfnull) return null;

            SignalNetwork newNet = CreateNetwork();
            return newNet;
        }

        internal void OnDeviceRemoved(ISignalNodeProvider device)
        {
            Dictionary<NodePos,ISignalNode> nodes = device.GetNodes();
            foreach(ISignalNode node in nodes.Values)
            {
                SignalNetwork net = GetNetworkAt(node.Pos, false);
                if (net == null) return;
                net.RemoveNode(node.Pos);
            }
        }

        internal void OnDeviceUnloaded(ISignalNodeProvider device)
        {
            
        }

        internal void OnDeviceInitialized(ISignalNodeProvider device)
        {

            devicesToLoad.Add(device);
        }
    }


    #endregion
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SignalNetworkPacket
    {
        public long networkId;
        public byte state;

    }
}
