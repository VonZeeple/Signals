using signals.src.hangingwires;
using signals.src.signalNetwork;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{

    public class WireAnchor: RotatableCube
    {
        public int Index;

        public WireAnchor(int index, float MinX, float MinY, float MinZ, float MaxX, float MaxY, float MaxZ) : base(MinX, MinY, MinZ, MaxX, MaxY, MaxZ)
        {
            Index = index;
        }
    }

    class BlockConnection : Block, IHangingWireAnchor
    {

        protected WireAnchor[] wireAnchors;

        public BlockConnection(): base()
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            wireAnchors = new WireAnchor[0];
            JsonObject[] jsonObj = Attributes?["signalNodes"]?.AsArray();
            if (jsonObj != null)
            {
                try
                {
                    wireAnchors = new WireAnchor[jsonObj.Length];
                    for(int i=0;i<jsonObj.Length;i++)
                    {
                        wireAnchors[i] = jsonObj[i].AsObject<WireAnchor>();
                    }
                }
                catch (Exception e)
                {
                    api.World.Logger.Error("Failed loading SignalNodes for item/block {0}. Will ignore. Exception: {1}", Code, e);
                    wireAnchors = new WireAnchor[0];
                }
            }


        }


        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
        {
            List<Cuboidf> boxes = new List<Cuboidf>();
            foreach(WireAnchor nb in wireAnchors)
            {
                boxes.Add(nb.RotatedCopy());
            }
            boxes.AddRange(base.GetSelectionBoxes(world, pos));
            return boxes.ToArray();
        }



        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }


        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            base.OnBlockRemoved(world, pos);
            HangingWiresMod mod = api.ModLoader.GetModSystem<HangingWiresMod>();
            mod.RemoveAllNodesAtBlockPos(pos);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

            HangingWiresMod mod = api.ModLoader.GetModSystem<HangingWiresMod>();
            if (mod == null)
            {
                api.Logger.Error("HangingWiresMod mod system not found");
            }
            else
            {
                NodePos pos = GetNodePosForWire(world, blockSel, mod.GetPendingNode());
                if (CanAttachWire(world, pos, mod.GetPendingNode()))
                {
                    if (pos != null) mod.ConnectWire(pos, byPlayer, this);
                    return true;
                }
            }



            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }


        #region Wire anchor
        public Vec3f GetAnchorPosInBlock(NodePos pos)
        {
            foreach (WireAnchor box in wireAnchors)
            {
                Cuboidf cube =  box.RotatedCopy();
                Vec3f position = new Vec3f(cube.MidX, cube.MidY, cube.MidZ);
                if (box.Index == pos.index) return position;
            }
            return new Vec3f(0f, 0f, 0f);
        }

        public NodePos GetNodePosForWire(IWorldAccessor world, BlockSelection blockSel, NodePos posInit = null)
        {
            foreach (WireAnchor box in wireAnchors)
            {
                if (box.Index == blockSel.SelectionBoxIndex) return new NodePos(blockSel.Position, blockSel.SelectionBoxIndex);
            }
            return null;
        }

        public bool CanAttachWire(IWorldAccessor world, NodePos pos, NodePos posInit = null)
        {
            if (pos == null) return false;
            if (posInit != null && posInit.blockPos == pos.blockPos) return false;
            return pos != null;
        }

        public NodePos[] GetWireAnchors(IWorldAccessor world, BlockPos pos)
        {
            NodePos[] nodes = new NodePos[wireAnchors.Length];
            for(int i=0;i<wireAnchors.Length;i++)
            {
                nodes[i] = new NodePos(pos, wireAnchors[i].Index);
            }
            return nodes;
        }
        #endregion
    }
}
