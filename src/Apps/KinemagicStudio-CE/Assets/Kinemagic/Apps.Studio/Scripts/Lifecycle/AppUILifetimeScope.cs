using CinematicSequencer.UI;
using EngineLooper.VContainer;
using Kinemagic.Apps.Studio.UI.AppCore;
using Kinemagic.Apps.Studio.UI.Character;
using Kinemagic.Apps.Studio.UI.CinematicSequencer;
using Kinemagic.Apps.Studio.UI.MotionCapture;
using Kinemagic.Apps.Studio.UI.SpatialEnvironment;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRMToolkit.UI;

namespace Kinemagic.Apps.Studio.Lifecycle
{
    public sealed class AppUILifetimeScope : LifetimeScope
    {
        [Header("Testing / Debugging")]
        [SerializeField] GameObject _debugModeObjects;
        [SerializeField] GameObject _spatialEnvironmentProvider;

        [Header("App Core")]
        [SerializeField] AppBarView _appBarView;
        [SerializeField] PageNavigationBarView _navigationBarView;

        [Header("Character")]
        [SerializeField] CharacterModelListView _characterModelListView;
        [SerializeField] CharacterLicenseView _characterLicenseView;

        [Header("Motion Capture")]
        [SerializeField] MotionCaptureSystemView _motionCaptureSystemView;
        [SerializeField] MotionDataSourceAdditionView _motionDataSourceAdditionView;
        [SerializeField] MotionCaptureNodeGraphEditorView _motionCaptureNodeGraphEditorView;

        [Header("Spatial Environment")]
        [SerializeField] EnvironmentModelListView _environmentModelListView;
        [SerializeField] ConfirmationDialog _environmentModelConfirmationDialog;

        [Header("Cinematic Sequencer")]
        [SerializeField] CinematicSequencerView _cinematicSequencerView;
        [SerializeField] SaveConfirmationDialogView _saveConfirmationDialogView;
        [SerializeField] TimelineEditorView _sequenceEditorView;
        [SerializeField] CinematicSequenceLibraryView _libraryView;
        [SerializeField] TimelineTrackEditorView _sequenceTrackEditorView;
        [SerializeField] TimelinePlaybackControlView _sequencePlaybackControlView;
        [SerializeField] KeyframeEditorView _animationEditorView;

        [Header("Help")]
        [SerializeField] LicenseView _licenseView;

        protected override void Awake()
        {
            if (SceneManager.GetActiveScene().name == SceneNames.UIView) // Run as a single scene for testing or debugging.
            {
                Debug.Log($"<color=orange>[{nameof(AppUILifetimeScope)}] Run as a single scene for testing or debugging.</color>");

                if (!_debugModeObjects.TryGetComponent<StageLifetimeScope>(out _))
                {
                    _debugModeObjects.AddComponent<StageLifetimeScope>();
                }

                _debugModeObjects.SetActive(true);
            }
            else
            {
                _debugModeObjects.SetActive(false);
            }

            parentReference = ParentReference.Create<StageLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            ConfigureAppCore(builder);
            ConfigureCharacterUI(builder);
            ConfigureMotionCaptureUI(builder);
            ConfigureEnvironmentUI(builder);
            ConfigureCinematicSequencerUI(builder);
            ConfigureLicenseUI(builder);
            Debug.Log($"<color=cyan>[{nameof(AppUILifetimeScope)}] Configured</color>");
        }

        private void ConfigureAppCore(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<AppBarPresenter>();
            builder.RegisterComponent(_appBarView);

            builder.RegisterEngineLooperEntryPoint<PageNavigationBarPresenter>();
            builder.RegisterComponent(_navigationBarView);
        }

        private void ConfigureCharacterUI(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<CharacterModelPresenter>();
            builder.RegisterComponent(_characterModelListView);
            builder.RegisterComponent(_characterLicenseView);
        }

        private void ConfigureLicenseUI(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<HelpPagePresenter>();
            builder.RegisterComponent(_licenseView);
        }

        private void ConfigureMotionCaptureUI(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<MotionCaptureNodeGraphEditor>(Lifetime.Singleton).AsSelf();
            builder.RegisterEngineLooperEntryPoint<MotionCaptureSystemPresenter>(Lifetime.Singleton);
            builder.RegisterComponent(_motionCaptureSystemView);
            builder.RegisterComponent(_motionDataSourceAdditionView);
            builder.RegisterComponent(_motionCaptureNodeGraphEditorView);
        }

        private void ConfigureEnvironmentUI(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<EnvironmentModelsPresenter>();
            builder.RegisterComponent(_environmentModelListView);
            builder.RegisterComponent(_environmentModelConfirmationDialog);
        }

        private void ConfigureCinematicSequencerUI(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<CinematicSequencerPresenter>();
            builder.RegisterComponent(_cinematicSequencerView);

            builder.RegisterComponent(_saveConfirmationDialogView);

            builder.Register<TimelinePresenter>(Lifetime.Singleton);
            builder.RegisterComponent(_sequenceEditorView);
            builder.RegisterComponent(_libraryView);
            builder.RegisterComponent(_sequenceTrackEditorView);
            builder.RegisterComponent(_sequencePlaybackControlView);

            builder.Register<KeyframeAnimationEditorPresenter>(Lifetime.Singleton);
            builder.RegisterComponent(_animationEditorView);
        }
    }
}