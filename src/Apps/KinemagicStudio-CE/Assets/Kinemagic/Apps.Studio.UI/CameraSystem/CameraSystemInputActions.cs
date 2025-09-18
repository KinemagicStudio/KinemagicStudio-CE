using System.Collections.Generic;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class CameraSystemInputActions : MonoBehaviour
    {
        [SerializeField] private UIDocument _cameraOutputViewDocument;
        [SerializeField] private UIDocument _cameraControlViewDocument;
        [SerializeField] private Vector3 _movementScaleFactor = new Vector3(0.01f, 0.01f, 0.005f);
        [SerializeField] private float _rotationScaleFactor = 0.1f;

        private readonly KeyCode[] _cameraKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8,
            KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4,
            KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8,
        };
        private readonly Dictionary<KeyCode, CameraId> _cameraIdMap = new()
        {
            { KeyCode.Alpha1, new CameraId(1) },
            { KeyCode.Alpha2, new CameraId(2) },
            { KeyCode.Alpha3, new CameraId(3) },
            { KeyCode.Alpha4, new CameraId(4) },
            { KeyCode.Alpha5, new CameraId(5) },
            { KeyCode.Alpha6, new CameraId(6) },
            { KeyCode.Alpha7, new CameraId(7) },
            { KeyCode.Alpha8, new CameraId(8) },
            { KeyCode.Keypad1, new CameraId(1) },
            { KeyCode.Keypad2, new CameraId(2) },
            { KeyCode.Keypad3, new CameraId(3) },
            { KeyCode.Keypad4, new CameraId(4) },
            { KeyCode.Keypad5, new CameraId(5) },
            { KeyCode.Keypad6, new CameraId(6) },
            { KeyCode.Keypad7, new CameraId(7) },
            { KeyCode.Keypad8, new CameraId(8) }
        };

        private readonly Subject<CameraId> _onMainCameraSwitched = new();
        private readonly Subject<CameraId> _onControlTargetCameraSwitched = new();
        private readonly Subject<Vector3> _onMove = new();
        private readonly Subject<Vector2> _onRotate = new();
        private readonly Subject<bool> _onVerticalModeChanged = new();
        private readonly Subject<int> _onActiveCameraCountChanged = new();

        private UIViewContext _uiViewContext;

        private bool _enabled = true; // TODO
        private UIPageType _currentPage = UIPageType.Unknown;
        private DropdownField _cameraSelector;
        private VisualElement _cameraOutputImage;
        private VisualElement _raycastArea;
        private Vector2 _lastPointerPosition;
        private bool _isMovement;
        private bool _isRotation;
        
        private Toggle _verticalModeToggle;
        private DropdownField _numOfCamera;

        public Observable<CameraId> MainCameraSwitched => _onMainCameraSwitched.AsObservable();
        public Observable<CameraId> ControlTargetCameraSwitched => _onControlTargetCameraSwitched.AsObservable();
        public Observable<Vector3> Moved => _onMove.AsObservable();
        public Observable<Vector2> Rotated => _onRotate.AsObservable();
        public Observable<bool> VerticalModeChanged => _onVerticalModeChanged.AsObservable();
        public Observable<int> ActiveCameraCountChanged => _onActiveCameraCountChanged.AsObservable();

        #region MonoBehaviour Functions

        private void Awake()
        {
            _uiViewContext = GlobalContextProvider.UIViewContext;
            _uiViewContext.CurrentPage
                .Skip(1)
                .Subscribe(pageType =>
                {
                    _currentPage = pageType;
                    _enabled = _currentPage == UIPageType.CameraSwitcher ||
                               _currentPage == UIPageType.CameraControl;
                })
                .AddTo(this);

            _cameraOutputImage = _cameraOutputViewDocument.rootVisualElement.Q<VisualElement>("camera-output-image");
            _cameraOutputImage.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _cameraOutputImage.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _cameraOutputImage.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _cameraOutputImage.RegisterCallback<WheelEvent>(OnWheel);

            _raycastArea = _cameraControlViewDocument.rootVisualElement.Q<VisualElement>("raycast-area");
            _raycastArea.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _raycastArea.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _raycastArea.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _raycastArea.RegisterCallback<WheelEvent>(OnWheel);

            var cameraIdList = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8" };
            _cameraSelector = _cameraControlViewDocument.rootVisualElement.Q<DropdownField>("camera-selector");
            _cameraSelector.choices = cameraIdList;
            _cameraSelector.SetValueWithoutNotify("0");
            _cameraSelector.RegisterValueChangedCallback(evt =>
            {
                _onControlTargetCameraSwitched.OnNext(new CameraId(int.Parse(evt.newValue)));
            });

            _verticalModeToggle = _cameraOutputViewDocument.rootVisualElement.Q<Toggle>("vertical-mode-toggle");
            _verticalModeToggle.RegisterValueChangedCallback(evt =>
            {
                _onVerticalModeChanged.OnNext(evt.newValue);
            });

            _numOfCamera = _cameraOutputViewDocument.rootVisualElement.Q<DropdownField>("num-of-camera-dropdown");
            _numOfCamera.RegisterValueChangedCallback(evt =>
            {
                _onActiveCameraCountChanged.OnNext(int.Parse(evt.newValue));
            });
        }

        private void Start()
        {
            if (_cameraSelector != null)
            {
                _cameraSelector.value = "1";
            }
        }

        private void Update()
        {
            if (!_enabled) return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _currentPage = _currentPage == UIPageType.CameraSwitcher ? UIPageType.CameraControl : UIPageType.CameraSwitcher;
                _uiViewContext.CurrentPage.Value = _currentPage;
            }

            HandleCameraKeyInput();
        }

        #endregion

        private void HandleCameraKeyInput()
        {
            var cameraId = new CameraId(-1);
            foreach (var key in _cameraKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    _cameraIdMap.TryGetValue(key, out cameraId);
                }
            }

            if (_currentPage == UIPageType.CameraSwitcher && cameraId.Value != -1)
            {
                _onMainCameraSwitched.OnNext(cameraId);
            }
            else if (_currentPage == UIPageType.CameraControl && cameraId.Value != -1)
            {
                _cameraSelector?.SetValueWithoutNotify(cameraId.Value.ToString());
                _onControlTargetCameraSwitched.OnNext(cameraId);
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 2) // Middle mouse button
            {
                _isMovement = true;
                _lastPointerPosition = evt.position;
            }
            else if (evt.button == 1) // Right mouse button
            {
                _isRotation = true;
                _lastPointerPosition = evt.position;
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.button == 2) // Middle mouse button
            {
                _isMovement = false;
            }
            else if (evt.button == 1) // Right mouse button
            {
                _isRotation = false;
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            var delta = new Vector2(evt.position.x - _lastPointerPosition.x, evt.position.y - _lastPointerPosition.y);

            if (_isMovement)
            {
                _onMove.OnNext(new Vector3(-delta.x * _movementScaleFactor.x, delta.y * _movementScaleFactor.y, 0));
            }
            else if (_isRotation)
            {
                _onRotate.OnNext(delta * _rotationScaleFactor);
            }

            _lastPointerPosition = evt.position;
        }

        private void OnWheel(WheelEvent evt)
        {
            _onMove.OnNext(new Vector3(0, 0, evt.delta.y * _movementScaleFactor.z));
        }
    }
}
