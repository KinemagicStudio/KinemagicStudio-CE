using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    public sealed class SceneView : MonoBehaviour
    {
        private const string SceneViewImageContainerName = "scene-view-image-container";
        private const string CameraIdFieldName = "camera-id-field";
        private const string CameraIdDropdownName = "camera-id-dropdown";
        private const string CameraRollSliderName = "camera-roll-slider";
        private const string CameraRollValueLabelName = "camera-roll-value";

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private int _renderTextureWidth = 960;
        [SerializeField] private int _renderTextureHeight = 540;
        [SerializeField] private float _rotationScaleFactor = 0.5f;
        [SerializeField] private Vector3 _moveScaleFactor = new Vector3(0.01f, 0.01f, 0.001f);
        [SerializeField] private Camera _previewCamera;
        // [SerializeField] private Light _previewLight; // Experimental: Only AnimationEditorSceneView?

        private VisualElement _sceneViewContainer;
        private IntegerField _cameraIdField; // Only SequenceEditorSceneView?
        private DropdownField _cameraIdDropdown; // For TimelineEditor
        private Image _sceneViewImage;
        private RenderTexture _renderTexture;
        private Slider _cameraRollSlider;
        private Label _cameraRollValueLabel;
        private bool _initialized;
        private bool _isRotating;
        private bool _isMoving;
        private Vector2 _lastPointerPosition;
        private bool _isUpdatingSlider;

        public int CameraId 
        {
            get
            {
                // First try to get from dropdown (TimelineEditor)
                if (_cameraIdDropdown != null)
                {
                    var selectedIndex = _cameraIdDropdown.index;
                    // Map dropdown index to camera ID
                    // Index 0 = "Scene View" = -1
                    // Index 1 = "Camera 1" = 1
                    // Index 2 = "Camera 2" = 2, etc.
                    return selectedIndex == 0 ? -1 : selectedIndex;
                }
                // Fall back to integer field (KeyframeEditor)
                return _cameraIdField?.value ?? -1;
            }
        }
        public RenderTexture RenderTexture => _renderTexture;

        // --------------------------------
        // Only AnimationEditorSceneView?
        // --------------------------------
        Vector3 _previousPosition;
        Quaternion _previousRotation;
        float _previousRollAngle;
        public event Action<(Vector3 Position, Vector3 EulerAngles)> CameraPoseUpdated;
        // --------------------------------
        
        // Event for camera ID changes
        public event Action<int> CameraIdChanged;

        private void OnEnable()
        {
            Initialize();
            RegisterCallbacks();
            _sceneViewImage.style.display = DisplayStyle.Flex;
        }

        private void OnDisable()
        {
            UnregisterCallbacks();

            if (_sceneViewImage != null)
            {
                _sceneViewImage.style.display = DisplayStyle.None;
            }
        }

        private void OnDestroy()
        {
            _sceneViewContainer?.Clear();

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }

        // --------------------------------
        // Only AnimationEditorSceneView?
        // --------------------------------
        private void Update()
        {
            if (_previousPosition != _previewCamera.transform.position || _previousRotation != _previewCamera.transform.rotation)
            {
                CameraPoseUpdated?.Invoke((_previewCamera.transform.position, _previewCamera.transform.rotation.eulerAngles));
                _previousPosition = _previewCamera.transform.position;
                _previousRotation = _previewCamera.transform.rotation;
            }

            var currentRollAngle = _previewCamera.transform.rotation.eulerAngles.z;
            if (Mathf.Abs(currentRollAngle - _previousRollAngle) > 0.01f && !_isUpdatingSlider)
            {
                UpdateRollSlider(currentRollAngle);
                _previousRollAngle = currentRollAngle;
            }
        }
        // --------------------------------

        public void UpdateCameraTransform(Vector3 position, Quaternion rotation)
        {
            _previewCamera.transform.position = position;
            _previewCamera.transform.rotation = rotation;
        }

        public void UpdateCameraParameters(float fieldOfView)
        {
            _previewCamera.fieldOfView = fieldOfView;
        }

        private void Initialize()
        {
            if (_initialized) return;

            _sceneViewContainer = _uiDocument.rootVisualElement.Q(SceneViewImageContainerName);
            _cameraIdField = _uiDocument.rootVisualElement.Q<IntegerField>(CameraIdFieldName);
            _cameraIdDropdown = _uiDocument.rootVisualElement.Q<DropdownField>(CameraIdDropdownName);
            _cameraRollSlider = _uiDocument.rootVisualElement.Q<Slider>(CameraRollSliderName);
            _cameraRollValueLabel = _uiDocument.rootVisualElement.Q<Label>(CameraRollValueLabelName);

            _renderTexture = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 24);
            _previewCamera.targetTexture = _renderTexture;

            _sceneViewImage = new Image
            {
                image = _renderTexture,
                style =
                {
                    width = Length.Percent(100),
                    height = Length.Percent(100)
                }
            };
            _sceneViewContainer.Add(_sceneViewImage);

            if (_cameraRollSlider != null)
            {
                _cameraRollSlider.RegisterValueChangedCallback(OnRollSliderChanged);
                UpdateRollValueLabel(_cameraRollSlider.value);
            }
            
            if (_cameraIdDropdown != null)
            {
                _cameraIdDropdown.RegisterValueChangedCallback(OnCameraIdDropdownChanged);
            }

            _initialized = true;
        }

        private void RegisterCallbacks()
        {
            _sceneViewContainer?.RegisterCallback<PointerDownEvent>(PointerDownEventHandler);
            _sceneViewContainer?.RegisterCallback<PointerUpEvent>(PointerUpEventHandler);
            _sceneViewContainer?.RegisterCallback<PointerMoveEvent>(PointerMoveEventHandler);
            _sceneViewContainer?.RegisterCallback<WheelEvent>(WheelEventHandler);
        }
 
        private void UnregisterCallbacks()
        {
            _sceneViewContainer?.UnregisterCallback<PointerDownEvent>(PointerDownEventHandler);
            _sceneViewContainer?.UnregisterCallback<PointerUpEvent>(PointerUpEventHandler);
            _sceneViewContainer?.UnregisterCallback<PointerMoveEvent>(PointerMoveEventHandler);
            _sceneViewContainer?.UnregisterCallback<WheelEvent>(WheelEventHandler);
            _cameraRollSlider?.UnregisterValueChangedCallback(OnRollSliderChanged);
            _cameraIdDropdown?.UnregisterValueChangedCallback(OnCameraIdDropdownChanged);
        }

        private void PointerDownEventHandler(PointerDownEvent evt)
        {
            if (evt.button == 0) // Left mouse button
            {
                _isMoving = true;
                _lastPointerPosition = evt.position;
            }
            else if (evt.button == 1) // Right mouse button
            {
                _isRotating = true;
                _lastPointerPosition = evt.position;
            }
        }

        private void PointerUpEventHandler(PointerUpEvent evt)
        {
            if (evt.button == 0) // Left mouse button
            {
                _isMoving = false;
            }
            else if (evt.button == 1) // Right mouse button
            {
                _isRotating = false;
            }
        }

        private void PointerMoveEventHandler(PointerMoveEvent evt)
        {
            var delta = new Vector2(evt.position.x - _lastPointerPosition.x, evt.position.y - _lastPointerPosition.y);

            if (_isMoving)
            {
                Move(new Vector3(-delta.x * _moveScaleFactor.x, delta.y * _moveScaleFactor.y, 0));
            }
            else if (_isRotating)
            {
                Rotate(delta * _rotationScaleFactor);
            }

            _lastPointerPosition = evt.position;
        }

        private void WheelEventHandler(WheelEvent evt)
        {
            Move(new Vector3(0, 0, evt.delta.y * _moveScaleFactor.z));
        }

        private void Move(Vector3 deltaPosition)
        {
            var right = _previewCamera.transform.right * deltaPosition.x;
            var up = _previewCamera.transform.up * deltaPosition.y;
            var forward = _previewCamera.transform.forward * deltaPosition.z;
            _previewCamera.transform.position += (forward + right + up);
        }

        private void Rotate(Vector2 deltaAngle)
        {
            _previewCamera.transform.RotateAround(_previewCamera.transform.position, _previewCamera.transform.up, deltaAngle.x);
            _previewCamera.transform.RotateAround(_previewCamera.transform.position, _previewCamera.transform.right, deltaAngle.y);
        }

        private void OnRollSliderChanged(ChangeEvent<float> evt)
        {
            if (_isUpdatingSlider) return;
            
            var rollAngle = evt.newValue;
            var currentRotation = _previewCamera.transform.rotation.eulerAngles;
            _previewCamera.transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, rollAngle);

            UpdateRollValueLabel(rollAngle);
            _previousRollAngle = rollAngle;
        }
        
        private void UpdateRollSlider(float rollAngle)
        {
            if (_cameraRollSlider != null)
            {
                _isUpdatingSlider = true;
                
                if (rollAngle > 180f)
                {
                    rollAngle -= 360f;
                }
                
                _cameraRollSlider.SetValueWithoutNotify(rollAngle);
                UpdateRollValueLabel(rollAngle);
                
                _isUpdatingSlider = false;
            }
        }

        private void UpdateRollValueLabel(float rollAngle)
        {
            if (_cameraRollValueLabel != null)
            {
                _cameraRollValueLabel.text = $"{rollAngle:F0}°";
            }
        }
        
        private void OnCameraIdDropdownChanged(ChangeEvent<string> evt)
        {
            var newCameraId = CameraId;
            Debug.Log($"Camera ID changed to: {newCameraId} (dropdown value: {evt.newValue})");
            CameraIdChanged?.Invoke(newCameraId);
        }
    }
}
