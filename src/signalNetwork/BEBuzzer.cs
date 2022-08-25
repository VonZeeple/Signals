using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    class BEBuzzer : BlockEntity, IBESignalReceptor
    {
        private int pitch = 0;
        private bool playing;
        AssetLocation asset = new AssetLocation("Game:sounds/voice/clarinet");
        ILoadedSound buzzSound;
        
        void IBESignalReceptor.OnValueChanged(NodePos pos, byte value)
        {
            playing = (value != 0);
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

            if(!buzzSound.IsPlaying & playing){
                buzzSound.Start();
                buzzSound.FadeIn(0.01f, null);}
            if(buzzSound.IsPlaying & !playing){
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
            playing = tree.GetBool("playing", false);
            UpdateSound();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("pitch", pitch);
            tree.SetBool("playing", playing);
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