

using signals.src.transmission;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace signals.src.signalNetwork
{
    class BEScreen : BlockEntity, IBESignalReceptor
    {
        BEScreenRenderer renderer;

        public void OnValueChanged(NodePos pos, byte value)
        {
        }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Client)
            {
                // renderer = new BEScreenRenderer(api as ICoreClientAPI);
                (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "signalsceen");
            }
        }
    }
}