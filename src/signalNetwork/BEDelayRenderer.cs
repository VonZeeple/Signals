using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{
    public class BEDelayRenderer : IRenderer
    {
        public MeshRef lightsMeshRef;
        public Matrixf ModelMat = new Matrixf();
        public double RenderOrder => 0.5;

        public int RenderRange => 25;

        private ICoreClientAPI api;
        private BlockPos pos;

        private int texId;

        private Shape shape;

        private Block Block;
        protected BlockFacing facing;
        protected BlockFacing orientation;

        public BEDelayRenderer(ICoreClientAPI capi, BlockPos pos, BlockFacing face=null, BlockFacing orientation=null)
        {
            this.pos = pos;
            this.api = capi;
            this.facing = face;
            this.orientation = orientation;
            Block = capi.World.BlockAccessor.GetBlock(pos);
            byte[] values = new byte[6]{0,0,0,0,0,0};

            if (Block.BlockId != 0){
                IAsset asset = capi.Assets.TryGet(new AssetLocation("signals:shapes/block/delay_lights.json"));
                if(asset != null){
                    shape = asset.ToObject<Shape>();
                }
                TextureAtlasPosition tpos = capi.BlockTextureAtlas.GetPosition(Block, "filament");
                texId = tpos.atlasTextureId;
            }
            UpdateMesh(values);
        }

        public void UpdateMesh(byte[] values){
            if (shape == null) return;
            if (lightsMeshRef != null) lightsMeshRef.Dispose();
            for (int i = 0; i < Math.Min(values.Length, shape.Elements.Length); i++){
                ShapeElement el = shape.Elements[i];
                foreach (ShapeElementFace face in el.FacesResolved){
                    face.Glow = 255*values[i]/15;
                }
            }
            Vec3f rotate = new Vec3f(0, 0, 0);
            if (facing != null && orientation != null)
            {
                rotate = SignalsUtils.FacingToRotation(orientation, facing);
            }
            ITesselatorAPI mesher = api.Tesselator;
            MeshData mesh;
            mesher.TesselateShape(Block, shape, out mesh);
            mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), rotate.X * GameMath.PI / 180, rotate.Y * GameMath.PI / 180, rotate.Z * GameMath.PI / 180);
            lightsMeshRef = api.Render.UploadMesh(mesh);
        }

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            lightsMeshRef?.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage){
            if (lightsMeshRef == null) return;
            
            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();

            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            rpi.BindTexture2d(texId);

            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                .Values
            ;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(lightsMeshRef);
            prog.Stop();
        }
    }
}