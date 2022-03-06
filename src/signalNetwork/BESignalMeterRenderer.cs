using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{
    public class BESignalMeterRenderer : IRenderer
    {
        public MeshRef needleMeshRef;
        public Matrixf ModelMat = new Matrixf();
        public float AngleRad = 0;
        private ICoreClientAPI api;
        private BlockPos pos;


        private int texId;

        public BESignalMeterRenderer(ICoreClientAPI capi,BlockPos pos, MeshData mesh)
        {
            this.pos = pos;
            this.api = capi;
            needleMeshRef = capi.Render.UploadMesh(mesh);
            Block block = capi.World.GetBlock(new AssetLocation("signals:blockMeter"));
            TextureAtlasPosition tpos = capi.BlockTextureAtlas.GetPosition(block, "needle");
            texId = tpos.atlasTextureId;
        }

        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            needleMeshRef?.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (needleMeshRef == null) return;
            
            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            //rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            rpi.BindTexture2d(texId);

            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                .Translate(0.5f, 0.5f, 0.5f)
                .RotateZ(AngleRad)
                //.Translate(-0.5f, 0, -0.5f)
                .Values
            ;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(needleMeshRef);
            prog.Stop();
        }
    }
}