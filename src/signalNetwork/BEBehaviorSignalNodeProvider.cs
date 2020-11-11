using signals.src.hangingwires;
using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BEBehaviorSignalNodeProvider : BlockEntityBehavior, ISignalNodeProvider
    {

        HangingWiresMod wireMod;
        SignalNetworkMod signalMod;
        List<ISignalNode> nodes;

        public BEBehaviorSignalNodeProvider(BlockEntity blockentity) : base(blockentity)
        {
            nodes = new List<ISignalNode>();
        }

        
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            wireMod = api.ModLoader.GetModSystem<HangingWiresMod>();
            signalMod = api.ModLoader.GetModSystem<SignalNetworkMod>();

            //TODO, test if nodes have already been initialized via TreeAttributes
            JsonObject[] nodesTree = properties["signalNodes"]?.AsArray() ;
            if(nodesTree != null)
            {
                //TODO check duplicated index
                foreach(JsonObject json in nodesTree)
                {
                    int index = json["index"].AsInt();
                    bool isSource = json.KeyExists("isSource") ? json["isSource"].AsBool(): false;
                    BaseNode newNode = new BaseNode();
                    newNode.output = isSource ? (byte)15 : (byte)0;
                    newNode.Pos = new NodePos(this.Blockentity.Pos, index);
                    nodes.Add(newNode);
                }
            }
            signalMod.OnDeviceInitialized(this);

        }


        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            signalMod.OnDeviceRemoved(this);

        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            signalMod.OnDeviceUnloaded(this);
        }


        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAtributes(tree, worldAccessForResolve);

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            //dsc.AppendLine(String.Format("Node {0}, network:{1}", device.Pos, device.NetworkId));
        }

        public ISignalNode GetNodeAt(NodePos pos)
        {
            foreach(ISignalNode node in nodes)
            {
                if (node.Pos == pos) return node;
            }
            return null;
        }

        public Vec3f GetNodePosinBlock(NodePos pos)
        {
            return new Vec3f(0.5f, 0.5f, 0.5f);
        }

        public Dictionary<NodePos, ISignalNode> GetNodes()
        {
            Dictionary<NodePos, ISignalNode> output = new Dictionary<NodePos, ISignalNode>();
            foreach(ISignalNode node in nodes)
            {
                output.Add(node.Pos, node);
            }
            return output;
        }

        public void OnNodeUpdate(NodePos pos)
        {
            ISignalNode node = this.GetNodeAt(pos);
            if (node == null) return;
            IBESignalReceptor receptor = this.Blockentity as IBESignalReceptor;
            receptor?.OnValueChanged(pos, node.value);

            return;
        }
    }
}
