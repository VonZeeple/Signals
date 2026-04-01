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

        enum RebuildRequestMode
        {
            None,
            Partial,
            Full
        }

        RebuildRequestMode pendingRequestMode = RebuildRequestMode.None;
        HashSet<Vec3i> pendingPartialChunks = new HashSet<Vec3i>();
        HashSet<WireConnection> lastKnownConnections = new HashSet<WireConnection>();

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
        HashSet<Vec3i> activeRebuildChunks;
        bool activeRebuildIsFull;
        Dictionary<Vec3i, int> activeChunkPriority;

        bool isRebuilding;

        readonly AssetLocation wireTexName = new AssetLocation("signals:item/wire.png");
        int wireTextureId = -1;

        // Hard limits + time budget keep the per-tick cost bounded.
        const int MaxConnectionsPerTick = 20;
        const int MaxConnectionsPerTickPartial = 200;
        const int MaxChunkUploadsPerTick = 4;
        const double RebuildTimeBudgetMs = 2.0;
        const double RebuildTimeBudgetMsPartial = 6.0;


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
        Vec3i GetChunkPos(BlockPos pos)
        {
            int cx = (int)Math.Floor((double)pos.X / chunksize);
            int cy = (int)Math.Floor((double)pos.Y / chunksize);
            int cz = (int)Math.Floor((double)pos.Z / chunksize);
            return new Vec3i(cx, cy, cz);
        }

        HashSet<Vec3i> ComputeChangedChunks(HangingWiresData data)
        {
            HashSet<WireConnection> currentConnections = data == null
                ? new HashSet<WireConnection>()
                : new HashSet<WireConnection>(data.connections);

            HashSet<Vec3i> changedChunks = new HashSet<Vec3i>();

            foreach (WireConnection con in currentConnections)
            {
                if (!lastKnownConnections.Contains(con))
                {
                    changedChunks.Add(GetChunkPos(con.pos1.blockPos));
                }
            }

            foreach (WireConnection con in lastKnownConnections)
            {
                if (!currentConnections.Contains(con))
                {
                    changedChunks.Add(GetChunkPos(con.pos1.blockPos));
                }
            }

            lastKnownConnections = currentConnections;
            return changedChunks;
        }

        public void RequestFullRebuild()
        {
            pendingRequestMode = RebuildRequestMode.Full;
            // Keep pendingPartialChunks – they serve as priority hints for upload ordering
        }

        public void RequestIncrementalRebuild(HangingWiresData data)
        {
            if (pendingRequestMode == RebuildRequestMode.Full)
            {
                return;
            }

            HashSet<Vec3i> changedChunks = ComputeChangedChunks(data);
            if (changedChunks.Count == 0)
            {
                return;
            }

            pendingRequestMode = RebuildRequestMode.Partial;
            foreach (Vec3i chunk in changedChunks)
            {
                pendingPartialChunks.Add(chunk);
            }
        }

        Dictionary<Vec3i, int> BuildChunkPriority(HashSet<Vec3i> targetChunks)
        {
            Dictionary<Vec3i, int> chunkPriority = new Dictionary<Vec3i, int>();
            if (targetChunks == null || targetChunks.Count == 0)
            {
                return chunkPriority;
            }

            Vec3d camPos = capi.World.Player?.Entity?.CameraPos;
            if (camPos == null)
            {
                return chunkPriority;
            }

            List<KeyValuePair<Vec3i, double>> distances = new List<KeyValuePair<Vec3i, double>>();
            foreach (Vec3i chunk in targetChunks)
            {
                double cx = chunk.X * chunksize + chunksize * 0.5 - camPos.X;
                double cy = chunk.Y * chunksize + chunksize * 0.5 - camPos.Y;
                double cz = chunk.Z * chunksize + chunksize * 0.5 - camPos.Z;
                distances.Add(new KeyValuePair<Vec3i, double>(chunk, cx * cx + cy * cy + cz * cz));
            }

            distances.Sort((a, b) => a.Value.CompareTo(b.Value));
            for (int i = 0; i < distances.Count; i++)
            {
                chunkPriority[distances[i].Key] = i;
            }

            return chunkPriority;
        }

        void BeginMeshRebuild(HangingWiresData data, bool fullRebuild, HashSet<Vec3i> targetChunks, HashSet<Vec3i> priorityChunks = null)
        {
            IBlockAccessor accessor = capi?.World?.BlockAccessor;
            if (data == null || accessor == null) return;

            activeRebuildIsFull = fullRebuild;
            activeRebuildChunks = fullRebuild ? null : new HashSet<Vec3i>(targetChunks);
            activeChunkPriority = BuildChunkPriority(fullRebuild ? priorityChunks : activeRebuildChunks);

            if (fullRebuild)
            {
                rebuildConnections = new List<WireConnection>(data.connections);
            }
            else
            {
                rebuildConnections = new List<WireConnection>();
                foreach (WireConnection con in data.connections)
                {
                    if (targetChunks.Contains(GetChunkPos(con.pos1.blockPos)))
                    {
                        rebuildConnections.Add(con);
                    }
                }
            }

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
                int maxConnectionsThisTick = activeRebuildIsFull ? MaxConnectionsPerTick : MaxConnectionsPerTickPartial;
                double budgetMsThisTick = activeRebuildIsFull ? RebuildTimeBudgetMs : RebuildTimeBudgetMsPartial;
                for (; rebuildConnectionIndex < rebuildConnections.Count; rebuildConnectionIndex++)
                {
                    if (processedConnections >= maxConnectionsThisTick) break;
                    if (sw.Elapsed.TotalMilliseconds >= budgetMsThisTick) break;

                    WireConnection con = rebuildConnections[rebuildConnectionIndex];
                    processedConnections++;

                    IHangingWireAnchor block1 = accessor.GetBlock(con.pos1.blockPos) as IHangingWireAnchor;
                    IHangingWireAnchor block2 = accessor.GetBlock(con.pos2.blockPos) as IHangingWireAnchor;
                    if (block1 == null || block2 == null) continue;

                    Vec3i chunkpos = GetChunkPos(con.pos1.blockPos);

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
                uploadQueue.Sort((a, b) =>
                {
                    bool aHasPriority = activeChunkPriority.TryGetValue(a.Key, out int aPri);
                    bool bHasPriority = activeChunkPriority.TryGetValue(b.Key, out int bPri);
                    if (aHasPriority && bHasPriority) return aPri.CompareTo(bPri);
                    if (aHasPriority) return -1;
                    if (bHasPriority) return 1;
                    return 0;
                });
                rebuildConnections = null;
                rebuildMeshPerChunk = null;
            }

            int uploadedChunks = 0;
            double uploadBudgetMsThisTick = activeRebuildIsFull ? RebuildTimeBudgetMs : RebuildTimeBudgetMsPartial;
            for (; uploadIndex < uploadQueue.Count; uploadIndex++)
            {
                if (uploadedChunks >= MaxChunkUploadsPerTick) break;
                if (sw.Elapsed.TotalMilliseconds >= uploadBudgetMsThisTick) break;

                KeyValuePair<Vec3i, MeshData> mesh = uploadQueue[uploadIndex];
                mesh.Value.SetMode(EnumDrawMode.Triangles);
                nextMeshRefPerChunk[mesh.Key] = capi.Render.UploadMesh(mesh.Value);
                uploadedChunks++;
            }

            if (uploadIndex < uploadQueue.Count)
            {
                return;
            }

            if (activeRebuildIsFull)
            {
                if (MeshRefPerChunk != null)
                {
                    foreach (MeshRef meshRef in MeshRefPerChunk.Values)
                    {
                        meshRef?.Dispose();
                    }
                }

                MeshRefPerChunk = nextMeshRefPerChunk;
            }
            else
            {
                if (MeshRefPerChunk == null)
                {
                    MeshRefPerChunk = new Dictionary<Vec3i, MeshRef>();
                }

                foreach (KeyValuePair<Vec3i, MeshRef> entry in nextMeshRefPerChunk)
                {
                    if (MeshRefPerChunk.TryGetValue(entry.Key, out MeshRef oldRef))
                    {
                        oldRef?.Dispose();
                    }
                    MeshRefPerChunk[entry.Key] = entry.Value;
                }

                foreach (Vec3i chunk in activeRebuildChunks)
                {
                    if (nextMeshRefPerChunk.ContainsKey(chunk)) continue;
                    if (MeshRefPerChunk.TryGetValue(chunk, out MeshRef oldRef))
                    {
                        oldRef?.Dispose();
                        MeshRefPerChunk.Remove(chunk);
                    }
                }
            }
            nextMeshRefPerChunk = null;
            uploadQueue = null;
            activeRebuildChunks = null;
            activeChunkPriority = null;
            isRebuilding = false;

        }

        public void OnClientTick(float dt)
        {
            HangingWiresData data = capi.ModLoader.GetModSystem<HangingWiresMod>()?.data;

            if (!isRebuilding && pendingRequestMode != RebuildRequestMode.None)
            {
                if (pendingRequestMode == RebuildRequestMode.Full)
                {
                    BeginMeshRebuild(data, true, null, pendingPartialChunks.Count > 0 ? pendingPartialChunks : null);
                }
                else
                {
                    BeginMeshRebuild(data, false, pendingPartialChunks);
                }

                pendingRequestMode = RebuildRequestMode.None;
                pendingPartialChunks.Clear();
            }

            ContinueMeshRebuild();

            // If rebuild just finished and a new request arrived during the same tick,
            // only start it — do NOT call ContinueMeshRebuild again to avoid doubling
            // the per-tick time budget.
            if (!isRebuilding && pendingRequestMode != RebuildRequestMode.None)
            {
                if (pendingRequestMode == RebuildRequestMode.Full)
                {
                    BeginMeshRebuild(data, true, null, pendingPartialChunks.Count > 0 ? pendingPartialChunks : null);
                }
                else
                {
                    BeginMeshRebuild(data, false, pendingPartialChunks);
                }

                pendingRequestMode = RebuildRequestMode.None;
                pendingPartialChunks.Clear();
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
                double ox = mesh.Key.X * chunksize;
                double oy = mesh.Key.Y * chunksize;
                double oz = mesh.Key.Z * chunksize;
                double cx = ox + chunksize * 0.5 - camPos.X;
                double cy = oy + chunksize * 0.5 - camPos.Y;
                double cz = oz + chunksize * 0.5 - camPos.Z;
                if (cx * cx + cy * cy + cz * cz > maxRenderDistanceSq) continue;

                prog.ModelMatrix = ModelMat.Identity().Translate(ox - camPos.X, oy - camPos.Y, oz - camPos.Z).Values;
                rpi.RenderMesh(mesh.Value);
            }

            prog.Stop();

        }
    }
}
