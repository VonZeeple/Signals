using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{
    public interface INodeProvider
    {

        NodePos GetNodePos(IWorldAccessor world, BlockSelection blockSel);

        Vec3f GetNodePosInBlock(IWorldAccessor world, NodePos pos);

        bool CanAttachWire(IWorldAccessor world, BlockSelection blockSel);

        bool CanAttachWire(IWorldAccessor world, NodePos pos);

        bool CanAttachWire(IWorldAccessor world, NodePos posInit, NodePos pos);

    }
}
