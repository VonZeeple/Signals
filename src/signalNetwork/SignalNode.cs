using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{


    /// <summary>
    /// The most basic element of a signal network
    /// </summary>
    public interface ISignalNode
    {
        NodePos Pos{get; set;}
        Byte State { get; set; }

        void LeaveNetwork();
        //Connected nodes position and attenuation
        Dictionary<NodePos, byte> ConnectedNodes { get; }

    }

    public interface ISignalDevice: ISignalNode
    {
        bool canStartsNetworkDiscovery { get; }
        SignalNetwork Network { get; }


        //bool JoinAndSpreadNetworkToConnections(ICoreAPI api, long propagationId, SignalNetwork network, out Vec3i missingChunkPos);
        SignalNetwork CreateJoinAndDiscoverNetwork();
    }

    public class SignalNodeBase : ISignalDevice
    {
        private static readonly bool DEBUG = true;
        protected SignalNetworkMod manager;
        protected SignalNetwork network;
        public SignalNetwork Network => network;
        public ICoreAPI api;
        public long NetworkId { get; set; }

        public bool canStartsNetworkDiscovery { get; }

        public void Initialize(ICoreAPI api)
        {
            this.api = api;
            manager = api.ModLoader.GetModSystem<SignalNetworkMod>();

            if (api.World.Side == EnumAppSide.Client)
            {
                if (NetworkId > 0)
                {
                    network = manager.GetOrCreateNetwork(NetworkId);
                    JoinNetwork(network);
                }
            }
            if (api.Side == EnumAppSide.Server && canStartsNetworkDiscovery)
            {
                CreateJoinAndDiscoverNetwork();
            }
        }

        public virtual void JoinNetwork(SignalNetwork network)
        {
            if (this.network != null && this.network != network)
            {
                LeaveNetwork();
            }

            if (this.network == null)
            {
                this.network = network;
                network?.Join(this);
            }

            if (network == null) NetworkId = 0;
            else
            {
                NetworkId = network.networkId;
            }
        }

        public virtual void LeaveNetwork()
        {
            if (DEBUG) api.Logger.Notification("Leaving network " + NetworkId + " at " + this.Pos);
            network?.Leave(this);
            network = null;
            NetworkId = 0;
            //currentPropagationId = -1;  //reset currentPropagationId to allow the block to be later reconnected to the same network without problems, if the connection to the network is re-placed
            //Blockentity.MarkDirty();
        }

        public virtual void OnRemove()
        {
            if (network != null)
            {
                manager.OnNodeRemoved(this);
            }

            LeaveNetwork();
        }

        public void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            long nowNetworkId = tree.GetLong("networkid");
            if (worldAccessForResolve.Side == EnumAppSide.Client)
            {
                if (NetworkId != nowNetworkId)   //don't ever change network settings from tree on server side - networkId is not data to be saved  (otherwise would mess up networks on chunk loading, if BE tree loaded after a BE has already had network assigned on the server by propagation from a neighbour)
                {
                    NetworkId = 0;
                    if (worldAccessForResolve.Side == EnumAppSide.Client)
                    {
                        NetworkId = nowNetworkId;
                        if (NetworkId == 0)
                        {
                            LeaveNetwork();
                            network = null;
                        }
                        else if (manager != null)
                        {
                            network = manager.GetOrCreateNetwork(NetworkId);
                            JoinNetwork(network);
                            //Blockentity.MarkDirty();
                        }
                    }
                }
            }
        }

        public void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetLong("networkid", NetworkId);
        }

        public NodePos Pos { get; set; }
        public byte State { get ; set; }

        public Dictionary<NodePos, byte> ConnectedNodes { get; set; }




        #region Network Discovery
        public SignalNetwork CreateJoinAndDiscoverNetwork()
        {

            //If no connected node, create a new network and ad this node

            //We look at the first connected node
            //If the node does have a network, we join
            //If it doesn't have a network, we create a new one and start discovery

            //Then the other nodes:
            //If the node does have a network we merge
            //If the node doesn't have a network, start discovery...
            return null;
        }


        #endregion
    }

}
