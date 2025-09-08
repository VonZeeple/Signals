using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent
{
    public class BlockBehaviorExchangeDuringInteract : BlockBehavior
    {
        AssetLocation blockCodeOn;
        AssetLocation blockCodeOff;
        string onSound;
        string offSound;
        string actionlangcode;

        public BlockBehaviorExchangeDuringInteract(Block block) : base(block)
        {

        }

        public override void Initialize(JsonObject properties)
        {
            string blockCodeOn = properties["onState"].AsString();
            string blockCodeOff = properties["offState"].AsString();

            this.blockCodeOn = AssetLocation.Create(blockCodeOn, block.Code.Domain);
            this.blockCodeOff = AssetLocation.Create(blockCodeOff, block.Code.Domain);

            onSound = properties["onSound"].AsString();
            offSound = properties["offSound"].AsString();
            actionlangcode = properties["actionLangCode"].AsString();
            base.Initialize(properties);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }

            handling = EnumHandling.PreventDefault;

            return DoExchangeOn(world, byPlayer, blockSel.Position);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            return DoExchangeOff(world, byPlayer, blockSel.Position);
        }

        private bool DoExchangeOn(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
        {
            if (!block.WildCardMatch(blockCodeOff)){
                return false;
            }
            AssetLocation loc = block.Code.WildCardReplace(blockCodeOff, blockCodeOn);
            Block nextBlock = world.GetBlock(loc);
            if (nextBlock == null) return false;

            world.BlockAccessor.ExchangeBlock(nextBlock.BlockId, pos);

            if (onSound != null)
            {
                world.PlaySoundAt(new AssetLocation("sounds/" + onSound), pos.X, pos.Y, pos.Z, byPlayer);
            }

            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            return true;
        }

        private bool DoExchangeOff(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
        {
            if (!block.WildCardMatch(blockCodeOn)){
                return false;
            }
            AssetLocation loc = block.Code.WildCardReplace(blockCodeOn, blockCodeOff);
            Block nextBlock = world.GetBlock(loc);
            if (nextBlock == null) return false;

            world.BlockAccessor.ExchangeBlock(nextBlock.BlockId, pos);

            if (offSound != null)
            {
                world.PlaySoundAt(new AssetLocation("sounds/" + offSound), pos.X, pos.Y, pos.Z, byPlayer);
            }
            return true;
        }

        public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs, ref EnumHandling handled)
        {
            if (activationArgs != null && activationArgs.HasAttribute("opened"))
            {
                if (activationArgs.GetBool("opened") == block.Code.Path.Contains("opened")) return;   // do nothing if already in the required state: NOTE this is only effective if the required state is "opened", works for trapdoors but might not work for something else
            }
            DoExchangeOn(world, caller.Player, blockSel.Position);
            //TODO: automatic reset
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handled)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = actionlangcode,
                    MouseButton = EnumMouseButton.Right
                }
            };
        }
    }
}
