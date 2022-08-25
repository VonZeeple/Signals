using signals.src.circuits;
using signals.src.circuits.components;
using signals.src.signalNetwork;
using signals.src.transmission;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo( "signals",
	Description = "To be added",
	Website     = "",
	Authors     = new []{ "unknown" } )]

namespace signals.src
{
    public class SignalsMod : ModSystem
    {
        static string MODID = "signals";
        ICoreAPI api;

        IServerNetworkChannel serverChannel;
        IClientNetworkChannel clientChannel;

        private IDictionary<string, Type> CircuitComponentsRegistry = new Dictionary<string, Type>();
        public override void Start(ICoreAPI api)
        {
            this.api = api;
            base.Start(api);

            api.RegisterBlockClass("BlockBreadboard", typeof(BlockCircuitBoard));
            api.RegisterBlockClass("BlockSignalConnection", typeof(BlockConnection));
            api.RegisterBlockClass("BlockSwitch", typeof(BlockSwitch));
            api.RegisterBlockClass("BlockPressurePlate", typeof(BlockPressurePlate));
            api.RegisterBlockClass("BlockDelay", typeof(BlockDelay));
            api.RegisterBlockClass("BlockSignalMeter", typeof(BlockSignalMeter));
            api.RegisterBlockClass("BlockBuzzer", typeof(BlockBuzzer));


            api.RegisterBlockEntityClass("BlockEntityLightBulb", typeof(BlockEntityLightBulb));
            api.RegisterBlockEntityClass("BlockEntitySwitch", typeof(BESwitch));
            api.RegisterBlockEntityClass("BlockEntityValve", typeof(BEValve));
            api.RegisterBlockEntityClass("BEPressurePlate", typeof(BEPressurePlate));
            api.RegisterBlockEntityClass("BESignalMeter", typeof(BESignalMeter));
            api.RegisterBlockEntityClass("BEDelay", typeof(BEDelay));
            api.RegisterBlockEntityClass("BEBuzzer", typeof(BEBuzzer));

            api.RegisterBlockBehaviorClass("BlockBehaviorCoverWithDirection", typeof(BlockBehaviorCoverWithDirection));

            api.RegisterBlockEntityBehaviorClass("BEBehaviorCircuitHolder", typeof(BEBehaviorCircuitHolder));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorSignalProvider", typeof(BEBehaviorSignalNodeProvider));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorSignalConnector",typeof(BEBehaviorSignalConnector));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorSignalSwitch", typeof(BEBehaviorSignalSwitch));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorSignalValve", typeof(BEBehaviorSignalValve));

            RegisterCircuitComponentClass("valve", typeof(CircuitComponentValve));
            RegisterCircuitComponentClass("source", typeof(CircuitComponentSource));
            RegisterCircuitComponentClass("resistor", typeof(CircuitComponentResistor));

        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.World.Logger.EntryAdded += OnClientLogEntry;
        }

        private void OnClientLogEntry(EnumLogType logType, string message, params object[] args)
        {
            if (logType == EnumLogType.VerboseDebug) return;
            System.Diagnostics.Debug.WriteLine("[Client " + logType + "] " + message, args);
        }

        public Type GetCircuitComponentClass(string key)
        {
            if (!CircuitComponentsRegistry.ContainsKey(key)) return null;
            return CircuitComponentsRegistry[key];
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

        public Type getCircuitComponentType(string code)
        {
            if (!CircuitComponentsRegistry.ContainsKey(code)) return null;
            return CircuitComponentsRegistry[code];

        }
    }
}
