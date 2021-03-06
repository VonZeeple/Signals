﻿using signals.src.hangingwires;
using signals.src.signalNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{

    public class WireAnchor
    {
        public int index;
        public float x1;
        public float x2;
        public float y1;
        public float y2;
        public float z1;
        public float z2;
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

            //NodeBox[] indices = this.Attributes?["signalNodes"]?.AsArray<NodeBox>();
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
                boxes.Add(new Cuboidf(nb.x1, nb.y1, nb.z1, nb.x2, nb.y2, nb.z2));
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
                    if (pos != null) mod.SetPendingNode(GetNodePosForWire(world, blockSel));
                    return true;
                }
            }



            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }


        #region Wire anchor
        public Vec3f GetAnchorPosInBlock(IWorldAccessor world, NodePos pos)
        {
            foreach (WireAnchor box in wireAnchors)
            {
                if (box.index == pos.index) return new Vec3f((box.x1 + box.x2) / 2, (box.y1 + box.y2) / 2, (box.z1 + box.z2) / 2);
            }
            return new Vec3f(0f, 0f, 0f);
        }

        public NodePos GetNodePosForWire(IWorldAccessor world, BlockSelection blockSel, NodePos posInit = null)
        {
            foreach (WireAnchor box in wireAnchors)
            {
                if (box.index == blockSel.SelectionBoxIndex) return new NodePos(blockSel.Position, blockSel.SelectionBoxIndex);
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
                nodes[i] = new NodePos(pos, wireAnchors[i].index);
            }
            return nodes;
        }
        #endregion
    }
}
