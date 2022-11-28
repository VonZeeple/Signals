using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    public class BEAnemometerRenderer : IRenderer
    {
        MeshRef rotorMeshRef;
        private ICoreClientAPI api;
        private BlockPos pos;
        private Block block;
        public Matrixf ModelMat = new Matrixf();

        public BEAnemometerRenderer(ICoreClientAPI capi, Block block, BlockPos pos)
        {
            api = capi;
            this.pos = pos;
            this.block = block;
            CreateMesh();
        }

        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        private float angle_deg = 0;

        private float targetSpeed = 0;

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            rotorMeshRef?.Dispose();
        }

        private void CreateMesh(){
            rotorMeshRef?.Dispose();
            Shape shape = Shape.TryGet(api, "signals:shapes/block/winddetector_rotor.json");
            if(shape == null) return;
            MeshData mesh;
            api.Tesselator.TesselateShape(block, shape, out mesh);
            rotorMeshRef = api.Render.UploadMesh(mesh);
        }

        public void UpdateWindSpeed(float speed){
            targetSpeed = 360*speed;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (rotorMeshRef == null) CreateMesh();
            if (rotorMeshRef == null) return;
            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;
            rpi.GlDisableCullFace();

            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            //this line make render disapear
            rpi.BindTexture2d(api.ItemTextureAtlas.AtlasTextureIds[0]);
            angle_deg += targetSpeed*deltaTime;
            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                .Translate(0.5f,0f,0.5f)
                .RotateYDeg(angle_deg)
                .Translate(-0.5f,0f,-0.5f)
                .Values
            ;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(rotorMeshRef);
            prog.Stop();
        }
    }
}