using System.Collections.Generic;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Kinemagic.Apps.Studio.FeatureCore.CameraSystem
{
    public sealed class CameraActorManager : MonoBehaviour
    {
        [SerializeField] UniversalAdditionalCameraData _mainCameraData;
        [SerializeField] CameraActor _sceneViewCamera;
        [SerializeField] List<CameraActor> _cameraActors = new();

        private const float VerticalModeRectOffsetX = (1920f - 1080f / 1920f * 1080f) / 2f / 1920f; // Normalized value

        private const float SplitViewScaleFactor2 = 0.5f;
        private const float VerticalModeSplitViewRectOffsetX2 = VerticalModeRectOffsetX * SplitViewScaleFactor2;
        private const float VerticalModeSplitViewWidth2 = SplitViewScaleFactor2 - VerticalModeSplitViewRectOffsetX2 * 2;

        private readonly Rect[] _multiCameraSplitViewRects2 =
        {
            new Rect(0f  , 0f, 0.5f, 0.5f), // Camera 1
            new Rect(0.5f, 0f, 0.5f, 0.5f), // Camera 2
        };

        private readonly Rect[] _verticalModeMultiCameraSplitViewRects2 =
        {
            new Rect(0f   + VerticalModeSplitViewRectOffsetX2, 0f, VerticalModeSplitViewWidth2, 0.5f), // Camera 1
            new Rect(0.5f + VerticalModeSplitViewRectOffsetX2, 0f, VerticalModeSplitViewWidth2, 0.5f), // Camera 2
        };

        private readonly Rect[] _multiCameraSplitViewRects =
        {
            new Rect(0f,    0.25f, 0.25f, 0.25f), // Camera 1
            new Rect(0.25f, 0.25f, 0.25f, 0.25f), // Camera 2
            new Rect(0.5f,  0.25f, 0.25f, 0.25f), // Camera 3
            new Rect(0.75f, 0.25f, 0.25f, 0.25f), // Camera 4
            new Rect(0f,    0f,    0.25f, 0.25f), // Camera 5
            new Rect(0.25f, 0f,    0.25f, 0.25f), // Camera 6
            new Rect(0.5f,  0f,    0.25f, 0.25f), // Camera 7
            new Rect(0.75f, 0f,    0.25f, 0.25f), // Camera 8
        };

        private const float SplitViewScaleFactor = 0.25f;
        private const float VerticalModeSplitViewRectOffsetX = VerticalModeRectOffsetX * SplitViewScaleFactor;
        private const float VerticalModeSplitViewWidth = SplitViewScaleFactor - VerticalModeSplitViewRectOffsetX * 2;

        private readonly Rect[] _verticalModeMultiCameraSplitViewRects =
        {
            new Rect(0f    + VerticalModeSplitViewRectOffsetX, 0.25f, VerticalModeSplitViewWidth, 0.25f), // Camera 1
            new Rect(0.25f + VerticalModeSplitViewRectOffsetX, 0.25f, VerticalModeSplitViewWidth, 0.25f), // Camera 2
            new Rect(0.5f  + VerticalModeSplitViewRectOffsetX, 0.25f, VerticalModeSplitViewWidth, 0.25f), // Camera 3
            new Rect(0.75f + VerticalModeSplitViewRectOffsetX, 0.25f, VerticalModeSplitViewWidth, 0.25f), // Camera 4
            new Rect(0f    + VerticalModeSplitViewRectOffsetX, 0f,    VerticalModeSplitViewWidth, 0.25f), // Camera 5
            new Rect(0.25f + VerticalModeSplitViewRectOffsetX, 0f,    VerticalModeSplitViewWidth, 0.25f), // Camera 6
            new Rect(0.5f  + VerticalModeSplitViewRectOffsetX, 0f,    VerticalModeSplitViewWidth, 0.25f), // Camera 7
            new Rect(0.75f + VerticalModeSplitViewRectOffsetX, 0f,    VerticalModeSplitViewWidth, 0.25f), // Camera 8
        };

        private bool _initialized;
        private RenderTextureRegistry _renderTextureRegistry;
        private Camera _mainCamera;
        private CameraOutputMode _cameraOutputMode;
        private int _singleViewCameraId = 1;
        private int _activeCameraCount;
        private bool _isVerticalMode;

        #region MonoBehaviour Functions

        private void Start()
        {
            Initialize();
            SetSceneViewOutput();
        }

        #endregion

        public void Initialize()
        {
            if (_initialized) return;

            _renderTextureRegistry = GlobalRenderTextureRegistry.Get();

            // Setup main camera
            _mainCamera = _mainCameraData.GetComponent<Camera>();

            var mainCameraTextureKey = Constants.MainCameraOutputTextureDataKey;
            var mainCameraTextureWidth = Constants.MainCameraOutputTextureWidth;
            var mainCameraTextureHeight = Constants.MainCameraOutputTextureHeight;
            _mainCamera.targetTexture = _renderTextureRegistry.GetOrCreate(mainCameraTextureKey, mainCameraTextureWidth, mainCameraTextureHeight);

            var verticalModeCameraTextureKey = Constants.VerticalModeMainCameraOutputTextureDataKey;
            var verticalModeCameraTextureWidth = Constants.VerticalModeMainCameraOutputTextureWidth;
            var verticalModeCameraTextureHeight = Constants.VerticalModeMainCameraOutputTextureHeight;
            _renderTextureRegistry.GetOrCreate(verticalModeCameraTextureKey, verticalModeCameraTextureWidth, verticalModeCameraTextureHeight);

            // Setup cameras
            var textureKey = Constants.MultiCameraViewOutputTextureDataKey;
            var textureWidth = Constants.MultiCameraViewOutputTextureWidth;
            var textureHeight = Constants.MultiCameraViewOutputTextureHeight;

            var excludeMask = 1 << LayerMask.NameToLayer(Constants.SceneViewCameraLayerName);
            foreach (var cameraLayerName in Constants.CameraLayerNames.Values)
            {
                excludeMask |= 1 << LayerMask.NameToLayer(cameraLayerName);
            }

            for (var i = 0; i < _cameraActors.Count; i++)
            {
                var id = i + 1;
                var cameraLayer = LayerMask.NameToLayer(Constants.CameraLayerNames[id]);

                _cameraActors[i].Id = id;
                _cameraActors[i].Camera.cullingMask = ~excludeMask | (1 << cameraLayer);
                _cameraActors[i].gameObject.layer = cameraLayer;

                _cameraActors[i].Camera.targetTexture = _renderTextureRegistry.GetOrCreate(textureKey, textureWidth, textureHeight);
                _cameraActors[i].Rect = new Rect(0f, 0f, 0f, 0f);
            }

            // Setup scene view camera
            if (_renderTextureRegistry.TryGet(textureKey, out var multiCameraViewOutputTexture))
            {
                _sceneViewCamera.Camera.targetTexture = multiCameraViewOutputTexture;
                _sceneViewCamera.Rect = new Rect(0f, 0f, 1f, 1f);
            }

            _activeCameraCount = _verticalModeMultiCameraSplitViewRects.Length;
            _initialized = true;
        }

        public bool TryGetSceneViewCameraActor(out CameraActor cameraActor)
        {
            cameraActor = _sceneViewCamera;
            return cameraActor != null;
        }

        public bool TryGetCameraActor(CameraId cameraId, out CameraActor cameraActor)
        {
            cameraActor = null;

            if (0 < cameraId.Value && cameraId.Value <= _cameraActors.Count)
            {
                cameraActor = _cameraActors[cameraId.Value - 1];
                return true;
            }

            return false;
        }

        public void SwitchCamera(int cameraId, bool isMainCamera)
        {
            if (cameraId > _activeCameraCount) return;

            if (isMainCamera)
            {
                if (Constants.CameraLayerNames.TryGetValue(cameraId, out var cameraLayerName))
                {
                    _mainCameraData.volumeLayerMask = (1 << LayerMask.NameToLayer("Default"))
                                                    | (1 << LayerMask.NameToLayer(cameraLayerName));
                }

                foreach (var cameraActor in _cameraActors)
                {
                    cameraActor.CinemachineCamera.Priority = cameraId == cameraActor.Id ? 100 : 0;
                }
            }
            else
            {
                _singleViewCameraId = cameraId;
                SetSingleCameraViewOutput();
            }
        }

        public void SetCameraOutputMode(CameraOutputMode mode)
        {
            _cameraOutputMode = mode;

            if (_cameraOutputMode == CameraOutputMode.SplitViewOutput)
            {
                SetMultiCameraSplitViewOutput();
            }
            else if (_cameraOutputMode == CameraOutputMode.SingleViewOutput)
            {
                SetSingleCameraViewOutput();
            }
            else if (_cameraOutputMode == CameraOutputMode.SceneViewOutput)
            {
                SetSceneViewOutput();
            }
        }

        public void SetActiveCameraCount(int value)
        {
            if (value < 2)
            {
                value = 2;
            }
            else if (value > _multiCameraSplitViewRects.Length)
            {
                value = _multiCameraSplitViewRects.Length;
            }

            _activeCameraCount = value;
            for (var i = 0; i < _cameraActors.Count; i++)
            {
                _cameraActors[i].gameObject.SetActive(i < _activeCameraCount);
            }

            if (_cameraOutputMode == CameraOutputMode.SplitViewOutput)
            {
                SetMultiCameraSplitViewOutput();
            }
            else if (_cameraOutputMode == CameraOutputMode.SingleViewOutput)
            {
                SetSingleCameraViewOutput();
            }
        }

        public void SetVerticalMode(bool isVerticalMode)
        {
            _isVerticalMode = isVerticalMode;

            var key = _isVerticalMode
                ? Constants.VerticalModeMainCameraOutputTextureDataKey
                : Constants.MainCameraOutputTextureDataKey;

            if (_renderTextureRegistry.TryGet(key, out var texture))
            {
                _mainCamera.targetTexture = texture;
            }

            if (_cameraOutputMode == CameraOutputMode.SplitViewOutput)
            {
                SetMultiCameraSplitViewOutput();
            }
            else if (_cameraOutputMode == CameraOutputMode.SingleViewOutput)
            {
                SetSingleCameraViewOutput();
            }
        }

        private void SetMultiCameraSplitViewOutput()
        {
            _sceneViewCamera.Rect = new Rect(0f, 0f, 0f, 0f);

            for (var index = 0; index < _cameraActors.Count; index++)
            {
                if (index < _activeCameraCount)
                {
                    if (_activeCameraCount == 2)
                    {
                        _cameraActors[index].Rect = _isVerticalMode
                                                    ? _verticalModeMultiCameraSplitViewRects2[index]
                                                    : _multiCameraSplitViewRects2[index];
                    }
                    else
                    {
                        _cameraActors[index].Rect = _isVerticalMode
                                                    ? _verticalModeMultiCameraSplitViewRects[index]
                                                    : _multiCameraSplitViewRects[index];
                    }
                }
                else
                {
                    _cameraActors[index].Rect = new Rect(0f, 0f, 0f, 0f);
                }
            }

            if (_renderTextureRegistry.TryGet(Constants.MultiCameraViewOutputTextureDataKey, out var renderTexture))
            {
                renderTexture.Release();
            }
        }

        private void SetSingleCameraViewOutput()
        {
            _sceneViewCamera.Rect = new Rect(0f, 0f, 0f, 0f);

            for (var id = 1; id <= _cameraActors.Count; id++)
            {
                var index = id - 1;
                if (id == _singleViewCameraId)
                {
                    _cameraActors[index].Rect = _isVerticalMode
                                                ? new Rect(VerticalModeRectOffsetX, 0f, 1f - VerticalModeRectOffsetX * 2, 1f)
                                                : new Rect(0f, 0f, 1f, 1f);
                }
                else
                {
                    _cameraActors[index].Rect = new Rect(0f, 0f, 0f, 0f);
                }
            }

            if (_renderTextureRegistry.TryGet(Constants.MultiCameraViewOutputTextureDataKey, out var renderTexture))
            {
                renderTexture.Release();
            }
        }

        private void SetSceneViewOutput()
        {
            _sceneViewCamera.Rect = new Rect(0f, 0f, 1f, 1f);

            foreach (var cameraActor in _cameraActors)
            {
                cameraActor.Rect = new Rect(0f, 0f, 0f, 0f);
            }
        }
    }
}