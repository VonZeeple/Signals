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
        IShaderProgram prog;


        MeshRef[] quadModelRefs;
        MeshRef singleWireRef;
        int chunksize;
        HashSet<BlockPos> ConPos;
        public Matrixf ModelMat = new Matrixf();

        Dictionary<Vec3i, MeshRef> MeshRefPerChunk;


        public HangingWiresRenderer(ICoreClientAPI capi, HangingWiresMod mod)
        {
            this.mod = mod;
            this.capi = capi;
            chunksize = capi.World.BlockAccessor.ChunkSize;
            capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "hangingwiresnetwork");

            capi.Event.ReloadShader += LoadShader;
            LoadShader();
        }

        public bool LoadShader()
        {
            prog = capi.Shader.NewShaderProgram();

            prog.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
            prog.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);

            capi.Shader.RegisterFileShaderProgram("hangingwires", prog);

            return prog.Compile();
        }

        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            if (quadModelRefs == null) return;
            foreach(MeshRef meshRef in quadModelRefs)
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

                foreach (Connection con in data.HangingWiresNetworks[netId].Connections)
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
                        MeshPerChunk[chunkpos].AddMeshData(MakeWireMesh(pos1, pos2, netId));

                    }
                    else
                    {
                        MeshPerChunk[chunkpos] = MakeWireMesh(pos1, pos2, netId);
                    }

                  
                }
            }

            foreach(KeyValuePair<Vec3i, MeshData> mesh in MeshPerChunk)
            {
                mesh.Value.SetMode(EnumDrawMode.Lines);
                MeshRefPerChunk[mesh.Key] = capi.Render.UploadMesh(mesh.Value);
            }
            MeshPerChunk.Clear();

        }
        
        private MeshData MakeWireMesh(Vec3f pos1, Vec3f pos2, long netId=0)
        {
            
            

            Vec3f dPos = pos2 - pos1;

            float dist = pos2.Distance(pos1);
            int nv = 10;

            MeshData mesh = new MeshData(nv,(nv-1)*2,false,false,true,false);
            mesh.xyz = new float[3 * nv];
            mesh.Rgba = new byte[4 * nv].Fill((byte)0);
            mesh.Indices = new int[(nv-1)*2];

            
            for (int j = 0; j < nv; j++)
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


        Vec4f outLineColorMul = new Vec4f(1, 1, 1, 1);
        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (MeshRefPerChunk == null) return;
            capi.Render.GlDisableCullFace();
            if (stage != EnumRenderStage.Opaque) return;

            IRenderAPI rpi = capi.Render;
            IClientWorldAccessor worldAccess = capi.World;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;

            //ModelMat.Set(rpi.CameraMatrixOriginf).Translate(- camPos.X, - camPos.Y, - camPos.Z);
            
            rpi.GLEnableDepthTest();
            rpi.GlToggleBlend(true);
            
            IShaderProgram prog = rpi.GetEngineShader(EnumShaderProgram.Wireframe);
            prog.Use();
            prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
            prog.UniformMatrix("modelViewMatrix", ModelMat.Values);
            prog.Uniform("colorIn", outLineColorMul);
            //rpi.RenderMesh(quadModelRefs);


            foreach(KeyValuePair<Vec3i,MeshRef> mesh in MeshRefPerChunk)
            {
                Vec3d offset = new Vec3d(mesh.Key.X*chunksize, mesh.Key.Y * chunksize, mesh.Key.Z * chunksize);

                ModelMat.Set(rpi.CameraMatrixOriginf).Translate(offset.X-camPos.X, offset.Y-camPos.Y, offset.Z-camPos.Z);
                prog.UniformMatrix("modelViewMatrix", ModelMat.Values);
                rpi.RenderMesh(mesh.Value);
            }


            prog.Stop();

        }
    }
}
