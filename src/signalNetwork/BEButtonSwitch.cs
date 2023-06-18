using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    class BEButtonSwitch : BESwitch
    {
        SignalNetworkMod signalMod;
        public bool waitForReset = false;
        public bool waitToTurnOn = false;

        public override void Initialize(ICoreAPI api)
        {
            Block block = this.Block as Block;
            state = false;
            base.Initialize(api);
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
            signalMod.RegisterSignalTickListener(OnSignalNetworkTick);
        }

        private void OnSignalNetworkTick(){
            BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
            if(waitToTurnOn){
                waitToTurnOn = false;
                return;
            }
            if(waitForReset){
                waitForReset = false;
                sw?.commute(false);
                state = false;
            }
        }

        internal override bool ReleaseInteract()
        {
            waitForReset = true;
            return true;
        }

        internal override bool OnInteract()
        {
            BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
            sw?.commute(true);
            state = true;
            waitForReset = false;
            waitToTurnOn = true;
            return true;
        }

      public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            signalMod.DisposeSignalTickListener(OnSignalNetworkTick);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            signalMod.DisposeSignalTickListener(OnSignalNetworkTick);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            waitForReset = tree.GetBool("waitForReset", false);
            waitToTurnOn = tree.GetBool("waitToTurnOn", false);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("waitForReset", waitForReset);
            tree.SetBool("waitToTurnOn", waitToTurnOn);
        }
    }
}