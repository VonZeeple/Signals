using System.Linq;
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
        BEDelayRenderer renderer;

        public override void Initialize(ICoreAPI api){
            base.Initialize(api);
            values = new byte[6]{15,0,0,0,0,0};
            SignalNetworkMod signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
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
            renderer?.Dispose();
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            renderer?.Dispose();
        }

        public void OnSignalNetworkTick()
        {
            if(values.All(o => o == 0)){
                values[0] = 15;
            }else{
                for (int i=0; i < values.Length-1; i++){
                    values[values.Length-i-1] = values[values.Length-i-2];
                }
                values[0] = 0;
            }
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