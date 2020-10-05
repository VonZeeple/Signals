﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace signals.src
{
    public class CachedModel
    {
        public MeshRef MeshRef;
        public float Age;
    }
    //We cache models for circuit blocks in a similar way than chisel blocks
    class CircuitBlockModelCache : ModSystem
    {
        Dictionary<long, CachedModel> cachedModels = new Dictionary<long, CachedModel>();
        long nextMeshId = 1;
        ICoreClientAPI capi;
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;

            api.Event.LeaveWorld += Event_LeaveWorld;
            api.Event.RegisterGameTickListener(OnSlowTick, 1000);
        }


        private void OnSlowTick(float dt)
        {
            List<long> toDelete = new List<long>();

            foreach (var val in cachedModels)
            {
                val.Value.Age++;

                if (val.Value.Age > 180)
                {
                    toDelete.Add(val.Key);
                }
            }

            foreach (long key in toDelete)
            {
                cachedModels[key].MeshRef.Dispose();
                cachedModels.Remove(key);
            }
        }


        public MeshRef GetOrCreateMeshRef(ItemStack forStack)
        {
            long meshid = forStack.Attributes.GetLong("meshId", 0);


            if (!cachedModels.ContainsKey(meshid))
            {
                MeshRef meshref = CreateModel(forStack);
                forStack.Attributes.SetLong("meshId", nextMeshId);
                cachedModels[nextMeshId++] = new CachedModel() { MeshRef = meshref, Age = 0 };
                return meshref;
            }
            else
            {
                cachedModels[meshid].Age = 0;
                return cachedModels[meshid].MeshRef;
            }
        }


        private MeshRef CreateModel(ItemStack forStack)
        {
            ITreeAttribute tree = forStack.Attributes;
            if (tree == null) tree = new TreeAttribute();
            MeshData mesh = BEbreadboard.CreateMeshForItem(capi, tree);
            //mesh.Rgba2 = null;

            return capi.Render.UploadMesh(mesh);
        }





        private void Event_LeaveWorld()
        {
            foreach (var val in cachedModels)
            {
                val.Value.MeshRef.Dispose();
            }
        }
    }
}
