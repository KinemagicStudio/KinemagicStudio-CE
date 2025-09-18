using System;
using System.Collections.Generic;
using RuntimeNodeGraph;

namespace RuntimeNodeGraph.Samples.MathCalculator
{
    public sealed class MathCalculatorNodeGraphEditor : NodeGraphEditor, IDisposable
    {
        private readonly MathCalculationEngine _calculationEngine = new();

        public event Action<Dictionary<string, float?>> CalculationCompleted;

        public void Initialize()
        {
            ConnectionAdded += OnConnectionChanged;
            ConnectionRemoved += OnConnectionChanged;
            NodeAdded += OnNodeChanged;
            NodeRemoved += OnNodeChanged;
        }

        public void Dispose()
        {
            ConnectionAdded -= OnConnectionChanged;
            ConnectionRemoved -= OnConnectionChanged;
            NodeAdded -= OnNodeChanged;
            NodeRemoved -= OnNodeChanged;
        }

        public NumberNode CreateNumberNode(float value)
        {
            var node = new NumberNode
            {
                Value = value,
                NormalizedPosition = GetRandomPosition()
            };
            
            node.OutputPorts.Add(new PortData
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Value",
                Direction = PortDirection.Output,
                PortType = "Number"
            });
            
            node.Name = $"Number ({value})";
            AddNode(node);
            return node;
        }

        public MathOperationNode CreateMathOperationNode(MathOperation operation)
        {
            var node = new MathOperationNode(
                operation,
                GetRandomPosition()
            );
            
            node.InputPorts.Add(new PortData
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "A",
                Direction = PortDirection.Input,
                PortType = "Number"
            });
            
            node.InputPorts.Add(new PortData
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "B",
                Direction = PortDirection.Input,
                PortType = "Number"
            });
            
            node.OutputPorts.Add(new PortData
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Result",
                Direction = PortDirection.Output,
                PortType = "Number"
            });
            
            AddNode(node);
            return node;
        }

        public ResultNode CreateResultNode()
        {
            var node = new ResultNode(
                Guid.NewGuid().ToString(),
                GetRandomPosition()
            );
            
            node.InputPorts.Add(new PortData
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Input",
                Direction = PortDirection.Input,
                PortType = "Number"
            });
            
            AddNode(node);
            return node;
        }

        public void Calculate()
        {
            if (Graph == null) return;
            
            try
            {
                var results = _calculationEngine.Calculate(Graph);
                UpdateResultNodes(results);
                CalculationCompleted?.Invoke(results);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Calculation error: {e.Message}");
            }
        }

        private void UpdateResultNodes(Dictionary<string, float?> results)
        {
            var allNodes = Graph.GetAllNodes();
            foreach (var node in allNodes)
            {
                if (node is ResultNode resultNode)
                {
                    if (results.TryGetValue(node.Id, out var value))
                    {
                        resultNode.Result = value;
                    }
                }
            }
        }

        private void OnConnectionChanged(ConnectionData connection)
        {
            Calculate();
        }

        private void OnConnectionChanged(string connectionId)
        {
            Calculate();
        }

        private void OnNodeChanged(NodeData node)
        {
            Calculate();
        }

        private System.Numerics.Vector2 GetRandomPosition()
        {
            return new System.Numerics.Vector2(
                UnityEngine.Random.Range(100, 800),
                UnityEngine.Random.Range(100, 500)
            );
        }
    }
}
