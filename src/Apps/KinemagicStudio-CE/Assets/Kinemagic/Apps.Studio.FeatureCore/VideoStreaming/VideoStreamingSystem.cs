using System;
using EngineLooper;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using MessagePipe;

namespace Kinemagic.Apps.Studio.FeatureCore.VideoStreaming
{
    public sealed class VideoStreamingSystem : IDisposable, IInitializable
    {
        private readonly IVideoFrameStreamer _videoFrameStreamer;
        private readonly RenderTextureRegistry _renderTextureRegistry;
        private readonly ISubscriber<ICameraSystemCommand> _commandSubscriber;

        private IDisposable _disposable;

        public VideoStreamingSystem(
            IVideoFrameStreamer videoFrameStreamer,
            RenderTextureRegistry renderTextureRegistry,
            ISubscriber<ICameraSystemCommand> commandSubscriber)
        {
            _videoFrameStreamer = videoFrameStreamer;
            _renderTextureRegistry = renderTextureRegistry;
            _commandSubscriber = commandSubscriber;
        }

        public void Dispose()
        {
            _disposable?.Dispose();
            _videoFrameStreamer.SetEnable(false);
            _videoFrameStreamer.SourceTexture = null;
        }

        public void Initialize()
        {
            var key = Constants.MainCameraOutputTextureDataKey;
            var width = Constants.MainCameraOutputTextureWidth;
            var height = Constants.MainCameraOutputTextureHeight;

            var mainCameraOutputTexture = _renderTextureRegistry.GetOrCreate(key, width, height);

            _videoFrameStreamer.Name = "[Kinemagic Studio] Main Camera";
            _videoFrameStreamer.AlphaSupport = true;
            _videoFrameStreamer.SourceTexture = mainCameraOutputTexture;
            _videoFrameStreamer.SetEnable(true);

            _disposable = _commandSubscriber.Subscribe(command =>
            {
                if (command is CameraVerticalModeUpdateCommand verticalModeUpdateCommand)
                {
                    SwitchSourceTexture(verticalModeUpdateCommand.IsVerticalMode);
                }
            });
        }

        private void SwitchSourceTexture(bool isVerticalMode)
        {
            var key = isVerticalMode
                          ? Constants.VerticalModeMainCameraOutputTextureDataKey
                          : Constants.MainCameraOutputTextureDataKey;

            if (_renderTextureRegistry.TryGet(key, out var texture))
            {
                _videoFrameStreamer.SourceTexture = texture;
            }
        }
    }
}
