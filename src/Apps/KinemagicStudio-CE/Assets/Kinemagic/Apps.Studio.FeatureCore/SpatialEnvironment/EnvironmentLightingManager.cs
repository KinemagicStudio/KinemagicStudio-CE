using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class EnvironmentLightingManager : MonoBehaviour
    {
        public const string SkyboxShaderName = "Skybox/Cubemap";
        public readonly int SkyboxTexId = Shader.PropertyToID("_Tex");

        [SerializeField] private CubemapSizeType _cubemapSize = CubemapSizeType.W1024xH1024;
        [SerializeField] private LayerMask _renderingLayerMask = 1; // 1: Default layer

        private readonly CubemapFace[] _cubemapFaces =
        {
            CubemapFace.PositiveZ,
            CubemapFace.NegativeZ,
            CubemapFace.PositiveY,
            CubemapFace.NegativeY,
            CubemapFace.PositiveX,
            CubemapFace.NegativeX
        };

        private bool _initialized;
        private Camera _renderCamera;
        private RenderTexture _cubemapRenderTexture;
        private Material _skyboxMaterial;

        private AmbientMode _previousAmbientMode;
        private Material _previousSkyboxMaterial;
        private DefaultReflectionMode _previousReflectionMode;
        private Texture _previousCustomReflection;
        private float _previousReflectionIntensity;

        public LayerMask RenderingLayerMask
        {
            set => _renderingLayerMask = value;
        }

        #region MonoBehaviour Functions

        void Awake()
        {
            _renderCamera = GetComponent<Camera>();
        }

        void OnEnable()
        {
            _renderCamera.enabled = false;
        }

        void OnDestroy()
        {
            Dispose();
        }

        #endregion

        public void Dispose()
        {
            if (_skyboxMaterial != null)
            {
                DestroyUnityObject(_skyboxMaterial);
            }

            if (_cubemapRenderTexture != null)
            {
                _cubemapRenderTexture.Release();
                DestroyUnityObject(_cubemapRenderTexture);
            }

            // Restore previous settings
            RenderSettings.ambientMode = _previousAmbientMode;
            RenderSettings.skybox = _previousSkyboxMaterial;
            RenderSettings.defaultReflectionMode = _previousReflectionMode;
            RenderSettings.customReflectionTexture = _previousCustomReflection;
            RenderSettings.reflectionIntensity = _previousReflectionIntensity;

            _initialized = false;
        }

        public void Initialize()
        {
            if (_initialized) return;

            _skyboxMaterial = new Material(Shader.Find(SkyboxShaderName));

            var (width, height) = _cubemapSize.GetDimensions();
            CreateCubemapRenderTexture(width, height);

            // Store previous settings
            _previousAmbientMode = RenderSettings.ambientMode;
            _previousSkyboxMaterial = RenderSettings.skybox;
            _previousReflectionMode = RenderSettings.defaultReflectionMode;
            _previousCustomReflection = RenderSettings.customReflectionTexture;
            _previousReflectionIntensity = RenderSettings.reflectionIntensity;

            _initialized = true;
        }

        /// <summary>
        /// Update environment lighting by rendering the scene to a cubemap.
        /// </summary>
        public void UpdateEnvironmentLightingFromScene()
        {
            if (!_initialized)
            {
                Initialize();
            }

            var (width, height) = _cubemapSize.GetDimensions();
            if (_cubemapRenderTexture?.width != width || _cubemapRenderTexture?.height != height)
            {
                CreateCubemapRenderTexture(width, height);
            }

            RenderToCubemap();
            UpdateReflections();
            UpdateAmbientLighting();
        }

        private void CreateCubemapRenderTexture(int width, int height)
        {
            if (_cubemapRenderTexture != null)
            {
                _cubemapRenderTexture.Release();
                DestroyUnityObject(_cubemapRenderTexture);
            }

            _cubemapRenderTexture = new RenderTexture(width, height, depth: 24, RenderTextureFormat.ARGBFloat);
            _cubemapRenderTexture.dimension = TextureDimension.Cube;
            _cubemapRenderTexture.useMipMap = true;
            _cubemapRenderTexture.autoGenerateMips = false;
            _cubemapRenderTexture.Create();
        }

        private void RenderToCubemap()
        {
            var renderRequest = new UniversalRenderPipeline.SingleCameraRequest();
            renderRequest.destination = _cubemapRenderTexture;

            // Check if the active render pipeline supports the render request
            if (!RenderPipeline.SupportsRenderRequest(_renderCamera, renderRequest))
            {
                Debug.LogError("The current render pipeline does not support single camera render requests.");
                return;
            }

            // These must be set before calling ResetProjectionMatrix.
            // これらはResetProjectionMatrixを呼び出す前に設定する必要があります。
            _renderCamera.fieldOfView = 90f;
            _renderCamera.targetTexture = _cubemapRenderTexture;

            // Modify the projection matrix to prevent the cubemap from inverting vertically.
            // Cubemapが上下反転しないようにプロジェクション行列を変更する。
            _renderCamera.ResetProjectionMatrix();
            var newProjection = math.mul(
                new float4x4(
                    1, 0, 0, 0,
                    0, -1, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1
                ),
                _renderCamera.projectionMatrix
            );
            _renderCamera.projectionMatrix = newProjection;

            // Render cubemap faces
            foreach (var cubemapFace in _cubemapFaces)
            {
                _renderCamera.cullingMask = _renderingLayerMask;
                _renderCamera.transform.rotation = cubemapFace switch
                {
                    CubemapFace.PositiveZ => Quaternion.Euler(0, 0, 0),
                    CubemapFace.NegativeZ => Quaternion.Euler(0, 180, 0),
                    CubemapFace.PositiveY => Quaternion.Euler(-90, 0, 0),
                    CubemapFace.NegativeY => Quaternion.Euler(90, 0, 0),
                    CubemapFace.PositiveX => Quaternion.Euler(0, 90, 0),
                    CubemapFace.NegativeX => Quaternion.Euler(0, -90, 0),
                    _ => throw new ArgumentOutOfRangeException()
                };
                renderRequest.face = cubemapFace;
                RenderPipeline.SubmitRenderRequest(_renderCamera, renderRequest);
            }

            _cubemapRenderTexture.GenerateMips();
        }

        private void UpdateAmbientLighting()
        {
            _skyboxMaterial.SetTexture(SkyboxTexId, _cubemapRenderTexture);

            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.skybox = _skyboxMaterial;

            DynamicGI.UpdateEnvironment();
        }

        private void UpdateReflections()
        {
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = _cubemapRenderTexture;
            RenderSettings.reflectionIntensity = 1.0f;
        }

        private static void DestroyUnityObject(UnityEngine.Object o)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(o);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(o);
            }
        }
    }

    public enum CubemapSizeType
    {
        W256xH256,
        W512xH512,
        W1024xH1024,
        W2048xH2048,
        W4096xH4096,
    }

    public static class CubemapSizeTypeExtensions
    {
        public static (int, int) GetDimensions(this CubemapSizeType sizeType)
        {
            return sizeType switch
            {
                CubemapSizeType.W256xH256 => (256, 256),
                CubemapSizeType.W512xH512 => (512, 512),
                CubemapSizeType.W1024xH1024 => (1024, 1024),
                CubemapSizeType.W2048xH2048 => (2048, 2048),
                CubemapSizeType.W4096xH4096 => (4096, 4096),
                _ => throw new ArgumentOutOfRangeException(nameof(sizeType), sizeType, null)
            };
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(EnvironmentLightingManager))]
    public sealed class EnvironmentLightingManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var manager = target as EnvironmentLightingManager;
            if (manager == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Environment Lighting Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Update Environment Lighting From Scene"))
            {
                manager.UpdateEnvironmentLightingFromScene();
            }

            if (GUILayout.Button("Reset"))
            {
                manager.Dispose();
            }
        }
    }
#endif
}