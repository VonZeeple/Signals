using Vintagestory.API.Common;

namespace signals.src.signalNetwork
{
    class BESwitch : BlockEntity
    {
        public bool state = false;

        public override void Initialize(ICoreAPI api)
        {
            Block block = this.Block as Block;
            state = block.LastCodePart() == "on";
            base.Initialize(api);
        }

        internal virtual bool OnInteract(IPlayer byPlayer)
        {
            state = !state;
            BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
            sw?.commute(state);
            return true;
        }
    }
}