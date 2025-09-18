using RuntimeNodeGraph.Samples.MathCalculator.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeNodeGraph.Samples.MathCalculator
{
    public class MathCalculatorApplication : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private MathCalculatorNodeGraphEditorView _view;

        private MathCalculatorNodeGraphEditor _nodeGraphEditor;
        private MathCalculatorPresenter _presenter;

        private void Start()
        {
            _nodeGraphEditor = new MathCalculatorNodeGraphEditor();
            _nodeGraphEditor.Initialize();

            _presenter = new MathCalculatorPresenter(_nodeGraphEditor, _view);
            _presenter.Initialize();

            CreateSampleNodes();
        }

        private void CreateSampleNodes()
        {
            _nodeGraphEditor.CreateNumberNode(5);
            _nodeGraphEditor.CreateNumberNode(3);
            _nodeGraphEditor.CreateMathOperationNode(MathOperation.Add);
            _nodeGraphEditor.CreateResultNode();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            _nodeGraphEditor?.Dispose();
        }
    }
}