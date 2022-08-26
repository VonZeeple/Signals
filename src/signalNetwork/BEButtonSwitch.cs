using Vintagestory.API.Common;

namespace signals.src.signalNetwork
{
    class BEButtonSwitch : BESwitch
    {
        SignalNetworkMod signalMod;

        public override void Initialize(ICoreAPI api)
        {
            Block block = this.Block as Block;
            state = false;
            base.Initialize(api);
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
        }

        public bool ReleaseInteract()
        {
            BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
            sw?.commute(false);
            state = false;
            return true;
        }

        internal override bool OnInteract(IPlayer byPlayer)
        {
            BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
            sw?.commute(true);
            state = true;
            return true;
        }
    }
}