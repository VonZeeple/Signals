using signals.src.signalNetwork;
using System.Collections.Generic;

namespace signals.src.transmission
{
    /// <summary>
    /// Node of a signal network.
    /// </summary>
    public interface ISignalNode
    {
        NodePos Pos{get; set;}
        List<Connection> Connections { get; }
        bool isSource { get;}
        byte value { get; set;}
        long? netId {get; set;}
    }

    public class BaseNode : ISignalNode
    {
        public byte output = 15;
        List<Connection> con = new List<Connection>();
        public NodePos Pos { get; set; }
        public List<Connection> Connections { get => con; }
        public bool isSource { get => output>0; }
        public byte value { get ; set; }
        public long? netId { get; set; }
    }
}
