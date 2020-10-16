using System;
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

    //The circuit contains all electrical components and wires
    public class VoxelCircuit
    {

        public MeshData Mesh;
        public int AvailableWireVoxels = 0;
        public List<CircuitComponent> components = new List<CircuitComponent>();
        public VoxelWiring wiring;//Contains list of wires

        
        //All the selection boxes avalaible for a given component
        //Plus selection boxes of placed components
        Cuboidf[] selectionBoxesVoxels = new Cuboidf[0];

        Vec3i currentSelectionSize = new Vec3i(1,1,1);
        EnumCircuitSelectionType currentSelectionType;
        bool selectionBoxesDidChanged = true;

        ICoreAPI api;

        public VoxelCircuit()
        {
            wiring = new VoxelWiring();
        }

        public void Initialize(ICoreAPI api)
        {
            this.api = api;
            if (api.Side == EnumAppSide.Client)
            {

            }
        }



        #region Circuit simulation

        public void updateFromNeighbor()
        {

        }

        internal void UpdateClientSide(List<Tuple<int, byte>> updatedNetworks)
        {
            foreach (Tuple<int,byte> tuple in updatedNetworks)
            {
                if (!wiring.networks.ContainsKey(tuple.Item1)) continue;
                wiring.networks[tuple.Item1].state = tuple.Item2;
            }
        }

        private byte getStateAtPos(Vec3i pos)
        {
            VoxelWire net = wiring.GetNetworkAtPos(pos);
            if (net == null) return 0;
            return net.state;
        }
        public List<Tuple<int, byte>> updateSimulation(float dt)
        {

            if (api.Side == EnumAppSide.Client) return null;

            List<Tuple<int, byte>> updatedNetworks = new List<Tuple<int, byte>>();

            foreach(CircuitComponent comp in components)
            {
                Vec3i[] inPos = comp.GetInputPositions();
                byte[] inputs = inPos.Select(x => getStateAtPos(x)).ToArray();
                comp.SetInputs(inputs);
                comp.Update(dt);
            }
            foreach(CircuitComponent comp in components)
            {
                Vec3i[] outPos = comp.GetOutputPositions();
                for (int i=0;i<outPos.Length;i++)
                {
                    int? netId = wiring.GetNetworkAtPos(outPos[i])?.id;
                    if (netId.GetValueOrDefault(-1) >= 0)
                    {
                        if (wiring.networks[netId.GetValueOrDefault(-1)].nextState < comp.GetOutputs()[i])
                        {
                            wiring.networks[netId.GetValueOrDefault(-1)].nextState = comp.GetOutputs()[i];
                        }
                    }
                    
                }
            }

            
            foreach(int key in wiring.networks.Keys)
            {
                if (wiring.networks[key].Update())
                {
                    updatedNetworks.Add(new Tuple<int, byte>(key, wiring.networks[key].state));
                }
            }

            return updatedNetworks;
        }

        #endregion
 


        #region voxel modification

        public void OnUseOver(IPlayer byPlayer, Vec3i voxelHitPos, Vec3i voxelBoxPos, BlockFacing facing, ItemStack itemStack, bool mouseBreakMode)
        {


            Item heldItem = itemStack?.Item;
            if (heldItem?.Code?.ToString() == "signals:el_wire")
                    {
                        if (mouseBreakMode)
                        {
                            if (wiring.OnRemove(voxelHitPos))
                            {
                                AvailableWireVoxels++;
                                if (AvailableWireVoxels >= 25)
                                {
                                    byPlayer.InventoryManager.TryGiveItemstack(new ItemStack(heldItem));
                                    AvailableWireVoxels = 0;
                                }
                            }
                            return;
                        }


                        if (canPlaceWireAt(voxelHitPos.AddCopy(facing)))
                        {

                            if (AvailableWireVoxels == 0)
                            {
                                AvailableWireVoxels = 25;
                                //slot.TakeOut(1);
                                //slot.MarkDirty();
                            }
                            wiring.OnAdd(voxelHitPos.AddCopy(facing));
                            AvailableWireVoxels--;     
                        }
                return;
            }


                        CircuitComponent comp = SignalsUtils.GetCircuitComponentFromItem(api, heldItem);
                        if (comp != null)
                        {
                            comp.Pos = voxelBoxPos.Clone();
                            components.Add(comp);

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

        public enum EnumCircuitSelectionType
        {
            PlaceWire,
            PlaceComponent,
            PlaceNothing
        }

        public Cuboidf[] GetCurrentSelectionBoxes()
        {
            return this.selectionBoxesVoxels;
        }

        internal Cuboidf[] GetSelectionBoxes(Vec3i forSize, EnumCircuitSelectionType selType, IPlayer forPlayer = null)
        {
            if(currentSelectionType == selType && ((forSize?.Equals(currentSelectionSize)).GetValueOrDefault(false) || (forSize == null && currentSelectionSize == null)) && !selectionBoxesDidChanged)
            {
                return selectionBoxesVoxels;
            }
            
            switch(selType)
            {
                case EnumCircuitSelectionType.PlaceWire:
                    GenerateSelectionVoxelBoxes(new Vec3i(1, 1, 1), true, true, true, false);
                    break;
                case EnumCircuitSelectionType.PlaceComponent:
                    GenerateSelectionVoxelBoxes(forSize, false, false, true, true);
                    break;
                case EnumCircuitSelectionType.PlaceNothing:
                    GenerateSelectionVoxelBoxes(new Vec3i(1, 1, 1), false, true, true, false);
                    break;
            }

            currentSelectionSize = forSize;
            currentSelectionType = selType;
            selectionBoxesDidChanged = false;
            return selectionBoxesVoxels;
        }


        //For a wire, should be either boxes at y = 0 with no wire above or boxes at placed wires and components

        public void GenerateSelectionVoxelBoxes(Vec3i size, bool board = false, bool placedWires = false, bool placedComponents = false, bool freeCompPlaces = false)
        {
            HashSet<Cuboidf> boxes = new HashSet<Cuboidf>();


            float sx = (float)size.X / 16f;
            float sy = (float)size.Y / 16f;
            float sz = (float)size.Z / 16f;

            Vec3i obstaclePos;

            //We first generate selectionboxes on the board (to add components where there is nothing)
            if (board || freeCompPlaces)
            {
                for (int i = 0; i <= 16-size.X; i++)
                {
                    for (int j = 0; j <= 16-size.Z; j++)
                    {
                        obstaclePos = GotElementInVolume(i, 1, j, size);
                        if ( obstaclePos == null) 
                        {

                            float px = (float)i / 16;
                            float pz = (float)j / 16;

                            if (board) boxes.Add(new Cuboidf(px, 0, pz, px + sx, 1f / 16, pz + sz));
                            if (freeCompPlaces) boxes.Add(new Cuboidf(px, 1f/16, pz, px + sx, 1f / 16+sy, pz + sz));

                            continue;
                        };

                        i += (obstaclePos.X-i);
                        j += (obstaclePos.Z-j);
                    }
                }
            }

            //We now add add a selection box of components and where there is a wire voxel
            if (placedWires)
            {
                for (int i = 0; i < 16; i++)
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
            }
            //We now add the selection boxes of the other components
            if (placedComponents)
            {
                foreach (CircuitComponent comp in components)
                {
                    boxes.Add(comp.GetSelectionBox());
                }
            }
            selectionBoxesVoxels = boxes.ToArray();
        }


        /// <summary>
        /// Find any wire or component in the volume, returns its position. Returns null of no element was found
        /// </summary>
        private Vec3i GotElementInVolume(int x,int y, int z, Vec3i size)
        {
            for(int i = x; i < x + size.X; i++)
            {
                for(int j = y; j < y + size.Y; j++){
                    for (int k = z; k < z + size.Z; k++)
                    {
                        if (wiring.gotWireAtPos(i, j, k)) return new Vec3i(i,j,k);

                    }
                }
            }
            return null;
        }

        public void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (tree == null) return;
            Byte[] bytes = tree.GetBytes("wiring");
            if(bytes != null)
            {
                wiring = VoxelWiring.deserialize(bytes);
            }
            AvailableWireVoxels = tree.TryGetInt("availableWireVoxels").GetValueOrDefault(0);
            ComponentsFromTree(ref components, tree.GetTreeAttribute("components"), worldForResolving);
            selectionBoxesDidChanged = true;
        }

        public void ToTreeAttributes(ITreeAttribute tree)
        {
            if(wiring != null)
            {
                tree.SetBytes("wiring", wiring.serialize());
            }
            tree.SetInt("availableWireVoxels", AvailableWireVoxels);

            ITreeAttribute comps = new TreeAttribute();
            ComponentsToTree(components, comps);
            tree["components"] = comps;
            
        }

        private void ComponentsFromTree(ref List<CircuitComponent> comps, ITreeAttribute tree, IWorldAccessor world)
        {
            ICoreAPI api = world.Api;
            if (tree == null || api == null) return;
            SignalsMod mod = api.ModLoader.GetModSystem<SignalsMod>();

            foreach (ITreeAttribute compTree in tree.Values)
            {
                string className = compTree.GetString("class", null);
                Type type = mod.getCircuitComponentType(className);
                if (type == null) continue;
                CircuitComponent newComponent = (CircuitComponent)Activator.CreateInstance(type);
                newComponent.FromTreeAtributes(compTree, api.World);
                comps.Add(newComponent);
            }

        }
        private void ComponentsToTree(List<CircuitComponent> comps, ITreeAttribute tree)
        {
            for(int i = 0; i < comps.Count; i++)
            {
                ITreeAttribute compTree = new TreeAttribute();
                comps[i].ToTreeAttributes(compTree);
                tree[i.ToString()] = compTree;
            }
            
        }
   


        #endregion


        //Get info for the itemblock
        public static void GetCircuitInfo(StringBuilder dsc, ITreeAttribute tree)
        {
            int n_voxels = VoxelWiring.GetNumberOfVoxelsFromBytes(tree.GetBytes("wiring"));
            dsc.AppendLine(Lang.Get("Wire voxels: {0}", n_voxels));
            //TODO: number of each components
            //TODO: implement circuit naming
        }
        public void GetBlockInfo(Vec3i pos, StringBuilder dsc)
        {
            int? networkId = wiring?.GetNetworkAtPos(pos)?.id;
            byte? networkState = wiring?.GetNetworkAtPos(pos)?.state;
            dsc.AppendLine(Lang.Get("Available Wire voxels: {0}", AvailableWireVoxels));
            dsc.AppendLine(Lang.Get("Number of wires: {0}", wiring?.networks.Count));
            if(networkId != null) dsc.AppendLine(Lang.Get("Wire id: {0}", networkId));
            if (networkId != null) dsc.AppendLine(Lang.Get("Wire state: {0}", networkState));

        }


    }
}
