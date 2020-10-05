using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace signals.src
{

    class Network
    {
        public int id;
        public HashSet<ushort> voxelpositions;
        static ushort SIZE = 16;
        public Network(int id) {
            this.id = id;
            voxelpositions = new HashSet<ushort>();
          }


        public Network(int id, List<Vec3i> voxelPos)
        {
            this.id = id;
            voxelpositions = new HashSet<ushort>();
            foreach (Vec3i pos in voxelPos)
            {
                if(getIndex(pos) != null)
                {
                    voxelpositions.Add(getIndex(pos).GetValueOrDefault());
                }
            }
        }

        public Network(int id, ushort[] voxelPos)
        {
            this.id = id;
            voxelpositions = new HashSet<ushort>(voxelPos);
        }

        public Vec3i[] getVoxelPos()
        {
            Vec3i[] vectors = voxelpositions.Select(x => getVector(x)).ToArray();
            return vectors;
        }
        private Vec3i getVector(ushort index)
        {
            if(index > ushort.MaxValue){ return null; }
            //There is probablyt a smarter way to do that
            int z = (int)Math.Floor((decimal)index / (16 * 16));
            int y = (int)Math.Floor((decimal)(index-z*16*16)/ 16);
            int x = index - (z * 16 * 16)-(y*16);
            return new Vec3i(x,y,z);
        }
        private ushort? getIndex(Vec3i vec)
        {
            if (!isValidPosition(vec)) return null;
            return (ushort)(vec.X + vec.Y*16+vec.Z*16*16);
        }
        private bool isValidPosition(Vec3i pos)
        {
            return pos.X < SIZE && pos.Y < SIZE && pos.Z < SIZE;
        }
        public bool GotVoxelAtPos(Vec3i pos)
        {
            if (!isValidPosition(pos)) { return false; }
            if (getIndex(pos) == null) { return false; }
            return voxelpositions.Contains(getIndex(pos).GetValueOrDefault());
        }

        public void MergeWith(Network net)
        {
            voxelpositions.AddRange(net.voxelpositions);
        }

        public bool AddVoxel(Vec3i pos)
        {
            if (!isValidPosition(pos)) { return false; }
            ushort? value = getIndex(pos);
            if (value == null) return false;
            if (!voxelpositions.Contains(value.GetValueOrDefault())) 
            { 
                voxelpositions.Add(value.GetValueOrDefault());
                return true;
            }
            return false;
        }

        public bool RemoveVoxel(Vec3i pos)
        {
            if (!isValidPosition(pos)) { return false; }
            ushort? value = getIndex(pos);
            if (value == null) return false;
            if (voxelpositions.Contains(value.GetValueOrDefault()))
            {
                voxelpositions.Remove(value.GetValueOrDefault());
                return true;
            }
            return false;
        }

    }
    //A wire is a special component composed of voxels
    class VoxelWire
    {
        MeshData mesh;
        bool hasChanged;
        public Dictionary<int, Network> networks;  
        private int nextNetworkId;

        public VoxelWire()
        {
            networks = new Dictionary<int, Network>();
            nextNetworkId = 1;
            hasChanged = true;
        }

        public bool gotWireAtPos(int x, int y, int z)
        {
            foreach(Network net in networks.Values){
                if(net.GotVoxelAtPos(new Vec3i(x, y, z)))
                {
                    return true;
                }
            }
            return false;
        }
        public bool gotWireAtPos(Vec3i pos)
        {
            return gotWireAtPos(pos.X, pos.Y, pos.Z);
        }

        public Network GetNetworkAtPos(Vec3i voxelPos)
        {
            foreach (Network net in networks.Values)
            {
                if (net.GotVoxelAtPos(voxelPos))
                {
                    return net;
                }
            }
            return null;
        }

        public bool isValidPosition(int x, int y, int z)
        {
            int SIZE = 16;
            return x < SIZE && y < SIZE && z < SIZE;
        }

        public bool OnAdd(Vec3i voxelPos)
        {
            //We first test if the voxel is already contained in a network
            foreach(Network net in networks.Values)
            {
                if (net.GotVoxelAtPos(voxelPos)) return false;
            }


            //We explore adjacent voxels, looking for a network
            //if a network is found, we take the id. if another network is found with different id, we merge the networks.
            Network current_net = null;
            foreach (BlockFacing face in BlockFacing.ALLFACES)
            {
                Vec3i pos2 = voxelPos.AddCopy(face);
                Network net = GetNetworkAtPos(pos2);
                if (net!=null)
                {
                    if(current_net == null)
                    {
                        net.AddVoxel(voxelPos);
                        current_net = net;
                    }else if(current_net != null && net.id != current_net.id)
                    {
                        current_net.MergeWith(net);
                        networks.Remove(net.id);
                    }
                    else if (net.id == current_net.id)
                    {
                        net.AddVoxel(voxelPos);
                    }
                }
            }
            if(current_net == null)
            {
                //We create a new network
                Network network = new Network(nextNetworkId);
                network.AddVoxel(voxelPos);
                networks.Add(nextNetworkId, network);
                nextNetworkId++;
            }

            hasChanged = true;
            return true;
        }

        public List<Vec3i> RebuildNetwork(Vec3i pos_init, Network old_network)
        {
            if (!old_network.GotVoxelAtPos(pos_init)) return new List<Vec3i>();
            List<Vec3i> to_explore = new List<Vec3i>();
            List<Vec3i> explored = new List<Vec3i>();

            to_explore.Add(pos_init);

            while (to_explore.Any())
            {
                Vec3i current_pos = to_explore.Last();
                foreach (BlockFacing facing in BlockFacing.ALLFACES)
                {
                    Vec3i offset_pos = current_pos.AddCopy(facing);
                    if (old_network.GotVoxelAtPos(offset_pos) && !explored.Contains(offset_pos))
                    {
                        to_explore.Add(offset_pos);
                    }
                }
                to_explore.Remove(current_pos);
                explored.Add(current_pos);
            }

            //At the end, the explored list contains connected voxels
            return explored;
        }

        public bool OnRemove(Vec3i voxelPos)
        {
            foreach (Network net in networks.Values)
            {
                if (net.RemoveVoxel(voxelPos)){
                    hasChanged = true;

                    //Simplest case, where the voxel was the only one left in the network
                    if(net.voxelpositions.Count == 0)
                    {
                        networks.Remove(net.id);
                        return true;
                    }

                    //This is where the fun begins...network exploration!
                    //We first build a new network starting from one face
                    //We then check if voxels on other face are in the previous networks, if not, start a new network etc..
                    List<Vec3i> explored_pos = new List<Vec3i>();
                    foreach (BlockFacing face in BlockFacing.ALLFACES)
                    {
                        if (explored_pos.Contains(voxelPos.AddCopy(face))) continue;
                        
                        List<Vec3i> voxels = RebuildNetwork(voxelPos.AddCopy(face), net);
                        if(voxels.Count > 0)
                        {

                            networks.Add(nextNetworkId, new Network(nextNetworkId, voxels));
                            nextNetworkId++;
                            explored_pos.AddRange(voxels);
                        }
                    }
                    //remove the original network
                    networks.Remove(net.id);
                    return true;
                }
            }
            
            return false;
        }

        internal byte[] serialize()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(networks.Count);
                foreach (Network net in networks.Values)
                {
                    writer.Write(net.voxelpositions.Count);
                    foreach(ushort pos in net.voxelpositions)
                    {
                        writer.Write(pos);
                    }
                }
                data = ms.ToArray();
            
            
            }
            return data;

        }

        static internal VoxelWire deserialize(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                VoxelWire wire = new VoxelWire();
                try
                {
                    
                    int num_networks = reader.ReadInt32();
                    for (int i = 0; i < num_networks; i++)
                    {
                        int id = wire.nextNetworkId;
                        wire.nextNetworkId++;
                        int size = reader.ReadInt32();
                        ushort[] voxels = new ushort[size];
                        for (int j = 0; j < size; j++)
                        {
                            voxels[j] = reader.ReadUInt16();
                        }
                        wire.networks.Add(id, new Network(id, voxels));
                    }

                }
                catch(Exception e)
                {
                    
                }
                    return wire;
                
            }

           

        }

        public MeshData GetMesh(ICoreClientAPI api)
        {
            if (hasChanged || this.mesh == null)
            {
                
                hasChanged = false;
            }
            RegenMesh(api);
            return this.mesh;
        }



        private byte[] getColor(int i)
        {
            byte[][] colors = { new byte[]{ 255, 255, 255 }, new byte[] { 255, 0, 0 }, new byte[] { 0, 255, 0 },
                new byte[]{ 0, 0, 255 }, new byte[]{ 255, 255, 0 }, new byte[]{ 255, 0, 255 }, new byte[]{ 0, 255, 255 }, new byte[]{ 0, 0, 0 } };

            return colors[i % colors.Length];

        }

        private void RegenMesh(ICoreClientAPI api)
        {
            //The final mesh is composed of 1x1x1 cuboids. Not optimal. We need to implement a greedy algo thing to decease the
            //number of cuboids
            Block wireblock = api.World.GetBlock(new AssetLocation("signals:blockwire"));
            if (wireblock == null) return;

            MeshData mesh = new MeshData(24, 36, false);

            float subPixelPaddingx = api.BlockTextureAtlas.SubPixelPaddingX;
            float subPixelPaddingy = api.BlockTextureAtlas.SubPixelPaddingY;

            TextureAtlasPosition tpos = api.BlockTextureAtlas.GetPosition(wireblock, wireblock.Textures.First().Key);

            //We first generate a mesh for a single voxel
            MeshData singleVoxelMesh = CubeMeshUtil.GetCubeOnlyScaleXyz(1 / 32f, 1 / 32f, new Vec3f(1 / 32f, 1 / 32f, 1 / 32f));
            singleVoxelMesh.Rgba = new byte[6 * 4 * 4].Fill((byte)255);
            CubeMeshUtil.SetXyzFacesAndPacketNormals(singleVoxelMesh);

            int texId = tpos.atlasTextureId;


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
            singleVoxelMesh.ClimateColorMapIds = new byte[6];
            singleVoxelMesh.SeasonColorMapIds = new byte[6];
            singleVoxelMesh.ColorMapIdsCount = 6;
            singleVoxelMesh.RenderPasses = new short[singleVoxelMesh.VerticesCount / 4].Fill((short)0);
            singleVoxelMesh.RenderPassCount = singleVoxelMesh.VerticesCount / 4;

            MeshData voxelMeshOffset = singleVoxelMesh.Clone();

            //We now place a mesh at each position
            foreach (Network net in networks.Values)
            {
                byte[] color = getColor(net.id).Append(new byte[] { 255 });

                foreach ( Vec3i vec in net.getVoxelPos())
                {

                    
                    float px = vec.X / 16f;
                    float py = vec.Y / 16f;
                    float pz = vec.Z / 16f;
                    
                    for (int i = 0; i < singleVoxelMesh.xyz.Length; i += 3)
                    {
                        voxelMeshOffset.xyz[i] = px + singleVoxelMesh.xyz[i];
                        voxelMeshOffset.xyz[i + 1] = py + singleVoxelMesh.xyz[i + 1];
                        voxelMeshOffset.xyz[i + 2] = pz + singleVoxelMesh.xyz[i + 2];

                        voxelMeshOffset.Rgba[i/3*4] = color[0];
                        voxelMeshOffset.Rgba[i / 3*4+1] = color[1];
                        voxelMeshOffset.Rgba[i / 3*4+2] = color[2];
                        voxelMeshOffset.Rgba[i / 3*4+3] = color[3];
                    }

                    float offsetX = ((((vec.X + 4 * vec.Y) % 16f / 16f)) * 32f) / api.BlockTextureAtlas.Size.Width;
                    float offsetY = (pz * 32f) / api.BlockTextureAtlas.Size.Height;

                    for (int i = 0; i < singleVoxelMesh.Uv.Length; i += 2)
                    {
                        voxelMeshOffset.Uv[i] = singleVoxelMesh.Uv[i] + offsetX;
                        voxelMeshOffset.Uv[i + 1] = singleVoxelMesh.Uv[i + 1] + offsetY;
                    }

                    mesh.AddMeshData(voxelMeshOffset);

                }
            }
            this.mesh = mesh;

        }



        }
}
