using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

// Part of this code is inspired from VSHUD by Novocain:
// https://github.com/Novocain1/MiscMods/blob/1.15/VSHUD/Renderer/PlacementRenderer.cs

namespace signals.src
{
    public class BlockPrevisualizationMod : ModSystem
    {
        public GhostBlockRenderer renderer;
        public ICoreClientAPI capi;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            renderer = new GhostBlockRenderer(api);
            capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque);
            capi.Event.LevelFinalize += () => api.Shader.ReloadShaders();
        }

    }

    public class GhostBlockRenderer : IRenderer
    {
        public ICoreClientAPI capi;
        IClientPlayer player { get => capi?.World?.Player; }
        BlockSelection selection { get => player?.CurrentBlockSelection; }
        Vec3d camPos { get => player?.Entity.CameraPos; }
        MeshRef mRef;
        bool shouldDispose = true;
        public double RenderOrder => 0.5;
        public int RenderRange => 20;

        public GhostBlockRenderer(ICoreClientAPI api)
        {
            capi = api;
        }

        public void Dispose()
        {
            if (shouldDispose) mRef?.Dispose();
        }

        public void UpdateBlockMesh(Block block, BlockPos altPos)
        {
            ITesselatorManager tes = capi.TesselatorManager;
            MeshData mesh = tes.GetDefaultBlockMesh(block).Clone();
            IRenderAPI rpi = capi.Render;
            if (mRef != null && shouldDispose) mRef.Dispose();
            shouldDispose = true;
            mRef = rpi.UploadMesh(mesh);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            ItemStack istack = player?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            if (istack == null) return;
            Block invBlock = istack?.Block;
            if (invBlock == null) return;
            if (invBlock.GetBehavior<BlockBehaviorCoverWithDirection>() == null) return;
            if (selection == null) return;
            IBlockAccessor ba = capi.World.GetBlockAccessor(false, false, false);
            Block block = ba.GetBlock(selection.Position);
            if (block == null) return;
            BlockPos pos = block.IsReplacableBy(invBlock) ? selection.Position : selection.Position.Offset(selection.Face);
            if (!ba.GetBlock(pos).IsReplacableBy(invBlock)) return;

            Block oBlock = invBlock.GetBehavior<BlockBehaviorCoverWithDirection>()?.GetOrientedBlock(capi.World, selection);
            if (oBlock == null) UpdateBlockMesh(invBlock, pos);
            else UpdateBlockMesh(oBlock, pos);
            if (mRef == null) return;
            //start rendering:
            IRenderAPI rpi = capi.Render;
            rpi.GlToggleBlend(true);
            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.Tex2D = capi.BlockTextureAtlas.AtlasTextures[0].TextureId;
            Matrixf ModelMat = new Matrixf();
            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                .Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            Vec4f col = new Vec4f(1.0f, 1.0f, 1.0f, 0.5f); //last float is for opacity
            prog.SsaoAttn = 0;
            prog.AlphaTest = 0.05f;
            prog.RgbaTint = col.Clone();
            prog.RgbaGlowIn = new Vec4f(col.R, col.G, col.B, 1.0f);
            prog.ExtraGlow = 255 / (int)(capi.World.Calendar.SunLightStrength * 64.0f);
            rpi.RenderMesh(mRef);
            prog.Stop();

        }
    }
}