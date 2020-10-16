﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace signals.src
{

    //The block entity
    public class BECircuitBoard : BlockEntity
    {

        public VoxelCircuit Circuit = new VoxelCircuit();

        long listenerId;
        CircuitBoardRenderer renderer;
        BlockFacing facing;
        BlockFacing orientation;

        public override void Initialize(ICoreAPI api)
        {
            this.Api = api;
            base.Initialize(api);
            Circuit.Initialize(api);
            listenerId = RegisterGameTickListener(Update, 100);
            facing = BlockFacing.FromCode(this.Block?.LastCodePart(0)?.ToString());
            orientation = BlockFacing.FromCode(this.Block?.LastCodePart(1)?.ToString());

            if(this.Block.Id == 0) { api.World.BlockAccessor.RemoveBlockEntity(this.Pos);
                return;
            }

            if (api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = (ICoreClientAPI)api;
                capi.Event.RegisterRenderer(renderer = new CircuitBoardRenderer(Pos, facing, orientation, capi), EnumRenderStage.Opaque, "circuitboard");
                renderer.RegenCircuitMesh(Circuit);
            }
        }

        private void UpdateNetworkStateInRenderer(int id, bool value)
        {
            if (renderer == null) return;
            renderer.UpdateNetworkState(id, value);
        }

       
        public void Update(float dt)
        {
            if (Api.Side == EnumAppSide.Client) return;

            List<Tuple<int, bool>> updatedNetworks = Circuit?.updateSimulation(dt);
            if (updatedNetworks != null)
            {
                if (updatedNetworks.Count > 0)
                {
                    SendUpdatedNetworksPacket(updatedNetworks);
                }
            }
        }

        //Called ClientSide
        public void OnUseOver(IPlayer byPlayer, BlockSelection blockSel, bool mouseBreakMode)
        {
            Cuboidf[] boxes = Circuit.GetCurrentSelectionBoxes();
            //If true, something is wrong with selection boxes or the board was selected.
            if (blockSel.SelectionBoxIndex >= boxes.Length) return;
            
            //From the hit position and the face we can infere a voxel position without using selection box index
            //It works but could be refined

            Vec3f rotation = SignalsUtils.FacingToRotation(this.orientation, this.facing);
            Vec3f hitPos = blockSel.HitPosition.ToVec3f().Mul(16);
            
            BlockFacing selectionFacing = blockSel.Face;
            //We translate using the normal to avoid rounding and precision issues
            Vec3f hitPos2 = hitPos.AddCopy(selectionFacing.Normalf.NormalizedCopy().Mul(-0.5f));

            //We need to apply rotation now
            selectionFacing = selectionFacing.FaceWhenRotatedBy(-rotation.X*GameMath.DEG2RAD, 0, 0);
            selectionFacing = selectionFacing.FaceWhenRotatedBy(0, -rotation.Y * GameMath.DEG2RAD, 0);
            selectionFacing = selectionFacing.FaceWhenRotatedBy(0, 0, -rotation.Z * GameMath.DEG2RAD);
            SignalsUtils.RotateVector(ref hitPos2, -rotation.X, 0, 0, new Vec3f(8, 8, 8));
            SignalsUtils.RotateVector(ref hitPos2, 0, -rotation.Y, 0, new Vec3f(8, 8, 8));
            SignalsUtils.RotateVector(ref hitPos2, 0, 0, -rotation.Z, new Vec3f(8, 8, 8));

            Vec3i voxelPos = new Vec3i((int)Math.Floor(hitPos2.X), (int)Math.Floor(hitPos2.Y), (int)Math.Floor(hitPos2.Z));

            Cuboidf box = boxes[blockSel.SelectionBoxIndex];
            Vec3i voxelBoxPos = new Vec3i((int)Math.Floor(box.MinX*16), (int)Math.Floor(box.MinY*16), (int)Math.Floor(box.MinZ*16));
            OnUseOver(byPlayer, voxelPos, voxelBoxPos, selectionFacing, mouseBreakMode);

        }

        public void OnUseOver(IPlayer byPlayer, Vec3i voxelHitPos, Vec3i voxelBoxPos, BlockFacing facing, bool mouseBreakMode)
        {
            // Send a custom network packet for server side, because
            // serverside blockselection index is inaccurate


            if (Api.Side == EnumAppSide.Client)
            {
                SendUseOverPacket(byPlayer, voxelHitPos, voxelBoxPos, facing, mouseBreakMode);
                return;
            }

            ItemStack itemStack = byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack;

            Circuit.OnUseOver(byPlayer, voxelHitPos, voxelBoxPos, facing,itemStack, mouseBreakMode);
            Api.World.PlaySoundAt(new AssetLocation("signals:sounds/buzz_short"), Pos.X, Pos.Y, Pos.Z);
            MarkDirty();

        }

        public override void OnBlockRemoved()
        {
            if (renderer != null)
            {
                renderer.Dispose();
                renderer = null;
            }
            base.OnBlockRemoved();
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            renderer?.Dispose();
        }





        internal Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ItemStack holdingItemStack = null)
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi == null) return null;
            VoxelCircuit.EnumCircuitSelectionType selType;

            
            Item heldItem = holdingItemStack?.Item;
            Vec3i compSize = SignalsUtils.GetCircuitComponentSizeFromItem(capi, heldItem);

            if(heldItem?.Code?.ToString() == "signals:el_wire")
            {
                selType = VoxelCircuit.EnumCircuitSelectionType.PlaceWire;
            }else if(compSize != null)
            {
                selType = VoxelCircuit.EnumCircuitSelectionType.PlaceComponent;
            }
            else
            {
                selType = VoxelCircuit.EnumCircuitSelectionType.PlaceNothing;
            }
            

            
            Cuboidf[] boxes = Circuit?.GetSelectionBoxes(compSize, selType);

            if(selType == VoxelCircuit.EnumCircuitSelectionType.PlaceNothing)
            {
                Array.Resize(ref boxes, boxes.Length + 1);
                boxes[boxes.Length - 1] = new Cuboidf(0, 0, 0, 1, 1f / 16, 1);
            }

            //-----------
            Cuboidf[] rotatedBoxes = new Cuboidf[boxes.Length];
            Vec3f rotation = SignalsUtils.FacingToRotation(orientation, facing);
            for(int i=0;i<boxes.Length;i++)
            {
                rotatedBoxes[i] = boxes[i].RotatedCopy(rotation.X, rotation.Y, rotation.Z, new Vec3d(0.5d, 0.5, 0.5));
            }
            return rotatedBoxes;
        }

        internal Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            return new Cuboidf[] { };
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            //dsc.AppendLine(Lang.Get("Available Wiring Voxels: {0}", 42));
            //Todo maybe test if the player is aiming at the right block
            int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
            BlockPos pos = forPlayer.CurrentBlockSelection.Position;
            Cuboidf[] boxes = Block.GetSelectionBoxes(this.Api.World.GetBlockAccessor(false, false, false), pos);
            if (index >= boxes.Length) return;
            Vec3i voxelPos = new Vec3i((int)(16 * boxes[index].X1), (int)(16 * boxes[index].Y1), (int)(16 * boxes[index].Z1));
            Circuit.GetBlockInfo(voxelPos, dsc);
        }

        #region data

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);
            Circuit.FromTreeAttributes(tree.GetTreeAttribute("circuit"), worldForResolving);
            MarkDirty(true);

            if (Api == null) return;
            if (Api.Side == EnumAppSide.Client)
            {
                renderer.RegenCircuitMesh(this.Circuit);

            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute circuittree = new TreeAttribute();
            Circuit.ToTreeAttributes(circuittree);
            tree["circuit"] = circuittree;
        }


        #endregion
        #region network

        private void SendUpdatedNetworksPacket(List<Tuple<int, bool>> updatedNetworks)
        {

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                foreach (Tuple<int, bool> item in updatedNetworks)
                {
                    writer.Write(item.Item1);
                    writer.Write(item.Item2);
                }
                data = ms.ToArray();


            }

            IPlayer[] playersInRange = Api.World.GetPlayersAround(Pos.ToVec3d(), 2400, 2400);

            foreach (IPlayer player in playersInRange)
            {
                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(player as IServerPlayer, Pos.X, Pos.Y, Pos.Z,
            (int)EnumBECircuitPacket.networkUpdate,
            data);
            }

        }

        //Notifies the server that the block have been interacted with
        public void SendUseOverPacket(IPlayer byPlayer, Vec3i voxelPos, Vec3i voxelBoxPos, BlockFacing facing, bool mouseMode)
        {
            byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(voxelPos.X);
                writer.Write(voxelPos.Y);
                writer.Write(voxelPos.Z);
                writer.Write(voxelBoxPos.X);
                writer.Write(voxelBoxPos.Y);
                writer.Write(voxelBoxPos.Z);

                writer.Write(mouseMode);
                writer.Write((ushort)facing.Index);
                data = ms.ToArray();
            }

            ((ICoreClientAPI)Api).Network.SendBlockEntityPacket(
                Pos.X, Pos.Y, Pos.Z,
                (int)EnumBECircuitPacket.OnUserOver,
                data
            );
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == (int)EnumBECircuitPacket.networkUpdate)
            {
                List<Tuple<int, bool>> updatedNetworks = new List<Tuple<int, bool>>();
                try
                {
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        while (ms.Position < ms.Length)
                        {
                            int net_id = reader.ReadInt32();
                            bool value = reader.ReadBoolean();

                            updatedNetworks.Add(new Tuple<int, bool>(net_id, value));
                        }


                    }
                }
                catch (Exception e)
                {

                }
                if (updatedNetworks.Count > 0)
                {
                    Circuit.UpdateClientSide(updatedNetworks);
                    foreach (Tuple<int, bool> tuple in updatedNetworks)
                    {
                        renderer?.UpdateNetworkState(tuple.Item1, tuple.Item2);
                    }
                }

            }
        }

        //When the server recieve a packet from a client
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid == (int)EnumBECircuitPacket.OnUserOver)
            {
                Vec3i voxelPos;
                Vec3i voxelBoxPos;
                bool mouseMode;
                BlockFacing facing;

                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    voxelPos = new Vec3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                    voxelBoxPos = new Vec3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                    mouseMode = reader.ReadBoolean();
                    facing = BlockFacing.ALLFACES[reader.ReadInt16()];

                }

                Api.World.Logger.Notification("ok got use over packet from {0} at pos {1}", player.PlayerName, voxelPos);
                OnUseOver(player, voxelPos, voxelBoxPos, facing, mouseMode);
            }


        }

        public enum EnumBECircuitPacket
        {
            networkUpdate = 1001,
            OnUserOver = 1002,
            networkModification = 1003
        }

        #endregion
        #region Rendering

        #endregion
    }
}