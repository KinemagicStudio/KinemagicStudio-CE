using System.Collections.Generic;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class CameraPropertyControlView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        [Header("Parameter Ranges")]
        [SerializeField] float _minFocalLength = 6f;
        [SerializeField] float _maxFocalLength = 200f;
        [SerializeField] float _minFocusDistance = 0.1f;
        [SerializeField] float _maxFocusDistance = 100f;
        [SerializeField] float _minAperture = 1f;
        [SerializeField] float _maxAperture = 22f;

        private bool _initialized;
        private Slider _focalLengthSlider;
        private Label _focalLengthLabel;
        private Slider _focusDistanceSlider;
        private Label _focusDistanceLabel;
        private Slider _apertureSlider;
        private Label _apertureLabel;

        private readonly Subject<(CameraPropertyType propertyType, float value)> _onValueChanged = new();
        public Observable<(CameraPropertyType propertyType, float value)> ValueChanged => _onValueChanged.AsObservable();

        private void Awake()
        {
            Initialize();
        }

        public void UpdateCameraProperties(CameraProperties cameraProperties)
        {
            var focalLength = Mathf.Clamp(cameraProperties.FocalLength, _minFocalLength, _maxFocalLength);
            var focusDistance = Mathf.Clamp(cameraProperties.FocusDistance, _minFocusDistance, _maxFocusDistance);
            var aperture = Mathf.Clamp(cameraProperties.Aperture, _minAperture, _maxAperture);

            _focalLengthSlider.SetValueWithoutNotify(focalLength);
            // _focusDistanceSlider.SetValueWithoutNotify(focusDistance);
            // _apertureSlider.SetValueWithoutNotify(aperture);

            _focalLengthLabel.text = $"{focalLength:F1}mm";
            // _focusDistanceLabel.text = $"{focusDistance:F2}m";
            // _apertureLabel.text = $"f/{aperture:F1}";
        }

        private void Initialize()
        {
            if (_initialized) return;

            var root = _document.rootVisualElement;

            _focalLengthSlider = root.Q<Slider>("focal-length-slider");
            _focalLengthLabel = root.Q<Label>("focal-length-value");
            // _focusDistanceSlider = root.Q<Slider>("focus-distance-slider");
            // _focusDistanceLabel = root.Q<Label>("focus-distance-value");
            // _apertureSlider = root.Q<Slider>("aperture-slider");
            // _apertureLabel = root.Q<Label>("aperture-value");

            InitializeSliders();

            _initialized = true;
        }

        private void InitializeSliders()
        {
            _focalLengthSlider.lowValue = _minFocalLength;
            _focalLengthSlider.highValue = _maxFocalLength;
            _focalLengthSlider.RegisterValueChangedCallback(evt =>
            {
                _focalLengthLabel.text = $"{evt.newValue:F1}mm";
                _onValueChanged.OnNext((CameraPropertyType.FocalLength, evt.newValue));
            });

            // TODO:
            // https://github.com/Unity-Technologies/UnityLiveCapture/blob/main/Packages/com.unity.live-capture/Runtime/VirtualCamera/Utilities/FocusDistanceUtility.cs

            if (_focusDistanceSlider != null) // UIがコメントアウトされている場合のnullチェック
            {
                _focusDistanceSlider.lowValue = _minFocusDistance;
                _focusDistanceSlider.highValue = _maxFocusDistance;
                _focusDistanceSlider.RegisterValueChangedCallback(evt =>
                {
                    _focusDistanceLabel.text = $"{evt.newValue:F2}m";
                    _onValueChanged.OnNext((CameraPropertyType.FocusDistance, evt.newValue));
                });
            }

            if (_apertureSlider != null) // UIがコメントアウトされている場合のnullチェック
            {
                _apertureSlider.lowValue = _minAperture;
                _apertureSlider.highValue = _maxAperture;
                _apertureSlider.RegisterValueChangedCallback(evt =>
                {
                    _apertureLabel.text = $"f/{evt.newValue:F1}";
                    _onValueChanged.OnNext((CameraPropertyType.Aperture, evt.newValue));
                });
            }
        }
    }
}