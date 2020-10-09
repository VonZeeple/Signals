using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace signals.src
{
    class CircuitBoardRenderer : IRenderer
    {
        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        protected ICoreClientAPI capi;
        protected BlockPos pos;
        protected MeshData circuitMesh;
        protected MeshRef circuitMeshRef;

        Matrixf ModelMat = new Matrixf();

        public CircuitBoardRenderer(BlockPos pos,ICoreClientAPI capi)
        {
            this.pos = pos;
            this.capi = capi;
        }
        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        }

        public MeshData getMeshForItem()
        {
            return circuitMesh;
        }

        private MeshData getsimpleVoxelMesh(ICoreClientAPI api, TextureAtlasPosition tpos)
        {
            //We first generate a mesh for a single voxel
            MeshData singleVoxelMesh = CubeMeshUtil.GetCubeOnlyScaleXyz(1 / 32f, 1 / 32f, new Vec3f(1 / 32f, 1 / 32f, 1 / 32f));
            singleVoxelMesh.Rgba = new byte[6 * 4 * 4].Fill((byte)255);
            CubeMeshUtil.SetXyzFacesAndPacketNormals(singleVoxelMesh);
            float subPixelPaddingx = api.BlockTextureAtlas.SubPixelPaddingX;
            float subPixelPaddingy = api.BlockTextureAtlas.SubPixelPaddingY;

            for (int i = 0; i < singleVoxelMesh.Uv.Length; i++)
            {
                if (i % 2 > 0)
                {
                    singleVoxelMesh.Uv[i] = tpos.y1 + singleVoxelMesh.Uv[i] * 2f / api.BlockTextureAtlas.Size.Height - subPixelPaddingy;
                }
                else
                {
                    singleVoxelMesh.Uv[i] = tpos.x1 + singleVoxelMesh.Uv[i] * 2f / api.BlockTextureAtlas.Size.Width - subPixelPaddingx;
                }
            }

            singleVoxelMesh.XyzFaces = (byte[])CubeMeshUtil.CubeFaceIndices.Clone();
            singleVoxelMesh.XyzFacesCount = 6;
            singleVoxelMesh.ClimateColorMapIds = new byte[6].Fill((byte)0);
            singleVoxelMesh.SeasonColorMapIds = new byte[6].Fill((byte)0);
            singleVoxelMesh.ColorMapIdsCount = 6;
            singleVoxelMesh.RenderPasses = new short[singleVoxelMesh.VerticesCount / 4].Fill((short)0);
            singleVoxelMesh.RenderPassCount = singleVoxelMesh.VerticesCount / 4;

            return singleVoxelMesh;
        }

        private byte[] getColor(int i)
        {
            byte[][] colors = { new byte[]{ 255, 255, 255 }, new byte[] { 255, 0, 0 }, new byte[] { 0, 255, 0 },
                new byte[]{ 0, 0, 255 }, new byte[]{ 255, 255, 0 }, new byte[]{ 255, 0, 255 }, new byte[]{ 0, 255, 255 }, new byte[]{ 0, 0, 0 } };

            return colors[i % colors.Length];

        }

        int texId;

        public void RegenCircuitMesh(VoxelCircuit circuit)
        {
            circuitMesh = new MeshData(24, 36, false);

            Block wireblock = capi.World.GetBlock(new AssetLocation("signals:blockwire"));
            TextureAtlasPosition tpos = capi.BlockTextureAtlas.GetPosition(wireblock, wireblock.Textures.First().Key);
            texId = tpos.atlasTextureId;

            MeshData singleVoxelMesh = getsimpleVoxelMesh(capi, tpos);

            //We now place a mesh at each position

            MeshData voxelMeshOffset = singleVoxelMesh.Clone();
            foreach (Network net in circuit.wiring.networks.Values)
            {
                byte[] color = getColor(net.id).Append(new byte[] { 255 });

                foreach (Vec3i vec in net.getVoxelPos())
                {


                    float px = vec.X / 16f;
                    float py = vec.Y / 16f;
                    float pz = vec.Z / 16f;

                    for (int i = 0; i < singleVoxelMesh.xyz.Length; i += 3)
                    {
                        voxelMeshOffset.xyz[i] = px + singleVoxelMesh.xyz[i];
                        voxelMeshOffset.xyz[i + 1] = py + singleVoxelMesh.xyz[i + 1];
                        voxelMeshOffset.xyz[i + 2] = pz + singleVoxelMesh.xyz[i + 2];

                        voxelMeshOffset.Rgba[i / 3 * 4] = color[0];
                        voxelMeshOffset.Rgba[i / 3 * 4 + 1] = color[1];
                        voxelMeshOffset.Rgba[i / 3 * 4 + 2] = color[2];
                        voxelMeshOffset.Rgba[i / 3 * 4 + 3] = color[3];
                    }

                    float offsetX = ((((vec.X + 4 * vec.Y) % 16f / 16f)) * 32f) / capi.BlockTextureAtlas.Size.Width;
                    float offsetY = (pz * 32f) / capi.BlockTextureAtlas.Size.Height;

                    for (int i = 0; i < singleVoxelMesh.Uv.Length; i += 2)
                    {
                        voxelMeshOffset.Uv[i] = singleVoxelMesh.Uv[i] + offsetX;
                        voxelMeshOffset.Uv[i + 1] = singleVoxelMesh.Uv[i + 1] + offsetY;
                    }

                    circuitMesh.AddMeshData(voxelMeshOffset);

                }
            }

            foreach (CircuitComponent comp in circuit.components)
            {
                circuitMesh.AddMeshData(comp.getMesh(capi));
            }

            circuitMeshRef = capi.Render.UploadMesh(circuitMesh);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (circuitMeshRef == null) return;


            IRenderAPI rpi = capi.Render;
            IClientWorldAccessor worldAccess = capi.World;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            IStandardShaderProgram prog = capi.Render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            rpi.BindTexture2d(texId);
            prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z).Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMesh(circuitMeshRef);
            prog.ExtraGlow = 1;

            prog.Stop();

        }
    }
}
