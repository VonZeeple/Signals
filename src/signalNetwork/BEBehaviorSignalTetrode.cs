using System;
using signals.src.transmission;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    public class BEBehaviorSignalTetrode : BEBehaviorSignalConnector, IBESignalReceptor
    {
        Connection con;
        SignalNetworkMod signalMod;

        public byte grid_signal;
        public byte screen_signal;

        public BEBehaviorSignalTetrode(BlockEntity blockentity) : base(blockentity)
        {
        }

        public void Commute(byte att)
        {
            if (signalMod.Api.Side == EnumAppSide.Client) return;
            signalMod.netManager.UpdateConnection(con, att, 15);
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
            base.Initialize(api, properties);

            NodePos pos1 = new NodePos(this.Pos, 1);
            NodePos pos2 = new NodePos(this.Pos, 2);
            NodePos pos3 = new NodePos(this.Pos, 3);

            ISignalNode node1 = GetNodeAt(pos1);
            ISignalNode node2 = GetNodeAt(pos2);
            ISignalNode node3 = GetNodeAt(pos3);

            if (node1 == null || node2 == null || node3 == null) return;
            if (signalMod.Api.Side == EnumAppSide.Client) return;
            con = new Connection(node1, node2, GetNewAtt(), 15);
            signalMod.netManager.AddConnection(con);
        }

        public byte GetNewAtt()
        {
            return (byte)Math.Clamp(grid_signal * (screen_signal + 1), 0, 15);
        }

        public void OnValueChanged(NodePos pos, byte value)
        {
            bool update = false;
            if (pos.index == 0)
            {
                grid_signal = value;
                update = true;
            }
            if (pos.index == 3)
            {
                screen_signal = value;
                update = true;
            }
            if (update)
            {
                Commute(GetNewAtt());
            }

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            grid_signal = tree.GetBytes("grid_signal", [0])[0];
            screen_signal = tree.GetBytes("screen_signal", [0])[0];
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBytes("grid_signal", [grid_signal]);
            tree.SetBytes("screen_signal", [screen_signal]);
        }

    }
}
