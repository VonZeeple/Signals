using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace signals.src.circuits.components
{
    class CircuitComponentResistor : CircuitComponent
    {

        public CircuitComponentResistor() : base()
        {
            this.className = "resistor";
            this.Size = new Vec3i(3, 1, 1);
        }

        private Vec3i[] leads = new Vec3i[] { new Vec3i(-1,0,0), new Vec3i(3,0,0) };
        public override Vec3i[] GetInputPositions()
        {
            return leads;
        }

        public override Vec3i[] GetOutputPositions()
        {
            return leads;
        }

        public override byte[] GetOutputs()
        {
            return new byte[leads.Length].Fill((byte)0);
        }

        public override bool DoesInstantUpdate()
        {
            return true;
        }

        public override void SetInputs(byte[] inputs)
        {
            return;
        }

        override public MeshData getMesh(ICoreClientAPI inCapi)
        {
            this.capi = inCapi;
            nowTesselatingItem = capi.World.GetItem(new AssetLocation("signals:el_resistor"));
            nowTesselatingShape = capi.TesselatorManager.GetCachedShape(nowTesselatingItem.Shape.Base);
            MeshData mesh;
            capi.Tesselator.TesselateItem(nowTesselatingItem, out mesh, this);
            mesh.Translate(new Vec3f(Pos.X / 16f, Pos.Y / 16f, Pos.Z / 16f));

            return mesh;
        }
    }
}
