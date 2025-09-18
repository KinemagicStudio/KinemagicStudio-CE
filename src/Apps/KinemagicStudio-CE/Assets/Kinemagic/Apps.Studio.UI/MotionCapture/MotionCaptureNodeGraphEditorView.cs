using System.Collections.Generic;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using RuntimeNodeGraph;
using RuntimeNodeGraph.UI;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.MotionCapture
{
    public sealed class MotionCaptureNodeGraphEditorView : NodeGraphEditorView
    {
        private readonly Dictionary<string, VisualElement> _nodeStatusContainerElements = new();

        public void UpdateDataSourceStatus(int dataSourceId, MotionDataSourceStatus status)
        {
            if (!IsInitialized) return;

            var nodeElementNamePrefix = NodeElementNamePrefix;
            var dataSourceNodeIdPrefix = MotionCaptureNodeGraphEditor.DataSourceNodeIdPrefix;

            var nodeElement = _root.Q<VisualElement>($"{nodeElementNamePrefix}{dataSourceNodeIdPrefix}{dataSourceId}");
            if (nodeElement != null && _nodeStatusContainerElements.TryGetValue($"{dataSourceNodeIdPrefix}{dataSourceId}", out var statusContainer))
            {
                statusContainer.RemoveFromClassList("status-not-started");
                statusContainer.RemoveFromClassList("status-in-progress");
                statusContainer.RemoveFromClassList("status-stalled");
                statusContainer.RemoveFromClassList("status-completed");
                statusContainer.AddToClassList(GetStatusContainerClass(status.ProcessingStatus));

                var statusLabel = statusContainer.Q<Label>(className: "status-label");
                statusLabel.text = GetStatusDisplayText(status.ProcessingStatus);
            }
        }

        protected override VisualElement GetNodeCustomContent(NodeData node)
        {
            if (node is DataSourceNode dataSourceNode)
            {
                var container = new VisualElement();
                container.AddToClassList("node-info-container");

                var statusContainer = new VisualElement();
                statusContainer.AddToClassList("status-container");

                var statusLabel = new Label(GetStatusDisplayText(dataSourceNode.Status));
                statusLabel.AddToClassList("status-label");

                statusContainer.Add(statusLabel);
                container.Add(statusContainer);

                // Server info
                var serverInfo = new VisualElement();
                serverInfo.AddToClassList("server-info");

                // TODO: Delete
                // if (dataSourceNode.DataSourceType == MotionDataSourceType.iFacialMocap ||
                //     dataSourceNode.DataSourceType == MotionDataSourceType.FaceMotion3d)
                // {
                //     var addressLabel = new Label($"Address: {dataSourceNode.ServerAddress}");
                //     addressLabel.AddToClassList("info-label");
                //     serverInfo.Add(addressLabel);
                // }

                var portLabel = new Label($"Port: {dataSourceNode.Port}");
                portLabel.AddToClassList("info-label");

                serverInfo.Add(portLabel);
                container.Add(serverInfo);

                _nodeStatusContainerElements[node.Id] = statusContainer;
                return container;
            }

            return base.GetNodeCustomContent(node);
        }

        protected override void OnNodeRemoved(string nodeId)
        {
            _nodeStatusContainerElements.Remove(nodeId);
        }

        private string GetStatusContainerClass(ProcessingStatus status)
        {
            return status switch
            {
                ProcessingStatus.NotStarted => "status-not-started",
                ProcessingStatus.InProgress => "status-in-progress",
                ProcessingStatus.Stalled => "status-stalled",
                ProcessingStatus.Completed => "status-completed",
                _ => "status-unknown"
            };
        }

        private string GetStatusDisplayText(ProcessingStatus status)
        {
            return status switch
            {
                ProcessingStatus.NotStarted => "Not Started",
                ProcessingStatus.InProgress => "In Progress",
                ProcessingStatus.Stalled => "Stalled",
                ProcessingStatus.Completed => "Completed",
                _ => "Unknown"
            };
        }
    }
}
