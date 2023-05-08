using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BEAccuator : BlockEntity, IBESignalReceptor
    {
        byte signalLevel = 0;

        BlockFacing Orientation = BlockFacing.NORTH;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Client)
            {

            }
        }

        public void OnValueChanged(NodePos pos, byte value)
        {
            Api.Logger.Notification("value changed");
            if(pos.blockPos == Pos)
            {
                if(signalLevel != value)
                {
                    signalLevel = value;
                    MarkDirty();
                    if(value>0)
                    {
                        Api.Logger.Notification("activating");
                        Activate();
                    }
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