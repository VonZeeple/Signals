﻿using signals.src.circuits;
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
        bool updated;
        bool state;
        protected Item nowTesselatingItem;
        protected Shape nowTesselatingShape;
        protected ICoreClientAPI capi;

        public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

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

        virtual public Vec3i[] GetOutputPositions()
        {
            Vec3i pos1 = new Vec3i(3, 0, 1).AddCopy(Pos.X, Pos.Y, Pos.Z);
            Vec3i pos2 = new Vec3i(0, 0, 1).AddCopy(Pos.X, Pos.Y, Pos.Z);
            return new Vec3i[]{ pos1, pos2 };
        }

        virtual public Vec3i[] GetInputPositions()
        {
            return new Vec3i[] { };
        }

        virtual public bool[] GetOutputs()
        {
            return new bool[] { true, true };
        }

        virtual public void SetInputs(bool[] inputs)
        {
            return;
        }

        virtual public void Update(float dt)
        {
            return;
        }

        virtual public void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            string newClassName = tree.GetString("class");
            //if(className != null && newClassName != className)
            this.Pos = new Vec3i(0, 0, 0);
            Pos.X = tree.GetInt("posX", 0);
            Pos.Y = tree.GetInt("posY", 0);
            Pos.Z = tree.GetInt("posZ", 0);
        }

        virtual public void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetString("class", this.className);
            tree.SetInt("posX", Pos.X);
            tree.SetInt("posY", Pos.Y);
            tree.SetInt("posZ", Pos.Z);
        }

        #endregion
    }
}