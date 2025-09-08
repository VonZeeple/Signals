using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace signals.src.signalNetwork
{
    class BlockEntityLightBulb : BlockEntity, IBESignalReceptor
    {

        bool currentState;
        Block onBlock;
        Block offBlock;

        Random random;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            AssetLocation onloc = Block.CodeWithVariant("powered", "on");
            AssetLocation offloc = Block.CodeWithVariant("powered", "off");
            currentState = Block.Code == onloc;
            onBlock = Api.World.GetBlock(onloc);
            offBlock = Api.World.GetBlock(offloc);
            if(api.Side == EnumAppSide.Client){
                random = new Random();
                RegisterGameTickListener(OnGameTick, 10000);
                }
        }

        private void OnGameTick(float dt)
        {
            if (currentState == false) return;
            if (random.NextDouble() > 0.2f) return;
            Api.World.PlaySoundAt(new AssetLocation("Game:sounds/creature/locust/idle"), Pos.X, Pos.Y, Pos.Z, randomizePitch:true, volume:1f, range:32);
        }

        public void OnValueChanged(NodePos pos, byte value)
        {
            if (value > 0 && currentState == false) SwitchOn();
            if (value == 0 && currentState == true) SwitchOff();
        }

        private void SwitchOn()
        {
            currentState = true;
            if (onBlock == null) return;
            Api.World.BlockAccessor.ExchangeBlock(onBlock.BlockId, Pos);
            Api.World.PlaySoundAt(new AssetLocation("signals:sounds/buzz_short"), Pos.X, Pos.Y, Pos.Z, randomizePitch:false, volume:1f);
        }

        private void SwitchOff()
        {
            currentState = false;
            if (offBlock == null) return;
            Api.World.BlockAccessor.ExchangeBlock(offBlock.BlockId, Pos);
        }
    }
}
