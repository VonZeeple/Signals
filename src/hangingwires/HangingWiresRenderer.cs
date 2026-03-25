using System.Collections.Generic;
using System;
using System.Diagnostics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace signals.src.hangingwires
{
    public class HangingWiresRenderer : IRenderer
    {
        public double RenderOrder => 0.5;

        public int RenderRange => 100;

        public bool dirty;

        HangingWiresMod mod;
        ICoreClientAPI capi;



        int chunksize;
        public Matrixf ModelMat = new Matrixf();

        Dictionary<Vec3i, MeshRef> MeshRefPerChunk;
        Dictionary<Vec3i, MeshData> rebuildMeshPerChunk;
        Dictionary<Vec3i, MeshRef> nextMeshRefPerChunk;
        List<WireConnection> rebuildConnections;
        int rebuildConnectionIndex;
        List<KeyValuePair<Vec3i, MeshData>> uploadQueue;
        int uploadIndex;

        bool isRebuilding;
        bool rebuildRequestedWhileRunning;

        readonly AssetLocation wireTexName = new AssetLocation("signals:item/wire.png");
        int wireTextureId = -1;

        // Hard limits + time budget keep the per-tick cost bounded.
        const int MaxConnectionsPerTick = 20;
        const int MaxChunkUploadsPerTick = 1;
        const double RebuildTimeBudgetMs = 2.0;


        public HangingWiresRenderer(ICoreClientAPI capi, HangingWiresMod mod)
        {
            this.mod = mod;
            this.capi = capi;
            chunksize = GlobalConstants.ChunkSize;
            capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "hangingwiresnetwork");

            //capi.Event.ReloadShader += LoadShader;
            //LoadShader();
        }


        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            if (MeshRefPerChunk != null)
            {
                foreach (MeshRef meshRef in MeshRefPerChunk.Values)
                {
                    meshRef?.Dispose();
                }
            }
            if (nextMeshRefPerChunk != null)
            {
                foreach (MeshRef meshRef in nextMeshRefPerChunk.Values)
                {
                    meshRef?.Dispose();
                }
            }
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
        void BeginMeshRebuild(HangingWiresData data)
        {
            IBlockAccessor accessor = capi?.World?.BlockAccessor;
            if (data == null || accessor == null) return;

            rebuildConnections = new List<WireConnection>(data.connections);
            rebuildConnectionIndex = 0;
            uploadQueue = null;
            uploadIndex = 0;
            rebuildMeshPerChunk = new Dictionary<Vec3i, MeshData>();
            nextMeshRefPerChunk = new Dictionary<Vec3i, MeshRef>();
            isRebuilding = true;
        }

        void ContinueMeshRebuild()
        {
            if (!isRebuilding) return;

            Stopwatch sw = Stopwatch.StartNew();

            IBlockAccessor accessor = capi?.World?.BlockAccessor;
            if (accessor == null)
            {
                isRebuilding = false;
                return;
            }

            if (uploadQueue == null)
            {
                int processedConnections = 0;
                for (; rebuildConnectionIndex < rebuildConnections.Count; rebuildConnectionIndex++)
                {
                    if (processedConnections >= MaxConnectionsPerTick) break;
                    if (sw.Elapsed.TotalMilliseconds >= RebuildTimeBudgetMs) break;

                    WireConnection con = rebuildConnections[rebuildConnectionIndex];
                    processedConnections++;

                    IHangingWireAnchor block1 = accessor.GetBlock(con.pos1.blockPos) as IHangingWireAnchor;
                    IHangingWireAnchor block2 = accessor.GetBlock(con.pos2.blockPos) as IHangingWireAnchor;
                    if (block1 == null || block2 == null) continue;

                    BlockPos blockPos1 = con.pos1.blockPos;
                    Vec3i chunkpos = new Vec3i(blockPos1.X / chunksize, blockPos1.Y / chunksize, blockPos1.Z / chunksize);

                    Vec3f pos1 = con.pos1.blockPos.ToVec3f().AddCopy(-chunkpos.X * chunksize, -chunkpos.Y * chunksize, -chunkpos.Z * chunksize) + block1.GetAnchorPosInBlock(con.pos1);
                    Vec3f pos2 = con.pos2.blockPos.ToVec3f().AddCopy(-chunkpos.X * chunksize, -chunkpos.Y * chunksize, -chunkpos.Z * chunksize) + block2.GetAnchorPosInBlock(con.pos2);

                    if (rebuildMeshPerChunk.TryGetValue(chunkpos, out MeshData mesh))
                    {
                        mesh.AddMeshData(WireMesh.MakeWireMesh(pos1, pos2));
                    }
                    else
                    {
                        rebuildMeshPerChunk[chunkpos] = WireMesh.MakeWireMesh(pos1, pos2);
                    }
                }

                if (rebuildConnectionIndex < rebuildConnections.Count)
                {
                    return;
                }

                uploadQueue = new List<KeyValuePair<Vec3i, MeshData>>(rebuildMeshPerChunk);
                rebuildConnections = null;
                rebuildMeshPerChunk = null;
            }

            int uploadedChunks = 0;
            for (; uploadIndex < uploadQueue.Count; uploadIndex++)
            {
                if (uploadedChunks >= MaxChunkUploadsPerTick) break;
                if (sw.Elapsed.TotalMilliseconds >= RebuildTimeBudgetMs) break;

                KeyValuePair<Vec3i, MeshData> mesh = uploadQueue[uploadIndex];
                mesh.Value.SetMode(EnumDrawMode.Triangles);
                nextMeshRefPerChunk[mesh.Key] = capi.Render.UploadMesh(mesh.Value);
                uploadedChunks++;
            }

            if (uploadIndex < uploadQueue.Count)
            {
                return;
            }

            if (MeshRefPerChunk != null)
            {
                foreach (MeshRef meshRef in MeshRefPerChunk.Values)
                {
                    meshRef?.Dispose();
                }
            }

            MeshRefPerChunk = nextMeshRefPerChunk;
            nextMeshRefPerChunk = null;
            uploadQueue = null;
            isRebuilding = false;

        }

        public void OnClientTick(float dt)
        {
            if (dirty)
            {
                if (isRebuilding)
                {
                    rebuildRequestedWhileRunning = true;
                }
                else
                {
                    HangingWiresData data = capi.ModLoader.GetModSystem<HangingWiresMod>()?.data;
                    BeginMeshRebuild(data);
                }
                dirty = false;
            }

            ContinueMeshRebuild();

            if (!isRebuilding && rebuildRequestedWhileRunning)
            {
                HangingWiresData data = capi.ModLoader.GetModSystem<HangingWiresMod>()?.data;
                BeginMeshRebuild(data);
                rebuildRequestedWhileRunning = false;
                ContinueMeshRebuild();
            }
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

            if (wireTextureId < 0)
            {
                wireTextureId = capi.Render.GetOrLoadTexture(wireTexName);
            }
            rpi.BindTexture2d(wireTextureId);

            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;

            prog.ModelMatrix = ModelMat.Values;

            float maxRenderDistance = RenderRange + chunksize;
            float maxRenderDistanceSq = maxRenderDistance * maxRenderDistance;

            foreach(KeyValuePair<Vec3i,MeshRef> mesh in MeshRefPerChunk)
            {
                Vec3d offset = new Vec3d(mesh.Key.X * chunksize, mesh.Key.Y * chunksize, mesh.Key.Z * chunksize);
                double cx = offset.X + chunksize * 0.5 - camPos.X;
                double cy = offset.Y + chunksize * 0.5 - camPos.Y;
                double cz = offset.Z + chunksize * 0.5 - camPos.Z;
                if (cx * cx + cy * cy + cz * cz > maxRenderDistanceSq) continue;

                prog.ModelMatrix = ModelMat.Identity().Translate(offset.X - camPos.X, offset.Y - camPos.Y, offset.Z - camPos.Z).Values;
                rpi.RenderMesh(mesh.Value);
            }

            prog.Stop();

        }
    }
}
