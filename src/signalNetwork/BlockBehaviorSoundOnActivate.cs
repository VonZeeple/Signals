using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    class BlockBehaviorSoundOnActivate : BlockBehavior
    {
        string sound = "effect/receptionbell";
        bool randomizePitch = false;

        public BlockBehaviorSoundOnActivate(Block block) : base(block)
        {
        }

        public override void Initialize(JsonObject properties)
        {
            sound = properties["sound"].AsString();
            randomizePitch = properties["randomizePitch"].AsBool(false);
            base.Initialize(properties);
        }

        public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs, ref EnumHandling handled)
        {
            world.PlaySoundAt(new AssetLocation(sound), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, null, randomizePitch);
            base.Activate(world, caller, blockSel, activationArgs, ref handled);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            world.PlaySoundAt(new AssetLocation(sound), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, randomizePitch);
            handling = EnumHandling.Handled;
            return true;
        }
    }
}