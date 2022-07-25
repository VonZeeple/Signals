using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src.signalNetwork
{
    class BEValve : BlockEntity, IBESignalReceptor
    {
        public byte state;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            BEBehaviorSignalValve valve = GetBehavior<BEBehaviorSignalValve>();
            valve?.commute(state);
        }

        public void OnServerGameTick(float dt)
        {
            //BEBehaviorSignalValve valve = GetBehavior<BEBehaviorSignalValve>();
            //valve?.commute(state);
        }

        public void OnValueChanged(NodePos pos, byte value)
        {
            if(pos.index != 0) return;
            state = value;
            BEBehaviorSignalValve valve = GetBehavior<BEBehaviorSignalValve>();
            valve?.commute(state);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            state = tree.GetBytes("state", new byte[1]{0})[0];
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBytes("state", new byte[1]{state});
        }
    }
}
