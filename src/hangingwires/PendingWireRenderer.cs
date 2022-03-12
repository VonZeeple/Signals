
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace signals.src.hangingwires
{
    public class PendingWireRenderer : IRenderer
    {
        public double RenderOrder => 0.5;
        public int RenderRange => 100;
        HangingWiresMod mod;
        ICoreClientAPI capi;

        MeshRef wireMesh;
        private BlockPos blockPos;
        private Vec3f posOffset;

        public PendingWireRenderer(ICoreClientAPI capi, HangingWiresMod mod, BlockPos pos, Vec3f offset)
        {
            this.mod = mod;
            this.capi = capi;
            this.blockPos = pos;
            this.posOffset = offset;
            capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "pendingwire");
        }

        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        }

        public void CreateMesh(Vec3f pos1, Vec3f pos2)
        {
            MeshData mesh = WireMesh.MakeWireMesh(pos1, pos2);
            mesh.SetMode(EnumDrawMode.Triangles);
            wireMesh = capi.Render.UploadMesh(mesh);
        }
        public Matrixf ModelMat = new Matrixf();
        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (stage != EnumRenderStage.Opaque) return;

            IRenderAPI rpi = capi.Render;
            IClientWorldAccessor worldAccess = capi.World;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;

            AssetLocation wireTexName = new AssetLocation("block/metal/plate/lead.png");
            int texid = capi.Render.GetOrLoadTexture(wireTexName);
            rpi.BindTexture2d(texid);
            IStandardShaderProgram prog = rpi.PreparedStandardShader(0, 0, 0);
            prog.Use();
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ModelMatrix = ModelMat.Values;

            Vec3d offset = blockPos.ToVec3d();
            CreateMesh(this.posOffset, camPos.SubCopy(offset).ToVec3f());//creates a new mesh each frame, not ideal
            ModelMat = ModelMat.Identity().Translate(offset.X - camPos.X, offset.Y - camPos.Y, offset.Z - camPos.Z);
            prog.ModelMatrix = ModelMat.Values;
            rpi.RenderMesh(wireMesh);
            wireMesh.Dispose();//to be removed when mesh is just updated
            prog.Stop();

        }
    }
}