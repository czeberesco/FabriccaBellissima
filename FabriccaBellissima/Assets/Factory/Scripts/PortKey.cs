using System;

namespace FabriccaBellissima.Factory
{
    public readonly struct PortKey : IEquatable<PortKey>
    {
        public PortKey(int nodeId, int portId)
        {
            NodeId = nodeId;
            PortId = portId;
        }

        public int NodeId { get; }
        public int PortId { get; }
        public bool Equals(PortKey other) => NodeId == other.NodeId && PortId == other.PortId;
        public override bool Equals(object obj) => obj is PortKey other && Equals(other);
        public override int GetHashCode() => (NodeId * 397) ^ PortId;
    }
}
