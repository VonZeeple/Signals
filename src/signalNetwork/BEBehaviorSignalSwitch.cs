using signals.src.transmission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.signalNetwork
{
    public class BEBehaviorSignalSwitch : BEBehaviorSignalNodeProvider
    {
        public BEBehaviorSignalSwitch(BlockEntity blockentity) : base(blockentity)
        {

        }

        List<Connection> internalConON = new List<Connection>();
        List<Connection> internalConOFF = new List<Connection>();

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            //initialize the nodes
            base.Initialize(api, properties);

            NodePos pos1 = new NodePos(this.Pos, 0);
            NodePos pos2 = new NodePos(this.Pos, 1);

            ISignalNode node1 = GetNodeAt(pos1);
            ISignalNode node2 = GetNodeAt(pos2);

            if (node1 == null || node2 == null) return;

            node1.Connections.Add(new Connection(pos1, pos2));
            node2.Connections.Add(new Connection(pos2, pos1));

            //return;

            /*JsonObject[] conON = properties["connectionsON"]?.AsArray();
            JsonObject[] conOFF = properties["connectionsOFF"]?.AsArray();

            if (conON != null)
            {
                foreach (JsonObject json in conON)
                {
                    int index1 = json["i1"].AsInt(-1);
                    int index2 = json["i2"].AsInt(-1);
                    if (!IsConnectionValid(index1, index2)) continue;
                    BlockPos pos = this.Blockentity.Pos;
                    NodePos pos1 = new NodePos(pos, index1);
                    NodePos pos2 = new NodePos(pos, index2);

                    Connection con = new Connection(pos1, pos2);
                    
                }
            }*/



        }

        private bool IsConnectionValid(int id1, int id2)
        {
            if (id1 == -1 || id2 == -1) return false;
            if (id1 == id2) return false;
            return true;
        }
    }
}
