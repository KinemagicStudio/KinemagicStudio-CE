using System;
using System.Numerics;

namespace RuntimeNodeGraph.UI
{
    public class NodeGraphEditorPresenter : IDisposable
    {
        private readonly NodeGraphEditor _graphEditor;
        private readonly NodeGraphEditorView _graphEditorView;

        protected NodeGraphEditorPresenter()
        {
        }

        public NodeGraphEditorPresenter(NodeGraphEditor graphEditor, NodeGraphEditorView graphEditorView)
        {
            _graphEditor = graphEditor;
            _graphEditorView = graphEditorView;
        }

        public virtual void Initialize()
        {
            _graphEditor.NodeAdded += OnNodeAdded;
            _graphEditor.NodeRemoved += OnNodeRemoved;
            _graphEditor.ConnectionAdded += OnConnectionAdded;
            _graphEditor.ConnectionRemoved += OnConnectionRemoved;

            _graphEditorView.NodePositionChanged += OnNodePositionChanged;
            _graphEditorView.ConnectionCompleted += OnConnectionCompleted;
            _graphEditorView.RemoveNodeRequested += OnRemoveNodeRequested;
            _graphEditorView.RemoveConnectionRequested += OnRemoveConnectionRequested;
        }

        public virtual void Dispose()
        {
            _graphEditor.NodeAdded -= OnNodeAdded;
            _graphEditor.NodeRemoved -= OnNodeRemoved;
            _graphEditor.ConnectionAdded -= OnConnectionAdded;
            _graphEditor.ConnectionRemoved -= OnConnectionRemoved;

            _graphEditorView.NodePositionChanged -= OnNodePositionChanged;
            _graphEditorView.ConnectionCompleted -= OnConnectionCompleted;
            _graphEditorView.RemoveNodeRequested -= OnRemoveNodeRequested;
            _graphEditorView.RemoveConnectionRequested -= OnRemoveConnectionRequested;
        }
        
        private void OnNodeAdded(NodeData node)
        {
            _graphEditorView.CreateNodeElement(node, isDeletable: false);
        }
        
        private void OnNodeRemoved(NodeData node)
        {
            _graphEditorView.RemoveNodeElement(node.Id);
        }
        
        private void OnConnectionAdded(ConnectionData connection)
        {
            _graphEditorView.AddConnectionElement(connection);
        }

        private void OnConnectionRemoved(string connectionId)
        {
            _graphEditorView.RemoveConnectionElement(connectionId);
        }

        private void OnNodePositionChanged(string nodeId, Vector2 position)
        {
            _graphEditor.UpdateNodePosition(nodeId, position);
            _graphEditorView.UpdateConnectionPositions(_graphEditor.Graph.Connections);
        }

        private void OnConnectionCompleted(string outputNodeId, string outputPortId, string inputNodeId, string inputPortId)
        {
            _graphEditor.CreateConnection(outputNodeId, outputPortId, inputNodeId, inputPortId);
        }

        private void OnRemoveNodeRequested(string nodeId)
        {
            _graphEditor.RemoveNode(nodeId);
        }

        private void OnRemoveConnectionRequested(string connectionId)
        {
            _graphEditor.RemoveConnection(connectionId);
        }
    }
}
