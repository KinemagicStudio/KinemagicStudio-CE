using System.Linq;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class DepthOfFieldParametersView : MonoBehaviour, IPostProcessingEffectView
    {
        [SerializeField] private UIDocument _document;

        private static readonly float[] StandardApertureValues =
        {
            1.0f, 1.4f, 2.0f, 2.8f, 4.0f, 5.6f, 8.0f, 11.0f, 16.0f, 22.0f, 32.0f
        };

        private bool _initialized;
        private VisualElement _container;
        private Toggle _enabledToggle;
        private SliderInt _bladeCountSlider;
        private Slider _bladeCurvatureSlider;
        private Slider _bladeRotationSlider;
        private Slider _focusDistanceSlider;
        private SliderInt _apertureSlider;

        private readonly Subject<BokehDepthOfFieldParameters> _onValueChanged = new();
        public Observable<BokehDepthOfFieldParameters> ValueChanged => _onValueChanged;

        private void Awake()
        {
            Initialize();
        }

        public void UpdateParameters(BokehDepthOfFieldParameters parameters)
        {
            _enabledToggle.SetValueWithoutNotify(parameters.IsEnabled);

            _bladeCountSlider.SetValueWithoutNotify(parameters.BladeCount);
            _bladeCurvatureSlider.SetValueWithoutNotify(parameters.BladeCurvature);
            _bladeRotationSlider.SetValueWithoutNotify(parameters.BladeRotation);
            _focusDistanceSlider.SetValueWithoutNotify(FocusDistanceToSlider(parameters.FocusDistance));

            var apertureIndex = System.Array.IndexOf(StandardApertureValues, parameters.Aperture);
            if (apertureIndex >= 0)
            {
                _apertureSlider.SetValueWithoutNotify(apertureIndex);
            }

            _bladeCountSlider.label = parameters.BladeCount.ToString();
            _bladeCurvatureSlider.label = parameters.BladeCurvature.ToString("F2");
            _bladeRotationSlider.label = $"{parameters.BladeRotation:F1}°";
            _focusDistanceSlider.label = $"{parameters.FocusDistance:F1}m";
            _apertureSlider.label = $"f/{parameters.Aperture:F1}";
        }

        public void SetActive(bool isActive)
        {
            _container.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Initialize()
        {
            if (_initialized) return;

            var root = _document.rootVisualElement;
            _container = root.Q<VisualElement>("depth-of-field-container");
            _enabledToggle = root.Q<Toggle>("depth-of-field-enabled");
            _bladeCountSlider = root.Q<SliderInt>("blade-count");
            _bladeCurvatureSlider = root.Q<Slider>("blade-curvature");
            _bladeRotationSlider = root.Q<Slider>("blade-rotation");
            _focusDistanceSlider = root.Q<Slider>("focus-distance");
            _apertureSlider = root.Q<SliderInt>("aperture-slider");

            _bladeCountSlider.lowValue = 3;
            _bladeCountSlider.highValue = 9;
            _bladeCountSlider.value = 5;

            _bladeCurvatureSlider.lowValue = 0f;
            _bladeCurvatureSlider.highValue = 1f;
            _bladeCurvatureSlider.value = 1f;
    
            _bladeRotationSlider.lowValue = -180f;
            _bladeRotationSlider.highValue = 180f;
            _bladeRotationSlider.value = 0f;
            
            _focusDistanceSlider.lowValue = 0f;
            _focusDistanceSlider.highValue = 1f;
            _focusDistanceSlider.value = FocusDistanceToSlider(10f); // Convert 10m to slider value
            
            _apertureSlider.lowValue = 0;
            _apertureSlider.highValue = StandardApertureValues.Length - 1;
            _apertureSlider.value = 5; // Index for f/5.6

            _enabledToggle.RegisterValueChangedCallback(evt => NotifyValueChanged());
            _bladeCountSlider.RegisterValueChangedCallback(evt => 
            {
                _bladeCountSlider.label = evt.newValue.ToString();
                NotifyValueChanged();
            });
            _bladeCurvatureSlider.RegisterValueChangedCallback(evt => 
            {
                _bladeCurvatureSlider.label = evt.newValue.ToString("F2");
                NotifyValueChanged();
            });
            _bladeRotationSlider.RegisterValueChangedCallback(evt => 
            {
                _bladeRotationSlider.label = $"{evt.newValue:F1}°";
                NotifyValueChanged();
            });
            _focusDistanceSlider.RegisterValueChangedCallback(evt => 
            {
                var actualDistance = SliderToFocusDistance(evt.newValue);
                _focusDistanceSlider.label = $"{actualDistance:F1}m";
                NotifyValueChanged();
            });
            _apertureSlider.RegisterValueChangedCallback(evt => 
            {
                var apertureValue = StandardApertureValues[evt.newValue];
                _apertureSlider.label = $"f/{apertureValue:F1}";
                NotifyValueChanged();
            });

            _initialized = true;
        }

        private void NotifyValueChanged()
        {
            var apertureValue = StandardApertureValues[_apertureSlider.value];
            var focusDistance = SliderToFocusDistance(_focusDistanceSlider.value);

            _onValueChanged.OnNext(new BokehDepthOfFieldParameters
            {
                IsEnabled = _enabledToggle.value,
                BladeCount = _bladeCountSlider.value,
                BladeCurvature = _bladeCurvatureSlider.value,
                BladeRotation = _bladeRotationSlider.value,
                FocusDistance = focusDistance,
                Aperture = apertureValue
            });
        }

        private static float SliderToFocusDistance(float sliderValue)
        {
            // Convert slider value (0-1) to distance (0.1m-100m) with non-linear scaling
            //  - Near range (0.1-2m) gets 60% of slider range for precise control
            //  - Mid range (2-10m) gets 25% of slider range  
            //  - Far range (10-100m) gets 15% of slider range

            if (sliderValue <= 0.6f)
            {
                // 0-60%: 0.1m - 2m (fine control for close distances)
                return Mathf.Lerp(0.1f, 2f, sliderValue / 0.6f);
            }
            else if (sliderValue <= 0.85f)
            {
                // 60-85%: 2m - 10m (mid range)
                return Mathf.Lerp(2f, 10f, (sliderValue - 0.6f) / 0.25f);
            }
            else
            {
                // 85-100%: 10m - 100m (far range)
                return Mathf.Lerp(10f, 100f, (sliderValue - 0.85f) / 0.15f);
            }
        }

        private static float FocusDistanceToSlider(float distance)
        {
            // Inverse conversion from distance to slider value
            if (distance <= 2f)
            {
                return Mathf.InverseLerp(0.1f, 2f, distance) * 0.6f;
            }
            else if (distance <= 10f)
            {
                return 0.6f + Mathf.InverseLerp(2f, 10f, distance) * 0.25f;
            }
            else
            {
                return 0.85f + Mathf.InverseLerp(10f, 100f, distance) * 0.15f;
            }
        }
    }
}
