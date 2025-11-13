using System;
using System.Collections.Generic;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using MessagePipe;
using R3;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CameraProperties = Kinemagic.Apps.Studio.Contracts.CameraSystem.CameraProperties;
using Debug = UnityEngine.Debug;

namespace Kinemagic.Apps.Studio.FeatureCore.CameraSystem
{
    public sealed class CameraSystem : IDisposable, IInitializable
    {
        private const string TrackingTargetShaderName = "Unlit/Color";

        private readonly CompositeDisposable _disposables = new();
        private readonly ISubscriber<ICameraSystemCommand> _commandSubscriber;
        private readonly IPublisher<ICameraSystemSignal> _signalPublisher;
        private readonly CameraActorManager _cameraActorManager;
        private readonly Dictionary<CameraId, Transform> _cameraTrackingTargets = new();
        // private readonly Dictionary<CameraId, CameraRotationController> _rotationControllers = new(); // TODO
        private readonly Vector2 _rotationControlScaleFactor = new Vector2(0.45f / 0.5f, 0.35f / 0.5f);
        private readonly string[] _excludeLayerNames;

        private bool _initialized;

        public CameraSystem(
            ISubscriber<ICameraSystemCommand> commandSubscriber,
            IPublisher<ICameraSystemSignal> signalPublisher,
            CameraActorManager cameraActorManager)
        {
            _commandSubscriber = commandSubscriber;
            _signalPublisher = signalPublisher;
            _cameraActorManager = cameraActorManager;

            var layerNameList = new List<string>
            {
                "Ignore Raycast",
                "UI",
                Constants.SceneViewCameraLayerName
            };
            layerNameList.AddRange(Constants.CameraLayerNames.Values);
            _excludeLayerNames = layerNameList.ToArray();

            Debug.Log($"[{nameof(CameraSystem)}] Constructed");
        }

        public void Dispose()
        {
            _disposables.Dispose();
            Debug.Log($"[{nameof(CameraSystem)}] Disposed");
        }

        public void Initialize()
        {
            if (_initialized) return;

            _cameraActorManager.Initialize();
            _cameraActorManager.SwitchCamera(1, isMainCamera: true);
            _commandSubscriber.Subscribe(OnCommandReceived).AddTo(_disposables);

            _initialized = true;
            Debug.Log($"[{nameof(CameraSystem)}] Initialized");
        }

        private void OnCommandReceived(ICameraSystemCommand command)
        {
            if (command is CameraSwitchCommand cameraSwitchCommand)
            {
                Debug.Log($"[{nameof(CameraSystem)}] CameraSwitchCommand - CameraId: {cameraSwitchCommand.CameraId}");
                _cameraActorManager.SwitchCamera(cameraSwitchCommand.CameraId.Value, cameraSwitchCommand.IsMainCamera);
            }
            else if (command is CameraOutputModeUpdateCommand outputModeUpdateCommand)
            {
                _cameraActorManager.SetCameraOutputMode(outputModeUpdateCommand.OutputMode);
            }
            else if (command is CameraVerticalModeUpdateCommand verticalModeUpdateCommand)
            {
                _cameraActorManager.SetVerticalMode(verticalModeUpdateCommand.IsVerticalMode);
            }
            else if (command is ActiveCameraCountUpdateCommand activeCameraCountUpdateCommand)
            {
                _cameraActorManager.SetActiveCameraCount(activeCameraCountUpdateCommand.Count);
            }
            else if (command is CameraPositionUpdateCommand cameraPositionUpdateCommand)
            {
                if (cameraPositionUpdateCommand.CameraId.Value == 0)
                {
                    if (_cameraActorManager.TryGetSceneViewCameraActor(out var sceneViewCameraActor))
                    {
                        sceneViewCameraActor.MoveCamera(cameraPositionUpdateCommand.Position);
                    }
                }
                else if (_cameraActorManager.TryGetCameraActor(cameraPositionUpdateCommand.CameraId, out var cameraActor))
                {
                    if (cameraPositionUpdateCommand.Type == CameraPositionUpdateCommandType.Movement)
                    {
                        cameraActor.MoveCamera(cameraPositionUpdateCommand.Position);
                    }
                    else if (cameraPositionUpdateCommand.Type == CameraPositionUpdateCommandType.WorldSpacePosition)
                    {
                        cameraActor.SetWorldPosition(cameraPositionUpdateCommand.Position);
                    }
                }
            }
            else if (command is CameraRotationUpdateCommand cameraRotationUpdateCommand)
            {
                if (cameraRotationUpdateCommand.CameraId.Value == 0)
                {
                    if (_cameraActorManager.TryGetSceneViewCameraActor(out var sceneViewCameraActor))
                    {
                        sceneViewCameraActor.RotateCamera(cameraRotationUpdateCommand.EulerAngles, false);
                    }
                }
                else if (_cameraActorManager.TryGetCameraActor(cameraRotationUpdateCommand.CameraId, out var cameraActor))
                {
                    if (cameraRotationUpdateCommand.Type == CameraRotationUpdateCommandType.RotateAroundWorldUp)
                    {
                        cameraActor.RotateCamera(cameraRotationUpdateCommand.EulerAngles, false);
                    }
                    else if (cameraRotationUpdateCommand.Type == CameraRotationUpdateCommandType.RotateAroundLocalUp)
                    {
                        cameraActor.RotateCamera(cameraRotationUpdateCommand.EulerAngles, true);
                    }
                    else if (cameraRotationUpdateCommand.Type == CameraRotationUpdateCommandType.WorldSpaceRotation)
                    {
                        cameraActor.SetWorldRotation(cameraRotationUpdateCommand.EulerAngles);
                    }
                }
            }
            else if (command is CameraPropertiesUpdateCommand cameraPropertiesUpdateCommand)
            {
                var cameraId = cameraPropertiesUpdateCommand.CameraId;
                var cameraProperties = cameraPropertiesUpdateCommand.CameraProperties;

                Debug.Log($"[{nameof(CameraSystem)}] CameraPropertiesUpdated - CameraId: {cameraId}");

                if (_cameraActorManager.TryGetCameraActor(cameraId, out var cameraActor))
                {
                    cameraActor.UpdateCameraProperties(cameraProperties);
                    _signalPublisher.Publish(new CameraPropertiesUpdatedSignal(
                        new CameraId(cameraActor.Id),
                        new CameraProperties()
                        {
                            FocalLength = cameraActor.FocalLength,
                            FocusDistance = cameraActor.FocusDistance,
                            Aperture = cameraActor.Aperture,
                        }));
                }
            }
            else if (command is CameraPropertiesNotifyCommand cameraPropertiesNotifyCommand)
            {
                Debug.Log($"[{nameof(CameraSystem)}] CameraPropertiesNotify: {cameraPropertiesNotifyCommand.CameraId}");

                if (_cameraActorManager.TryGetCameraActor(cameraPropertiesNotifyCommand.CameraId, out var cameraActor))
                {
                    _signalPublisher.Publish(new CameraPropertiesUpdatedSignal(
                        new CameraId(cameraActor.Id),
                        new CameraProperties()
                        {
                            FocalLength = cameraActor.FocalLength,
                            FocusDistance = cameraActor.FocusDistance,
                            Aperture = cameraActor.Aperture,
                        }));
                }
            }
            else if (command is PostProcessingParametersNotifyCommand postProcessingParametersNotifyCommand)
            {
                Debug.Log($"[{nameof(CameraSystem)}] PostProcessingParametersNotify: {postProcessingParametersNotifyCommand.CameraId}");

                if (_cameraActorManager.TryGetCameraActor(postProcessingParametersNotifyCommand.CameraId, out var cameraActor))
                {
                    var colorAdjustment = cameraActor.GetColorAdjustmentParameters();
                    _signalPublisher.Publish(new PostProcessingUpdatedSignal(new CameraId(cameraActor.Id), colorAdjustment));

                    var tonemapping = cameraActor.GetTonemappingParameters();
                    _signalPublisher.Publish(new PostProcessingUpdatedSignal(new CameraId(cameraActor.Id), tonemapping));

                    var depthOfField = cameraActor.GetBokehDepthOfFieldParameters();
                    _signalPublisher.Publish(new PostProcessingUpdatedSignal(new CameraId(cameraActor.Id), depthOfField));

                    var bloom = cameraActor.GetBloomParameters();
                    _signalPublisher.Publish(new PostProcessingUpdatedSignal(new CameraId(cameraActor.Id), bloom));

                    var screenSpaceLensFlare = cameraActor.GetScreenSpaceLensFlareParameters();
                    _signalPublisher.Publish(new PostProcessingUpdatedSignal(new CameraId(cameraActor.Id), screenSpaceLensFlare));

                    var screenEdgeColor = cameraActor.GetScreenEdgeColorParameters();
                    _signalPublisher.Publish(new PostProcessingUpdatedSignal(new CameraId(cameraActor.Id), screenEdgeColor));
                }
            }
            else if (command is PostProcessingUpdateCommand postProcessingUpdateCommand)
            {
                Debug.Log($"[{nameof(CameraSystem)}] PostProcessingUpdate - CameraId: {postProcessingUpdateCommand.CameraId}");

                if (_cameraActorManager.TryGetCameraActor(postProcessingUpdateCommand.CameraId, out var cameraActor))
                {
                    if (postProcessingUpdateCommand.Parameters is ColorAdjustmentParameters colorAdjustment)
                    {
                        cameraActor.UpdateColorAdjustmentParameters(colorAdjustment);
                        _signalPublisher.Publish(new PostProcessingUpdatedSignal(
                            new CameraId(cameraActor.Id),
                            new ColorAdjustmentParameters()
                            {
                                IsEnabled = colorAdjustment.IsEnabled,
                                PostExposure = colorAdjustment.PostExposure,
                                Contrast = colorAdjustment.Contrast,
                                HueShift = colorAdjustment.HueShift,
                                Saturation = colorAdjustment.Saturation,
                            }));
                    }
                    else if (postProcessingUpdateCommand.Parameters is TonemappingParameters toneMapping)
                    {
                        cameraActor.UpdateTonemappingParameters(toneMapping);
                        _signalPublisher.Publish(new PostProcessingUpdatedSignal(
                            new CameraId(cameraActor.Id),
                            new TonemappingParameters()
                            {
                                IsEnabled = toneMapping.IsEnabled,
                                Mode = toneMapping.Mode
                            }));
                    }
                    else if (postProcessingUpdateCommand.Parameters is BokehDepthOfFieldParameters depthOfField)
                    {
                        cameraActor.UpdateBokehDepthOfFieldParameters(depthOfField);
                        _signalPublisher.Publish(new PostProcessingUpdatedSignal(
                            new CameraId(cameraActor.Id),
                            new BokehDepthOfFieldParameters()
                            {
                                IsEnabled = depthOfField.IsEnabled,
                                BladeCount = depthOfField.BladeCount,
                                BladeCurvature = depthOfField.BladeCurvature,
                                BladeRotation = depthOfField.BladeRotation,
                                FocusDistance = depthOfField.FocusDistance,
                                Aperture = depthOfField.Aperture
                            }));
                    }
                    else if (postProcessingUpdateCommand.Parameters is BloomParameters bloom)
                    {
                        cameraActor.UpdateBloomParameters(bloom);
                        _signalPublisher.Publish(new PostProcessingUpdatedSignal(
                            new CameraId(cameraActor.Id),
                            new BloomParameters()
                            {
                                IsEnabled = bloom.IsEnabled,
                                Intensity = bloom.Intensity,
                                Threshold = bloom.Threshold,
                                Scatter = bloom.Scatter
                            }));
                    }
                    else if (postProcessingUpdateCommand.Parameters is ScreenSpaceLensFlareParameters lensFlare)
                    {
                        cameraActor.UpdateScreenSpaceLensFlareParameters(lensFlare);
                        _signalPublisher.Publish(new PostProcessingUpdatedSignal(
                            new CameraId(cameraActor.Id),
                            new ScreenSpaceLensFlareParameters()
                            {
                                IsEnabled = lensFlare.IsEnabled,
                                Intensity = lensFlare.Intensity
                            }));
                    }
                    else if (postProcessingUpdateCommand.Parameters is ScreenEdgeColorParameters screenEdgeColor)
                    {
                        cameraActor.UpdateScreenEdgeColorParameters(screenEdgeColor);
                        _signalPublisher.Publish(new PostProcessingUpdatedSignal(
                            new CameraId(cameraActor.Id),
                            new ScreenEdgeColorParameters()
                            {
                                IsEnabled = screenEdgeColor.IsEnabled,
                                Intensity = screenEdgeColor.Intensity,
                                TopLeftColor = screenEdgeColor.TopLeftColor,
                                TopRightColor = screenEdgeColor.TopRightColor,
                                BottomLeftColor = screenEdgeColor.BottomLeftColor,
                                BottomRightColor = screenEdgeColor.BottomRightColor
                            }));
                    }
                }
            }
            else if (command is TrackingStateUpdateCommand trackingStateUpdateCommand)
            {
                if (_cameraActorManager.TryGetCameraActor(trackingStateUpdateCommand.CameraId, out var cameraActor))
                {
                    if (!trackingStateUpdateCommand.IsTrackingEnabled)
                    {
                        RemoveTrackingTarget(trackingStateUpdateCommand.CameraId);
                    }
                }
            }
            else if (command is TrackingTargetUpdateCommand trackingTargetUpdateCommand)
            {
                UpdateTrackingTarget(trackingTargetUpdateCommand);
            }
            else if (command is RotationComposerUpdateCommand rotationComposerUpdateCommand)
            {
                UpdateRotationController(rotationComposerUpdateCommand);
            }
        }

        private void UpdateTrackingTarget(TrackingTargetUpdateCommand command)
        {
            if (!_cameraActorManager.TryGetCameraActor(command.CameraId, out var cameraActor))
            {
                return;
            }

            var excludeLayerMask = LayerMask.GetMask(_excludeLayerNames);
            var raycastTargetLayers = ~excludeLayerMask;

            var screenPointX = command.NormalizedScreenPoint.x * cameraActor.Camera.pixelWidth;
            var screenPointY = command.NormalizedScreenPoint.y * cameraActor.Camera.pixelHeight;
            var ray = cameraActor.Camera.ScreenPointToRay(new Vector2(screenPointX, screenPointY));

            if (Physics.Raycast(ray, out var raycastHit, Mathf.Infinity, raycastTargetLayers))
            {
                RemoveTrackingTarget(command.CameraId);

                var name = $"CameraTrackingTarget_{command.CameraId}";
                var trackingTarget = CreateTrackingTarget(name, raycastHit.point, raycastHit.transform);

                // if (MeshSurfacePointTrackingProvider.Find(ray, out var meshSurfacePointAnchor))
                // {
                //     var pointAnchorObject = trackingTarget.AddComponent<MeshSurfacePointAnchorObject>();
                //     pointAnchorObject.PointAnchor = meshSurfacePointAnchor;
                // }

                SetTrackingTarget(cameraActor, trackingTarget);

                UpdateRotationControllerScreenPosition(cameraActor, raycastHit.point);
            }
        }

        private void UpdateRotationControllerScreenPosition(CameraActor cameraActor, Vector3 trackingTargetPosition)
        {
            var cameraId = new CameraId(cameraActor.Id);

            // TODO
            // if (!_rotationControllers.ContainsKey(cameraId))
            // {
            //     _rotationControllers[cameraId] = cameraActor.RotationController;
            // }

            // Calculate screen position using perspective projection
            var targetScreenPoint = cameraActor.Camera.WorldToScreenPoint(trackingTargetPosition);

            var normalizedScreenPointX = targetScreenPoint.x / cameraActor.Camera.pixelWidth;
            var normalizedScreenPointY = targetScreenPoint.y / cameraActor.Camera.pixelHeight;

            // Convert coordinate system
            var rotationControllerScreenPoint = new Vector2(normalizedScreenPointX - 0.5f, 0.5f - normalizedScreenPointY);
            // _rotationControllers[cameraId].ScreenPosition = rotationControllerScreenPoint * _rotationControlScaleFactor;
        }

        private void UpdateRotationController(RotationComposerUpdateCommand command)
        {
            if (!_cameraActorManager.TryGetCameraActor(command.CameraId, out var cameraActor))
            {
                return;
            }

            // TODO
            // if (!_rotationControllers.ContainsKey(command.CameraId))
            // {
            //     _rotationControllers[command.CameraId] = cameraActor.RotationController;
            // }
            //
            // _rotationControllers[command.CameraId].ScreenPosition = command.ScreenPosition * _rotationControlScaleFactor;
        }

        private void RemoveTrackingTarget(CameraId cameraId)
        {
            if (_cameraTrackingTargets.Remove(cameraId, out var trackingTarget))
            {
                if (trackingTarget != null)
                {
                    UnityEngine.Object.Destroy(trackingTarget.gameObject);
                }
            }
        }

        private void SetTrackingTarget(CameraActor cameraActor, GameObject trackingTarget)
        {
            cameraActor.CinemachineCamera.Target = new CameraTarget()
            {
                TrackingTarget = trackingTarget.transform,
                LookAtTarget = trackingTarget.transform,
            };

            var cameraId = new CameraId(cameraActor.Id);
            // TODO
            // if (!_rotationControllers.ContainsKey(cameraId))
            // {
            //     _rotationControllers[cameraId] = cameraActor.RotationController;
            // }
            // _rotationControllers[cameraId].LookAtTarget = trackingTarget.transform;

            var targetLayer = LayerMask.NameToLayer(Constants.CameraLayerNames[cameraActor.Id]);
            trackingTarget.layer = targetLayer;

            _cameraTrackingTargets[new CameraId(cameraActor.Id)] = trackingTarget.transform;
        }

        private GameObject CreateTrackingTarget(string name, Vector3 position, Transform parentTransform)
        {
            var targetGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetGameObject.name = name;
            targetGameObject.transform.position = position;
            targetGameObject.transform.localScale = Vector3.one * 0.015f;

            if (parentTransform != null)
            {
                targetGameObject.transform.SetParent(parentTransform);
                Debug.Log($"<color=green>Tracking target created as child of: {parentTransform.name}</color>");
            }

            var material = new Material(Shader.Find(TrackingTargetShaderName));
            material.name = name;
            material.color = new Color(1f, 0.5f, 0f, 0.8f);

            if (!targetGameObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                meshRenderer = targetGameObject.AddComponent<MeshRenderer>();
            }
            meshRenderer.material = material;

            return targetGameObject;
        }
    }
}