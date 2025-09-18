using System;
using System.Collections.Generic;
using RuntimeNodeGraph;
using RuntimeNodeGraph.UI;
using UnityEngine.UIElements;

namespace RuntimeNodeGraph.Samples.MathCalculator.UI
{
    public sealed class MathCalculatorNodeGraphEditorView : NodeGraphEditorView
    {
        private readonly Dictionary<string, Label> _resultLabels = new();
        
        public event Action AddNumberNodeButtonClicked;
        public event Action AddOperationNodeButtonClicked;
        public event Action AddResultNodeButtonClicked;
        
        protected override void Awake()
        {
            base.Awake();
            
            var addNumberButton = _root.Q<Button>("add-number-button");
            if (addNumberButton != null)
            {
                addNumberButton.clicked += () => AddNumberNodeButtonClicked?.Invoke();
            }
            
            var addOperationButton = _root.Q<Button>("add-operation-button");
            if (addOperationButton != null)
            {
                addOperationButton.clicked += () => AddOperationNodeButtonClicked?.Invoke();
            }
            
            var addResultButton = _root.Q<Button>("add-result-button");
            if (addResultButton != null)
            {
                addResultButton.clicked += () => AddResultNodeButtonClicked?.Invoke();
            }
        }

        public void UpdateResultValue(string nodeId, float? value)
        {
            if (!IsInitialized) return;

            if (_resultLabels.TryGetValue(nodeId, out var label))
            {
                label.text = value.HasValue ? $"= {value.Value:F2}" : "= ?";
            }
        }

        protected override string GetNodeCustomClassName(NodeData node)
        {
            return node switch
            {
                NumberNode => "number-node",
                MathOperationNode => "operation-node",
                ResultNode => "result-node",
                _ => base.GetNodeCustomClassName(node)
            };
        }

        protected override VisualElement GetNodeCustomContent(NodeData node)
        {
            switch (node)
            {
                case NumberNode numberNode:
                    var numberContainer = new VisualElement();
                    numberContainer.AddToClassList("node-value-container");
                    
                    var valueLabel = new Label($"Value: {numberNode.Value}");
                    valueLabel.AddToClassList("value-label");
                    numberContainer.Add(valueLabel);
                    
                    return numberContainer;

                case MathOperationNode operationNode:
                    var operationContainer = new VisualElement();
                    operationContainer.AddToClassList("node-operation-container");
                    
                    var operationSymbol = GetOperationSymbol(operationNode.Operation);
                    var symbolLabel = new Label(operationSymbol);
                    symbolLabel.AddToClassList("operation-symbol");
                    operationContainer.Add(symbolLabel);
                    
                    return operationContainer;

                case ResultNode resultNode:
                    var resultContainer = new VisualElement();
                    resultContainer.AddToClassList("node-result-container");
                    
                    var resultLabel = new Label(resultNode.Result.HasValue ? $"= {resultNode.Result.Value:F2}" : "= ?");
                    resultLabel.AddToClassList("result-label");
                    resultContainer.Add(resultLabel);
                    
                    _resultLabels[node.Id] = resultLabel;
                    
                    return resultContainer;

                default:
                    return base.GetNodeCustomContent(node);
            }
        }

        protected override void OnNodeRemoved(string nodeId)
        {
            _resultLabels.Remove(nodeId);
        }

        private string GetOperationSymbol(MathOperation operation)
        {
            return operation switch
            {
                MathOperation.Add => "+",
                MathOperation.Subtract => "-",
                MathOperation.Multiply => "×",
                MathOperation.Divide => "÷",
                _ => "?"
            };
        }
    }
}
