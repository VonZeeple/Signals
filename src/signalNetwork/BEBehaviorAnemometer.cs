
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace signals.src.signalNetwork
{
    public class BEBehaviorAnemometer : BlockEntityBehavior
    {
        private WeatherSystemServer weatherSystem => Api.ModLoader.GetModSystem<WeatherSystemServer>();
        private BEBehaviorSignalNodeProvider nodeProvider;
        private NodePos sourceNodePos;
        private double currentWindSpeed = 0;
        private BEAnemometerRenderer renderer;
        public BEBehaviorAnemometer(BlockEntity blockentity) : base(blockentity) { }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            if (api.Side == EnumAppSide.Client)
            {
                renderer = new BEAnemometerRenderer(api as ICoreClientAPI, Blockentity.Block, Blockentity.Pos);
                (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "anemometer");
                return;
            }
            if(properties.KeyExists("sourceNodeIndex"))
            {
                int index = properties["sourceNodeIndex"].AsInt();
                sourceNodePos = new NodePos(Blockentity.Pos, index);
            }
            nodeProvider = this.Blockentity.GetBehavior<BEBehaviorSignalNodeProvider>();
            initListener();

        }

      public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            renderer?.Dispose();
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            renderer?.Dispose();
        }

        private void initListener()
        {
            Blockentity.RegisterGameTickListener(OnSlowServerTick, 1000); 
        }

        private byte GetSignalValue()
        {
            if(currentWindSpeed <= 0) {return (byte)0;}
            if(currentWindSpeed > 1.5) {return (byte)15;}
            return (byte) Math.Floor(currentWindSpeed*10);
        }

        private void OnSlowServerTick(float dt)
        {
            
            currentWindSpeed = weatherSystem.WeatherDataSlowAccess.GetWindSpeed(Blockentity.Pos.ToVec3d());
            if(sourceNodePos != null){
                nodeProvider?.UpdateSource(sourceNodePos, GetSignalValue());
                Blockentity.MarkDirty();
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            currentWindSpeed = tree.GetDouble("windValue", 0);
            renderer?.UpdateWindSpeed((float)currentWindSpeed);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("windValue", currentWindSpeed);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine(string.Format(Lang.Get("Wind speed: {0}%", (int)(100*currentWindSpeed))));
        }
    }
}