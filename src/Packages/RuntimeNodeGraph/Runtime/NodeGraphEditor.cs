using System;
using System.Linq;
using Vector2 = System.Numerics.Vector2;

namespace RuntimeNodeGraph
{
    /// <summary>
    /// UI非依存のノードグラフ管理クラス
    /// </summary>
    public class NodeGraphEditor
    {
        // private bool _hasUnsavedChanges; // TODO

        public NodeGraph Graph { get; private set; } = new();

        public event Action<NodeData> NodeAdded;
        public event Action<NodeData> NodeRemoved;
        public event Action<ConnectionData> ConnectionAdded;
        public event Action<string> ConnectionRemoved;

        public void CreateNewGraph()
        {
            Graph = new();
        }

        public void AddNode(NodeData node)
        {
            Graph.AddNode(node);
            NodeAdded?.Invoke(node);
        }

        public void RemoveNode(string nodeId)
        {
            var node = Graph.Find(nodeId);
            if (node != null)
            {
                // 関連する接続も削除
                var connectionsToRemove = Graph.FindConnections(nodeId);

                foreach (var connection in connectionsToRemove)
                {
                    RemoveConnection(connection.Id);
                }

                Graph.RemoveNode(node);
                NodeRemoved?.Invoke(node);
            }
        }

        public ConnectionData CreateConnection(string outputNodeId, string outputPortId, string inputNodeId, string inputPortId)
        {
            if (!CanConnect(outputNodeId, outputPortId, inputNodeId, inputPortId))
            {
                return null;
            }

            // 既存の接続をチェック（入力は1つだけ）
            var existingConnection = Graph.Connections.FirstOrDefault(c =>
                c.InputNodeId == inputNodeId && c.InputPortId == inputPortId);

            if (existingConnection != null)
            {
                RemoveConnection(existingConnection.Id);
            }

            var connection = new ConnectionData
            {
                Id = Guid.NewGuid().ToString(),
                OutputNodeId = outputNodeId,
                OutputPortId = outputPortId,
                InputNodeId = inputNodeId,
                InputPortId = inputPortId
            };

            Graph.Connections.Add(connection);
            ConnectionAdded?.Invoke(connection);

            return connection;
        }

        public void RemoveConnection(string connectionId)
        {
            Graph.RemoveConnection(connectionId);
            ConnectionRemoved?.Invoke(connectionId);
        }

        public void UpdateNodePosition(string nodeId, Vector2 position)
        {
            var node = Graph.Find(nodeId);
            if (node != null)
            {
                node.NormalizedPosition = position;
            }
        }

        public bool CanConnect(string outputNodeId, string outputPortId, string inputNodeId, string inputPortId)
        {
            // 同じノード間の接続を防ぐ
            if (outputNodeId == inputNodeId)
            {
                return false;
            }

            // ポートの存在確認
            var outputNode = Graph.Find(outputNodeId);
            var inputNode = Graph.Find(inputNodeId);

            if (outputNode == null || inputNode == null)
            {
                return false;
            }

            var outputPort = outputNode.OutputPorts.FirstOrDefault(p => p.Id == outputPortId);
            var inputPort = inputNode.InputPorts.FirstOrDefault(p => p.Id == inputPortId);

            if (outputPort == null || inputPort == null)
            {
                return false;
            }

            // ポートタイプの互換性チェック
            if (!string.IsNullOrEmpty(outputPort.PortType) && !string.IsNullOrEmpty(inputPort.PortType))
            {
                return outputPort.PortType == inputPort.PortType;
            }

            return true;
        }

        // // TODO
        // public async UniTask SaveGraphAsync(string filePath)
        // {
        //     // グラフをJSON形式で保存する処理を実装
        //     // 例: await File.WriteAllTextAsync(filePath, JsonUtility.ToJson(Graph));
        // }

        // // TODO
        // public async UniTask<NodeGraph> LoadGraphAsync(string filePath)
        // {
        //     // グラフをJSON形式で読み込む処理を実装
        //     // 例: var json = await File.ReadAllTextAsync(filePath);
        //     // return JsonUtility.FromJson<NodeGraph>(json);
        //     return new NodeGraph(); // 仮の実装
        // }
    }
}
