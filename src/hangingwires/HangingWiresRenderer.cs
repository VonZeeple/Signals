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
                        MeshData newMesh = MakeWireMesh(pos1, pos2, 0);
                        MeshPerChunk[chunkpos].AddMeshData(newMesh);

                    }
                    else
                    {
                        MeshPerChunk[chunkpos] = MakeWireMesh(pos1, pos2, 0);
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

            float t = 0.015f;//thickness
            Vec3f dPos = pos2 - pos1;
            float dist = pos2.Distance(pos1);

            int nSec = (int)Math.Floor(dist*2);//number of section
            nSec = nSec > 5 ? nSec : 5;

            MeshData mesh = new MeshData(4,6);
            mesh.SetMode(EnumDrawMode.Triangles);


            MeshData mesh_top = new MeshData(4, 6);
            mesh_top.SetMode(EnumDrawMode.Triangles);

            MeshData mesh_bot = new MeshData(4, 6);
            mesh_bot.SetMode(EnumDrawMode.Triangles);

            MeshData mesh_side = new MeshData(4, 6);
            mesh_side.SetMode(EnumDrawMode.Triangles);

            MeshData mesh_side2 = new MeshData(4, 6);
            mesh_side2.SetMode(EnumDrawMode.Triangles);

            //out of plane translation vector:
            Vec3f b = new Vec3f(-dPos.Z, 0, dPos.X).Normalize();
            if(dPos.Z == 0 && dPos.X == 0)
            {
                b = new Vec3f(1, 0, 0);
            }
            int color = debugColors[netId % debugColors.Length];
            Vec3f pos;

            mesh_top.Flags.Fill(0);
            mesh_bot.Flags.Fill(0);
            mesh_side.Flags.Fill(0);
            mesh_side2.Flags.Fill(0);

            //Add vertices
            for (int j=0; j <= nSec; j++)
            {
                float x = dPos.X / nSec * j;
                float y = dPos.Y / nSec * j;
                float z = dPos.Z / nSec * j;
                float l = (float)Math.Sqrt(x * x + y * y + z * z);
                float dy = Catenary(l / dist, 1, 0.5f);
                pos = new Vec3f(x, y + dy, z);


                float du = dist/2/t/nSec;

                mesh_top.AddVertex(pos1.X + pos.X - b.X * t, pos1.Y + pos.Y+t, pos1.Z + pos.Z - b.Z * t,j*du,0, color);
                mesh_top.AddVertex(pos1.X + pos.X + b.X * t, pos1.Y + pos.Y+t, pos1.Z + pos.Z + b.Z * t,j*du,1, color);


                mesh_bot.AddVertex(pos1.X + pos.X - b.X * t, pos1.Y + pos.Y - t, pos1.Z + pos.Z - b.Z * t, j * du, 0, color);
                mesh_bot.AddVertex(pos1.X + pos.X + b.X * t, pos1.Y + pos.Y - t, pos1.Z + pos.Z + b.Z * t, j * du, 1, color);

                mesh_side.AddVertex(pos1.X + pos.X - b.X * t, pos1.Y + pos.Y + t, pos1.Z + pos.Z - b.Z * t, j * du, 1, color);
                mesh_side.AddVertex(pos1.X + pos.X - b.X * t, pos1.Y + pos.Y - t, pos1.Z + pos.Z - b.Z * t, j * du, 0, color);


                mesh_side2.AddVertex(pos1.X + pos.X + b.X * t, pos1.Y + pos.Y + t, pos1.Z + pos.Z + b.Z * t, j * du, 1, color);
                mesh_side2.AddVertex(pos1.X + pos.X + b.X * t, pos1.Y + pos.Y - t, pos1.Z + pos.Z + b.Z * t, j * du, 0, color);


                mesh_top.Flags[2 * j] = VertexFlags.NormalToPackedInt(new Vec3f(0,1,0)) << 15;
                mesh_top.Flags[2 * j+1] = VertexFlags.NormalToPackedInt(new Vec3f(0, 1, 0)) << 15;
                
                mesh_bot.Flags[2 * j] = VertexFlags.NormalToPackedInt(new Vec3f(0, -1, 0)) << 15;
                mesh_bot.Flags[2 * j + 1] = VertexFlags.NormalToPackedInt(new Vec3f(0, -1, 0)) << 15;
                
                mesh_side.Flags[2 * j] = VertexFlags.NormalToPackedInt(-b.X,-b.Y,-b.Z) << 15;
                mesh_side.Flags[2 * j + 1] = VertexFlags.NormalToPackedInt(-b.X, -b.Y, -b.Z) << 15;
                
                mesh_side2.Flags[2 * j] = VertexFlags.NormalToPackedInt(b) << 15;
                mesh_side2.Flags[2 * j + 1] = VertexFlags.NormalToPackedInt(b) << 15;

                int flag = VertexFlags.LeavesWindWaveBitMask;
                
                if(j!=0 && j<nSec)
                {
                    //mesh_top.Flags[2 * j] = flag;
                    //mesh_top.Flags[2 * j+1] = flag;
                    //mesh_bot.Flags[2 * j] = flag;
                    //mesh_bot.Flags[2 * j + 1] = flag;
                    //mesh_side.Flags[2 * j] = flag;
                    //mesh_side.Flags[2 * j + 1] = flag;
                    //mesh_side2.Flags[2 * j] = flag;
                    //mesh_side2.Flags[2 * j + 1] = flag;
                }

            }
            //add indices
            for(int j=0; j < nSec; j++)
            {
                //upper stripe
                int offset = 2 * j;
                mesh_top.AddIndex(offset);
                mesh_top.AddIndex(offset+3);
                mesh_top.AddIndex(offset+2);
                mesh_top.AddIndex(offset);
                mesh_top.AddIndex(offset+1);
                mesh_top.AddIndex(offset+3);

                //lower stripe
                mesh_bot.AddIndex(offset);
                mesh_bot.AddIndex(offset + 3);
                mesh_bot.AddIndex(offset + 1);
                mesh_bot.AddIndex(offset);
                mesh_bot.AddIndex(offset + 2);
                mesh_bot.AddIndex(offset + 3);

                //sides 
                mesh_side.AddIndex(offset);
                mesh_side.AddIndex(offset + 3);
                mesh_side.AddIndex(offset + 1);
                mesh_side.AddIndex(offset);
                mesh_side.AddIndex(offset + 2);
                mesh_side.AddIndex(offset + 3);


                mesh_side2.AddIndex(offset);
                mesh_side2.AddIndex(offset + 3);
                mesh_side2.AddIndex(offset + 2);
                mesh_side2.AddIndex(offset);
                mesh_side2.AddIndex(offset + 1);
                mesh_side2.AddIndex(offset + 3);

            }

            mesh.AddMeshData(mesh_top);
            mesh.AddMeshData(mesh_bot);
            mesh.AddMeshData(mesh_side);
            mesh.AddMeshData(mesh_side2);
            mesh.Rgba.Fill((byte)255);


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
