using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace signals.src
{
    class SignalsMod : ModSystem
    {
        static string MODID = "signals";
        ICoreAPI api;


        private IDictionary<string, Type> CircuitComponentsRegistry = new Dictionary<string, Type>();
        public override void Start(ICoreAPI api)
        {
            this.api = api;
            base.Start(api);
            api.RegisterBlockClass("BlockBreadboard", typeof(BlockBreadboard));
            api.RegisterBlockEntityClass("BlockEntityBreadboard", typeof(BEBreadboard));
            
        }

        public void RegisterCircuitComponent(string code, Type type)
        {
            if (type.IsAssignableFrom(typeof(CircuitComponent)))
            {
                CircuitComponentsRegistry.Add(code, type);
            }
            else
            {
                api.World.Logger.Warning("Tried to register class {0} with name {1}, but it doesn't inherit from CircuitComponent", type, code);
            }
            
        }
    }
}
