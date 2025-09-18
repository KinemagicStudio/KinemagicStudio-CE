using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;

namespace RuntimeNodeGraph
{
    [Serializable]
    public abstract class NodeData // Node?
    {
        public string Id { get; protected set; }
        public string Name { get; set; }
        public Vector2 NormalizedPosition { get; set; }
        public List<PortData> InputPorts = new();
        public List<PortData> OutputPorts = new();
    }

    [Serializable]
    public class PortData // NodePort?
    {
        public string Id;
        public string DisplayName;
        public PortDirection Direction;
        public string PortType;
    }

    public enum PortDirection
    {
        Input,
        Output
    }
}