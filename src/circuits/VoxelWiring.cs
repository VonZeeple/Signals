using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace signals.src
{

    public class VoxelWire
    {
        public int id;
        public HashSet<ushort> voxelpositions;
        static ushort SIZE = 16;
        public byte state = 0;
        public byte nextState = 0;
        public bool asUpdated;

        public VoxelWire(int id) {
            this.id = id;
            voxelpositions = new HashSet<ushort>();
          }

        
        // return true if something as changed
        public bool Update()
        {
            bool flag = false;
            if (nextState != state)
            {
                state = nextState;
                flag = true;
            }
            nextState = 0;
            return flag;
        }

        public VoxelWire(int id, List<Vec3i> voxelPos)
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

        public VoxelWire(int id, ushort[] voxelPos)
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

        public void MergeWith(VoxelWire net)
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
    public class VoxelWiring
    {
        bool hasChanged;
        public Dictionary<int, VoxelWire> networks;
        private int SIZEX;
        private int SIZEY;

        public VoxelWiring(int sizex, int sizey)
        {
            networks = new Dictionary<int, VoxelWire>();
            hasChanged = true;
        }

        public bool gotWireAtPos(int x, int y, int z)
        {
            foreach(VoxelWire net in networks.Values){
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

        public int GetWireIdAtPos(Vec3i voxelPos)
        {
            foreach (VoxelWire net in networks.Values)
            {
                if (net.GotVoxelAtPos(voxelPos))
                {
                    return net.id;
                }
            }
            return -1;
        }

        private VoxelWire GetWireAtPos(Vec3i voxelPos)
        {
            foreach (VoxelWire net in networks.Values)
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
            foreach(VoxelWire net in networks.Values)
            {
                if (net.GotVoxelAtPos(voxelPos)) return false;
            }


            //We explore adjacent voxels, looking for a network
            //if a network is found, we take the id. if another network is found with different id, we merge the networks.
            VoxelWire current_net = null;
            foreach (BlockFacing face in BlockFacing.ALLFACES)
            {
                Vec3i pos2 = voxelPos.AddCopy(face);
                VoxelWire net = GetWireAtPos(pos2);
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
                int new_id = getNewId();
                VoxelWire network = new VoxelWire(new_id);
                network.AddVoxel(voxelPos);
                networks.Add(new_id, network);
            }

            hasChanged = true;
            return true;
        }

        private int getNewId()
        {
            if(networks.Count == 0)
            {
                return 1;
            }
            return networks.Keys.Max()+1;
        }

        public List<Vec3i> RebuildNetwork(Vec3i pos_init, VoxelWire old_network)
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
            foreach (VoxelWire net in networks.Values)
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
                            int newId = getNewId();
                            networks.Add(newId, new VoxelWire(newId, voxels));
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


        static public int GetNumberOfVoxelsFromBytes(byte[] data)
        {
            if (data == null) return 0;
            int quantity = 0;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                try
                {

                    int num_networks = reader.ReadInt32();
                    for (int i = 0; i < num_networks; i++)
                    {
                        int id = reader.ReadInt32();
                        int size = reader.ReadInt32();
                        _ = reader.ReadBoolean();
                        quantity += size;
                        for (int j = 0; j < size; j++)
                        {
                            reader.ReadUInt16();
                        }
                    }

                }
                catch (Exception e)
                {

                }
            }
                return quantity;

        }

        internal byte[] serialize()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(networks.Count);
                foreach (VoxelWire net in networks.Values)
                {
                    writer.Write(net.id);
                    writer.Write(net.voxelpositions.Count);
                    writer.Write(net.state);
                    foreach(ushort pos in net.voxelpositions)
                    {
                        writer.Write(pos);
                    }
                }
                data = ms.ToArray();
            
            
            }
            return data;

        }

        static internal VoxelWiring deserialize(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                VoxelWiring wire = new VoxelWiring(16,16);
                try
                {
                    
                    int num_networks = reader.ReadInt32();
                    for (int i = 0; i < num_networks; i++)
                    {
                        int id = reader.ReadInt32();
                        int size = reader.ReadInt32();
                        byte state = reader.ReadByte();
                        ushort[] voxels = new ushort[size];
                        for (int j = 0; j < size; j++)
                        {
                            voxels[j] = reader.ReadUInt16();
                        }
                        wire.networks.Add(id, new VoxelWire(id, voxels));
                        wire.networks[id].state = state;
                    }

                }
                catch(Exception e)
                {
                    
                }
                    return wire;
                
            }

           

        }
    }
}
