using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class BloomParametersView : MonoBehaviour, IPostProcessingEffectView
    {
        [SerializeField] private UIDocument _document;

        private bool _initialized;
        private VisualElement _container;
        private Toggle _enabledToggle;
        private Slider _thresholdSlider;
        private Slider _intensitySlider;
        private Slider _scatterSlider;

        public float IntensityMaxValue { get; } = 10f;
        public float ThresholdMaxValue { get; } = 3f;

        private readonly Subject<BloomParameters> _onValueChanged = new();
        public Observable<BloomParameters> ValueChanged => _onValueChanged;

        private void Awake()
        {
            Initialize();
        }

        public void UpdateParameters(BloomParameters parameters)
        {
            _enabledToggle.value = parameters.IsEnabled;

            _thresholdSlider.value = parameters.Threshold;
            _intensitySlider.value = parameters.Intensity;
            _scatterSlider.value = parameters.Scatter;

            _thresholdSlider.label = parameters.Threshold.ToString("F2");
            _intensitySlider.label = parameters.Intensity.ToString("F2");
            _scatterSlider.label = parameters.Scatter.ToString("F2");
        }

        public void SetActive(bool isActive)
        {
            _container.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Initialize()
        {
            if (_initialized) return;

            var root = _document.rootVisualElement;
            _container = root.Q<VisualElement>("bloom-container");
            _enabledToggle = root.Q<Toggle>("bloom-enabled");
            _thresholdSlider = root.Q<Slider>("bloom-threshold");
            _intensitySlider = root.Q<Slider>("bloom-intensity");
            _scatterSlider = root.Q<Slider>("bloom-scatter");

            _thresholdSlider.lowValue = 0f;
            _thresholdSlider.highValue = ThresholdMaxValue;
            _thresholdSlider.value = 0.9f;

            _intensitySlider.lowValue = 0f;
            _intensitySlider.highValue = IntensityMaxValue;
            _intensitySlider.value = 0.0f;

            _scatterSlider.lowValue = 0f;
            _scatterSlider.highValue = 1f;
            _scatterSlider.value = 0.7f;

            _enabledToggle.RegisterValueChangedCallback(evt => NotifyValueChanged());
            _thresholdSlider.RegisterValueChangedCallback(evt => 
            {
                _thresholdSlider.label = evt.newValue.ToString("F2");
                NotifyValueChanged();
            });
            _intensitySlider.RegisterValueChangedCallback(evt => 
            {
                _intensitySlider.label = evt.newValue.ToString("F2");
                NotifyValueChanged();
            });
            _scatterSlider.RegisterValueChangedCallback(evt => 
            {
                _scatterSlider.label = evt.newValue.ToString("F2");
                NotifyValueChanged();
            });

            _initialized = true;
        }

        private void NotifyValueChanged()
        {
            _onValueChanged.OnNext(new BloomParameters
            {
                IsEnabled = _enabledToggle.value,
                Threshold = _thresholdSlider.value,
                Intensity = _intensitySlider.value,
                Scatter = _scatterSlider.value
            });
        }
    }
}
