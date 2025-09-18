using System;
using Cysharp.Threading.Tasks;
using EngineLooper;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using MessagePipe;
using Debug = UnityEngine.Debug;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource
{
    public sealed class MotionDataSourceSystem : IDisposable, IInitializable, ILateTickable
    {
        private readonly ISubscriber<IMotionDataSourceCommand> _commandSubscriber;
        private readonly IPublisher<IMotionDataSignal> _signalPublisher;

        private readonly ITimeSystem _timeSystem = TimeSystemProvider.GetTimeSystem();
        private readonly IMotionDataSourceManager _motionDataSourceManager;
        private readonly MotionDataSourceRegistry _motionDataSourceRegistry;
        private readonly MotionDataSourceMonitor _motionDataSourceMonitor;

        private readonly BodyTrackingDataUpdatedSignal _bodyTrackingSignalBuffer = new();
        private readonly FingerTrackingDataUpdatedSignal _fingerTrackingSignalBuffer = new();
        private readonly FaceTrackingDataUpdatedSignal _faceTrackingSignalBuffer = new();
        private readonly EyeTrackingDataUpdatedSignal _eyeTrackingSignalBuffer = new();

        private IDisposable _disposable;

        public MotionDataSourceSystem(
            IMotionDataSourceManager motionDataSourceManager,
            MotionDataSourceRegistry motionDataSourceRegistry,
            MotionDataSourceMonitor motionDataSourceMonitor,
            ISubscriber<IMotionDataSourceCommand> commandSubscriber,
            IPublisher<IMotionDataSignal> signalPublisher)
        {
            _motionDataSourceRegistry = motionDataSourceRegistry;
            _motionDataSourceMonitor = motionDataSourceMonitor;
            _motionDataSourceManager = motionDataSourceManager;
            _commandSubscriber = commandSubscriber;
            _signalPublisher = signalPublisher;
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }

        public void Initialize()
        {
            var disposableBag = DisposableBag.CreateBuilder();
            
            _commandSubscriber.Subscribe(command =>
            {
                switch (command)
                {
                    case MotionDataSourceAddCommand addCommand:
                        AddMotionDataSourceAsync(addCommand.Key, addCommand.DataSourceType).Forget();
                        break;
                }
            }).AddTo(disposableBag);

            _disposable = disposableBag.Build();
        }

        public void LateTick()
        {
            var currentTime = (float)_timeSystem.GetElapsedTime().TotalSeconds;

            for (var i = 0; i < _motionDataSourceRegistry.BodyTrackingDataSources.Count; i++) // NOTE: Avoid allocation
            {
                // Not all data sources provide body-tracking data.
                // If a data source does not provide body-tracking data, the corresponding index value is null.
                // See the MotionDataSourceRegistry class for details.
                var dataSource = _motionDataSourceRegistry.BodyTrackingDataSources[i];
                if (dataSource != null && dataSource.TryGetSample(currentTime, out var bodyTrackingFrame))
                {
                    _bodyTrackingSignalBuffer.SetData(dataSource.Id.Value, bodyTrackingFrame);
                    _signalPublisher.Publish(_bodyTrackingSignalBuffer);
                }
            }

            for (var i = 0; i < _motionDataSourceRegistry.FingerTrackingDataSources.Count; i++) // NOTE: Avoid allocation
            {
                // Not all data sources provide finger-tracking data.
                // If a data source does not provide finger-tracking data, the corresponding index value is null.
                // See the MotionDataSourceRegistry class for details.
                var dataSource = _motionDataSourceRegistry.FingerTrackingDataSources[i];
                if (dataSource != null && dataSource.TryGetSample(currentTime, out var fingerTrackingFrame))
                {
                    _fingerTrackingSignalBuffer.SetData(dataSource.Id.Value, fingerTrackingFrame);
                    _signalPublisher.Publish(_fingerTrackingSignalBuffer);
                }
            }

            for (var i = 0; i < _motionDataSourceRegistry.FaceTrackingDataSources.Count; i++) // NOTE: Avoid allocation
            {
                // Not all data sources provide face-tracking data.
                // If a data source does not provide face-tracking data, the corresponding index value is null.
                // See the MotionDataSourceRegistry class for details.
                var dataSource = _motionDataSourceRegistry.FaceTrackingDataSources[i];
                if (dataSource != null && dataSource.TryGetSample(currentTime, out var faceTrackingFrame))
                {
                    _faceTrackingSignalBuffer.SetData(dataSource.Id.Value, faceTrackingFrame);
                    _signalPublisher.Publish(_faceTrackingSignalBuffer);
                }
            }

            for (var i = 0; i < _motionDataSourceRegistry.EyeTrackingDataSources.Count; i++) // NOTE: Avoid allocation
            {
                // Not all data sources provide eye-tracking data.
                // If a data source does not provide eye-tracking data, the corresponding index value is null.
                // See the MotionDataSourceRegistry class for details.
                var dataSource = _motionDataSourceRegistry.EyeTrackingDataSources[i];
                if (dataSource != null && dataSource.TryGetSample(currentTime, out var eyeTrackingFrame))
                {
                    _eyeTrackingSignalBuffer.SetData(dataSource.Id.Value, eyeTrackingFrame);
                    _signalPublisher.Publish(_eyeTrackingSignalBuffer);
                }
            }
        }

        private async UniTask AddMotionDataSourceAsync(MotionDataSourceKey dataSourceKey, MotionDataSourceType dataSourceType)
        {
            if (_motionDataSourceRegistry.Contains(dataSourceKey))
            {
                Debug.Log($"<color=yellow>[{nameof(MotionDataSourceSystem)}] Motion data source with key '{dataSourceKey}' already exists.</color>");
                return;
            }

            var dataSource = await _motionDataSourceManager.CreateAsync(dataSourceKey, dataSourceType);
            var dataSourceInfo = new MotionDataSourceInfo(dataSourceKey, dataSourceType, dataSource.Id);

            var success = _motionDataSourceRegistry.TryAdd(dataSourceInfo, dataSource);
            if (success)
            {
                _motionDataSourceMonitor.Register(dataSource);
                Debug.Log($"<color=cyan>[{nameof(MotionDataSourceSystem)}] Added motion data source with ID: {dataSourceInfo.DataSourceId}.</color>");
            }
        }
    }
}