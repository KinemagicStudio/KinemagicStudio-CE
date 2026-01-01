using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using Kinemagic.Rendering.Universal;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CameraProperties = Kinemagic.Apps.Studio.Contracts.CameraSystem.CameraProperties;
using TonemappingMode = Kinemagic.Apps.Studio.Contracts.CameraSystem.TonemappingMode;

namespace Kinemagic.Apps.Studio.FeatureCore.CameraSystem
{
    public sealed class CameraActor : MonoBehaviour
    {
        // [SerializeField] int _id; // DEBUG ONLY?
        [SerializeField] Camera _camera;
        [SerializeField] CinemachineCamera _cinemachineCamera;
        // [SerializeField] CameraRotationController _rotationController;
        [SerializeField] Volume _volume;

        private ColorAdjustments _colorAdjustments;
        private DepthOfField _depthOfField;
        private Tonemapping _tonemapping;
        private Bloom _bloom;
        private ScreenSpaceLensFlare _screenSpaceLensFlare;
        private ScreenEdgeColor _screenEdgeColor;

        void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            if (_cinemachineCamera == null)
            {
                _cinemachineCamera = GetComponent<CinemachineCamera>();
            }

            // if (_rotationController == null)
            // {
            //     _rotationController = GetComponent<CameraRotationController>();
            // }

            if (_volume == null)
            {
                _volume = GetComponent<Volume>();
            }

            if (_volume != null && _volume.profile != null)
            {
                _volume.profile.TryGet(out _colorAdjustments);
                _volume.profile.TryGet(out _depthOfField);
                _volume.profile.TryGet(out _tonemapping);
                _volume.profile.TryGet(out _bloom);
                _volume.profile.TryGet(out _screenSpaceLensFlare);
                _volume.profile.TryGet(out _screenEdgeColor);
            }
        }

        // TODO: Delete
        // public int Id
        // {
        //     get => _id;
        //     set => _id = value;
        // }

        public int Id { get; set; }
        public Camera Camera => _camera;
        public CinemachineCamera CinemachineCamera => _cinemachineCamera;
        // public CameraRotationController RotationController => _rotationController;
        public Volume Volume => _volume;

        public Rect Rect
        {
            set => _camera.rect = value;
        }

        public float FocalLength
        {
            get => _camera.focalLength;
            set
            {
                _camera.focalLength = value;
                _cinemachineCamera.Lens.FieldOfView = Camera.FocalLengthToFieldOfView(Mathf.Max(0.01f, value), _camera.sensorSize.y);
                _depthOfField.focalLength.value = value;
            }
        }

        public float FocusDistance
        {
            get => _camera.focusDistance;
            set
            {
                _camera.focusDistance = value;
                _cinemachineCamera.Lens.PhysicalProperties.FocusDistance = value;
                _depthOfField.focusDistance.value = value;
            }
        }

        public float Aperture
        {
            get => _camera.aperture;
            set
            {
                var clampedValue = Mathf.Clamp(value, 1f, 32f);
                _camera.aperture = clampedValue;
                _cinemachineCamera.Lens.PhysicalProperties.Aperture = clampedValue;
                _depthOfField.aperture.value = clampedValue;
            }
        }

        public void SetWorldPosition(System.Numerics.Vector3 position)
        {
            transform.position = position.ToUnityVector3();
        }

        public void SetWorldRotation(System.Numerics.Vector3 eulerAngles)
        {
            transform.rotation = Quaternion.Euler(eulerAngles.X, eulerAngles.Y, eulerAngles.Z);
        }

        public void MoveCamera(System.Numerics.Vector3 deltaPosition)
        {
            var right = transform.right * deltaPosition.X;
            var up = transform.up * deltaPosition.Y;
            var forward = transform.forward * deltaPosition.Z;
            transform.position += (forward + right + up);
        }

        public void RotateCamera(System.Numerics.Vector3 deltaAngles, bool useLocalUpDirection)
        {
            if (useLocalUpDirection)
            {
                transform.RotateAround(transform.position, transform.up, deltaAngles.X);
                transform.RotateAround(transform.position, transform.right, deltaAngles.Y);
            }
            else
            {
                transform.RotateAround(transform.position, Vector3.up, deltaAngles.X);
                transform.RotateAround(transform.position, transform.right, deltaAngles.Y);
            }
        }

        public ColorAdjustmentParameters GetColorAdjustmentParameters()
        {
            return new ColorAdjustmentParameters
            {
                IsEnabled = _colorAdjustments.active,
                PostExposure = _colorAdjustments.postExposure.value,
                Contrast = _colorAdjustments.contrast.value,
                HueShift = _colorAdjustments.hueShift.value,
                Saturation = _colorAdjustments.saturation.value
            };
        }

        public BokehDepthOfFieldParameters GetBokehDepthOfFieldParameters()
        {
            return new BokehDepthOfFieldParameters
            {
                IsEnabled = _depthOfField.active,
                BladeCount = _depthOfField.bladeCount.value,
                BladeCurvature = _depthOfField.bladeCurvature.value,
                BladeRotation = _depthOfField.bladeRotation.value,
                FocusDistance = _depthOfField.focusDistance.value,
                Aperture = _depthOfField.aperture.value
            };
        }

        public TonemappingParameters GetTonemappingParameters()
        {
            return new TonemappingParameters
            {
                IsEnabled = _tonemapping.active,
                Mode = (TonemappingMode)_tonemapping.mode.value
            };
        }

        public BloomParameters GetBloomParameters()
        {
            return new BloomParameters
            {
                IsEnabled = _bloom.active,
                Intensity = _bloom.intensity.value,
                Threshold = _bloom.threshold.value
            };
        }

        public ScreenSpaceLensFlareParameters GetScreenSpaceLensFlareParameters()
        {
            return new ScreenSpaceLensFlareParameters
            {
                IsEnabled = _screenSpaceLensFlare.active,
                Intensity = _screenSpaceLensFlare.intensity.value
            };
        }

        public ScreenEdgeColorParameters GetScreenEdgeColorParameters()
        {
            return new ScreenEdgeColorParameters
            {
                IsEnabled = _screenEdgeColor.active,
                Intensity = _screenEdgeColor.Intensity,
                TopLeftColor = ColorToVector4(_screenEdgeColor.TopLeftColor),
                TopRightColor = ColorToVector4(_screenEdgeColor.TopRightColor),
                BottomLeftColor = ColorToVector4(_screenEdgeColor.BottomLeftColor),
                BottomRightColor = ColorToVector4(_screenEdgeColor.BottomRightColor)
            };
        }

        public void UpdateCameraProperties(CameraProperties cameraProperties)
        {
            FocalLength = cameraProperties.FocalLength;
            FocusDistance = cameraProperties.FocusDistance;
            Aperture = cameraProperties.Aperture;
        }

        public void UpdateColorAdjustmentParameters(ColorAdjustmentParameters parameters)
        {
            _colorAdjustments.active = parameters.IsEnabled;
            _colorAdjustments.postExposure.value = parameters.PostExposure;
            _colorAdjustments.contrast.value = parameters.Contrast;
            _colorAdjustments.hueShift.value = parameters.HueShift;
            _colorAdjustments.saturation.value = parameters.Saturation;
        }

        public void UpdateBokehDepthOfFieldParameters(BokehDepthOfFieldParameters parameters)
        {
            _depthOfField.active = parameters.IsEnabled;
            _depthOfField.bladeCount.value = parameters.BladeCount;
            _depthOfField.bladeCurvature.value = parameters.BladeCurvature;
            _depthOfField.bladeRotation.value = parameters.BladeRotation;
            FocusDistance = parameters.FocusDistance;
            Aperture = parameters.Aperture;
        }

        public void UpdateTonemappingParameters(TonemappingParameters parameters)
        {
            _tonemapping.active = parameters.IsEnabled;
            _tonemapping.mode.value = (UnityEngine.Rendering.Universal.TonemappingMode)parameters.Mode;
        }

        public void UpdateBloomParameters(BloomParameters parameters)
        {
            _bloom.active = parameters.IsEnabled;
            _bloom.intensity.value = parameters.Intensity;
            _bloom.threshold.value = parameters.Threshold;
        }

        public void UpdateScreenSpaceLensFlareParameters(ScreenSpaceLensFlareParameters parameters)
        {
            _screenSpaceLensFlare.active = parameters.IsEnabled;
            _screenSpaceLensFlare.intensity.value = parameters.Intensity;
        }

        public void UpdateScreenEdgeColorParameters(ScreenEdgeColorParameters parameters)
        {
            _screenEdgeColor.active = parameters.IsEnabled;
            _screenEdgeColor.IntensityParam.value = parameters.Intensity;
            _screenEdgeColor.TopLeftColorParam.value = Vector4ToColor(parameters.TopLeftColor);
            _screenEdgeColor.TopRightColorParam.value = Vector4ToColor(parameters.TopRightColor);
            _screenEdgeColor.BottomLeftColorParam.value = Vector4ToColor(parameters.BottomLeftColor);
            _screenEdgeColor.BottomRightColorParam.value = Vector4ToColor(parameters.BottomRightColor);
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
