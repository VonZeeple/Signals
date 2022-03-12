using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.hangingwires
{
    public class HangingWiresRenderer : IRenderer
    {
        public double RenderOrder => 0.5;

        public int RenderRange => 100;

        HangingWiresMod mod;
        ICoreClientAPI capi;



        int chunksize;
        public Matrixf ModelMat = new Matrixf();

        Dictionary<Vec3i, MeshRef> MeshRefPerChunk;


        public HangingWiresRenderer(ICoreClientAPI capi, HangingWiresMod mod)
        {
            this.mod = mod;
            this.capi = capi;
            chunksize = capi.World.BlockAccessor.ChunkSize;
            capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "hangingwiresnetwork");

            //capi.Event.ReloadShader += LoadShader;
            //LoadShader();
        }


        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            if (MeshRefPerChunk == null) return;
            foreach(MeshRef meshRef in MeshRefPerChunk.Values)
            {
                meshRef?.Dispose();
            }
        }


        //Catenary: https://en.wikipedia.org/wiki/Catenary
        private float Catenary(float x, float d=1, float a=2)
        {
            return a*((float)Math.Cosh((x-(d/2))/a) - (float)Math.Cosh((d / 2) / a));  
        }

        private int[] debugColors = new int[]
        {
            (255 << 24) | (0 << 16) | (255 << 8) | (0),
            (255 << 24) | (255 << 16) | (0 << 8) | (0),
            (255 << 24) | (0 << 16) | (0 << 8) | (255),
            (255 << 24) | (255 << 16) | (255 << 8) | (0),
            (255 << 24) | (0 << 16) | (255 << 8) | (255),
            (255 << 24) | (255 << 16) | (0 << 8) | (255),
            (255 << 24) | (255 << 16) | (255 << 8) | (255)
        };
        public void UpdateWiresMesh(HangingWiresData data)
        {

            IBlockAccessor accessor = capi?.World?.BlockAccessor;
            IClientWorldAccessor world = capi?.World;
            if (data == null || accessor == null) return;

            Dictionary<Vec3i, MeshData> MeshPerChunk = new Dictionary<Vec3i, MeshData>();

            if (MeshRefPerChunk != null)
            {
                foreach (MeshRef meshRef in MeshRefPerChunk.Values)
                {
                    meshRef?.Dispose();
                }
            }
            MeshRefPerChunk = new Dictionary<Vec3i, MeshRef>();


            foreach (WireConnection con in data.connections)
            {

                    IHangingWireAnchor block1 = accessor.GetBlock(con.pos1.blockPos) as IHangingWireAnchor;
                    IHangingWireAnchor block2 = accessor.GetBlock(con.pos2.blockPos) as IHangingWireAnchor;
                    if (block1 == null || block2 == null) continue;


                    BlockPos blockPos1 = con.pos1.blockPos;
                    Vec3i chunkpos = new Vec3i(blockPos1.X / chunksize, blockPos1.Y / chunksize, blockPos1.Z / chunksize);

                    Vec3f pos1 = con.pos1.blockPos.ToVec3f().AddCopy(-chunkpos.X*chunksize,-chunkpos.Y*chunksize,-chunkpos.Z*chunksize) + block1.GetAnchorPosInBlock(con.pos1);
                    Vec3f pos2 = con.pos2.blockPos.ToVec3f().AddCopy(-chunkpos.X * chunksize, -chunkpos.Y * chunksize, -chunkpos.Z * chunksize) + block2.GetAnchorPosInBlock(con.pos2);

                    if (MeshPerChunk.ContainsKey(chunkpos))
                    {
                        MeshData newMesh = WireMesh.MakeWireMesh(pos1, pos2);
                        MeshPerChunk[chunkpos].AddMeshData(newMesh);

                    }
                    else
                    {
                        MeshPerChunk[chunkpos] = WireMesh.MakeWireMesh(pos1, pos2);
                    }

            }

            foreach(KeyValuePair<Vec3i, MeshData> mesh in MeshPerChunk)
            {
                mesh.Value.SetMode(EnumDrawMode.Triangles);
                MeshRefPerChunk[mesh.Key] = capi.Render.UploadMesh(mesh.Value);
            }
            MeshPerChunk.Clear();

        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (MeshRefPerChunk == null) return;
            if (stage != EnumRenderStage.Opaque) return;

            IRenderAPI rpi = capi.Render;
            IClientWorldAccessor worldAccess = capi.World;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;
            
            rpi.GLEnableDepthTest();
            rpi.GlEnableCullFace();

            //IStandardShaderProgram prog = rpi.StandardShader;
            IStandardShaderProgram prog = rpi.PreparedStandardShader(0, 0, 0);
            prog.Use();

            AssetLocation wireTexName = new AssetLocation("block/metal/plate/lead.png");

            int texid = capi.Render.GetOrLoadTexture(wireTexName);
            rpi.BindTexture2d(texid);

            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;

            prog.ModelMatrix = ModelMat.Values;

            foreach(KeyValuePair<Vec3i,MeshRef> mesh in MeshRefPerChunk)
            {
                //worldAccess.BlockAccessor.GetLightRGBs()
                Vec3d offset = new Vec3d(mesh.Key.X*chunksize, mesh.Key.Y * chunksize, mesh.Key.Z * chunksize);
                prog.ModelMatrix = ModelMat.Identity().Translate(offset.X - camPos.X, offset.Y - camPos.Y, offset.Z - camPos.Z).Values;
                rpi.RenderMesh(mesh.Value);
            }

            prog.Stop();

        }
    }
}
