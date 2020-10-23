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
            Renderer.UpdateWiresMesh(data.connections);
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
            if (data != null)
            {
                this.data = SerializerUtil.Deserialize<HangingWiresData>(data);
            } else {
                this.data = new HangingWiresData();
            }

        }

        public void RemoveAllNodesAtBlockPos(BlockPos pos)
        {
            if (api.Side == EnumAppSide.Client) return;
            int removedlinks = data.connections.RemoveWhere((Connection con) => {
                return con.pos1.blockPos == pos || con.pos2.blockPos == pos;
            });
            serverChannel.BroadcastPacket(data);
        }

        public void OnAddConnectionFromClient(IServerPlayer fromPlayer, AddConnectionPacket networkMessage)
        {
            var connection = networkMessage.connection as Connection;
            if (connection == null) return;

            //TODO: add checks to be sure that their is a node provider at the position

            data.TryToAdd(connection);
            //sapi.SendMessage(fromPlayer, 0, "Connection added on server", EnumChatType.Notification);
            serverChannel.BroadcastPacket(data);
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

                Connection connection = new Connection(pendingNode, pos);
                clientChannel.SendPacket(new AddConnectionPacket() { connection = connection});
                pendingNode = null;
            }
        }

    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class AddConnectionPacket
    {
        public Connection connection;

        public AddConnectionPacket()
        {
        }
    }



    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class HangingWiresData
    {
        public HashSet<Connection> connections = new HashSet<Connection>();

        public bool TryToAdd(Connection connection)
        {
            if (connections.Contains(connection))
            {
                return false;
            }
            connections.Add(connection);
            return true;
        }
        public bool TryToAdd(NodePos pos1, NodePos pos2)
        {

            if (connections.Contains(new Connection(pos1, pos2)) )
            {
                return false;
            }
            connections.Add(new Connection(pos1, pos2));
            return true;
        }

    }
}
