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

        Vec3i anodePos;
        Vec3i cathodePos;
        Vec3i gatePos;

        public CircuitComponentValve() : base()
        {
            this.className = "valve";
            this.Size = new Vec3i(3, 6, 3);
        }
    }
}
