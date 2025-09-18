using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ContextualMenuManipulator = ContextualMenuPlayer.ContextualMenuManipulator;

namespace RuntimeNodeGraph.UI
{
    public class NodeGraphEditorView : MonoBehaviour
    {
        public const string NodeElementNamePrefix = "node-";

        private class PortInfo
        {
            public string PortId { get; set; }
            public string NodeId { get; set; }
            public PortDirection Direction { get; set; }
            public string PortType { get; set; }
        }

        [SerializeField] protected UIDocument _uiDocument;

        protected VisualElement _root;
        protected VisualElement _graphContainer;
        
        private readonly NodeConnectionLineRenderer _connectionLineRenderer = new();
        
        private float _width;
        private float _height;
        
        // Dragging
        private bool isDragging = false;
        private Vector2 dragStartPosition;
        private VisualElement currentDragElement;
        private string currentDragNodeId;
        
        // Node Connection
        private bool _isConnecting = false;
        private string _startPortId;
        private string _startNodeId;
        private PortDirection _startPortDirection;
        private string _startPortType;

        public bool IsInitialized
        {
            get
            {
                if (_root == null || _graphContainer == null)
                {
                    Debug.LogError($"NodeGraphEditorView is not initialized. Please ensure the UIDocument is set and Awake method has been called.");
                    return false;
                }
                return _root != null && _graphContainer != null;
            }
        }

        // Events
        public event Action<string, string, string, string> ConnectionCompleted;
        public event Action<string, System.Numerics.Vector2> NodePositionChanged;
        public event Action<string> RemoveNodeRequested;
        public event Action<string> RemoveConnectionRequested;

        protected virtual void Awake()
        {
            _root = _uiDocument.rootVisualElement;
            _root.AddManipulator(new ContextualMenuManipulator());

            _graphContainer = _root.Q<VisualElement>("graph-container");
            _graphContainer = _root.Q<VisualElement>("graph-container");

            _graphContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _graphContainer.RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuPopulate);
            _graphContainer.generateVisualContent += GenerateVisualContent;
        }

        protected virtual void OnDestroy()
        {
            _graphContainer.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _graphContainer.UnregisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuPopulate);
            _graphContainer.generateVisualContent -= GenerateVisualContent;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            _width = evt.newRect.width;
            _height = evt.newRect.height;
        }

        private void OnContextualMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            if (_connectionLineRenderer.TryGetConnectionAtPoint(evt.localMousePosition, out var connectionId))
            {
                evt.menu.AppendAction("Delete Connection", action =>
                {
                    RemoveConnectionRequested?.Invoke(connectionId);
                });
            }
        }

        private void GenerateVisualContent(MeshGenerationContext context)
        {
            _connectionLineRenderer.Draw(context);
        }

        protected virtual string GetNodeCustomClassName(NodeData node)
        {
            return null;
        }

        protected virtual VisualElement GetNodeCustomContent(NodeData node)
        {
            return null;
        }

        protected virtual void OnNodeContextualMenuPopulate(NodeData node, ContextualMenuPopulateEvent evt)
        {
        }

        protected virtual void OnNodeRemoved(string nodeId)
        {
        }

        public void Clear()
        {
            _connectionLineRenderer.Clear();
            _graphContainer.Clear();
            _graphContainer.Clear();
        }
        
        public void CreateNodeElement(NodeData node, bool isDeletable)
        {
            if (!IsInitialized) return;

            // Header
            var title = new Label(node.Name);
            title.AddToClassList("node-title");

            var header = new VisualElement();
            header.AddToClassList("node-header");
            header.Add(title);

            // Content
            var content = new VisualElement();
            content.AddToClassList("node-content");

            // Add custom content if provided
            var customContent = GetNodeCustomContent(node);
            if (customContent != null)
            {
                content.Add(customContent);
            }

            foreach (var port in node.InputPorts)
            {
                content.Add(CreatePortElement(port, node.Id));
            }
            foreach (var port in node.OutputPorts)
            {
                content.Add(CreatePortElement(port, node.Id));
            }

            // Node Element
            var nodeElement = new VisualElement();
            nodeElement.name = $"{NodeElementNamePrefix}{node.Id}";
            nodeElement.AddToClassList("node");
            nodeElement.transform.position = new Vector3(node.NormalizedPosition.X, node.NormalizedPosition.Y, 0);

            // Add custom class if provided
            var customClassName = GetNodeCustomClassName(node);
            if (!string.IsNullOrEmpty(customClassName))
            {
                nodeElement.AddToClassList(customClassName);
            }

            nodeElement.Add(header);
            nodeElement.Add(content);
            _graphContainer.Add(nodeElement);

            // Add context menu
            nodeElement.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                if (isDeletable)
                {
                    evt.menu.AppendAction("Delete Node", action =>
                    {
                        RemoveNodeRequested?.Invoke(node.Id);
                    });
                }
                
                // Add custom context menu items if provided
                OnNodeContextualMenuPopulate(node, evt);
                
                evt.StopPropagation();
            });

            // Add drag operation
            nodeElement.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    isDragging = true;
                    currentDragElement = nodeElement;
                    currentDragNodeId = node.Id;
                    dragStartPosition = new Vector2(evt.position.x, evt.position.y)
                                        - new Vector2(nodeElement.transform.position.x, nodeElement.transform.position.y);
                    nodeElement.CapturePointer(evt.pointerId);
                }
            });
            nodeElement.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (isDragging && currentDragElement == nodeElement)
                {
                    var newPosition = new Vector2(evt.position.x, evt.position.y) - dragStartPosition;
                    nodeElement.transform.position = new Vector3(newPosition.x, newPosition.y, 0);
                    NodePositionChanged?.Invoke(currentDragNodeId, newPosition.ToSystemNumericsVector2());
                }
            });
            nodeElement.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (isDragging && currentDragElement == nodeElement)
                {
                    isDragging = false;
                    currentDragElement = null;
                    nodeElement.ReleasePointer(evt.pointerId);
                }
            });
        }
        
        public void RemoveNodeElement(string nodeId)
        {
            var nodeElement = _graphContainer.Q<VisualElement>($"{NodeElementNamePrefix}{nodeId}");
            if (nodeElement != null)
            {
                nodeElement.RemoveFromHierarchy();
                OnNodeRemoved(nodeId);
            }
        }
        
        public void AddConnectionElement(ConnectionData connection)
        {
            if (!IsInitialized) return;

            var outputPortElement = _root.Q<VisualElement>($"connector-{connection.OutputPortId}");
            var inputPortElement = _root.Q<VisualElement>($"connector-{connection.InputPortId}");
            
            if (outputPortElement == null || inputPortElement == null) return;
            
            var startPoint = new Vector2(outputPortElement.worldBound.center.x - _graphContainer.worldBound.x, 
                                        outputPortElement.worldBound.center.y - _graphContainer.worldBound.y);
            var endPoint = new Vector2(inputPortElement.worldBound.center.x - _graphContainer.worldBound.x, 
                                    inputPortElement.worldBound.center.y - _graphContainer.worldBound.y);
            
            _connectionLineRenderer.AddConnection(connection.Id, startPoint, endPoint);
            _graphContainer.MarkDirtyRepaint();
        }
        
        public void RemoveConnectionElement(string connectionId)
        {
            _connectionLineRenderer.RemoveConnection(connectionId);
            _graphContainer.MarkDirtyRepaint();
        }
        
        public void UpdateConnectionPositions(List<ConnectionData> connections)
        {
            if (!IsInitialized) return;

            foreach (var connection in connections)
            {
                var outputPortElement = _root.Q<VisualElement>($"connector-{connection.OutputPortId}");
                var inputPortElement = _root.Q<VisualElement>($"connector-{connection.InputPortId}");

                if (outputPortElement == null || inputPortElement == null) continue;

                var startPoint = new Vector2(outputPortElement.worldBound.center.x - _graphContainer.worldBound.x,
                                           outputPortElement.worldBound.center.y - _graphContainer.worldBound.y);
                var endPoint = new Vector2(inputPortElement.worldBound.center.x - _graphContainer.worldBound.x,
                                        inputPortElement.worldBound.center.y - _graphContainer.worldBound.y);

                _connectionLineRenderer.UpdateConnection(connection.Id, startPoint, endPoint);
            }
            
            _graphContainer.MarkDirtyRepaint();
        }
        
        private VisualElement CreatePortElement(PortData port, string nodeId)
        {
            var portElement = new VisualElement();
            portElement.name = $"port-{port.Id}";
            portElement.AddToClassList("port");

            var direction = port.Direction == PortDirection.Input ? "input" : "output";
            portElement.style.flexDirection = port.Direction == PortDirection.Input ? FlexDirection.Row : FlexDirection.RowReverse;

            var connector = new VisualElement();
            connector.name = $"connector-{port.Id}";
            connector.AddToClassList($"port-{direction}");
            
            // ポートタイプに基づくスタイルクラスを追加
            if (!string.IsNullOrEmpty(port.PortType))
            {
                connector.AddToClassList($"port-type-{port.PortType.ToLower()}");
            }

            connector.userData = new PortInfo 
            { 
                PortId = port.Id, 
                NodeId = nodeId, 
                Direction = port.Direction,
                PortType = port.PortType 
            };

            var label = new Label(port.DisplayName);
            label.AddToClassList("port-label");

            portElement.Add(connector);
            portElement.Add(label);

            connector.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    evt.StopPropagation();
                    StartConnectionDrag(port.Id, nodeId, port.Direction, port.PortType, evt);
                }
            });

            return portElement;
        }
        
        
        private void StartConnectionDrag(string portId, string nodeId, PortDirection direction, string portType, PointerDownEvent evt)
        {
            _isConnecting = true;
            _startPortId = portId;
            _startNodeId = nodeId;
            _startPortDirection = direction;
            _startPortType = portType;
            
            var startPortElement = _root.Q<VisualElement>($"connector-{portId}");
            var startRect = startPortElement.worldBound;
            var startPoint = new Vector2(startRect.center.x - _graphContainer.worldBound.x,
                                       startRect.center.y - _graphContainer.worldBound.y);
            
            _connectionLineRenderer.StartTemporaryConnection(startPoint, direction);
            
            _root.RegisterCallback<PointerMoveEvent>(UpdateTemporaryConnection);
            _root.RegisterCallback<PointerUpEvent>(CompleteConnection);
            _root.CapturePointer(evt.pointerId);
            
            _graphContainer.MarkDirtyRepaint();
        }
        
        private void UpdateTemporaryConnection(PointerMoveEvent evt)
        {
            if (!_isConnecting) return;
            
            var mousePos = new Vector2(evt.position.x - _graphContainer.worldBound.x,
                                     evt.position.y - _graphContainer.worldBound.y);
            
            _connectionLineRenderer.UpdateTemporaryConnection(mousePos);
            _graphContainer.MarkDirtyRepaint();
            
            HighlightCompatiblePort(evt.position);
        }
        
        private void CompleteConnection(PointerUpEvent evt)
        {
            if (!_isConnecting) return;
            
            _root.ReleasePointer(evt.pointerId);
            _root.UnregisterCallback<PointerMoveEvent>(UpdateTemporaryConnection);
            _root.UnregisterCallback<PointerUpEvent>(CompleteConnection);
            
            var targetElement = _root.panel.visualTree.Query<VisualElement>()
                .Where(e => e.ClassListContains("port-input") || e.ClassListContains("port-output"))
                .Where(e => e.worldBound.Contains(evt.position))
                .First();
            
            if (targetElement != null && targetElement.userData is PortInfo targetPortInfo)
            {
                if (_startPortDirection != targetPortInfo.Direction && _startNodeId != targetPortInfo.NodeId)
                {
                    string outputNodeId, outputPortId, inputNodeId, inputPortId;
                    
                    if (_startPortDirection == PortDirection.Output)
                    {
                        outputNodeId = _startNodeId;
                        outputPortId = _startPortId;
                        inputNodeId = targetPortInfo.NodeId;
                        inputPortId = targetPortInfo.PortId;
                    }
                    else
                    {
                        outputNodeId = targetPortInfo.NodeId;
                        outputPortId = targetPortInfo.PortId;
                        inputNodeId = _startNodeId;
                        inputPortId = _startPortId;
                    }
                    
                    ConnectionCompleted?.Invoke(outputNodeId, outputPortId, inputNodeId, inputPortId);
                }
            }
            
            _connectionLineRenderer.EndTemporaryConnection();
            _graphContainer.MarkDirtyRepaint();
            ClearPortHighlights();
            
            _isConnecting = false;
        }
                
        private void HighlightCompatiblePort(UnityEngine.Vector2 position)
        {
            ClearPortHighlights();
            
            var targetElement = _root.panel.visualTree.Query<VisualElement>()
                .Where(e => e.ClassListContains("port-input") || e.ClassListContains("port-output"))
                .Where(e => e.worldBound.Contains(position))
                .First();
            
            if (targetElement != null && targetElement.userData is PortInfo portInfo)
            {
                if (_startPortDirection != portInfo.Direction && _startNodeId != portInfo.NodeId)
                {
                    // ポートタイプの互換性チェック
                    var isCompatible = _startPortType == portInfo.PortType ||
                                        string.IsNullOrEmpty(_startPortType) ||
                                        string.IsNullOrEmpty(portInfo.PortType);

                    if (isCompatible)
                    {
                        targetElement.AddToClassList("port-highlight");
                    }
                }
            }
        }
        
        private void ClearPortHighlights()
        {
            var highlightedPorts = _root.Query(null, "port-highlight").ToList();
            foreach (var port in highlightedPorts)
            {
                port.RemoveFromClassList("port-highlight");
            }
        }
    }
}
