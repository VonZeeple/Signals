using signals.src.transmission;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    public class BEBehaviorSignalSwitch : BEBehaviorSignalConnector
    {
        Connection con;
        SignalNetworkMod signalMod;
        byte onAttenuation = 0;
        byte offAttenuation = 15;

        public BEBehaviorSignalSwitch(BlockEntity blockentity) : base(blockentity)
        {
        }

        public virtual void commute(bool state){
            if(signalMod.Api.Side == EnumAppSide.Client) return;
            byte newValue = state? onAttenuation: offAttenuation;
            signalMod.netManager.UpdateConnection(con, newValue, newValue);
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
            base.Initialize(api, properties);
            onAttenuation = properties.KeyExists("onAttenuation") ? (byte)(properties["onAttenuation"].AsInt()): (byte)0;
            offAttenuation = properties.KeyExists("offAttenuation") ? (byte)(properties["offAttenuation"].AsInt()): (byte)15;

            NodePos pos1 = new NodePos(this.Pos, 0);
            NodePos pos2 = new NodePos(this.Pos, 1);

            ISignalNode node1 = GetNodeAt(pos1);
            ISignalNode node2 = GetNodeAt(pos2);

            if (node1 == null || node2 == null) return;
            if(signalMod.Api.Side == EnumAppSide.Client) return;
            BESwitch be = this.Blockentity as BESwitch;
            if(be == null) return;
            con = new Connection(node1, node2, be.state? onAttenuation: offAttenuation);
            signalMod.netManager.AddConnection(con);
        }
    }
}
