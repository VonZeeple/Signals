using System;
using signals.src.transmission;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BEDelay : BlockEntity, IBESignalReceptor
    {
        private byte[] values = new byte[6]{0,0,0,0,0,0};
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
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
            signalMod.RegisterSignalTickListener(OnSignalNetworkTick);


            if (api.Side == EnumAppSide.Client){
                BlockFacing facing = BlockFacing.DOWN;
                BlockFacing orientation = BlockFacing.NORTH;
                BlockBehaviorCoverWithDirection bb = block.GetBehavior<BlockBehaviorCoverWithDirection>();
                if (bb!=null) {
                    string facing_str = block.Variant[bb.sideCode];
                    string orientation_str = block.Variant[bb.orientationCode];
                    if( facing_str!=null && orientation_str!=null){
                        facing = BlockFacing.FromCode(facing_str);
                        orientation = BlockFacing.FromCode(orientation_str);
                    }
                }

                renderer = new BEDelayRenderer(api as ICoreClientAPI, Pos, facing, orientation);
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
            string val_str = block.Variant["value"];
            if (val_str != null){
                state = Int32.Parse(val_str);
            }else{
                state = 0;
            }
            for (int i=0; i < values.Length-1; i++){
                if(values.Length-i-1 <=state){
                    values[values.Length-i-1] = values[values.Length-i-2];
                }else{
                    values[values.Length-i-1] = 0;
                }
            }
            BEBehaviorSignalConnector beb = GetBehavior<BEBehaviorSignalConnector>();
            if (beb == null) return;
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