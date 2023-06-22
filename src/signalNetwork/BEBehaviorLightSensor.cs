using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace signals.src.signalNetwork
{
    public class BEBehaviorLightSensor : BlockEntityBehavior
    {
        Connection con;
        NodePos pos1;
        NodePos pos2;
        SignalNetworkMod signalMod => Api.ModLoader.GetModSystem<SignalNetworkMod>();
        private BEBehaviorSignalNodeProvider nodeProvider;
        public BEBehaviorLightSensor(BlockEntity blockentity) : base(blockentity)
        {
        }


        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            if (properties.KeyExists("Node1Index") && properties.KeyExists("Node2Index"))
            {
                pos1 = new NodePos(this.Pos,properties["Node1Index"].AsInt());
                pos2 = new NodePos(this.Pos,properties["Node2Index"].AsInt());
            }
            nodeProvider = this.Blockentity.GetBehavior<BEBehaviorSignalNodeProvider>();
            Blockentity.RegisterGameTickListener(OnSlowServerTick, 1000);
        }

        private void OnSlowServerTick(float dt)
        {
            if (pos1==null || pos2==null) return;
            byte signal = GetSignalValue();
            con = signalMod.GetConnection(pos1, pos2);
            updateCon(signal);
        }

        private byte GetSignalValue()
        {
            int light = Api.World.GetBlockAccessor(false,false,false).GetLightLevel(this.Pos, EnumLightLevelType.MaxLight);
            light = 32-light;
            if(light >= 30) {return (byte)15;}
            return (byte) Math.Floor(light*0.5d);
        }
        public void updateCon(byte state)
        {
            if (Api.Side == EnumAppSide.Client || con == null) return;
            signalMod.netManager.UpdateConnection(con, state, state);
            Blockentity.MarkDirty();
        }
    }
}