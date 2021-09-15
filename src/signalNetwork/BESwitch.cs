using Vintagestory.API.Common;

namespace signals.src.signalNetwork
{
    class BESwitch : BlockEntity
    {
        public bool state = false;

        public override void Initialize(ICoreAPI api)
        {
            BlockSwitch block = this.Block as BlockSwitch;
            state = block.LastCodePart() == "on";
            base.Initialize(api);
        }

        internal bool OnInteract(IPlayer byPlayer)
        {
            state = !state;
            BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
            sw?.commute(state);
            return true;
        }
    }
}