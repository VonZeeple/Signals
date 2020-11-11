using signals.src.signalNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src.transmission
{


    /// <summary>
    /// The most basic element of a signal network
    /// </summary>
    public interface ISignalNode
    {
        NodePos Pos{get; set;}
        List<Connection> Connections { get; }

        bool isSource { get;}

        byte value { get; set;}

    }

    public class BaseNode : ISignalNode
    {
        public byte output = 15;
        List<Connection> con = new List<Connection>();
        public NodePos Pos { get; set; }
        public List<Connection> Connections { get => con; }
        public bool isSource { get => output>0; }
        public byte value { get ; set; }
    }

}
