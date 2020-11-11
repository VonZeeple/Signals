using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    public class SignalNetworkDebugRenderer : IRenderer
    {

        ICoreClientAPI capi;
        public double RenderOrder => 0.94;

        public int RenderRange => 100;

        private List<Vec3d> nodePositions;
        private List<(Vec3d, Vec3d)> connections;
        private MeshRef meshRef;
        private MeshRef[] cachedConnectionsMesh;
        
        
        public void Dispose()
        {
            meshRef?.Dispose();
            nodePositions?.Clear();
            if (cachedConnectionsMesh == null) return;
            foreach (MeshRef mesh in cachedConnectionsMesh)
            {
                mesh.Dispose();
            }
            
        }

        public SignalNetworkDebugRenderer(ICoreClientAPI capi, SignalNetworkMod mod)
        {
            this.capi = capi;

            capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "signalnetworkdebugrender");

            //cross mesh
            int color = (255 << 24) | (0 << 16) | (255 << 8) | (0);
            float size = 0.05f;
            MeshData mesh = new MeshData(6, 6, false, false, true, false);
            mesh.SetMode(EnumDrawMode.Lines);
            mesh.AddVertex(-size, 0, 0, color);
            mesh.AddVertex(size, 0, 0, color);
            mesh.Indices[mesh.IndicesCount++] = 0;
            mesh.Indices[mesh.IndicesCount++] = 1;

            mesh.AddVertex(0, -size, 0, color);
            mesh.AddVertex(0, size, 0, color);
            mesh.Indices[mesh.IndicesCount++] = 2;
            mesh.Indices[mesh.IndicesCount++] = 3;

            mesh.AddVertex(0,0,-size, color);
            mesh.AddVertex(0,0,size, color);
            mesh.Indices[mesh.IndicesCount++] = 4;
            mesh.Indices[mesh.IndicesCount++] = 5;

            meshRef = capi.Render.UploadMesh(mesh);

        }

        private MeshData MakeLineMesh(Vec3f pos1, Vec3f pos2)
        {
            int color = (255 << 24) | (0 << 16) | (255 << 8) | (0);
            MeshData mesh = new MeshData(2, 2, false, false, true, false);
            mesh.SetMode(EnumDrawMode.Lines);

            mesh.AddVertex(pos1.X,pos1.Y,pos1.Z, color);
            mesh.AddVertex(pos2.X, pos2.Y, pos2.Z, color);

            mesh.Indices[mesh.IndicesCount++] = 0;
            mesh.Indices[mesh.IndicesCount++] = 1;

            return mesh;
        }

        private Vec3f GetNodePosition(NodePos pos, IBlockAccessor access)
        {
            ISignalNodeProvider provider = access.GetBlockEntity(pos.blockPos) as ISignalNodeProvider;
            if (provider == null && access.GetBlockEntity(pos.blockPos) != null)
            {
                foreach (BlockEntityBehavior beb in access.GetBlockEntity(pos.blockPos).Behaviors)
                {
                    provider = beb as ISignalNodeProvider;
                    if (provider != null) break;
                }
            }
            Vec3f vec = provider?.GetNodePosinBlock(pos);
            return vec != null ? vec : new Vec3f(0, 0, 0);
        }

        public void RebuildMesh(SignalNetworksData data)
        {
            
            IBlockAccessor access = capi.World.BlockAccessor;
            nodePositions = new List<Vec3d>();
            connections = new List<(Vec3d, Vec3d)>();

            foreach (SignalNetwork net in data.networksById.Values)
            {
                NodePos[] noPos = net.nodes.Keys.ToArray();
                
                foreach (KeyValuePair<NodePos, ISignalNode> kv in net.nodes)
                {

                    Vec3f vec = GetNodePosition(kv.Key, access);

                    nodePositions.Add(kv.Key.blockPos.ToVec3d().Add(vec.X, vec.Y, vec.Z));

                    foreach(Connection con in kv.Value.Connections)
                    {
                        connections.Add((con.pos1.blockPos.ToVec3d().Add(vec.X, vec.Y, vec.Z),

                            con.pos2.blockPos.ToVec3d().Add(vec.X, vec.Y, vec.Z)));
                    }

                }   
            }

            cachedConnectionsMesh = new MeshRef[connections.Count];
            int i = 0;
            foreach ((Vec3d, Vec3d) tuple in connections)
            {
                MeshData mesh = MakeLineMesh(new Vec3f(0, 0, 0), tuple.Item2.SubCopy(tuple.Item1).ToVec3f());
                cachedConnectionsMesh[i] = this.capi.Render.UploadMesh(mesh);
                i++;
            }

        }


        public Matrixf ModelMat = new Matrixf();
        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (stage != EnumRenderStage.Opaque) return;
            if (nodePositions == null) return;

            IRenderAPI rpi = capi.Render;
            IShaderProgram prog = rpi.GetEngineShader(EnumShaderProgram.Wireframe);
            prog.Use();
            prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
            prog.UniformMatrix("modelViewMatrix", ModelMat.Values);
            rpi.GLDisableDepthTest();

            IClientWorldAccessor worldAccess = capi.World;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;

            int i = 0;
            foreach((Vec3d, Vec3d) tuple in connections)
            {
                ModelMat.Set(rpi.CameraMatrixOriginf).Translate(tuple.Item1.X - camPos.X, tuple.Item1.Y - camPos.Y, tuple.Item1.Z - camPos.Z);
                prog.UniformMatrix("modelViewMatrix", ModelMat.Values);
                rpi.RenderMesh(cachedConnectionsMesh[i++]);

            }
            foreach (Vec3d vec in nodePositions)
            {

                ModelMat.Set(rpi.CameraMatrixOriginf).Translate(vec.X - camPos.X, vec.Y - camPos.Y, vec.Z - camPos.Z);
                prog.UniformMatrix("modelViewMatrix", ModelMat.Values);
                rpi.RenderMesh(meshRef);
                
            }
            
            rpi.GLEnableDepthTest();
            prog.Stop();
        }
    }
}
