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

        HashSet<Connection> local_connections;
        public Matrixf ModelMat = new Matrixf();


        public HangingWiresRenderer(ICoreClientAPI capi, HangingWiresMod mod)
        {
            this.mod = mod;
            this.capi = capi;

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
        public void UpdateWiresMesh(HashSet<Connection> connections)
        {

            IBlockAccessor accessor = capi?.World?.BlockAccessor;
            IClientWorldAccessor world = capi?.World;
            if (connections == null || accessor == null) return;

            if (quadModelRefs != null)
            {
                foreach (MeshRef meshRef in quadModelRefs)
                {
                    meshRef?.Dispose();
                }
            }
            quadModelRefs = new MeshRef[connections.Count];

            local_connections = connections;
            int greenCol = (156 << 24) | (100 << 16) | (200 << 8) | (100);

            int i = 0;
            foreach (Connection con in connections)
            {
                IHangingWireAnchor block1 = accessor.GetBlock(con.pos1.blockPos) as IHangingWireAnchor;
                IHangingWireAnchor block2 = accessor.GetBlock(con.pos2.blockPos) as IHangingWireAnchor;

                MeshData mesh = new MeshData(24, 36, false, false, true, false);
                mesh.SetMode(EnumDrawMode.LineStrip);

                int startVertex = mesh.GetVerticesCount();
                Vec3f pos1 = block1.GetAnchorPosInBlock(world, con.pos1);
                Vec3f pos2 = con.pos2.blockPos.ToVec3f()
                    + block2.GetAnchorPosInBlock(world, con.pos2)
                    - con.pos1.blockPos.ToVec3f();

                Vec3f dPos = pos2 - pos1;

                float dist = pos2.Distance(pos1);
                int nv = 10;
                for(int j = 0; j < nv; j++)
                {
                    float x = dPos.X / (nv - 1) * j;
                    float y = dPos.Y / (nv - 1) * j;
                    float z = dPos.Z / (nv - 1) * j;
                    float t = (float)Math.Sqrt(x*x + y*y + z*z);

                    float dy = Catenary(t / dist, 1, 0.5f);
                    LineMeshUtil.AddVertex(mesh, pos1.X+x, pos1.Y+y+ dy, pos1.Z+z, greenCol);

                    mesh.Indices[mesh.IndicesCount++] = startVertex + j;
                }


                quadModelRefs[i] = capi.Render.UploadMesh(mesh);
                i++;
            }


        }

        Vec4f outLineColorMul = new Vec4f(1, 1, 1, 1);
        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (quadModelRefs == null) return;
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
            int i = 0;
            foreach(Connection con in local_connections)
            {
                Vec3d pos1 = con.pos1.blockPos.ToVec3d();

                ModelMat.Set(rpi.CameraMatrixOriginf).Translate(pos1.X-camPos.X, pos1.Y-camPos.Y, pos1.Z-camPos.Z);
                prog.UniformMatrix("modelViewMatrix", ModelMat.Values);
                rpi.RenderMesh(quadModelRefs[i]);
                i++;
            }


            prog.Stop();

        }
    }
}
