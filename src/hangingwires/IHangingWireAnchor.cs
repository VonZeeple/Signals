using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.hangingwires
{
    public interface IHangingWireAnchor
    {
        Vec3f GetAnchorPosInBlock(IWorldAccessor world, NodePos pos);

        NodePos GetNodePosForWire(IWorldAccessor world, BlockSelection blockSel, NodePos posInit = null);
        bool CanAttachWire(IWorldAccessor world, NodePos pos, NodePos posInit = null);

        NodePos[] GetWireAnchors(IWorldAccessor world, BlockPos pos);
    }
}
