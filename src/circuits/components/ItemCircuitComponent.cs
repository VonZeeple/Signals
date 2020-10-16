using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace signals.src.circuits
{
    public class ItemCircuitComponent : Item
    {
        SkillItem[] toolModes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if(api is ICoreClientAPI capi)
            {
                toolModes = ObjectCacheUtil.GetOrCreate(api, "ItemCircuitCompModes", () =>
                 {
                     SkillItem[] modes = new SkillItem[2];
                     modes[0] = new SkillItem() { Code = new AssetLocation("rotate"), Name = Lang.Get("rotate") };
                     modes[1] = new SkillItem() { Code = new AssetLocation("change"), Name = Lang.Get("change") };

                     return modes;
                 });
            }
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            for (int i = 0; toolModes != null && i < toolModes.Length; i++)
            {
                toolModes[i]?.Dispose();
            }
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            return slot.Itemstack.Attributes.GetInt("toolMode");
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }

    }
}
