using EngineLooper.VContainer;
using Kinemagic.Apps.Studio.FeatureCore.CameraSystem;
using Kinemagic.Apps.Studio.FeatureCore.VideoStreaming;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Kinemagic.Apps.Studio.Lifecycle
{
    public sealed class CameraSystemLifetimeScope : LifetimeScope
    {
        [Header("Camera System")]
        [SerializeField] CameraActorManager _cameraActorManager;

        [Header("Video Streaming")]
        [SerializeField] SpoutVideoFrameStreamer _spoutVideoFrameStreamer;
        [SerializeField] SyphonVideoFrameStreamer _syphonVideoFrameStreamer;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_cameraActorManager);
            builder.RegisterEngineLooperEntryPoint<CameraSystem>();

            builder.RegisterEngineLooperEntryPoint<VideoStreamingSystem>();
#if UNITY_STANDALONE_WIN
            builder.RegisterComponent(_spoutVideoFrameStreamer).AsImplementedInterfaces();
#elif UNITY_STANDALONE_OSX
            builder.RegisterComponent(_syphonVideoFrameStreamer).AsImplementedInterfaces();
#endif

            Debug.Log($"<color=cyan>[{nameof(CameraSystemLifetimeScope)}] Configured</color>");
        }
    }
}
