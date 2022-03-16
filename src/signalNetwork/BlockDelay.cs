using System;
using signals.src.transmission;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BlockDelay : BlockConnection
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if(base.OnBlockInteractStart(world, byPlayer, blockSel)) return true;

            BlockPos pos = blockSel.Position;
            world.PlaySoundAt(AssetLocation.Create(Attributes["triggerSound"].AsString("sounds/block/door"), Code.Domain), pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f, byPlayer);
            AssetLocation newCode;
            try {
                int value = Int32.Parse(this.Variant["value"]);
                newCode = CodeWithVariant("value", ((value + 1) % 6).ToString());
                Block newBlock = world.BlockAccessor.GetBlock(newCode);
                world.BlockAccessor.ExchangeBlock(newBlock.BlockId, pos);
                world.BlockAccessor.MarkBlockDirty(pos);
            }
            catch (Exception){
                api.Logger.Debug("Can't parse variant for delay.");
            };
            return true;
        }
    }
}
