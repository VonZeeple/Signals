using signals.src.hangingwires;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;

namespace signals.src.transmission
{
    class BlockSource : BlockConnection, ISignalSource
    {

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            WireAnchor nb = new WireAnchor() { index = 0, x1 = 0, x2 = 1, y1 = 0, y2 = 1, z1 = 0, z2 = 1};
            wireAnchors = new WireAnchor[1] { nb };

        }


    }
}
