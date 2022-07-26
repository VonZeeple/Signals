using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BEBuzzer : BlockEntity, IBESignalReceptor
    {
        private int pitch = 0;
        AssetLocation asset = new AssetLocation("Game:sounds/voice/accordion");
        
        void IBESignalReceptor.OnValueChanged(NodePos pos, byte value)
        {
            if (value != 0){PlaySound();}
        }

        internal void PlaySound(IPlayer byPlayer = null){
            Api.World.PlaySoundAt(asset, Pos.X, Pos.Y, Pos.Z, dualCallByPlayer: byPlayer, pitch: ((float)Math.Pow(2, pitch*1.0/12)));
        }

        internal void OnInteract(IPlayer byPlayer)
        {
            pitch++;
            if (pitch > 12){pitch = -12;}
            PlaySound(byPlayer);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            pitch = tree.GetInt("pitch", 0);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("pitch", pitch);
        }
    }
}