using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace signals.src.signalNetwork
{
    class BlockEntityLightBulb : BlockEntity, IBESignalReceptor
    {

        bool currentState;
        Block onBlock;
        Block offBlock;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            AssetLocation onloc = Block.CodeWithPart("on",1);
            AssetLocation offloc = Block.CodeWithPart("off",1);
            currentState = false;
            onBlock = Api.World.GetBlock(onloc);
            offBlock = Api.World.GetBlock(offloc);
            currentState = false;
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
            Api.World.PlaySoundAt(new AssetLocation("signals:sounds/buzz_short"), Pos.X, Pos.Y, Pos.Z);
        }

        private void SwitchOff()
        {
            currentState = false;
            if (offBlock == null) return;
            Api.World.BlockAccessor.ExchangeBlock(offBlock.BlockId, Pos);
        }
    }
}
