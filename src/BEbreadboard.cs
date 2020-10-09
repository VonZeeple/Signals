using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

    //The block entity
    public class BEBreadboard : BlockEntity, IBlockEntityRotatable
    {

        public VoxelCircuit Circuit = new VoxelCircuit();

        long listenerId;
        CircuitBoardRenderer renderer;

        public override void Initialize(ICoreAPI api)
        {
            this.Api = api;
            base.Initialize(api);
            Circuit.Initialize(api);
            listenerId = RegisterGameTickListener(Update, 100);


            if (api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = (ICoreClientAPI)api;
                capi.Event.RegisterRenderer(renderer = new CircuitBoardRenderer(Pos, capi), EnumRenderStage.Opaque, "circuitboard");
            }
        }

        private void UpdateRenderer()
        {
            
        }

        public void Update(float dt)
        {
            Circuit?.updateSimulation();
        }

        public void OnUseOver(IPlayer byPlayer, BlockSelection blockSel, bool mouseBreakMode)
        {
            //From the hit position and the face we can infere a voxel position without using selection box index
            //It works but could be refined
            Vec3f hitPos = blockSel.HitPosition.ToVec3f().Mul(16);
            BlockFacing facing = blockSel.Face;
            //We translate using the normal to avoid rounding and precision issues
            Vec3f hitPos2 = hitPos.AddCopy(facing.Normalf.NormalizedCopy().Mul(-0.5f));

            Vec3i voxelPos = new Vec3i((int)Math.Floor(hitPos2.X), (int)Math.Floor(hitPos2.Y), (int)Math.Floor(hitPos2.Z));

            OnUseOver(byPlayer, voxelPos, facing, mouseBreakMode);

        }

        public void OnUseOver(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool mouseBreakMode)
        {
            // Send a custom network packet for server side, because
            // serverside blockselection index is inaccurate
            if (Api.Side == EnumAppSide.Client)
            {
                SendUseOverPacket(byPlayer, voxelPos, facing, mouseBreakMode);
                return;
            }

            Circuit.OnUseOver(byPlayer, voxelPos, facing, mouseBreakMode);
            //Api.World.PlaySoundAt(new AssetLocation("signals:buzz_short"), Pos.X, Pos.Y, Pos.Z);
            Api.World.PlaySoundAt(new AssetLocation("signals:sounds/buzz_short"), Pos.X, Pos.Y, Pos.Z);
            MarkDirty();
            
        }

        internal static MeshData CreateMeshForItem(ICoreClientAPI capi, ITreeAttribute tree)
        {
            VoxelCircuit circuit = new VoxelCircuit();
            circuit.FromTreeAttributes(tree.GetTreeAttribute("circuit"), capi.World);
            CircuitBoardRenderer renderer = new CircuitBoardRenderer(null, capi);
            renderer.RegenCircuitMesh(circuit);
            return renderer.getMeshForItem();
        }


        //Notifies the server that the block have been interacted with
        public void SendUseOverPacket(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool mouseMode)
        {
            byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(voxelPos.X);
                writer.Write(voxelPos.Y);
                writer.Write(voxelPos.Z);
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


        //When the server recieve a packet from a client
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if(packetid == (int)EnumBECircuitPacket.OnUserOver)
            {
                Vec3i voxelPos;
                bool mouseMode;
                BlockFacing facing;
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    voxelPos = new Vec3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                    mouseMode = reader.ReadBoolean();
                    facing = BlockFacing.ALLFACES[reader.ReadInt16()];

                }

                Api.World.Logger.Notification("ok got use over packet from {0} at pos {1}", player.PlayerName, voxelPos);
                OnUseOver(player, voxelPos, facing, mouseMode);
            }
        }


        public override void OnBlockRemoved()
        {
            if (renderer != null)
            {
                renderer.Dispose();
                renderer = null;
            }
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            renderer?.Dispose();
        }

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

        //Rotatable entity
        public void OnTransformed(ITreeAttribute tree, int degreeRotation, EnumAxis? flipAxis)
        {
            return;
        }

 

        internal Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            Item held_item = capi?.World?.Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Item;
            return Circuit?.GetSelectionBoxes();
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


        public enum EnumBECircuitPacket
        {
            //SelectRecipe = 1001,
            OnUserOver = 1002,
            //CancelSelect = 1003
        }
    }
}
