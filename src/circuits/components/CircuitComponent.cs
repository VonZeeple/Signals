using signals.src.circuits;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src
{
    public abstract class CircuitComponent: ICircuitComponent, ITexPositionSource
    {

        public CircuitComponent()
        {

        }

        public Vec3i Size;
        public int rotation;
        public Vec3i Pos;
        public string className;
        protected ItemStack itemStack;
        bool updated;
        byte state;
        protected Item nowTesselatingItem;
        protected Shape nowTesselatingShape;
        protected ICoreClientAPI capi;

        public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

        public ItemStack ItemStack { get =>  (itemStack == null)?new ItemStack():itemStack ; set{ itemStack = value; } }

        //Overload of the [] operator
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                AssetLocation texturePath = null;
                CompositeTexture tex;
                if (nowTesselatingItem.Textures.TryGetValue(textureCode, out tex))
                {
                    texturePath = tex.Baked.BakedName;
                }
                else
                {
                    nowTesselatingShape?.Textures.TryGetValue(textureCode, out texturePath);
                }

                if (texturePath == null)
                {
                    texturePath = new AssetLocation(textureCode);
                }

                TextureAtlasPosition texpos = capi.BlockTextureAtlas[texturePath];



                if (texpos == null)
                {
                    IAsset texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                    if (texAsset != null)
                    {
                        BitmapRef bmp = texAsset.ToBitmap(capi);
                        capi.BlockTextureAtlas.InsertTextureCached(texturePath, bmp, out _, out texpos);
                    }
                    else
                    {
                        capi.World.Logger.Warning("Display cased item {0} defined texture {1}, not no such texture found.", nowTesselatingItem.Code, texturePath);
                    }
                }

                return texpos;
            }
        }



        public virtual bool doesIntersect(Cuboidi box)
        {
            return new Cuboidi(Pos, Pos.AddCopy(Size.X,Size.Y,Size.Z)).Intersects(box);
        }


        #region ICircuitComponentImplementation


        //tmp mesh, maybe use a meshref instead
        public virtual MeshData getMesh(ICoreClientAPI inCapi)
        {
            this.capi = inCapi;
            nowTesselatingItem = capi.World.GetItem(new AssetLocation("signals:el_valve"));
            nowTesselatingShape = capi.TesselatorManager.GetCachedShape(nowTesselatingItem.Shape.Base);
            MeshData mesh;
            capi.Tesselator.TesselateItem(nowTesselatingItem, out mesh, this);
            mesh.Translate(new Vec3f(Pos.X / 16f, Pos.Y / 16f, Pos.Z / 16f));

            return mesh;
        }



        //Used to select the component once placed
        public virtual Cuboidf GetSelectionBox()
        {
            return new Cuboidf((float)Pos.X / 16, (float)Pos.Y / 16, (float)Pos.Z / 16, (float)(Pos.X+Size.X) / 16, (float)(Pos.Y + Size.Y) / 16, (float)(Pos.Z + Size.Z) / 16);
        }

        //Used to detect collisions with other components of the circuit
        public virtual Cuboidf GetCollisionBox()
        {
            return new Cuboidf((float)Pos.X / 16, (float)Pos.Y / 16, (float)Pos.Z / 16, (float)(Pos.X + Size.X) / 16, (float)(Pos.Y + Size.Y) / 16, (float)(Pos.Z + Size.Z) / 16);
        }

        //Smaller selection box might be conveninent for placement sometimes, as the mouse cursor tend to be on top of the SB.
        public virtual Cuboidf GetSelectionBoxForPlacement()
        {
            return GetSelectionBox();
        }

        abstract public Vec3i[] GetOutputPositions();

        abstract public Vec3i[] GetInputPositions();

        abstract public byte[] GetOutputs();

        abstract public void SetInputs(byte[] inputs);

        virtual public void Update(float dt)
        {
            return;
        }

        virtual public void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            string newClassName = tree.GetString("class","");
            this.Pos = new Vec3i(0, 0, 0);
            Pos.X = tree.GetInt("posX", 0);
            Pos.Y = tree.GetInt("posY", 0);
            Pos.Z = tree.GetInt("posZ", 0);
            this.ItemStack = tree.GetItemstack("itemStack", new ItemStack());
        }

        virtual public void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetString("class", this.className);
            tree.SetInt("posX", Pos.X);
            tree.SetInt("posY", Pos.Y);
            tree.SetInt("posZ", Pos.Z);
            tree.SetItemstack("itemStack", this.ItemStack);
        }

        virtual public ItemStack GetItemStackOnRemove()
        {
            return this.ItemStack;
        }

        virtual public bool DoesInstantUpdate()
        {
            return false;
        }

        virtual public bool IsSource() {
            return false;
        }

        #endregion
    }
}
