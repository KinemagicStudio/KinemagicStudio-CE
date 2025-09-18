using System;
using System.Collections.Generic;
using System.Linq;

namespace RuntimeNodeGraph
{
    [Serializable]
    public sealed class NodeGraph
    {
        private readonly List<NodeData> _nodes = new();

        public Guid Id { get; }
        public List<ConnectionData> Connections { get; } = new();
        
        public NodeGraph()
        {
            Id = Guid.NewGuid();
        }

        public NodeGraph(string id)
        {
            Id = Guid.Parse(id);
        }

        public List<NodeData> GetAllNodes()
        {
            return new List<NodeData>(_nodes);
        }

        public void AddNode(NodeData node)
        {
            _nodes.Add(node);
        }

        public void RemoveNode(NodeData node)
        {
            _nodes.Remove(node);
        }

        public NodeData Find(string nodeId)
        {
            return _nodes.Find(node => node.Id == nodeId);
        }

        public PortData FindPort(string portId)
        {
            return _nodes.SelectMany(node => node.InputPorts.Concat(node.OutputPorts)).FirstOrDefault(port => port.Id == portId);
        }

        public List<ConnectionData> FindConnections(string nodeId)
        {
            return Connections.Where(c => c.InputNodeId == nodeId || c.OutputNodeId == nodeId).ToList();
        }

        public void RemoveConnection(string connectionId)
        {
            var connections = Connections.FindAll(c => c.Id == connectionId);
            foreach (var connection in connections)
            {
                Connections.Remove(connection);
            }
        }
    }
}
