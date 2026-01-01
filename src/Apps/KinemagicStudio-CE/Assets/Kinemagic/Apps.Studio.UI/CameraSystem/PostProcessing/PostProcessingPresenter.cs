using System;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using MessagePipe;
using R3;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class PostProcessingPresenter : IDisposable, IInitializable
    {
        private readonly PostProcessingView _postProcessingView;
        private readonly CameraSystemInputActions _inputActions;
        private readonly IPublisher<ICameraSystemCommand> _commandPublisher;
        private readonly ISubscriber<ICameraSystemSignal> _signalSubscriber;
        private readonly CompositeDisposable _disposables = new();

        private CameraId _currentCameraId = new(0);

        public PostProcessingPresenter(
            PostProcessingView postProcessingView,
            CameraSystemInputActions inputActions,
            IPublisher<ICameraSystemCommand> commandPublisher,
            ISubscriber<ICameraSystemSignal> signalSubscriber)
        {
            _postProcessingView = postProcessingView;
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
            _signalSubscriber.Subscribe(OnPostProcessingSignalReceived)
                .AddTo(_disposables);

            _inputActions.ControlTargetCameraSwitched
                .Subscribe(cameraId =>
                {
                    _currentCameraId = cameraId;
                    RequestSignal(cameraId);
                })
                .AddTo(_disposables);

            _postProcessingView.ColorAdjustmentView.ValueChanged
                .Subscribe(parameters => PublishUpdateCommand(_currentCameraId, parameters))
                .AddTo(_disposables);

            _postProcessingView.ToneMappingView.ValueChanged
                .Subscribe(parameters => PublishUpdateCommand(_currentCameraId, parameters))
                .AddTo(_disposables);

            _postProcessingView.DepthOfFieldView.ValueChanged
                .Subscribe(parameters => PublishUpdateCommand(_currentCameraId, parameters))
                .AddTo(_disposables);

            _postProcessingView.BloomView.ValueChanged
                .Subscribe(parameters => PublishUpdateCommand(_currentCameraId, parameters))
                .AddTo(_disposables);

            _postProcessingView.ScreenSpaceLensFlareView.ValueChanged
                .Subscribe(parameters => PublishUpdateCommand(_currentCameraId, parameters))
                .AddTo(_disposables);

            _postProcessingView.ScreenEdgeColorView.ValueChanged
                .Subscribe(parameters => PublishUpdateCommand(_currentCameraId, parameters))
                .AddTo(_disposables);
        }

        private void OnPostProcessingSignalReceived(ICameraSystemSignal signal)
        {
            if (signal is PostProcessingUpdatedSignal postProcessingSignal &&
                postProcessingSignal.CameraId == _currentCameraId)
            {
                switch (postProcessingSignal.Parameters)
                {
                    case ColorAdjustmentParameters colorAdjustment:
                        _postProcessingView.ColorAdjustmentView.UpdateParameters(colorAdjustment);
                        break;

                    case TonemappingParameters toneMapping:
                        _postProcessingView.ToneMappingView.UpdateParameters(toneMapping);
                        break;

                    case BokehDepthOfFieldParameters depthOfField:
                        _postProcessingView.DepthOfFieldView.UpdateParameters(depthOfField);
                        break;

                    case BloomParameters bloom:
                        _postProcessingView.BloomView.UpdateParameters(bloom);
                        break;

                    case ScreenSpaceLensFlareParameters screenSpaceLensFlare:
                        _postProcessingView.ScreenSpaceLensFlareView.UpdateParameters(screenSpaceLensFlare);
                        break;

                    case ScreenEdgeColorParameters screenEdgeColor:
                        _postProcessingView.ScreenEdgeColorView.UpdateParameters(screenEdgeColor);
                        break;
                }
            }
        }

        private void PublishUpdateCommand(CameraId cameraId, IPostProcessingParameters parameters)
        {
            _commandPublisher.Publish(new PostProcessingUpdateCommand(cameraId, parameters));
        }

        private void RequestSignal(CameraId cameraId)
        {
            _commandPublisher.Publish(new PostProcessingParametersNotifyCommand(cameraId));
        }
    }
}
