using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using R3;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class ScreenEdgeColorParametersView : MonoBehaviour, IPostProcessingEffectView
    {
        [SerializeField] private UIDocument _document;

        private bool _initialized;
        private VisualElement _container;
        private Toggle _enabledToggle;
        private Slider _intensitySlider;
        private ColorField _topLeftColorField;
        private ColorField _topRightColorField;
        private ColorField _bottomLeftColorField;
        private ColorField _bottomRightColorField;

        public float IntensityMaxValue { get; } = 1f;

        private readonly Subject<ScreenEdgeColorParameters> _onValueChanged = new();
        public Observable<ScreenEdgeColorParameters> ValueChanged => _onValueChanged;

        private void Awake()
        {
            Initialize();
        }

        public void UpdateParameters(ScreenEdgeColorParameters parameters)
        {
            _enabledToggle.value = parameters.IsEnabled;
            _intensitySlider.value = parameters.Intensity;
            _topLeftColorField.value = Vector4ToColor(parameters.TopLeftColor);
            _topRightColorField.value = Vector4ToColor(parameters.TopRightColor);
            _bottomLeftColorField.value = Vector4ToColor(parameters.BottomLeftColor);
            _bottomRightColorField.value = Vector4ToColor(parameters.BottomRightColor);

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
            _container = root.Q<VisualElement>("screen-edge-color-container");
            _enabledToggle = root.Q<Toggle>("screen-edge-color-enabled");
            _intensitySlider = root.Q<Slider>("screen-edge-color-intensity");
            _topLeftColorField = root.Q<ColorField>("screen-edge-color-top-left");
            _topRightColorField = root.Q<ColorField>("screen-edge-color-top-right");
            _bottomLeftColorField = root.Q<ColorField>("screen-edge-color-bottom-left");
            _bottomRightColorField = root.Q<ColorField>("screen-edge-color-bottom-right");

            _intensitySlider.lowValue = 0f;
            _intensitySlider.highValue = IntensityMaxValue;
            _intensitySlider.value = 0f;

            _enabledToggle.RegisterValueChangedCallback(evt => NotifyValueChanged());
            _intensitySlider.RegisterValueChangedCallback(evt =>
            {
                _intensitySlider.label = evt.newValue.ToString("F2");
                NotifyValueChanged();
            });
            _topLeftColorField.RegisterValueChangedCallback(evt => NotifyValueChanged());
            _topRightColorField.RegisterValueChangedCallback(evt => NotifyValueChanged());
            _bottomLeftColorField.RegisterValueChangedCallback(evt => NotifyValueChanged());
            _bottomRightColorField.RegisterValueChangedCallback(evt => NotifyValueChanged());

            _initialized = true;
        }

        private void NotifyValueChanged()
        {
            _onValueChanged.OnNext(new ScreenEdgeColorParameters
            {
                IsEnabled = _enabledToggle.value,
                Intensity = _intensitySlider.value,
                TopLeftColor = ColorToVector4(_topLeftColorField.value),
                TopRightColor = ColorToVector4(_topRightColorField.value),
                BottomLeftColor = ColorToVector4(_bottomLeftColorField.value),
                BottomRightColor = ColorToVector4(_bottomRightColorField.value)
            });
        }

        private static System.Numerics.Vector4 ColorToVector4(Color color)
        {
            return new System.Numerics.Vector4(color.r, color.g, color.b, color.a);
        }

        private static Color Vector4ToColor(System.Numerics.Vector4 vector)
        {
            return new Color(vector.X, vector.Y, vector.Z, vector.W);
        }
    }
}
