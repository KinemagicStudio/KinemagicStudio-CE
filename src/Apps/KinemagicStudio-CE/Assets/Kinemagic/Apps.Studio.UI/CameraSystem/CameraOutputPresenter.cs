using System;
using EngineLooper;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using MessagePipe;
using R3;
using UnityEngine;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class CameraOutputPresenter : IDisposable, IInitializable, IStartable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly UIViewContext _context;
        private readonly CameraOutputView _view;
        private readonly RenderTextureRegistry _textureRegistry;
        private readonly CameraSystemInputActions _inputActions;
        private readonly IPublisher<ICameraSystemCommand> _commandPublisher;

        private RenderTexture _mainCameraTexture;
        private RenderTexture _multiCameraViewTexture;
        private RenderTexture _verticalModeMainCameraTexture;

        private bool _initialized;

        public CameraOutputPresenter(
            UIViewContext context,
            CameraOutputView view,
            RenderTextureRegistry textureRegistry,
            CameraSystemInputActions inputActions,
            IPublisher<ICameraSystemCommand> commandPublisher)
        {
            _context = context;
            _view = view;
            _textureRegistry = textureRegistry;
            _inputActions = inputActions;
            _commandPublisher = commandPublisher;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void Initialize()
        {
        }

        public void Start()
        {
            if (_initialized) return;

            var mainCameraTextureKey = Constants.MainCameraOutputTextureDataKey;
            var mainCameraTextureWidth = Constants.MainCameraOutputTextureWidth;
            var mainCameraTextureHeight = Constants.MainCameraOutputTextureHeight;
            _mainCameraTexture = _textureRegistry.GetOrCreate(mainCameraTextureKey, mainCameraTextureWidth, mainCameraTextureHeight);

            var verticalModeMainCameraTextureKey = Constants.VerticalModeMainCameraOutputTextureDataKey;
            var verticalModeMainCameraTextureWidth = Constants.VerticalModeMainCameraOutputTextureWidth;
            var verticalModeMainCameraTextureHeight = Constants.VerticalModeMainCameraOutputTextureHeight;
            _verticalModeMainCameraTexture = _textureRegistry.GetOrCreate(verticalModeMainCameraTextureKey, verticalModeMainCameraTextureWidth, verticalModeMainCameraTextureHeight);

            var multiCameraViewTextureKey = Constants.MultiCameraViewOutputTextureDataKey;
            var multiCameraViewTextureWidth = Constants.MultiCameraViewOutputTextureWidth;
            var multiCameraViewTextureHeight = Constants.MultiCameraViewOutputTextureHeight;
            _multiCameraViewTexture = _textureRegistry.GetOrCreate(multiCameraViewTextureKey, multiCameraViewTextureWidth, multiCameraViewTextureHeight);

            _view.SetSingleCameraOutputView(_multiCameraViewTexture);

            _context.CurrentPage
                .Skip(1)
                .Subscribe(OnCurrentPageChanged)
                .AddTo(_disposables);

            _inputActions.MainCameraSwitched
                .Subscribe(cameraId =>
                {
                    _commandPublisher.Publish(new CameraSwitchCommand(cameraId, isMainCamera: true));
                })
                .AddTo(_disposables);

            _inputActions.ControlTargetCameraSwitched
                .Subscribe(cameraId =>
                {
                    if (_context.CurrentPage.Value != UIPageType.CameraControl) return;
                    _commandPublisher.Publish(new CameraSwitchCommand(cameraId, isMainCamera: false));
                })
                .AddTo(_disposables);

            _inputActions.VerticalModeChanged
                .Subscribe(isVerticalMode =>
                {
                    _commandPublisher.Publish(new CameraVerticalModeUpdateCommand(isVerticalMode));
                    var mainCameraTexture = isVerticalMode ? _verticalModeMainCameraTexture : _mainCameraTexture;
                    _view.SetCameraSwitcherView(mainCameraTexture, _multiCameraViewTexture);
                })
                .AddTo(_disposables);

            _inputActions.ActiveCameraCountChanged
                .Subscribe(cameraCount =>
                {
                    _commandPublisher.Publish(new ActiveCameraCountUpdateCommand(cameraCount));
                })
                .AddTo(_disposables);

            _initialized = true;
        }

        private void OnCurrentPageChanged(UIPageType pageType)
        {
            if (pageType == UIPageType.CameraSwitcher)
            {
                _commandPublisher.Publish(new CameraOutputModeUpdateCommand(CameraOutputMode.SplitViewOutput));
                _view.SetCameraSwitcherView(_mainCameraTexture, _multiCameraViewTexture);
            }
            else if (pageType == UIPageType.CameraControl)
            {
                _commandPublisher.Publish(new CameraOutputModeUpdateCommand(CameraOutputMode.SingleViewOutput));
                _view.SetSingleCameraOutputView(_multiCameraViewTexture);
            }
            else if (pageType == UIPageType.Output)
            {
                _commandPublisher.Publish(new CameraOutputModeUpdateCommand(CameraOutputMode.SingleViewOutput));
                _view.SetSingleCameraOutputView(_mainCameraTexture);
            }
            else
            {
                _commandPublisher.Publish(new CameraOutputModeUpdateCommand(CameraOutputMode.SceneViewOutput));
                _view.SetSingleCameraOutputView(_multiCameraViewTexture);
            }
        }
    }
}
