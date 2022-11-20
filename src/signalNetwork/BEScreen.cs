

using System.Linq;
using signals.src.transmission;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    class BEScreen : BlockEntity, IBESignalReceptor
    {
        BEScreenRenderer renderer;
        int SCREEN_SIZE_X = 16;
        int SCREEN_SIZE_Y = 16;
        private byte[] values;
        byte X = 0;
        byte Y = 0;
        byte Z = 0;
        byte Reset = 0;

        public void OnValueChanged(NodePos pos, byte value)
        {
            //0: X, 1: Y, 2:Z, 3:Reset
            if(pos.index == 0){
                X = value;
            }else if(pos.index == 1){
                Y = value;
            }else if(pos.index == 2){
                Z = value;
            }else if(pos.index == 3){
                Reset = value;
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

        SignalNetworkMod signalMod;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            values = new byte[SCREEN_SIZE_X*SCREEN_SIZE_Y];
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
            signalMod.RegisterSignalTickListener(OnSignalNetworkTick);
            if (api.Side == EnumAppSide.Client)
            {
                renderer = new BEScreenRenderer(api as ICoreClientAPI, this.Pos,SCREEN_SIZE_X, SCREEN_SIZE_Y);
                (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "signalsceen");
                renderer.UpdateMesh(values);
            }
        }
        public void OnSignalNetworkTick()
        {
            bool hasChanged = false;
            if(Reset > 0){
                for (int x = 0; x < values.Length; x++){
                    if(values[x]!=0){hasChanged=true;}
                    values[x] = 0;
                }
            }
            if(values[X+SCREEN_SIZE_X*Y] != Z){hasChanged=true;}
            values[X+SCREEN_SIZE_X*Y] = Z;
            if(hasChanged){
                MarkDirty();
                }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            values = tree.GetBytes("values", new byte[SCREEN_SIZE_X*SCREEN_SIZE_Y]);
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