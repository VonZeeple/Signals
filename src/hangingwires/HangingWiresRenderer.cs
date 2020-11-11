using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

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


            foreach (long netId in data.HangingWiresNetworks.Keys)
            {

                foreach (WireConnection con in data.HangingWiresNetworks[netId].Connections)
                {
                    IHangingWireAnchor block1 = accessor.GetBlock(con.pos1.blockPos) as IHangingWireAnchor;
                    IHangingWireAnchor block2 = accessor.GetBlock(con.pos2.blockPos) as IHangingWireAnchor;
                    if (block1 == null || block2 == null) continue;


                    BlockPos blockPos1 = con.pos1.blockPos;
                    Vec3i chunkpos = new Vec3i(blockPos1.X / chunksize, blockPos1.Y / chunksize, blockPos1.Z / chunksize);

                    Vec3f pos1 = con.pos1.blockPos.ToVec3f().AddCopy(-chunkpos.X*chunksize,-chunkpos.Y*chunksize,-chunkpos.Z*chunksize) + block1.GetAnchorPosInBlock(world, con.pos1);
                    Vec3f pos2 = con.pos2.blockPos.ToVec3f().AddCopy(-chunkpos.X * chunksize, -chunkpos.Y * chunksize, -chunkpos.Z * chunksize) + block2.GetAnchorPosInBlock(world, con.pos2);

                    if (MeshPerChunk.ContainsKey(chunkpos))
                    {
                        MeshData newMesh = MakeWireMesh(pos1, pos2, netId);
                        MeshPerChunk[chunkpos].AddMeshData(newMesh);

                    }
                    else
                    {
                        MeshPerChunk[chunkpos] = MakeWireMesh(pos1, pos2, netId);
                    }

                  
                }
            }

            foreach(KeyValuePair<Vec3i, MeshData> mesh in MeshPerChunk)
            {
                mesh.Value.SetMode(EnumDrawMode.Triangles);
                MeshRefPerChunk[mesh.Key] = capi.Render.UploadMesh(mesh.Value);
            }
            MeshPerChunk.Clear();

        }
        
        private MeshData MakeWireMesh(Vec3f pos1, Vec3f pos2, long netId)
        {

            float t = 0.05f;//thickness
            Vec3f dPos = pos2 - pos1;
            float dist = pos2.Distance(pos1);

            int nSec = (int)Math.Floor(dist*2);//number of section

            //int nSec = 3;
            //Start with a simple strip:
            //number of triangles: nsec*2
            //number of vertices (nsec+1)*2
            //number of indices: nsec*6
            MeshData mesh = new MeshData((nSec + 1) * 2 * 2, nSec *3* 6, false, true, true, false);
            mesh.SetMode(EnumDrawMode.Triangles);
            //mesh.Flags = new int[(nSec + 1) * 2].Fill(0);

            mesh.SetVerticesCount((nSec + 1) * 4);
            mesh.SetIndicesCount(nSec * 6*3);


            //out of plane translation vector:
            Vec3f b = new Vec3f(-dPos.Z, 0, dPos.X).Normalize();
            if(dPos.Z == 0 && dPos.X == 0)
            {
                b = new Vec3f(1, 0, 0);
            }
            int color = debugColors[netId % debugColors.Length];

            mesh.Rgba.Fill((byte)255);
            Vec3f pos;
            //Add vertices
            for(int j=0; j <= nSec; j++)
            {
                float x = dPos.X / nSec * j;
                float y = dPos.Y / nSec * j;
                float z = dPos.Z / nSec * j;
                float l = (float)Math.Sqrt(x * x + y * y + z * z);
                float dy = Catenary(l / dist, 1, 0.5f);
                pos = new Vec3f(x, y + dy, z);


                //upper strip, contains (nSec+1)*2 vertices
                //mesh.AddVertex(pos1.X + pos.X - b.X * t, pos1.Y + pos.Y+t, pos1.Z + pos.Z - b.Z * t, color);
                //mesh.AddVertex(pos1.X + pos.X + b.X * t, pos1.Y + pos.Y+t, pos1.Z + pos.Z + b.Z * t, color);
                mesh.xyz[j * 6] = pos1.X + pos.X - b.X * t;
                mesh.xyz[j * 6 + 1] = pos1.Y + pos.Y + t;
                mesh.xyz[j * 6 + 2] = pos1.Z + pos.Z - b.Z * t;

                mesh.xyz[j * 6 + 3] = pos1.X + pos.X + b.X * t;
                mesh.xyz[j * 6 + 4] = pos1.Y + pos.Y + t;
                mesh.xyz[j * 6 + 5] = pos1.Z + pos.Z + b.Z * t;


                if(j%2 == 0)
                {
                    mesh.Uv[j * 4] = 0;
                    mesh.Uv[j * 4 + 1] = 0;
                    mesh.Uv[j * 4 + 2] = 1;
                    mesh.Uv[j * 4 + 3] = 0;
                }
                else
                {
                    mesh.Uv[j * 4] = 0;
                    mesh.Uv[j * 4 + 1] = 1;
                    mesh.Uv[j * 4 + 2] = 1;
                    mesh.Uv[j * 4 + 3] = 1;
                }


                //lower strip

                //mesh.AddVertex(pos1.X + pos.X - b.X * t, pos1.Y + pos.Y - t, pos1.Z + pos.Z - b.Z * t, color);
                //mesh.AddVertex(pos1.X + pos.X + b.X * t, pos1.Y + pos.Y - t, pos1.Z + pos.Z + b.Z * t, color);
                int offset = ((nSec + 1) * 2 * 3) + (j * 6);
                mesh.xyz[offset] = pos1.X + pos.X - b.X * t;
                mesh.xyz[offset+1] = pos1.Y + pos.Y - t;
                mesh.xyz[offset+2] = pos1.Z + pos.Z - b.Z * t;

                mesh.xyz[offset+3] = pos1.X + pos.X + b.X * t;
                mesh.xyz[offset+4] = pos1.Y + pos.Y - t;
                mesh.xyz[offset+5] = pos1.Z + pos.Z + b.Z * t;


                offset = ((nSec + 1) * 2 * 2) + (j * 4);
                if (j % 2 == 0)
                {
                    mesh.Uv[offset] = 0;
                    mesh.Uv[offset + 1] = 0;
                    mesh.Uv[offset + 2] = 1;
                    mesh.Uv[offset + 3] = 0;
                }
                else
                {
                    mesh.Uv[offset] = 0;
                    mesh.Uv[offset + 1] = 1;
                    mesh.Uv[offset + 2] = 1;
                    mesh.Uv[offset + 3] = 1;
                }

            }
            //add indices
            for(int j=0; j < nSec; j++)
            {
                //upper stripe
                int offset = 2 * j;
                mesh.AddIndex(offset);
                mesh.AddIndex(offset+3);
                mesh.AddIndex(offset+2);
                mesh.AddIndex(offset);
                mesh.AddIndex(offset+1);
                mesh.AddIndex(offset+3);

                //lower stripe
                offset = (nSec+1)*2+(2 * j);
                mesh.AddIndex(offset);
                mesh.AddIndex(offset + 3);
                mesh.AddIndex(offset + 1);
                mesh.AddIndex(offset);
                mesh.AddIndex(offset + 2);
                mesh.AddIndex(offset + 3);

                //side stripes
                offset = j * 2;

                mesh.AddIndex(offset + 1);
                mesh.AddIndex(offset + 3 + (nSec + 1) * 2);
                mesh.AddIndex(offset + 1 + (nSec + 1) * 2);

                mesh.AddIndex(offset+1);
                mesh.AddIndex(offset + 3);
                mesh.AddIndex(offset + 3 + (nSec + 1) * 2);

                
                


            }
            

            return mesh;

        }


        private MeshData MakeSimpleWireMesh(Vec3f pos1, Vec3f pos2, long netId=0)
        {
            Vec3f dPos = pos2 - pos1;

            float dist = pos2.Distance(pos1);
            int nv = 5;

            MeshData mesh = new MeshData(nv,(nv-1)*2,false,false,true,false);
            mesh.xyz = new float[3 * nv];
            mesh.Rgba = new byte[4 * nv].Fill((byte)0);
            mesh.Indices = new int[(nv-1)*2];

            
            for (int j = 0; j <= nv; j++)
            {
                int startVertex = mesh.GetVerticesCount();
                float x = dPos.X / (nv - 1) * j;
                float y = dPos.Y / (nv - 1) * j;
                float z = dPos.Z / (nv - 1) * j;
                float t = (float)Math.Sqrt(x * x + y * y + z * z);

                float dy = Catenary(t / dist, 1, 0.5f);
                LineMeshUtil.AddVertex(mesh, pos1.X + x, pos1.Y + y + dy, pos1.Z + z, debugColors[netId % debugColors.Length]);

                if (j < nv - 1)
                {
                    mesh.Indices[mesh.IndicesCount++] = startVertex + 0;
                    mesh.Indices[mesh.IndicesCount++] = startVertex + 1;
                }


            }

            return mesh;
        }


        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (MeshRefPerChunk == null) return;
            if (stage != EnumRenderStage.Opaque) return;

            IRenderAPI rpi = capi.Render;
            IClientWorldAccessor worldAccess = capi.World;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;

            rpi.GLEnableDepthTest();
            rpi.GlToggleBlend(true);
            rpi.GlEnableCullFace();

            IStandardShaderProgram prog = rpi.StandardShader;

            prog.Use();

            AssetLocation wireTexName = new AssetLocation("block/metal/plate/lead.png");
            wireTexName = new AssetLocation("block/tech/tech-powergeneratorbottom.png");

            int texid = capi.Render.GetOrLoadTexture(wireTexName);
            rpi.BindTexture2d(texid);

            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;

            prog.ModelMatrix = ModelMat.Values;

            foreach(KeyValuePair<Vec3i,MeshRef> mesh in MeshRefPerChunk)
            {
                Vec3d offset = new Vec3d(mesh.Key.X*chunksize, mesh.Key.Y * chunksize, mesh.Key.Z * chunksize);
                prog.ModelMatrix = ModelMat.Identity().Translate(offset.X - camPos.X, offset.Y - camPos.Y, offset.Z - camPos.Z).Values;
                rpi.RenderMesh(mesh.Value);
            }

            prog.Stop();

        }
    }
}
