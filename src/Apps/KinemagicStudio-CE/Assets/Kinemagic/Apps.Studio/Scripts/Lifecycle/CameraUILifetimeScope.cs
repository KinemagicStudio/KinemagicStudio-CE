using EngineLooper.VContainer;
using Kinemagic.Apps.Studio.UI;
using Kinemagic.Apps.Studio.UI.CameraSystem;
using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace Kinemagic.Apps.Studio.Lifecycle
{
    public sealed class CameraUILifetimeScope : LifetimeScope
    {
        [Header("Camera System UI")]
        [SerializeField] CameraSystemInputActions _cameraSystemInputActions;
        [SerializeField] CameraOutputView _cameraOutputView;
        [SerializeField] CameraControlView _cameraControlView;
        [SerializeField] CameraPropertyControlView _cameraPropertyView;
        [SerializeField] PostProcessingView _postProcessingView;

        protected override void Awake()
        {
            Debug.Log($"[{nameof(CameraUILifetimeScope)}] Awake");
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<CameraOutputPresenter>();
            builder.RegisterEngineLooperEntryPoint<CameraPoseController>();
            builder.RegisterEngineLooperEntryPoint<CameraPropertyControlPresenter>();
            builder.RegisterEngineLooperEntryPoint<PostProcessingPresenter>();

            builder.RegisterComponent(_cameraSystemInputActions);
            builder.RegisterComponent(_cameraOutputView);
            builder.RegisterComponent(_cameraControlView);
            builder.RegisterComponent(_cameraPropertyView);
            builder.RegisterComponent(_postProcessingView);
    
            builder.RegisterBuildCallback(container =>
            {
                _cameraControlView.Construct(container.Resolve<UIViewContext>());
            });

            Debug.Log($"<color=cyan>[{nameof(CameraUILifetimeScope)}] Configured</color>");
        }
    }
}
