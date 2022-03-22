using System;
using signals.src.transmission;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    class BEDelay : BlockEntity, IBESignalReceptor
    {
        private byte[] values;
        private byte lastInput;

        private int state = 0;
        BEDelayRenderer renderer;
        SignalNetworkMod signalMod;

        public override void Initialize(ICoreAPI api){
            base.Initialize(api);

            Block block = this.Block as Block;
            if( block.Variant["value"] != null ){
                state = Int32.Parse(block.Variant["value"]);
            }
            values = new byte[6]{0,0,0,0,0,0};
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
            signalMod.RegisterSignalTickListener(OnSignalNetworkTick);
            if (api.Side == EnumAppSide.Client){
                renderer = new BEDelayRenderer(api as ICoreClientAPI, Pos);
                (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "signaldelay");
                renderer.UpdateMesh(values);
            }
        }

      public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            signalMod.DisposeSignalTickListener(OnSignalNetworkTick);
            renderer?.Dispose();
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            signalMod.DisposeSignalTickListener(OnSignalNetworkTick);
            renderer?.Dispose();
        }

        public void OnSignalNetworkTick()
        {
            Block block = this.Block as Block;
            state = Int32.Parse(block.Variant["value"]);
            for (int i=0; i < values.Length-1; i++){
                if(values.Length-i-1 <=state){
                    values[values.Length-i-1] = values[values.Length-i-2];
                }else{
                    values[values.Length-i-1] = 0;
                }
            }
            BEBehaviorSignalConnector beb = GetBehavior<BEBehaviorSignalConnector>();
            ISignalNode nodeProbe = beb.GetNodeAt(new NodePos(this.Pos, 0));
            ISignalNode nodeSource = beb.GetNodeAt(new NodePos(this.Pos, 1));
            values[0] = nodeProbe.value;
            signalMod.netManager.UpdateSource(nodeSource, values[state]);
            MarkDirty();
        }

        public void OnValueChanged(NodePos pos, byte value)
        {
            if(pos.index == 0){
                lastInput = value;
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            values = tree.GetBytes("values", new byte[6]{0,0,0,0,0,0});
            if (worldForResolving.Side == EnumAppSide.Client && this.renderer != null)
            {
                renderer.UpdateMesh(values);
                MarkDirty(true);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBytes("values", values);
        }
    }
}