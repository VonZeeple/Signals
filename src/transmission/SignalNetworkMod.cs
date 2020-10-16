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
    // This is inspired by the implementation of the mechanical network by Tyron.
    // A network is a set of connected blocks and is materialised in game by "hanging wires"

    //Nodes in a network are indexed by a SignalNodePos, wich contains a BlockPos and an index. This allows multiple connections within a block, for circuits by example.
    //Since it is necessary to render wires, networks are created upon the first link between two isolated blocks.

    //for now, hanging wires only convey an 16 levels signal. Maybe in the futur they will convey electrical power.

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SignalNetworksData
    {
        public Dictionary<long, SignalNetwork> networksById = new Dictionary<long, SignalNetwork>();
        public long nextNetworkId = 1;
        public long tickNumber = 0;
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
                api.World.RegisterGameTickListener(OnServerGameTick, 20);
                serverNetworkChannel =
                ((ICoreServerAPI)api).Network.RegisterChannel("signalnetwork");
                //.RegisterMessageType(typeof(MechNetworkPacket))
                //.RegisterMessageType(typeof(NetworkRemovedPacket))
                //.RegisterMessageType(typeof(MechClientRequestPacket))
                //.SetMessageHandler<MechClientRequestPacket>(OnClientRequestPacket);
            }
        }

        protected void OnServerGameTick(float dt)
        {
            data.tickNumber++;

            List<SignalNetwork> clone = data.networksById.Values.ToList();
            foreach (SignalNetwork network in clone)
            {
                if (network.fullyLoaded)
                {
                    network.ServerTick(dt, data.tickNumber);
                }
            }
        }


        public SignalNetwork CreateNetwork(ISignalNode node)
        {
            SignalNetwork sn = new SignalNetwork(this, data.nextNetworkId);
            sn.fullyLoaded = true;
            data.networksById[data.nextNetworkId] = sn;
            data.nextNetworkId++;
            return sn;
        }
        public void DeleteNetwork(SignalNetwork network)
        {
            data.networksById.Remove(network.networkId);
            //serverNwChannel.BroadcastPacket<NetworkRemovedPacket>(new NetworkRemovedPacket() { networkId = network.networkId });
        }
    }
}
