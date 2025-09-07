using ProtoBuf;
using signals.src.hangingwires;
using signals.src.signalNetwork;
using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;


namespace signals.src
{
    public class SignalNetworkMod : ModSystem
    {
        public SignalNetworkDebugRenderer Renderer;

        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        public ICoreAPI Api;
        IClientNetworkChannel clientNetworkChannel;
        IServerNetworkChannel serverNetworkChannel;
        HangingWiresMod wireMod;

        bool needRenderUpdate = true;

        public NetworkManager netManager;

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
                ((ICoreClientAPI)api).Network.RegisterChannel("signalnetwork")
                .RegisterMessageType(typeof(SignalNetworkPacket))
                .SetMessageHandler<SignalNetworkPacket>(OnNetworkPacket);
            }
            else
            {
                api.World.RegisterGameTickListener(OnServerGameTick, 20);
                serverNetworkChannel =
                ((ICoreServerAPI)api).Network.RegisterChannel("signalnetwork")
                .RegisterMessageType(typeof(SignalNetworkPacket));
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            api.Event.ChunkColumnLoaded += Event_ChunkColumnLoaded;
            netManager = new NetworkManager(api, this);
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

        public void broadcastNetwork()
        {
            SignalNetworkPacket packet = new SignalNetworkPacket();
            packet.networks = new Dictionary<long, List<NodePos>>();
            foreach(KeyValuePair<long, ISignalNetwork> el in netManager.networks)
            {
                ISignalNetwork net = el.Value;
                packet.networks.Add(el.Key,net.GetNodePositions().ToList());
            }
            serverNetworkChannel.BroadcastPacket<SignalNetworkPacket>(packet);
        }

        #endregion

        #region network stuff

        List<Action> ToDoOnSignalTick = new List<Action>();

        public void RegisterSignalTickListener(Action OnSignalTick){
            ToDoOnSignalTick.Add(OnSignalTick);
        }
        public void DisposeSignalTickListener(Action OnSignalTick){
            ToDoOnSignalTick.Remove(OnSignalTick);
        }
        List<ISignalNodeProvider> devicesToLoad = new List<ISignalNodeProvider>();

        private float timeFromLastTick = 0;
        public void OnServerGameTick(float dt)
        {
            timeFromLastTick += dt;
            if (timeFromLastTick < 0.1) return;
            timeFromLastTick = 0;

            LoadDevices();
            foreach (Action toDo in ToDoOnSignalTick){
                toDo();
            }

            foreach(SignalNetwork net in netManager.networks.Values)
            {
                if (!net.isValid)
                {
                    net.Simulate();
                }
            }

            if(needRenderUpdate){
                needRenderUpdate = false;
                broadcastNetwork();
            }
        }

        public void LoadDevices()
        {
            if (devicesToLoad.Count == 0) return;
            foreach (ISignalNodeProvider device in devicesToLoad)
            {
                netManager.AddNodes(device.GetNodes().Values.ToArray());
            }
            devicesToLoad.Clear();
        }

        public void OnWireRemoved(WireConnection wire)
        {
            if (Api.Side == EnumAppSide.Client) return;
            netManager.RemoveConnections([GetConnection(wire.pos1, wire.pos2), GetConnection(wire.pos2, wire.pos1)]);
            needRenderUpdate = true;

        }

        public void OnWireAdded(WireConnection wire)
        {
            ISignalNode node1 = GetDeviceAt(wire.pos1.blockPos)?.GetNodeAt(wire.pos1);
            ISignalNode node2 = GetDeviceAt(wire.pos2.blockPos)?.GetNodeAt(wire.pos2);
            Connection con = new Connection(node1, node2);
            netManager.AddConnection(con);
            needRenderUpdate = true;
        }

        public ISignalNodeProvider GetDeviceAt(BlockPos pos)
        {
            BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(pos);
            if (be == null) return null;
            ISignalNodeProvider device = be as ISignalNodeProvider;
            if (device != null) return device;
            return be.GetBehavior<BEBehaviorSignalNodeProvider>() as ISignalNodeProvider;
        }

        public Connection GetConnection(NodePos pos1, NodePos pos2)
        {
            ISignalNode node1 = GetDeviceAt(pos1.blockPos)?.GetNodeAt(pos1);
            ISignalNode node2 = GetDeviceAt(pos2.blockPos)?.GetNodeAt(pos2);
            if ( node1 == null || node2 == null) return null;
            foreach(Connection con in node1.Connections){
                if(con.node1 == node2 || con.node2 == node2) return con;
            }
            return null;
        }

        private SignalNetwork GetNetworkAt(NodePos pos)
        {
            foreach(SignalNetwork net in netManager.networks.Values)
            {
                if (net.nodes.ContainsKey(pos))
                {
                    return net;
                }
            }

            return null;
        }

        internal void OnDeviceRemoved(ISignalNodeProvider device)
        {
            if(this.Api.Side == EnumAppSide.Client) return;
            Dictionary<NodePos,ISignalNode> nodes = device.GetNodes();
            netManager.RemoveNodes(nodes.Values.ToArray());
            needRenderUpdate = true;
        }

        internal void OnDeviceUnloaded(ISignalNodeProvider device)
        {
            
        }

        internal void OnDeviceInitialized(ISignalNodeProvider device)
        {
            if (Api.Side == EnumAppSide.Client) {return;}
            foreach (ISignalNode node in device.GetNodes().Values) {
                List<WireConnection> wires = wireMod.GetWireConnectionsFrom(node.Pos);
                foreach (WireConnection wire in wires) {
                    ISignalNode node2 = GetDeviceAt(wire.pos2.blockPos)?.GetNodeAt(wire.pos2);
                    if(node2 != null){
                        Connection con = new Connection(node,node2);
                        netManager.AddConnection(con);
                    }
                }
            }
            devicesToLoad.Add(device);
        }

        internal void OnNetworkPacket(SignalNetworkPacket packet)
        {
            //Renderer.RebuildMesh(packet.networks);
        }
    }


    #endregion
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SignalNetworkPacket
    {
        public Dictionary<long,List<NodePos>> networks;
    }
}
