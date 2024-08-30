using signals.src.signalNetwork;
using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace signals.src.circuits.components
{
    class BEBehaviorCircuitHolder : BlockEntityBehavior
    {
        public VoxelCircuit Circuit;

        long listenerId;
        CircuitBoardRenderer renderer;
        BlockFacing facing;
        BlockFacing orientation;
        Block block;
        BlockPos Pos;

        public BEBehaviorCircuitHolder(BlockEntity entity) : base(entity)
        {
            Circuit = new VoxelCircuit(16, 16, 16, this.Blockentity);
        }

        #region Block Entity Behavior
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            Circuit.Initialize(api);
            listenerId = Blockentity.RegisterGameTickListener(Update, 50);
            block = Blockentity.Block;
            //facing = BlockFacing.FromCode(block?.LastCodePart(0)?.ToString());
            //orientation = BlockFacing.FromCode(block?.LastCodePart(1)?.ToString());
            facing = BlockFacing.FromCode(properties["side"]?.AsString("down"));
            orientation = BlockFacing.FromCode(properties["orientation"]?.AsString("north"));

            if (api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = (ICoreClientAPI)api;
                capi.Event.RegisterRenderer(renderer = new CircuitBoardRenderer(this.Blockentity.Pos, facing, orientation, capi), EnumRenderStage.Opaque, "circuitboard");
                renderer.RegenCircuitMesh(Circuit);
            }
        }


        public override void OnBlockRemoved()
        {
            if (renderer != null)
            {
                renderer.Dispose();
                renderer = null;
            }
            Circuit?.Remove();
            base.OnBlockRemoved();
        }


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            renderer?.Dispose();
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            //dsc.AppendLine(Lang.Get("Available Wiring Voxels: {0}", 42));
            //Todo maybe test if the player is aiming at the right block
            int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
            BlockPos pos = forPlayer.CurrentBlockSelection.Position;
            Cuboidf[] boxes = block.GetSelectionBoxes(this.Api.World.GetBlockAccessor(false, false, false), pos);
            if (index >= boxes.Length) return;
            Vec3i voxelPos = new Vec3i((int)(16 * boxes[index].X1), (int)(16 * boxes[index].Y1), (int)(16 * boxes[index].Z1));
            Circuit.GetBlockInfo(voxelPos, dsc);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            Circuit.FromTreeAttributes(tree.GetTreeAttribute("circuit"), worldForResolving);
            Blockentity.MarkDirty(true);

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


        public void Update(float dt){}

        #region Interactions
        internal Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ItemStack holdingItemStack = null)
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi == null) return null;
            VoxelCircuit.EnumCircuitSelectionType selType;


            Item heldItem = holdingItemStack?.Item;
            Vec3i compSize = CircuitMod.GetCircuitComponentSizeFromItem(capi, heldItem);

            if (heldItem?.Code?.ToString() == "signals:el_wire")
            {
                selType = VoxelCircuit.EnumCircuitSelectionType.PlaceWire;
            }
            else if (compSize != null)
            {
                selType = VoxelCircuit.EnumCircuitSelectionType.PlaceComponent;
            }
            else
            {
                selType = VoxelCircuit.EnumCircuitSelectionType.PlaceNothing;
            }



            Cuboidf[] boxes = Circuit?.GetSelectionBoxes(compSize, selType);

            if (selType == VoxelCircuit.EnumCircuitSelectionType.PlaceNothing)
            {
                Array.Resize(ref boxes, boxes.Length + 1);
                boxes[boxes.Length - 1] = new Cuboidf(0, 0, 0, 1, 1f / 16, 1);
            }

            //-----------
            Cuboidf[] rotatedBoxes = new Cuboidf[boxes.Length];
            Vec3f rotation = SignalsUtils.FacingToRotation(orientation, facing);
            for (int i = 0; i < boxes.Length; i++)
            {
                rotatedBoxes[i] = boxes[i].RotatedCopy(rotation.X, rotation.Y, rotation.Z, new Vec3d(0.5d, 0.5, 0.5));
            }
            return rotatedBoxes;
        }

        internal Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            return new Cuboidf[] { };
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
            RotateFromBEtoCircuit(ref hitPos2, ref selectionFacing, new Vec3f(8, 8, 8));

            Vec3i voxelPos = new Vec3i((int)Math.Floor(hitPos2.X), (int)Math.Floor(hitPos2.Y), (int)Math.Floor(hitPos2.Z));

            Cuboidf box = boxes[blockSel.SelectionBoxIndex];
            Vec3i voxelBoxPos = new Vec3i((int)Math.Floor(box.MinX * 16), (int)Math.Floor(box.MinY * 16), (int)Math.Floor(box.MinZ * 16));
            OnUseOver(byPlayer, voxelPos, voxelBoxPos, selectionFacing, mouseBreakMode);

        }

        private void RotateFromBEtoCircuit(ref Vec3f vector, ref BlockFacing facing, Vec3f center)
        {
            Vec3f rotation = SignalsUtils.FacingToRotation(this.orientation, this.facing);
            if (vector != null)
            {
                SignalsUtils.RotateVector(ref vector, -rotation.X, 0, 0, center);
                SignalsUtils.RotateVector(ref vector, 0, -rotation.Y, 0, center);
                SignalsUtils.RotateVector(ref vector, 0, 0, -rotation.Z, center);
            }
            if(facing != null)
            {
                facing = facing.FaceWhenRotatedBy(-rotation.X * GameMath.DEG2RAD, 0, 0);
                facing = facing.FaceWhenRotatedBy(0, -rotation.Y * GameMath.DEG2RAD, 0);
                facing = facing.FaceWhenRotatedBy(0, 0, -rotation.Z * GameMath.DEG2RAD);
            }

        }
        private void RotateFromCircuittoBE(ref Vec3f vector, ref BlockFacing facing, Vec3f center)
        {
            Vec3f rotation = SignalsUtils.FacingToRotation(this.orientation, this.facing);
            if (vector != null)
            {
                SignalsUtils.RotateVector(ref vector, 0, 0, rotation.Z, center);
                SignalsUtils.RotateVector(ref vector, 0, rotation.Y, 0, center);
                SignalsUtils.RotateVector(ref vector, rotation.X, 0, 0, center);
            }
            if (facing != null)
            {
                facing = facing.FaceWhenRotatedBy(0, 0, rotation.Z * GameMath.DEG2RAD);
                facing = facing.FaceWhenRotatedBy(0, rotation.Y * GameMath.DEG2RAD, 0);
                facing = facing.FaceWhenRotatedBy(rotation.X * GameMath.DEG2RAD, 0, 0);
            }
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

            Circuit.OnUseOver(byPlayer, voxelHitPos, voxelBoxPos, facing, itemStack, mouseBreakMode);
            Api.World.PlaySoundAt(new AssetLocation("signals:sounds/buzz_short"), Pos.X, Pos.Y, Pos.Z);
            Blockentity.MarkDirty();

        }

        #endregion

        #region network


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
        }

        //When the server recieve a packet from a client
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
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

        public ISignalNode GetNodeAt(NodePos pos)
        {
            throw new NotImplementedException();
        }

        public Vec3f GetNodePosinBlock(NodePos pos)
        {
            Vec3f vec = Circuit.getNodePosinBlock(pos);
            BlockFacing dummy = null;
            RotateFromCircuittoBE(ref vec, ref dummy, new Vec3f(0.5f, 0.5f, 0.5f));
            return vec;
        }

        public Dictionary<NodePos, ISignalNode> GetNodes()
        {
            throw new NotImplementedException();
        }

        public enum EnumBECircuitPacket
        {
            networkUpdate = 1001,
            OnUserOver = 1002,
            networkModification = 1003
        }
        #endregion


    }

}
