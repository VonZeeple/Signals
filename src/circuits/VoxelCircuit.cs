﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src
{


    //Represent an element (diode, resistor...)
    class CircuitElement
    {

        //CircuitComponent component
        Vec3i position;
        bool hasbeenupdated = false;
        public CircuitElement(Vec3i pos)
        {
            this.position = pos;
        }




    }
    //The circuit contains all electrical components and wires
    public class VoxelCircuit
    {

        public MeshData Mesh;
        public int AvailableWireVoxels = 0;
        public CircuitComponent[] components = new CircuitComponent[] { new CircuitComponent(new Vec3i(3,6,3),new Vec3i(5, 1, 5)) };
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
            
            if (api.Side == EnumAppSide.Client)
            {

                GenerateSelectionVoxelBoxes();
            }
        }



        #region Circuit simulation

        public void updateFromNeighbor()
        {

        }

        internal void UpdateClientSide(List<Tuple<int, bool>> updatedNetworks)
        {
            foreach (Tuple<int,bool> tuple in updatedNetworks)
            {
                if (!wiring.networks.ContainsKey(tuple.Item1)) continue;
                wiring.networks[tuple.Item1].state = tuple.Item2;
            }
        }

        public List<Tuple<int, bool>> updateSimulation()
        {

            if (api.Side == EnumAppSide.Client) return null;

            List<Tuple<int, bool>> updatedNetworks = new List<Tuple<int, bool>>();

            foreach(CircuitComponent comp in components)
            {
                foreach(Vec3i outPutPos in comp.outputPos())
                {
                    int? netId = wiring.GetNetworkAtPos(outPutPos)?.id;
                    if (netId.GetValueOrDefault(-1) >= 0)
                    {
                        wiring.networks[netId.GetValueOrDefault(-1)].nextState = comp.getOutput();
                    }
                    
                }
            }

            
            foreach(int key in wiring.networks.Keys)
            {
                if (wiring.networks[key].Update())
                {
                    updatedNetworks.Add(new Tuple<int, bool>(key, wiring.networks[key].state));
                }
            }

            return updatedNetworks;
        }

        #endregion
 


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
                foreach (CircuitComponent comp in components)
                {
                    boxes.Add(comp.GetSelectionBox());
                }

            selectionBoxesVoxels = boxes.ToArray();
        }

        public void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (tree == null) return;
            Byte[] bytes = tree.GetBytes("wiring");
            if(bytes != null)
            {
                wiring = VoxelWire.deserialize(bytes);
            }
            AvailableWireVoxels = tree.TryGetInt("availableWireVoxels").GetValueOrDefault(0);


            GenerateSelectionVoxelBoxes();
            
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


        //Get info for the itemblock
        public static void GetCircuitInfo(StringBuilder dsc, ITreeAttribute tree)
        {
            int n_voxels = VoxelWire.GetNumberOfVoxelsFromBytes(tree.GetBytes("wiring"));
            dsc.AppendLine(Lang.Get("Wire voxels: {0}", n_voxels));
            //TODO: number of each components
            //TODO: implement circuit naming
        }
        public void GetBlockInfo(Vec3i pos, StringBuilder dsc)
        {
            int? networkId = wiring?.GetNetworkAtPos(pos)?.id;
            bool? networkState = wiring?.GetNetworkAtPos(pos)?.state;
            dsc.AppendLine(Lang.Get("Available Wire voxels: {0}", AvailableWireVoxels));
            dsc.AppendLine(Lang.Get("Networks count: {0}", wiring?.networks.Count));
            if(networkId != null) dsc.AppendLine(Lang.Get("Network id: {0}", networkId));
            if (networkId != null) dsc.AppendLine(Lang.Get("Network state: {0}", networkState));

        }


    }
}
