using System;
using System.Collections.Generic;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using MessagePipe;
using R3;
using Debug = UnityEngine.Debug;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class CameraPropertyControlPresenter : IDisposable, IInitializable
    {
        private readonly CameraPropertyControlView _view;
        private readonly CameraSystemInputActions _inputActions;
        private readonly IPublisher<ICameraSystemCommand> _commandPublisher;
        private readonly ISubscriber<ICameraSystemSignal> _signalSubscriber;
        private readonly Dictionary<CameraId, CameraProperties> _cameraPropertiesCache = new();
        private readonly CompositeDisposable _disposables = new();

        private bool _initialized;
        private CameraId _currentCameraId = new(1);

        public CameraPropertyControlPresenter(
            CameraPropertyControlView view,
            CameraSystemInputActions inputActions,
            IPublisher<ICameraSystemCommand> commandPublisher,
            ISubscriber<ICameraSystemSignal> signalSubscriber)
        {
            _view = view;
            _inputActions = inputActions;
            _commandPublisher = commandPublisher;
            _signalSubscriber = signalSubscriber;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void Initialize()
        {
            if (_initialized) return;

            _signalSubscriber
                .Subscribe(signal =>
                {
                    if (signal is CameraPropertiesUpdatedSignal cameraPropertiesUpdatedSignal)
                    {
                        var cameraId = cameraPropertiesUpdatedSignal.CameraId;
                        var cameraProperties = cameraPropertiesUpdatedSignal.CameraProperties;

                        Debug.Log($"[{nameof(CameraPropertyControlPresenter)}] CameraPropertiesUpdated: {cameraId}");
                        _cameraPropertiesCache[cameraId] = cameraProperties;

                        if (cameraId == _currentCameraId)
                        {
                            _view.UpdateCameraProperties(cameraProperties);
                        }
                    }
                })
                .AddTo(_disposables);

            _inputActions.ControlTargetCameraSwitched
                .Subscribe(cameraId =>
                {
                    _currentCameraId = cameraId;
                    if (_cameraPropertiesCache.TryGetValue(cameraId, out var cameraProperties))
                    {
                        _view.UpdateCameraProperties(cameraProperties);
                    }
                    else
                    {
                        _commandPublisher.Publish(new CameraPropertiesNotifyCommand(cameraId));
                    }
                })
                .AddTo(_disposables);

            _view.ValueChanged
                .Subscribe(x =>
                {
                    Debug.Log($"[{nameof(CameraPropertyControlPresenter)}] OnCameraPropertyChanged: {x.propertyType}, {x.value}");
                    if (_cameraPropertiesCache.TryGetValue(_currentCameraId, out var cameraProperties))
                    {
                        switch (x.propertyType)
                        {
                            case CameraPropertyType.FocalLength:
                                cameraProperties.FocalLength = x.value;
                                break;
                            case CameraPropertyType.FocusDistance:
                                cameraProperties.FocusDistance = x.value;
                                break;
                            case CameraPropertyType.Aperture:
                                cameraProperties.Aperture = x.value;
                                break;
                        }
                        _commandPublisher.Publish(new CameraPropertiesUpdateCommand(_currentCameraId, cameraProperties));
                    }
                })
                .AddTo(_disposables);

            _initialized = true;
            Debug.Log($"[{nameof(CameraPropertyControlPresenter)}] Initialized");
        }
    }
}
