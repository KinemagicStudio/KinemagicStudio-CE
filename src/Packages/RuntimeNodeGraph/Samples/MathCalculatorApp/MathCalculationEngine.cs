using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RuntimeNodeGraph.Samples.MathCalculator
{
    public class MathCalculationEngine
    {
        private Dictionary<string, float?> _nodeValues = new Dictionary<string, float?>();
        
        public Dictionary<string, float?> Calculate(NodeGraph graph)
        {
            _nodeValues.Clear();
            
            // トポロジカルソートでノードを処理順に並べる
            var sortedNodes = TopologicalSort(graph);
            
            // 各ノードを順番に計算
            foreach (var node in sortedNodes)
            {
                CalculateNode(node, graph);
            }
            
            return _nodeValues;
        }
        
        private List<NodeData> TopologicalSort(NodeGraph graph)
        {
            var result = new List<NodeData>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();
            
            // すべてのノードに対してDFSを実行
            var allNodes = graph.GetAllNodes();
            foreach (var node in allNodes)
            {
                if (!visited.Contains(node.Id))
                {
                    Visit(node, graph, visited, visiting, result);
                }
            }
            
            return result;
        }
        
        private void Visit(NodeData node, NodeGraph graph, HashSet<string> visited, 
            HashSet<string> visiting, List<NodeData> result)
        {
            if (visiting.Contains(node.Id))
            {
                throw new InvalidOperationException("Circular dependency detected in the graph");
            }
            
            if (visited.Contains(node.Id))
            {
                return;
            }
            
            visiting.Add(node.Id);
            
            // このノードに接続されている上流のノードを先に訪問
            var upstreamConnections = graph.Connections
                .Where(c => c.InputNodeId == node.Id)
                .ToList();
            
            foreach (var connection in upstreamConnections)
            {
                var upstreamNode = graph.Find(connection.OutputNodeId);
                if (upstreamNode != null)
                {
                    Visit(upstreamNode, graph, visited, visiting, result);
                }
            }
            
            visiting.Remove(node.Id);
            visited.Add(node.Id);
            result.Add(node);
        }
        
        private void CalculateNode(NodeData node, NodeGraph graph)
        {
            switch (node)
            {
                case NumberNode numberNode:
                    _nodeValues[node.Id] = numberNode.Value;
                    break;
                    
                case MathOperationNode operationNode:
                    CalculateMathOperation(operationNode, graph);
                    break;
                    
                case ResultNode resultNode:
                    CalculateResult(resultNode, graph);
                    break;
            }
        }
        
        private void CalculateMathOperation(MathOperationNode node, NodeGraph graph)
        {
            // 入力ポートから値を取得
            var inputConnections = graph.Connections
                .Where(c => c.InputNodeId == node.Id)
                .OrderBy(c => node.InputPorts.FindIndex(p => p.Id == c.InputPortId))
                .ToList();
            
            if (inputConnections.Count < 2)
            {
                _nodeValues[node.Id] = null;
                return;
            }
            
            // 入力値を取得
            var value1 = GetNodeValue(inputConnections[0].OutputNodeId);
            var value2 = GetNodeValue(inputConnections[1].OutputNodeId);
            
            if (!value1.HasValue || !value2.HasValue)
            {
                _nodeValues[node.Id] = null;
                return;
            }
            
            // 演算を実行
            float result = node.Operation switch
            {
                MathOperation.Add => value1.Value + value2.Value,
                MathOperation.Subtract => value1.Value - value2.Value,
                MathOperation.Multiply => value1.Value * value2.Value,
                MathOperation.Divide => value2.Value != 0 ? value1.Value / value2.Value : float.NaN,
                _ => float.NaN
            };
            
            _nodeValues[node.Id] = result;
        }
        
        private void CalculateResult(ResultNode node, NodeGraph graph)
        {
            // 入力から値を取得
            var inputConnection = graph.Connections
                .FirstOrDefault(c => c.InputNodeId == node.Id);
            
            if (inputConnection != null)
            {
                var value = GetNodeValue(inputConnection.OutputNodeId);
                _nodeValues[node.Id] = value;
                node.Result = value;
            }
            else
            {
                _nodeValues[node.Id] = null;
                node.Result = null;
            }
        }
        
        private float? GetNodeValue(string nodeId)
        {
            return _nodeValues.TryGetValue(nodeId, out var value) ? value : null;
        }
    }
}
