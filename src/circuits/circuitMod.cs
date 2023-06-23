

using System;
using System.Collections.Generic;
using signals.src.circuits;
using signals.src.circuits.components;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src
{
    public class CircuitMod : ModSystem
    {
        ICoreAPI api;
        private IDictionary<string, Type> CircuitComponentsRegistry = new Dictionary<string, Type>();

        public override bool ShouldLoad(EnumAppSide side)
        {
            return true;
        }

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            base.Start(api);

            api.RegisterBlockClass("BlockBreadboard", typeof(BlockCircuitBoard));

            api.RegisterBlockEntityBehaviorClass("BEBehaviorCircuitHolder", typeof(BEBehaviorCircuitHolder));

            RegisterCircuitComponentClass("valve", typeof(CircuitComponentValve));
            RegisterCircuitComponentClass("source", typeof(CircuitComponentSource));
            RegisterCircuitComponentClass("resistor", typeof(CircuitComponentResistor));
        }

        public void RegisterCircuitComponentClass(string code, Type type)
        {
            if (typeof(ICircuitComponent).IsAssignableFrom(type))
            {
                CircuitComponentsRegistry.Add(code, type);
            }
            else
            {
                api.World.Logger.Warning("Tried to register class {0} with name {1}, but it doesn't implements ICircuitComponent", type, code);
            }

        }

        public Type GetCircuitComponentClass(string key)
        {
            if (!CircuitComponentsRegistry.ContainsKey(key)) return null;
            return CircuitComponentsRegistry[key];
        }


        public Type getCircuitComponentType(string code)
        {
            if (!CircuitComponentsRegistry.ContainsKey(code)) return null;
            return CircuitComponentsRegistry[code];

        }

        public static CircuitComponent GetCircuitComponentFromItem(ICoreAPI api, Item item)
        {
            if (item == null) return null;
            CircuitMod mod = api.ModLoader.GetModSystem<CircuitMod>();
            JsonObject jsonObj = item.Attributes?["circuitComponent"];
            if (jsonObj == null) return null;
            string className = jsonObj["class"]?.AsString();
            Type type = mod.getCircuitComponentType(className);
            return (CircuitComponent)Activator.CreateInstance(type);

        }
        public static Vec3i GetCircuitComponentSizeFromItem(ICoreAPI api, Item item)
        {
            if (item == null) return null;
            SignalsMod mod = api.ModLoader.GetModSystem<SignalsMod>();
            JsonObject jsonObj = item.Attributes?["circuitComponent"];
            if (jsonObj == null) return null;
            Vec3i size = jsonObj["size"]?.AsObject<Vec3i>();

            return size;
        }
    }
}