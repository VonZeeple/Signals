using ProtoBuf;
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
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;

namespace signals.src.hangingwires
{
    public class HangingWiresMod : ModSystem
    {

        public HangingWiresRenderer Renderer;

        IServerNetworkChannel serverChannel;
        IClientNetworkChannel clientChannel;

        HangingWiresData data = new HangingWiresData();
        ICoreAPI api;
        ICoreServerAPI sapi;
        ICoreClientAPI capi;

        #region ModSystem

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.api = api;

            if (api.World is IClientWorldAccessor)
            {
                clientChannel = ((ICoreClientAPI)api).Network.RegisterChannel("hangingwires")
                .RegisterMessageType(typeof(HangingWiresData))
                .RegisterMessageType(typeof(AddConnectionPacket))
                .SetMessageHandler<HangingWiresData>(onDataFromServer);

            }
            else
            {
                serverChannel = ((ICoreServerAPI)api).Network.RegisterChannel("hangingwires")
               .RegisterMessageType(typeof(HangingWiresData))
               .RegisterMessageType(typeof(AddConnectionPacket))
               .SetMessageHandler<AddConnectionPacket>(OnAddConnectionFromClient);
            }
        }

        internal List<WireConnection> GetWireConnectionsFrom(NodePos pos)
        {
            List<WireConnection> toProcess = data.connections.Where(c => c.pos1 == pos || c.pos2 == pos).ToList();
            List<WireConnection> output = new List<WireConnection>();
            foreach(WireConnection con in toProcess)
            {
                if(con.pos1 == pos)
                {
                    output.Add(new WireConnection(con.pos1, con.pos2));
                }
                else
                {
                    output.Add(new WireConnection(con.pos2, con.pos1));
                }
            }
            return output;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;


            api.Event.BlockTexturesLoaded += onLoaded;
            api.Event.LeaveWorld += () =>
            {
                Renderer?.Dispose();
            };

        }

        private void onDataFromServer(HangingWiresData data)
        {
            this.data = data;
            Renderer.UpdateWiresMesh(data);
        }


        private void onLoaded()
        {
            Renderer = new HangingWiresRenderer(capi, this);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;

            api.Event.GameWorldSave += Event_GameWorldSave;
            api.Event.SaveGameLoaded += Event_SaveGameLoaded;
            sapi.Event.PlayerJoin += Event_OnPlayerJoin;
        }

        private void Event_GameWorldSave()
        {
            sapi.WorldManager.SaveGame.StoreData("hangingWiresData", SerializerUtil.Serialize(data));
        }

        private void Event_OnPlayerJoin(IServerPlayer player)
        {
            serverChannel.SendPacket<HangingWiresData>(data, player);
        }

        private void Event_SaveGameLoaded()
        {
            
            byte[] data = sapi.WorldManager.SaveGame.GetData("hangingWiresData");

                try
                {
                    this.data = SerializerUtil.Deserialize<HangingWiresData>(data);
                }
                catch(Exception e)
                {
                    this.data = new HangingWiresData();
                }

        }

        #endregion

        #region interactions

        public void RemoveAllNodesAtBlockPos(BlockPos pos)
        {
            if (api.Side == EnumAppSide.Client) return;

            List<WireConnection> toRemove = data.connections.Where((WireConnection con) => {
                return con.pos1.blockPos == pos || con.pos2.blockPos == pos;
            }).ToList();
            if(toRemove.Count > 0)
            {
                foreach(WireConnection con in toRemove)
                {
                    data.connections.Remove(con);
                }
                

                serverChannel.BroadcastPacket(data);
            }
            
        }

        public void OnAddConnectionFromClient(IServerPlayer fromPlayer, AddConnectionPacket networkMessage)
        {
            var connection = networkMessage.connection as WireConnection;
            if (connection == null) return;

            //TODO: add checks to be sure that their is a node provider at the position (never trust the client)

            //CreateConnection(connection, fromPlayer);
            bool added = data.connections.Add(connection);
            if (added)
            {
                api.ModLoader.GetModSystem<SignalNetworkMod>()?.OnWireAdded(connection);
                serverChannel.BroadcastPacket(data);
            }
        }

        NodePos pendingNode = null;
        public NodePos GetPendingNode()
        {
            return this.pendingNode;
        }
        public void SetPendingNode(NodePos pos)
        {
            if (api.Side == EnumAppSide.Server) return;

            if(pendingNode == null)
            {
                pendingNode = pos;
                capi?.ShowChatMessage(String.Format("Pending {0}:{1}",pos.blockPos,pos.index));
            }
            else
            {
                capi?.ShowChatMessage(String.Format("trying to attach {0}:{1}", pos.blockPos, pos.index));
                WireConnection connection = new WireConnection(pendingNode, pos);
                clientChannel.SendPacket(new AddConnectionPacket() { connection = connection});
                pendingNode = null;
            }
        }
        #endregion

        #region Networks

        /*public bool CreateConnection(WireConnection connection, IServerPlayer fromPlayer)
        {
            if (connection.pos1 == connection.pos2) return false;

            HangingWiresNetwork net = FindNetworkOf(connection.pos1);
            HangingWiresNetwork net2 = FindNetworkOf(connection.pos2);
            
            if (net == null && net2 == null)
            {
                
                HangingWiresNetwork newNet = CreateNetwork(connection);
                sapi.SendMessage(fromPlayer, 0, String.Format("creating new net {0}", newNet.NetworkId), EnumChatType.Notification);
                return true;
            }

            if(net == net2)
            {
                if (net.Connections.Contains(connection)) { return false; }

                sapi.SendMessage(fromPlayer, 0, String.Format("adding connection to net {0}", net.NetworkId), EnumChatType.Notification);
                net.Connections.Add(connection);
                return true;
            }
            

            if (net != null)
            {
                if(net2 != null)
                {
                    net.Connections.Add(connection);
                    MergeNetworks(net.NetworkId, net2.NetworkId);
                    sapi.SendMessage(fromPlayer, 0, String.Format("Merging networks {0} and {1}", net.NetworkId, net2.NetworkId), EnumChatType.Notification);
                    return true;
                }
                else
                {
                    net.Connections.Add(connection);
                    sapi.SendMessage(fromPlayer, 0, String.Format("adding connection to networks {0}", net.NetworkId), EnumChatType.Notification);
                    return true;
                }

            }
            if(net2 != null)
            {
                if (net != null)
                {
                    net2.Connections.Add(connection);
                    MergeNetworks(net.NetworkId, net2.NetworkId);
                    sapi.SendMessage(fromPlayer, 0, String.Format("Merging networks {0} and {1}", net.NetworkId, net2.NetworkId), EnumChatType.Notification);
                    return true;
                }
                else
                {
                    net2.Connections.Add(connection);
                    sapi.SendMessage(fromPlayer, 0, String.Format("adding connection to networks {0}", net2.NetworkId), EnumChatType.Notification);
                    return true;
                }
            }


            return false;
        }*/

        public void MergeNetworks(long netId1, long netId2)
        {
            //if (!data.HangingWiresNetworks.ContainsKey(netId1) || !data.HangingWiresNetworks.ContainsKey(netId2)) return;

            //data.HangingWiresNetworks[netId1].Connections.AddRange(data.HangingWiresNetworks[netId2].Connections);
            //data.HangingWiresNetworks.Remove(netId2);
        }

        //HangingWiresNetwork CreateNetwork(WireConnection connection)
        //{
        //    HangingWiresNetwork newNet = new HangingWiresNetwork(data.nextNetworkId);
        //    newNet.Connections.Add(connection);
        //    data.HangingWiresNetworks[data.nextNetworkId] = newNet;
        //    data.nextNetworkId++;
        //    return newNet;
        //}
        //HangingWiresNetwork FindNetworkOf(NodePos pos)
        //{
            //foreach (HangingWiresNetwork net in data.HangingWiresNetworks.Values)
            //{
            //    if (net.Connections.FirstOrDefault(x => (x.pos1 == pos || x.pos2 == pos)) != null)
            //    {
            //        return net;
            //    }
            //}
            //return null;
        //}


        #endregion
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class AddConnectionPacket
    {
        public WireConnection connection;

        public AddConnectionPacket()
        {
        }
    }


    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class HangingWiresNetwork
    {
        public long NetworkId;
        public HashSet<WireConnection> Connections = new HashSet<WireConnection>();

        public HangingWiresNetwork() { }
        public HangingWiresNetwork(long netId)
        {
            NetworkId = netId;
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class HangingWiresData
    {
        //public Dictionary<long, HangingWiresNetwork> HangingWiresNetworks = new Dictionary<long, HangingWiresNetwork>();
        //public long nextNetworkId = 1;
        public HashSet<WireConnection> connections = new HashSet<WireConnection>();

    }
}
