


using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{
    public class BEScreenRenderer : IRenderer
    {
        public MeshRef screenMeshRef;
        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        private ICoreClientAPI api;

        public BEScreenRenderer(ICoreClientAPI capi, BlockPos pos, MeshData mesh)
        {
            this.api = capi;
            screenMeshRef = capi.Render.UploadMesh(mesh);
        }

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            screenMeshRef.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            throw new System.NotImplementedException();
        }
    }
}