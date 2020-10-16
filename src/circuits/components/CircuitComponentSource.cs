using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.circuits.components
{
    class CircuitComponentSource : CircuitComponent
    {
        public CircuitComponentSource() : base()
        {
            this.className = "source";
            this.Size = new Vec3i(3, 3, 3);
        }

        Vec3i[] inputs = new Vec3i[] { new Vec3i(-1, 0, 1), new Vec3i(1, 0, -1), new Vec3i(3, 0, 1), new Vec3i(1, 0, 3) };
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
    }
}
