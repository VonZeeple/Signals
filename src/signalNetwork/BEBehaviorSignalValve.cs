using signals.src.transmission;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    public class BEBehaviorSignalValve : BEBehaviorSignalConnector
    {
        Connection con;
        SignalNetworkMod signalMod;

        public BEBehaviorSignalValve(BlockEntity blockentity) : base(blockentity)
        {
        }

        public void commute(byte state){
            if (signalMod.Api.Side == EnumAppSide.Client) return;
            signalMod.netManager.UpdateConnection(con, state, 15);
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
            base.Initialize(api, properties);

            NodePos pos1 = new NodePos(this.Pos, 1);
            NodePos pos2 = new NodePos(this.Pos, 2);

            ISignalNode node1 = GetNodeAt(pos1);
            ISignalNode node2 = GetNodeAt(pos2);

            if (node1 == null || node2 == null) return;
            if (signalMod.Api.Side == EnumAppSide.Client) return;
            BEValve be = Blockentity as BEValve; 
            con = new Connection(node1, node2, be.state, 15);
            signalMod.netManager.AddConnection(con);
        }
    }
}
