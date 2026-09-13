using System;
using FluffyClouds;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace signals.src.signalNetwork
{
    class BEButtonSwitch : BESwitch
    {
        protected SignalNetworkMod signalMod;
        public bool waitForReset = false;
        public bool waitToTurnOn = false;

        
        BlockEntityAnimationUtil animUtil
        {
            get { return GetBehavior<BEBehaviorAnimatable>().animUtil; }
        }
        
        public override void Initialize(ICoreAPI api)
        {
            Block block = this.Block as Block;
            state = false;
            base.Initialize(api);
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
            signalMod.RegisterSignalTickListener(OnSignalNetworkTick);
        }

        private void OnSignalNetworkTick(){
            BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
            if(waitToTurnOn){
                waitToTurnOn = false;
                return;
            }
            if(waitForReset){
                waitForReset = false;
                sw?.commute(false);
                state = false;
            }
        }

        internal override bool ReleaseInteract()
        {
            waitForReset = true;
            animUtil?.StopAnimation("activate");
            return true;
        }

        internal override bool OnInteract() {
            BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
            animUtil?.StartAnimation(new AnimationMetaData() { Animation = "activate", Code = "activate", EaseInSpeed = 10, EaseOutSpeed = 10, AnimationSpeed = 5f });
            MarkDirty(true);
            sw?.commute(true);
            state = true;
            waitForReset = false;
            waitToTurnOn = true;
            return true;
        }

      public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            signalMod.DisposeSignalTickListener(OnSignalNetworkTick);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            signalMod.DisposeSignalTickListener(OnSignalNetworkTick);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            waitForReset = tree.GetBool("waitForReset", false);
            waitToTurnOn = tree.GetBool("waitToTurnOn", false);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("waitForReset", waitForReset);
            tree.SetBool("waitToTurnOn", waitToTurnOn);
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (animUtil?.animator == null)
            {
                float rotX = this.Block.Shape.rotateX;
                float rotY = this.Block.Shape.rotateY;
                float rotZ = this.Block.Shape.rotateZ;
                animUtil?.InitializeAnimator("bebuttonswitch");
                if (animUtil?.renderer != null)
                {
                    // The default renderer only rotates around Y; match the block's XYZ rotation around its center.
                    animUtil.renderer.CustomTransform = new Matrixf()
                        .Translate(0.5f, 0.5f, 0.5f)
                        .RotateX(rotX * GameMath.DEG2RAD)
                        .RotateY(rotY * GameMath.DEG2RAD)
                        .RotateZ(rotZ * GameMath.DEG2RAD)
                        .Translate(-0.5f, -0.5f, -0.5f)
                        .Values;
                }
            }

            return base.OnTesselation(mesher, tessThreadTesselator);
        }
    }
}