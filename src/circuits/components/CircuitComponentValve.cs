using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace signals.src.circuits
{
    public class CircuitComponentValve : CircuitComponent
    {

        bool[] inputs;

        public CircuitComponentValve() : base()
        {
            this.className = "valve";
            this.Size = new Vec3i(3, 6, 3);
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

        override public void SetInputs(bool[] inputs)
        {
            this.inputs = inputs;
        }

        override public bool[] GetOutputs()
        {
            if (inputs == null) return new bool[] { false};
            if (inputs[1])
            {
                return new bool[] { false };
            }
            else
            {
                return new bool[] { inputs[0] };
            }
            
        }
    }
}
