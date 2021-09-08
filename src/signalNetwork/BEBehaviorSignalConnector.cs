using signals.src.hangingwires;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    public class BEBehaviorSignalConnector : BEBehaviorSignalNodeProvider
    {
        public BEBehaviorSignalConnector(BlockEntity blockentity) : base(blockentity)
        {

        }

        public override Vec3f GetNodePosinBlock(NodePos pos)
        {
            IHangingWireAnchor anchor = base.Blockentity.Block as IHangingWireAnchor;
            //TODO: test for null
            Vec3f vec = anchor.GetAnchorPosInBlock(pos);
            return vec;
        }
    }
}