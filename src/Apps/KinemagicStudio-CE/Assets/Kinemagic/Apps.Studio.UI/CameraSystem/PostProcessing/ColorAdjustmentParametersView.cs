using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class ColorAdjustmentParametersView : MonoBehaviour, IPostProcessingEffectView
    {
        [SerializeField] private UIDocument _document;

        private bool _initialized;
        private VisualElement _container;
        private Toggle _enabledToggle;
        private Slider _postExposureSlider;
        private Slider _contrastSlider;
        private Slider _hueShiftSlider;
        private Slider _saturationSlider;

        public float MinPostExposure { get; } = -10f;
        public float MaxPostExposure { get; } = 10f;
        public float MinContrast { get; } = -100f;
        public float MaxContrast { get; } = 100f;
        public float MinHueShift { get; } = -180f;
        public float MaxHueShift { get; } = 180f;
        public float MinSaturation { get; } = -100f;
        public float MaxSaturation { get; } = 100f;

        private readonly Subject<ColorAdjustmentParameters> _onValueChanged = new();
        public Observable<ColorAdjustmentParameters> ValueChanged => _onValueChanged;

        private void Awake()
        {
            Initialize();
        }

        public void UpdateParameters(ColorAdjustmentParameters parameters)
        {
            _enabledToggle.SetValueWithoutNotify(parameters.IsEnabled);

            _postExposureSlider.SetValueWithoutNotify(Mathf.Clamp(parameters.PostExposure, MinPostExposure, MaxPostExposure));
            _contrastSlider.SetValueWithoutNotify(Mathf.Clamp(parameters.Contrast, MinContrast, MaxContrast));
            _hueShiftSlider.SetValueWithoutNotify(Mathf.Clamp(parameters.HueShift, MinHueShift, MaxHueShift));
            _saturationSlider.SetValueWithoutNotify(Mathf.Clamp(parameters.Saturation, MinSaturation, MaxSaturation));

            _postExposureSlider.label = parameters.PostExposure.ToString("F1");
            _contrastSlider.label = parameters.Contrast.ToString("F1");
            _hueShiftSlider.label = $"{parameters.HueShift:F1}°";
            _saturationSlider.label = parameters.Saturation.ToString("F1");
        }

        public void SetActive(bool isActive)
        {
            _container.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Initialize()
        {
            if (_initialized) return;

            var root = _document.rootVisualElement;
            _container = root.Q<VisualElement>("color-adjustment-container");
            _enabledToggle = root.Q<Toggle>("color-adjustment-enabled");
            _postExposureSlider = root.Q<Slider>("post-exposure-slider");
            _contrastSlider = root.Q<Slider>("contrast-slider");
            _hueShiftSlider = root.Q<Slider>("hue-shift-slider");
            _saturationSlider = root.Q<Slider>("saturation-slider");

            _postExposureSlider.lowValue = MinPostExposure;
            _postExposureSlider.highValue = MaxPostExposure;
            _postExposureSlider.value = 0f;

            _contrastSlider.lowValue = MinContrast;
            _contrastSlider.highValue = MaxContrast;
            _contrastSlider.value = 0f;

            _hueShiftSlider.lowValue = MinHueShift;
            _hueShiftSlider.highValue = MaxHueShift;
            _hueShiftSlider.value = 0f;

            _saturationSlider.lowValue = MinSaturation;
            _saturationSlider.highValue = MaxSaturation;
            _saturationSlider.value = 0f;

            _postExposureSlider.label = "0.0";
            _contrastSlider.label = "0.0";
            _hueShiftSlider.label = "0.0°";
            _saturationSlider.label = "0.0";

            _enabledToggle.RegisterValueChangedCallback(evt => NotifyValueChanged());
            _postExposureSlider.RegisterValueChangedCallback(evt => 
            {
                _postExposureSlider.label = evt.newValue.ToString("F1");
                NotifyValueChanged();
            });
            _contrastSlider.RegisterValueChangedCallback(evt => 
            {
                _contrastSlider.label = evt.newValue.ToString("F1");
                NotifyValueChanged();
            });
            _hueShiftSlider.RegisterValueChangedCallback(evt => 
            {
                _hueShiftSlider.label = $"{evt.newValue:F1}°";
                NotifyValueChanged();
            });
            _saturationSlider.RegisterValueChangedCallback(evt => 
            {
                _saturationSlider.label = evt.newValue.ToString("F1");
                NotifyValueChanged();
            });

            _initialized = true;
        }

        private void NotifyValueChanged()
        {
            _onValueChanged.OnNext(new ColorAdjustmentParameters
            {
                IsEnabled = _enabledToggle.value,
                PostExposure = Mathf.Clamp(_postExposureSlider.value, MinPostExposure, MaxPostExposure),
                Contrast = Mathf.Clamp(_contrastSlider.value, MinContrast, MaxContrast),
                HueShift = Mathf.Clamp(_hueShiftSlider.value, MinHueShift, MaxHueShift),
                Saturation = Mathf.Clamp(_saturationSlider.value, MinSaturation, MaxSaturation)
            });
        }
    }
}