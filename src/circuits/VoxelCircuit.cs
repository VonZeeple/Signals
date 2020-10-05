using signals.src.circuits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace signals.src
{



    //The circuit contains all electrical components and wires
    class VoxelCircuit
    {

        public MeshData Mesh;
        public int AvailableWireVoxels;
        public CircuitComponent[] components;
        public VoxelWire wiring;
        
        //All the selection boxes avalaible for a given component
        //Plus selection boxes of placed components
        Cuboidf[] selectionBoxesVoxels = new Cuboidf[0];


        ICoreAPI api;

        public VoxelCircuit()
        {
            wiring = new VoxelWire();
        }

        public void Initialize(ICoreAPI api)
        {
            this.api = api;
            if (AvailableWireVoxels == null) AvailableWireVoxels = 0;
 
            GenerateSelectionVoxelBoxes();

            components = new CircuitComponent[] { new CCValve(new Vec3i(5,1,5))
            };

            
            if (api is ICoreClientAPI)
            {
                //updateMeshes();
            }
        }

        #region voxel modification

        public void OnUseOver(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool mouseBreakMode)
        {

            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if(slot.Itemstack?.Item?.Code.ToString() == "signals:el_wire")
            {

                if (mouseBreakMode)
                {
                    if(wiring.OnRemove(voxelPos))
                    {
                        AvailableWireVoxels++;
                        if (AvailableWireVoxels >= 25)
                        {
                            byPlayer.InventoryManager.TryGiveItemstack(new ItemStack(slot.Itemstack.Item));
                            AvailableWireVoxels = 0;
                        }
                    }
                        return;
                }


                if (canPlaceWireAt(voxelPos.AddCopy(facing)))
                {

                    if(AvailableWireVoxels == 0)
                    {
                        AvailableWireVoxels = 25;
                        slot.TakeOut(1);
                        slot.MarkDirty();
                    }
                    wiring.OnAdd(voxelPos.AddCopy(facing));
                    AvailableWireVoxels--; 
                }
                    
            }
            
        }

        public bool canPlaceWireAt(Vec3i voxelPos)
        {
            if (voxelPos.Y == 0) return false;
            if (wiring.gotWireAtPos(voxelPos.X, voxelPos.Y, voxelPos.Z)) return false;
            Cuboidi voxelBox = new Cuboidi(voxelPos, voxelPos.AddCopy(1,1,1));
            foreach(CircuitComponent comp in components)
            {
                if (comp.doesIntersect(voxelBox)) { return false; }
            }
            
            return true;
        }


        #endregion

        #region Rendering

        public MeshData getCircuitMesh(ICoreClientAPI capi)
        {

            MeshData mesh =  wiring.GetMesh(api as ICoreClientAPI);
            foreach (CircuitComponent comp in components) {
                mesh.AddMeshData(comp.getMesh(capi));
                }
            return mesh;

        }




        #endregion
        #region selection box:

        internal Cuboidf[] GetSelectionBoxes(IPlayer forPlayer = null)
        {
            return selectionBoxesVoxels;
        }


        public void GenerateSelectionVoxelBoxes()
        {
            HashSet<Cuboidf> boxes = new HashSet<Cuboidf>();


            float sx = 1f / 16f;
            float sy = 1f / 16f;
            float sz = 1f / 16f;

            //We first generate selectionboxes on the board (to add components where there is nothing)
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    //We check if there is already a wire above
                    if (wiring.gotWireAtPos(i, j, 1)) continue;

                    float px = (float)i / 16;
                    float py = (float)0 / 16;
                    float pz = (float)j / 16;

                    boxes.Add(new Cuboidf(px, py, pz, px + sx, py + sy, pz + sz));
                }
            }

            //We now add add a selection box of components and where there is a wire voxel
            for (int i  = 0; i < 16; i++)
            {
                for (int j = 1; j < 16; j++)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        if (!wiring.gotWireAtPos(i, j, k)) continue;
                        
                        float px = (float)i / 16;
                        float py = (float)j / 16;
                        float pz = (float)k / 16;

                        boxes.Add(new Cuboidf(px, py, pz, px + sx, py + sy, pz + sz));
                    }
                }

            }
            //We now add the selection boxes of the other components
            if (components != null)
            {
                foreach (CircuitComponent comp in components)
                {
                    boxes.Add(comp.GetSelectionBox());
                }
            }

            selectionBoxesVoxels = boxes.ToArray();
        }

        public void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (tree == null) return;
            Byte[] bytes = tree.GetBytes("wiring");
            if(bytes != null)
            {
                wiring.deserialize(bytes);
            }
            AvailableWireVoxels = tree.TryGetInt("availableWireVoxels").GetValueOrDefault(0);
        }

        public void ToTreeAttributes(ITreeAttribute tree)
        {
            if(wiring != null)
            {
                tree.SetBytes("wiring", wiring.serialize());
            }
            tree.SetInt("availableWireVoxels", AvailableWireVoxels);
            
        }


        #endregion


        public void GetBlockInfo(Vec3i pos, StringBuilder dsc)
        {
            int? networkId = wiring?.GetNetworkAtPos(pos)?.id;
            dsc.AppendLine(Lang.Get("Available Wire voxels: {0}", AvailableWireVoxels));
            dsc.AppendLine(Lang.Get("Networks count: {0}", wiring?.networks.Count));
            if(networkId != null) dsc.AppendLine(Lang.Get("Network id: {0}", networkId));
            
        }


    }
}
