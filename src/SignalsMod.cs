using signals.src.circuits;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace signals.src
{
    public class SignalsMod : ModSystem
    {
        static string MODID = "signals";
        ICoreAPI api;


        private IDictionary<string, Type> CircuitComponentsRegistry = new Dictionary<string, Type>();
        public override void Start(ICoreAPI api)
        {
            this.api = api;
            base.Start(api);
            api.RegisterBlockClass("BlockBreadboard", typeof(BlockCircuitBoard));
            api.RegisterBlockEntityClass("BlockEntityBreadboard", typeof(BECircuitBoard));

            RegisterCircuitComponentClass("valve", typeof(CircuitComponentValve));


        }

        public Type GetCircuitComponentClass(string key)
        {
            if (!CircuitComponentsRegistry.ContainsKey(key)) return null;
            return CircuitComponentsRegistry[key];
        }
        public void RegisterCircuitComponentClass(string code, Type type)
        {
            if (typeof(CircuitComponent).IsAssignableFrom(type))
            {
                CircuitComponentsRegistry.Add(code, type);
            }
            else
            {
                api.World.Logger.Warning("Tried to register class {0} with name {1}, but it doesn't inherit from CircuitComponent", type, code);
            }
            
        }

        public Type getCircuitComponentType(string code)
        {
            if (!CircuitComponentsRegistry.ContainsKey(code)) return null;
            return CircuitComponentsRegistry[code];

        }
    }
}
