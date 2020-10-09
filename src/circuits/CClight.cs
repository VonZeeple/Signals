using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.circuits
{
    class CClight : CircuitComponent
    {

        public CClight(Vec3i position) : base(new Vec3i(1, 1, 1), position)
        {
        }


    }


}
