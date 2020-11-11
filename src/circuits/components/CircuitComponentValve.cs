using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using signals.src.transmission;

namespace signals.src.circuits
{
    public class CircuitComponentValve : CircuitComponent
    {


        public CircuitComponentValve() : base()
        {
            this.className = "valve";
            this.Size = new Vec3i(3, 6, 3);
            inputs = new Vec3i[] { new Vec3i(0, 0, 1), new Vec3i(2, 0, 1), new Vec3i(1, 5, 1) };

            //outputNodes = new SignalNodeBase[inputs.Length];
            //for (int i = 0; i < inputs.Length; i++)
            //{
            //    outputNodes[i] = new SignalNodeBase()
            //    {
            //        canStartsNetworkDiscovery = true
            //    };

           // }
        }

        public CircuitComponentValve(VoxelCircuit circuit) : this()
        {
            this.myCircuit = circuit;
        }

        public override void Initialize(ICoreAPI api, VoxelCircuit circuit)
        {
            if (circuit == null) return;
            myCircuit = circuit;
            BlockPos blockPos = myCircuit?.myBE?.Pos;

            for (int i = 0; i < inputs.Length; i++)
            {
                ushort? index = myCircuit?.getIndexFromVector(this.Pos.AddCopy(inputs[i].X, inputs[i].Y, inputs[i].Z));
                if (blockPos == null || index == null) continue;
                //outputNodes[i].Pos = new NodePos(blockPos, index.Value);
                //outputNodes[i].myBlockEntity = myCircuit?.myBE;
                //if(i == 0)
                //{
                //    ushort? index2 = myCircuit.getIndexFromVector(this.Pos.AddCopy(inputs[1].X, inputs[1].Y, inputs[1].Z));
                //    outputNodes[i].Initialize(api, new Dictionary<NodePos, byte> { { new NodePos(blockPos, index2.Value), (byte)0 } });
                //}
                //else
                //{
                //    outputNodes[i].Initialize(api);
               // }
                
            }

        }

        override public Vec3i[] GetOutputPositions()
        {
            Vec3i pos1 = new Vec3i(3, 0, 1).AddCopy(Pos.X, Pos.Y, Pos.Z);

            return new Vec3i[] { pos1};
        }

        public override Vec3i[] GetInputPositions()
        {
            Vec3i pos1 = new Vec3i(-1, 0, 1).AddCopy(Pos.X, Pos.Y, Pos.Z);
            Vec3i pos2 = new Vec3i(1, 6, 1).AddCopy(Pos.X, Pos.Y, Pos.Z);
            return new Vec3i[] { pos1, pos2};
        }

        override public void SetInputs(byte[] inputs)
        {
            //this.inputs = inputs;
        }

        override public byte[] GetOutputs()
        {
            //if (inputs == null) return new byte[] {0};
            //if (inputs[1] > 0)
            //{
            //    return new byte[] { 0 };
            //}
            // else
            //{
            //    return new byte[] { inputs[0] };
            //}
            return new byte[]{0};
            
        }

        override public MeshData getMesh(ICoreClientAPI inCapi)
        {
            this.capi = inCapi;
            nowTesselatingItem = capi.World.GetItem(new AssetLocation("signals:el_valve"));
            nowTesselatingShape = capi.TesselatorManager.GetCachedShape(nowTesselatingItem.Shape.Base);
            MeshData mesh;
            capi.Tesselator.TesselateItem(nowTesselatingItem, out mesh, this);
            mesh.Translate(new Vec3f(Pos.X / 16f, Pos.Y / 16f, Pos.Z / 16f));

            return mesh;
        }
    }
}
