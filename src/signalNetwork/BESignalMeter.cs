using signals.src.transmission;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    class BESignalMeter : BlockEntity, IBESignalReceptor
    {
        byte signalLevel;
        BESignalMeterRenderer renderer;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Client)
            {
                string type = "wall";
                string orientation = BlockFacing.NORTH.Code;
                Block.Variant.TryGetValue("type", out type);
                Block.Variant.TryGetValue("orientation", out orientation);

                renderer = new BESignalMeterRenderer(api as ICoreClientAPI, Pos, GenMesh(), type, BlockFacing.FromCode(orientation));
                (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "signalmeter");
            }
        }

        private void UpdateRenderer()
        {
            if(renderer == null) return;
            renderer.AngleRad = signalLevel*-2*3.14f/16f;
        }

        internal MeshData GenMesh()
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            if (block.BlockId == 0) return null;

            MeshData mesh;
            ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;
            IAsset asset = Api.Assets.TryGet("signals:shapes/block/signalmeter_needle.json");
            //IAsset asset = Api.Assets.TryGet("game:shapes/block/wood/echochamber-needle.json");
            if(asset == null) return null;
            mesher.TesselateShape(block, asset.ToObject<Shape>(), out mesh);
            return mesh;
        }

        public void OnValueChanged(NodePos pos, byte value)
        {
            if((pos.blockPos == Pos) && (pos.index == 0))
            {
                signalLevel = value;
                MarkDirty();
            }
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

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            byte[] values = tree.GetBytes("signalLevel", new byte[1]{0});
            signalLevel = values[0];
            if (worldForResolving.Side == EnumAppSide.Client && this.Api != null)
            {
                UpdateRenderer();
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
