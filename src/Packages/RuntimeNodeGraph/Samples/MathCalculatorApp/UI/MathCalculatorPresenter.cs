using System;
using System.Collections.Generic;
using RuntimeNodeGraph.UI;
using UnityEngine;

namespace RuntimeNodeGraph.Samples.MathCalculator.UI
{
    public sealed class MathCalculatorPresenter : NodeGraphEditorPresenter
    {
        private readonly MathCalculatorNodeGraphEditor _mathEditor;
        private readonly MathCalculatorNodeGraphEditorView _mathEditorView;

        public MathCalculatorPresenter(
            MathCalculatorNodeGraphEditor mathEditor,
            MathCalculatorNodeGraphEditorView mathEditorView)
            : base(mathEditor, mathEditorView)
        {
            _mathEditor = mathEditor;
            _mathEditorView = mathEditorView;
        }

        public override void Initialize()
        {
            base.Initialize();

            _mathEditor.CalculationCompleted += OnCalculationCompleted;

            _mathEditorView.AddNumberNodeButtonClicked += OnAddNumberNodeRequested;
            _mathEditorView.AddOperationNodeButtonClicked += OnAddOperationNodeRequested;
            _mathEditorView.AddResultNodeButtonClicked += OnAddResultNodeRequested;
        }

        public override void Dispose()
        {
            _mathEditorView.AddNumberNodeButtonClicked -= OnAddNumberNodeRequested;
            _mathEditorView.AddOperationNodeButtonClicked -= OnAddOperationNodeRequested;
            _mathEditorView.AddResultNodeButtonClicked -= OnAddResultNodeRequested;

            _mathEditor.CalculationCompleted -= OnCalculationCompleted;
            _mathEditor.Dispose();

            base.Dispose();
        }

        private void OnAddNumberNodeRequested()
        {
            var value = UnityEngine.Random.Range(1f, 10f);
            _mathEditor.CreateNumberNode(value);
        }

        private void OnAddOperationNodeRequested()
        {
            var operations = new[] {
                MathOperation.Add,
                MathOperation.Subtract,
                MathOperation.Multiply,
                MathOperation.Divide
            };
            
            var randomOp = operations[UnityEngine.Random.Range(0, operations.Length)];
            _mathEditor.CreateMathOperationNode(randomOp);
        }

        private void OnAddResultNodeRequested()
        {
            _mathEditor.CreateResultNode();
        }

        private void OnCalculationCompleted(Dictionary<string, float?> results)
        {
            foreach (var kvp in results)
            {
                var node = _mathEditor.Graph.Find(kvp.Key);
                if (node is ResultNode)
                {
                    _mathEditorView.UpdateResultValue(kvp.Key, kvp.Value);
                }
            }
        }
    }
}
