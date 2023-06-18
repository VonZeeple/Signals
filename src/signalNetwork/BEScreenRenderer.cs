using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace signals.src.transmission
{
    public class BEScreenRenderer : IRenderer
    {
        public MeshRef screenMeshRef;
        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        private ICoreClientAPI api;
        private BlockPos pos;
        TextureAtlasPosition texpos;
        Random random;
        int screenSizeX = 16;
        int screenSizeY = 16;
        protected BlockFacing orientation;

        public BEScreenRenderer(ICoreClientAPI capi, BlockPos pos, int screenSizeX, int screenSizeY, BlockFacing orientation=null)
        {
            this.screenSizeX = screenSizeX;
            this.screenSizeY = screenSizeY;
            this.api = capi;
            this.pos = pos;
            this.orientation = orientation;
            random = new Random();
            api.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation("game:textures/block/machine/statictranslocator/rustyglow"), out _, out texpos);
            screenMeshRef = capi.Render.UploadMesh(GetSquareMesh());
        }

        private MeshData GetSquareMesh(int brightness = 0){
            MeshData mesh = QuadMeshUtil.GetCustomQuad(0f,0f,0f,1f/16,1f/16, 255,255,255,255);
            for (int i = 0; i < mesh.Uv.Length; i+=2)
            {
                mesh.Uv[i + 0] = texpos.x1 + mesh.Uv[i + 0] * 1f / api.BlockTextureAtlas.Size.Width;
                mesh.Uv[i + 1] = texpos.y1 + mesh.Uv[i + 1] * 1f / api.BlockTextureAtlas.Size.Height;
            }
            mesh.Flags = new int[] { brightness, brightness, brightness, brightness };
            return mesh;
        }

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            screenMeshRef?.Dispose();
        }

        public void UpdateMesh(byte[] values){
            MeshData mesh = null;
            screenMeshRef?.Dispose();
            for (int x = 0; x < values.Length; x++){
                if(values[x] > 0){
                    if(mesh == null){
                        mesh = GetSquareMesh(values[x]*17).Scale(new Vec3f(0,0,0), 12f/16, 12f/16, 1f).Translate(1f/16*12f/16*(x%screenSizeX),1f/16*12f/16*(x/screenSizeX),0);
                    }else{
                        mesh.AddMeshData(GetSquareMesh(values[x]*17).Scale(new Vec3f(0,0,0), 12f/16, 12f/16, 1f).Translate(1f/16*12f/16*(x%screenSizeX),1f/16*12f/16*(x/screenSizeX),0));
                    }
                }
            }
            if(mesh != null){
                mesh.Translate(new Vec3f(2f/16, 2f/16, 15.001f/16));
                mesh.Rotate(new Vec3f(0.5f,0,0.5f),0, (orientation.HorizontalAngleIndex-1) * 90 * GameMath.PI / 180, 0);
                screenMeshRef = api.Render.UploadMesh(mesh);
            }
        }

        Matrixf ModelMat = new Matrixf();

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (screenMeshRef == null) return;
            if (screenMeshRef.Disposed) return;
            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);;
            if(texpos != null) rpi.BindTexture2d(texpos.atlasTextureId);

            prog.RgbaGlowIn = new Vec4f(1f, 1f, 1f, 1);
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            prog.ModelMatrix = ModelMat
            .Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
            //.Scale(12f/16, 12f/16, 1f)
            .Values;
            rpi.RenderMesh(screenMeshRef);

            prog.Stop();
        }
    }
}