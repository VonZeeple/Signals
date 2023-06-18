using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    class BEBuzzer : BlockEntity, IBESignalReceptor
    {
        private int pitch = 0;
        private byte signalValue;
        AssetLocation asset = new AssetLocation("Game:sounds/voice/clarinet");
        ILoadedSound buzzSound;
        
        void IBESignalReceptor.OnValueChanged(NodePos pos, byte value)
        {
            signalValue = value;
            MarkDirty();
        }

        private float GetPitch(){return (float)Math.Pow(2, pitch*1.0/12);}

        public int GetPitchInfo(){return pitch;}

        internal void UpdateSound(){
            if(Api?.Side != EnumAppSide.Client){return;}
            if (buzzSound == null)
            {
                buzzSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
                {
                    Location = asset,
                    Pitch = GetPitch(),
                    ShouldLoop = false,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 1f,
                    Range = 16,
                });
            }
            buzzSound.SetVolume((float)signalValue/15f);
            if(!buzzSound.IsPlaying & signalValue != (byte)0){
                buzzSound.Start();
                buzzSound.FadeIn(0.0f, null);
                }
            if(buzzSound.IsPlaying & signalValue == (byte)0){
                buzzSound.FadeOutAndStop(0.01f);}
        }

        internal void OnInteract(IPlayer byPlayer)
        {
            pitch++;
            if (pitch > 12){pitch = -12;}
            if (buzzSound != null)
            {
                if(buzzSound.IsPlaying){
                    buzzSound.Stop();}
                    buzzSound.Dispose();
            }
            buzzSound = null;
            UpdateSound();
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            pitch = tree.GetInt("pitch", 0);
            signalValue = (byte)tree.GetInt("value", 0);
            UpdateSound();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("pitch", pitch);
            tree.SetInt("value", (int)signalValue);
        }

        ~BEBuzzer()
        {
            if (buzzSound != null)
            {
                buzzSound.Dispose();
            }
        }


        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (buzzSound != null) buzzSound.Stop();
        }
    }
}