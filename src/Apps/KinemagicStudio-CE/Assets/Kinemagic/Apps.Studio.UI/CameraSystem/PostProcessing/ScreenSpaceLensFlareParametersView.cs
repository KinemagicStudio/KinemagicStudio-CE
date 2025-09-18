using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class ScreenSpaceLensFlareParametersView : MonoBehaviour, IPostProcessingEffectView
    {
        [SerializeField] private UIDocument _document;

        private bool _initialized;
        private VisualElement _container;
        private Toggle _enabledToggle;
        private Slider _intensitySlider;

        public float IntensityMaxValue { get; } = 10f;

        private readonly Subject<ScreenSpaceLensFlareParameters> _onValueChanged = new();
        public Observable<ScreenSpaceLensFlareParameters> ValueChanged => _onValueChanged;

        private void Awake()
        {
            Initialize();
        }

        public void UpdateParameters(ScreenSpaceLensFlareParameters parameters)
        {
            _enabledToggle.value = parameters.IsEnabled;
            _intensitySlider.value = parameters.Intensity;
            _intensitySlider.label = parameters.Intensity.ToString("F2");
        }

        public void SetActive(bool isActive)
        {
            _container.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Initialize()
        {
            if (_initialized) return;

            var root = _document.rootVisualElement;
            _container = root.Q<VisualElement>("lens-flare-container");
            _enabledToggle = root.Q<Toggle>("lens-flare-enabled");
            _intensitySlider = root.Q<Slider>("lens-flare-intensity");

            _intensitySlider.lowValue = 0f;
            _intensitySlider.highValue = IntensityMaxValue;
            _intensitySlider.value = 0f;

            _enabledToggle.RegisterValueChangedCallback(evt => NotifyValueChanged());
            _intensitySlider.RegisterValueChangedCallback(evt => 
            {
                _intensitySlider.label = evt.newValue.ToString("F2");
                NotifyValueChanged();
            });

            _initialized = true;
        }

        private void NotifyValueChanged()
        {
            _onValueChanged.OnNext(new ScreenSpaceLensFlareParameters
            {
                IsEnabled = _enabledToggle.value,
                Intensity = _intensitySlider.value,
            });
        }
    }
}
