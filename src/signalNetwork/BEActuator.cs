using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BEActuator : BlockEntity, IBESignalReceptor
    {
        byte signalLevel = 0;

        BlockFacing Orientation = BlockFacing.NORTH;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if(this.Block.Variant["orientation"] != null){
                BlockFacing facing = BlockFacing.FromCode(this.Block.Variant["orientation"]);
                if(facing != null){
                    Orientation = facing;
                }
                Api.Logger.Notification(this.Block.Variant["orientation"]);
            }
        }

        private void SwapBlock(bool is_on){
            //TODO: put this code in block
            AssetLocation newCode;
            Block block = this.Block;
            try {
                if(is_on){
                    newCode = block.CodeWithVariant("powered", "on");
                }else{
                    newCode = block.CodeWithVariant("powered", "off");
                }
                Block newBlock = this.Api.World.BlockAccessor.GetBlock(newCode);
                this.Api.World.BlockAccessor.ExchangeBlock(newBlock.BlockId, this.Pos);
                this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos);
            }
            catch (Exception){
                this.Api.Logger.Debug("Can't swap actuator block");
            };
        }

        public void OnValueChanged(NodePos pos, byte value)
        {
            if(pos.blockPos == Pos)
            {
                if(signalLevel != value)
                {
                    signalLevel = value;
                    MarkDirty();
                    if(value>0)
                    {
                        Activate();
                        SwapBlock(true);
                    }else{
                    SwapBlock(false);}
                }

            }
        }

        private void Activate()
        {
            TryInteract(Orientation);
        }

        private void TryInteract(BlockFacing facing)
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos.X + facing.Normali.X, Pos.Y + facing.Normali.Y, Pos.Z + facing.Normali.Z);
            if (block != null)
            {
                try
                {
                    var caller = new Caller() {
                        CallerPrivileges = new string[] { "*" },
                        Pos = Pos.ToVec3d(),
                        Type = EnumCallerType.Block
                    };

                    block.Activate(Api.World, caller, new BlockSelection(Pos.AddCopy(facing), facing.Opposite, block));
                }
                catch (Exception e)
                {
                    Api.Logger.Warning("Exception thrown when trying to interact with block {0}: {1}", block.Code.ToShortString(), e);
                }
            }
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            byte[] values = tree.GetBytes("signalLevel", new byte[1]{0});
            signalLevel = values[0];
            if (worldForResolving.Side == EnumAppSide.Client && this.Api != null)
            {
                MarkDirty(true);
            }

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBytes("signalLevel", new byte[1]{signalLevel});
        }
    }
}