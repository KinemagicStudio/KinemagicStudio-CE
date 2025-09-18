using System;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.Character;
using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using MessagePipe;
using R3;
using RuntimeNodeGraph.UI;

namespace Kinemagic.Apps.Studio.UI.MotionCapture
{
    public sealed class MotionCaptureSystemPresenter : NodeGraphEditorPresenter, IInitializable
    {
        private readonly CompositeDisposable _compositeDisposable = new();
        private readonly UIViewContext _context;
        private readonly MotionCaptureSystemView _motionCaptureSystemView;
        private readonly MotionDataSourceAdditionView _dataSourceAdditionView;
        private readonly MotionCaptureNodeGraphEditorView _nodeGraphEditorView;
        private readonly MotionCaptureNodeGraphEditor _nodeGraphEditor;
        private readonly ICharacterInstanceRegistry _characterInstanceRegistry;
        private readonly IMotionDataSourceRegistry _motionDataSourceRegistry;
        private readonly IMotionDataSourceMonitor _motionDataSourceMonitor;
        private readonly IPublisher<ICharacterCommand> _characterCommandPublisher;
        private readonly IPublisher<IMotionDataSourceCommand> _motionDataSourceCommandPublisher;
        
        public MotionCaptureSystemPresenter(
            UIViewContext context,
            MotionCaptureSystemView motionCaptureSystemView,
            MotionDataSourceAdditionView dataSourceAdditionView,
            MotionCaptureNodeGraphEditorView nodeGraphEditorView,
            MotionCaptureNodeGraphEditor nodeGraphEditor,
            ICharacterInstanceRegistry characterInstanceRegistry,
            IMotionDataSourceRegistry motionDataSourceRegistry,
            IMotionDataSourceMonitor motionDataSourceMonitor,
            IPublisher<ICharacterCommand> characterCommandPublisher,
            IPublisher<IMotionDataSourceCommand> motionDataSourceCommandPublisher)
            : base(nodeGraphEditor, nodeGraphEditorView)
        {
            _context = context;
            _motionCaptureSystemView = motionCaptureSystemView;
            _dataSourceAdditionView = dataSourceAdditionView;
            _nodeGraphEditorView = nodeGraphEditorView;
            _nodeGraphEditor = nodeGraphEditor;
            _characterInstanceRegistry = characterInstanceRegistry;
            _motionDataSourceRegistry = motionDataSourceRegistry;
            _motionDataSourceMonitor = motionDataSourceMonitor;
            _characterCommandPublisher = characterCommandPublisher;
            _motionDataSourceCommandPublisher = motionDataSourceCommandPublisher;
        }

        public override void Dispose()
        {
            _compositeDisposable.Dispose();
            base.Dispose();
        }

        public override void Initialize()
        {
            base.Initialize();

            _context.CurrentPage
                .Skip(1)
                .Subscribe(pageType =>
                {
                    UnityEngine.Debug.Log($"<color=cyan>[MotionCaptureSystemPresenter][OnCurrentPageChanged] PageType: {pageType}</color>");
                    if (pageType == UIPageType.MotionCapture)
                    {
                        _motionCaptureSystemView.Show();
                    }
                    else
                    {
                        _motionCaptureSystemView.Hide();
                    }
                })
                .AddTo(_compositeDisposable);

            _nodeGraphEditor.OnMappingCreated += OnMappingCreated;
            _nodeGraphEditor.OnMappingRemoved += OnMappingRemoved;

            _dataSourceAdditionView.DataSourceAdditionRequested
                .Subscribe(request =>
                {
                    UnityEngine.Debug.Log("<color=lime>[MotionCaptureSystemPresenter] OnDataSourceAdditionRequested</color>");
                    _motionDataSourceCommandPublisher.Publish(new MotionDataSourceAddCommand(request.DataSourceKey, request.DataSourceType));
                })
                .AddTo(_compositeDisposable);

            _characterInstanceRegistry.Added
                .Subscribe(OnCharacterInstanceAdded)
                .AddTo(_compositeDisposable);

            _characterInstanceRegistry.Removed
                .Subscribe(OnCharacterInstanceRemoved)
                .AddTo(_compositeDisposable);

            _motionDataSourceRegistry.Added
                .Subscribe(info =>
                {
                    UnityEngine.Debug.Log($"<color=lime>[MotionCaptureSystemPresenter][OnMotionDataSourceAdded] DataSourceId: {info.DataSourceId}, Type: {info.DataSourceType}</color>");
                    _nodeGraphEditor.CreateDataSourceNode(
                        info.DataSourceId.Value,
                        info.DataSourceType,
                        info.Key.ServerAddress,
                        info.Key.Port
                    );
                })
                .AddTo(_compositeDisposable);

            _motionDataSourceMonitor.StatusChanged
                .Subscribe(status =>
                {
                    _nodeGraphEditorView.UpdateDataSourceStatus(status.DataSourceId.Value, status);
                })
                .AddTo(_compositeDisposable);
        }

        private void OnCharacterInstanceAdded(CharacterInstanceInfo instance)
        {
            UnityEngine.Debug.Log($"<color=green>[MotionCaptureSystemPresenter][OnCharacterInstanceAdded] InstanceId: {instance.InstanceId}, Name: {instance.Name}</color>");
            _nodeGraphEditor.CreateCharacterNode(instance.InstanceId.Value, instance.Name);
        }

        private void OnCharacterInstanceRemoved(CharacterInstanceInfo instance)
        {
            UnityEngine.Debug.Log($"<color=red>[MotionCaptureSystemPresenter][OnCharacterInstanceRemoved] InstanceId: {instance.InstanceId}, Name: {instance.Name}</color>");
            _nodeGraphEditor.RemoveNode($"Character-{instance.InstanceId}");
        }

        private void OnMappingCreated(uint characterId, MotionDataType motionDataType, DataSourceId dataSourceId)
        {
            _characterCommandPublisher.Publish(new MotionDataSourceMappingAddCommand(new InstanceId(characterId), dataSourceId, motionDataType));
        }

        private void OnMappingRemoved(uint characterId, MotionDataType motionDataType, DataSourceId dataSourceId)
        {
            _characterCommandPublisher.Publish(new MotionDataSourceMappingRemoveCommand(new InstanceId(characterId), dataSourceId, motionDataType));
        }
    }
}