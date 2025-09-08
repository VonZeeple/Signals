using signals.src.circuits;
using signals.src.circuits.components;
using signals.src.hangingwires;
using signals.src.signalNetwork;
using signals.src.transmission;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

[assembly: ModInfo("signals",
    "signals",
    Description = "Wires, triodes and more.",
    Website = "",
    Version = "0.1.3",
    Authors = new[] { "PFev" })]

namespace signals.src
{
    public class SignalsMod : ModSystem
    {
        static string MODID = "signals";
        ICoreAPI api;

        IServerNetworkChannel serverChannel;
        IClientNetworkChannel clientChannel;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            base.Start(api);

            api.RegisterBlockClass("BlockSignalConnection", typeof(BlockConnection));
            api.RegisterBlockClass("BlockSwitch", typeof(BlockSwitch));
            api.RegisterBlockClass("BlockPressurePlate", typeof(BlockPressurePlate));
            api.RegisterBlockClass("BlockDelay", typeof(BlockDelay));
            api.RegisterBlockClass("BlockSignalMeter", typeof(BlockSignalMeter));
            api.RegisterBlockClass("BlockBuzzer", typeof(BlockBuzzer));
            api.RegisterBlockClass("BlockButtonSwitch", typeof(BlockButtonSwitch));

            api.RegisterBlockEntityClass("BlockEntityLightBulb", typeof(BlockEntityLightBulb));
            api.RegisterBlockEntityClass("BlockEntitySwitch", typeof(BESwitch));
            api.RegisterBlockEntityClass("BlockEntityValve", typeof(BEValve));
            api.RegisterBlockEntityClass("BEPressurePlate", typeof(BEPressurePlate));
            api.RegisterBlockEntityClass("BESignalMeter", typeof(BESignalMeter));
            api.RegisterBlockEntityClass("BEDelay", typeof(BEDelay));
            api.RegisterBlockEntityClass("BEBuzzer", typeof(BEBuzzer));
            api.RegisterBlockEntityClass("BEScreen", typeof(BEScreen));
            api.RegisterBlockEntityClass("BEButtonSwitch", typeof(BEButtonSwitch));
            api.RegisterBlockEntityClass("BEActuator", typeof(BEActuator));

            api.RegisterBlockBehaviorClass("BlockBehaviorCoverWithDirection", typeof(BlockBehaviorCoverWithDirection));
            api.RegisterBlockBehaviorClass("BlockBehaviorSoundOnActivate", typeof(BlockBehaviorSoundOnActivate));
            api.RegisterBlockBehaviorClass("BlockBehaviorExchangeDuringInteract", typeof(BlockBehaviorExchangeDuringInteract));

            api.RegisterBlockEntityBehaviorClass("BEBehaviorSignalProvider", typeof(BEBehaviorSignalNodeProvider));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorSignalConnector", typeof(BEBehaviorSignalConnector));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorSignalSwitch", typeof(BEBehaviorSignalSwitch));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorSignalValve", typeof(BEBehaviorSignalValve));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorAnemometer", typeof(BEBehaviorAnemometer));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorRiftDetector", typeof(BEBehaviorRiftDetector));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorLightSensor", typeof(BEBehaviorLightSensor));

            api.RegisterCollectibleBehaviorClass("WireCutterBehavior", typeof(WireCutterBehavior));
            api.RegisterItemClass("WireCutterItem", typeof(WireCutterItem));



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
    }
}
