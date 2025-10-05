using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    class BEPressurePlate : BESwitch
    {
        SignalNetworkMod signalMod;
        public bool waitForReset = false;
        public bool waitToTurnOn = false;

        protected Dictionary<Entity, long> collidingEntities = new Dictionary<Entity, long>();
        public override void Initialize(ICoreAPI api)
        {
            BlockPressurePlate block = this.Block as BlockPressurePlate;
            state = false;
            base.Initialize(api);
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();
            signalMod.RegisterSignalTickListener(OnSignalNetworkTick);
        }

        internal bool OnEntityCollide(Entity entity)
        {
            if (Api.Side == EnumAppSide.Client) return false;
            waitToTurnOn = true;
            waitForReset = false;
            return true;
        }

        private void OnSignalNetworkTick()
        {
            BEBehaviorSignalSwitch sw = GetBehavior<BEBehaviorSignalSwitch>();
            if (waitToTurnOn)
            {
                waitToTurnOn = false;
                sw?.commute(true);
                state = true;
                return;
            }
            else
            {
                if (!waitForReset)
                {
                    waitForReset = true;
                    return;
                }
            }
            if (waitForReset)
            {
                waitForReset = false;
                sw?.commute(false);
                state = false;
            }
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