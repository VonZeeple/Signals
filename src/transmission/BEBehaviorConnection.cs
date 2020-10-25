using signals.src.hangingwires;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace signals.src.transmission
{

    //Behavior that handles the connection of hanging wires for signal network
    public class BEBehaviorConnection : BlockEntityBehavior
    {

        private SignalNetworkMod Mod;
        private HangingWiresMod WireMod;
        public Dictionary<int, long> NetworkIds;

        public IHangingWireAnchor wireAnchor;
        public ISignalNode[] nodes;

        public BEBehaviorConnection(BlockEntity blockEntity) : base(blockEntity)
        {
            NetworkIds = new Dictionary<int, long>();
            wireAnchor = this.Blockentity.Block as IHangingWireAnchor;
            Mod = Api.ModLoader.GetModSystem<SignalNetworkMod>();
            WireMod = Api.ModLoader.GetModSystem<HangingWiresMod>();

        }


        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            NodePos[] nodePos = wireAnchor?.GetWireAnchors(this.Api.World, this.Blockentity.Pos);
            if (nodePos != null)
            {
                nodes = new ISignalNode[nodePos.Length];
                for(int i = 0; i < nodePos.Length;i++)
                {
                    SignalNode newNode = new SignalNode(nodePos[i]);
                    Connection[] newCon = WireMod.GetAllConnectionsFrom(nodePos[i]);
                    newNode.connectedNodes = newCon.Select(x => x.pos1 == nodePos[i] ? x.pos2 : x.pos1).ToList();
                    newNode.attenuations = new int[newCon.Length].Fill(0).ToList();
                    nodes[i] = newNode;
                }
            }
            
        }

        

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAtributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

        }

    }
}
