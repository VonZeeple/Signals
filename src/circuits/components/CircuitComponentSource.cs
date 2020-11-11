using signals.src.signalNetwork;
using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.circuits.components
{
    class CircuitComponentSource : CircuitComponent
    {

        public CircuitComponentSource() : base()
        {
            this.className = "source";
            this.Size = new Vec3i(3, 3, 3);
            inputs = new Vec3i[] { new Vec3i(0, 0, 1), new Vec3i(1, 0, 0), new Vec3i(2, 0, 1), new Vec3i(1, 0, 2) };
            //Vec3i[] input_dir = new Vec3i[] { new Vec3i(-1,0,0), new Vec3i(0,0,-1), new Vec3i(1,0,0), new Vec3i(0,0,1) };

            //outputNodes = new SignalNodeBase[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                //outputNodes[i] = new SignalNodeBase()
                //{
                //    canStartsNetworkDiscovery = true
                //};

            }
        }
        

        override public void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);


        }

        public override void Remove()
        {
            //foreach (SignalNodeBase node in outputNodes)
           // {
            //    node.OnRemove();
           // }
        }

        override public void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

        }

        public override ISignalNode GetNodeAt(NodePos pos)
        {
            return base.GetNodeAt(pos);
        }


        override public MeshData getMesh(ICoreClientAPI inCapi)
        {
            this.capi = inCapi;
            nowTesselatingItem = capi.World.GetItem(new AssetLocation("signals:el_source"));
            nowTesselatingShape = capi.TesselatorManager.GetCachedShape(nowTesselatingItem.Shape.Base);
            MeshData mesh;
            capi.Tesselator.TesselateItem(nowTesselatingItem, out mesh, this);
            mesh.Translate(new Vec3f(Pos.X / 16f, Pos.Y / 16f, Pos.Z / 16f));

            return mesh;
        }

        public override Vec3f GetNodePosinBlock(NodePos pos)
        {
            return null;
        }

        override public Vec3i[] GetOutputPositions()
        {

            return this.inputs.Select(x => x.AddCopy(Pos.X, Pos.Y, Pos.Z)).ToArray();
        }

        override public byte[] GetOutputs()
        {
            return new byte[] { 15, 15, 15, 15};
        }

        public override void SetInputs(byte[] inputs)
        {
            return;
        }

        public override Vec3i[] GetInputPositions()
        {
           return new Vec3i[] { };
        }

        public override bool IsSource()
        {
            return true;
        }
    }
}
