using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace signals.src.signalNetwork
{
    public class BEBehaviorRiftDetector : BlockEntityBehavior
    {
        private ModSystemRiftWeather riftWeather => Api.ModLoader.GetModSystem<ModSystemRiftWeather>();
        public BEBehaviorRiftDetector(BlockEntity blockentity) : base(blockentity) {}
        private string currentCode;

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            if (api.Side == EnumAppSide.Client) {return;}
            initListener();

        }

        private void initListener()
        {
            Blockentity.RegisterGameTickListener(OnSlowServerTick, 1000); 
        }

        private void OnSlowServerTick(float dt)
        {
            currentCode= riftWeather.CurrentPattern.Code;
            Blockentity.MarkDirty();
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            currentCode= tree.GetString("riftWeather", "");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("riftWeather", currentCode);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine(string.Format(Lang.Get("rift-activity-" + currentCode+"\r\n")));
        }
    }
}