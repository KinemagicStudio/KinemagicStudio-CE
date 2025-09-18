using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.Character;
using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using MessagePipe;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class CharacterSystem : IDisposable, IInitializable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ISubscriber<ICharacterCommand> _characterCommandSubscriber;
        private readonly ISubscriber<IMotionDataSignal> _motionDataSignalSubscriber;
        private readonly CharacterSystemConfig _config;
        private readonly VrmCharacterProvider _characterProvider;
        private readonly CharacterInstanceRegistry _characterInstanceRegistry;
        private readonly CharacterPoseHandlerRegistry _characterPoseHandlerRegistry;
        private readonly SpatialCoordinateProvider _spatialCoordinateProvider;

        private IDisposable _disposable;
        private IEnumerable<FaceExpressionKey> _faceExpressionKeys;

        public CharacterSystem(
            CharacterSystemConfig config,
            VrmCharacterProvider characterProvider,
            CharacterInstanceRegistry characterInstanceRegistry,
            CharacterPoseHandlerRegistry characterPoseHandlerRepository,
            SpatialCoordinateProvider spatialCoordinateProvider,
            ISubscriber<ICharacterCommand> characterCommandSubscriber,
            ISubscriber<IMotionDataSignal> motionDataSignalSubscriber)
        {
            _config = config;
            _characterProvider = characterProvider;
            _characterInstanceRegistry = characterInstanceRegistry;
            _characterPoseHandlerRegistry = characterPoseHandlerRepository;
            _spatialCoordinateProvider = spatialCoordinateProvider;
            _characterCommandSubscriber = characterCommandSubscriber;
            _motionDataSignalSubscriber = motionDataSignalSubscriber;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _disposable?.Dispose();
        }

        public void Initialize()
        {
            InitializeAsync().Forget();
        }

        public async UniTask InitializeAsync()
        {
            await StreamingAssetsSynchronizer.SynchronizeFilesAsync(Constants.PersistentDataDirectoryName, "*.vrm", _cancellationTokenSource.Token);

            _faceExpressionKeys = Enum.GetValues(typeof(FaceExpressionKey)).Cast<FaceExpressionKey>();

            var disposableBag = DisposableBag.CreateBuilder();
            _characterCommandSubscriber.Subscribe(HandleCharacterCommand).AddTo(disposableBag);
            _motionDataSignalSubscriber.Subscribe(HandleMotionDataSignal).AddTo(disposableBag);
            _disposable = disposableBag.Build();
        }

        private void HandleCharacterCommand(ICharacterCommand characterCommand)
        {
            switch (characterCommand)
            {
                case CharacterInstanceCreateCommand createCommand:
                    CreateCharacterInstanceAsync(createCommand).Forget();
                    break;
                case MotionDataSourceMappingAddCommand dataSourceMappingAddCommand:
                    AddMotionDataSourceMapping(dataSourceMappingAddCommand);
                    break;
                case MotionDataSourceMappingRemoveCommand dataSourceMappingRemoveCommand:
                    RemoveMotionDataSourceMapping(dataSourceMappingRemoveCommand);
                    break;
            }
        }

        private void HandleMotionDataSignal(IMotionDataSignal motionDataSignal)
        {
            switch (motionDataSignal)
            {
                case BodyTrackingDataUpdatedSignal bodyTrackingDataUpdatedSignal:
                    UpdateCharacterPose(bodyTrackingDataUpdatedSignal.DataSourceId, bodyTrackingDataUpdatedSignal.BodyTrackingFrame);
                    break;
                case FingerTrackingDataUpdatedSignal fingerTrackingDataUpdatedSignal:
                    UpdateCharacterPose(fingerTrackingDataUpdatedSignal.DataSourceId, fingerTrackingDataUpdatedSignal.FingerTrackingFrame);
                    break;
                case FaceTrackingDataUpdatedSignal faceTrackingDataUpdatedSignal:
                    UpdateFaceExpression(faceTrackingDataUpdatedSignal.DataSourceId, faceTrackingDataUpdatedSignal.FaceTrackingFrame);
                    break;
                case EyeTrackingDataUpdatedSignal eyeTrackingDataUpdatedSignal:
                    UpdateEyeTrackingPose(eyeTrackingDataUpdatedSignal.DataSourceId, eyeTrackingDataUpdatedSignal.EyeTrackingFrame);
                    break;
            }
        }

        private async UniTask CreateCharacterInstanceAsync(CharacterInstanceCreateCommand command)
        {
            var binaryDataStorageType = command.StorageType switch
            {
                "PersistentDataDirectory" => BinaryDataStorageType.LocalFileSystem,
                "StreamingAssetsDirectory" => BinaryDataStorageType.LocalFileSystem,
                _ => throw new NotSupportedException($"Unsupported storage type: {command.StorageType}")
            };

            var resourceKey = command.ResourceKey;
            if (command.StorageType == "PersistentDataDirectory")
            {
                resourceKey = Path.Combine(_config.PersistentDataPath, command.ResourceKey);
            }
            else if (command.StorageType == "StreamingAssetsDirectory")
            {
                resourceKey = Path.Combine(_config.StreamingAssetsPath, command.ResourceKey);
            }

            var characterInstance = await _characterProvider.LoadAsync(resourceKey, binaryDataStorageType);
            if (characterInstance == null)
            {
                throw new InvalidOperationException($"Failed to load character resource: {command.ResourceKey}");
            }

            if (_spatialCoordinateProvider.TryGetTransform(out var coordinateTransform))
            {
                characterInstance.RootTransform.SetParent(coordinateTransform, worldPositionStays: false);
            }

            var success = _characterInstanceRegistry.TryAddLocalInstance(characterInstance, out var instanceId);
            if (!success)
            {
                characterInstance.Dispose();
                throw new InvalidOperationException($"Failed to add character instance: {instanceId}");
            }

            if (!_characterPoseHandlerRegistry.TryAdd(characterInstance))
            {
                _characterInstanceRegistry.Remove(instanceId, out _);
                characterInstance.Dispose();
                throw new InvalidOperationException($"Failed to add character instance: {instanceId}");
            }
        }

        private void AddMotionDataSourceMapping(MotionDataSourceMappingAddCommand command)
        {
            _characterPoseHandlerRegistry.TryAddDataSourceMapping(command.ActorId, command.DataSourceId, command.MotionDataType);
        }

        private void RemoveMotionDataSourceMapping(MotionDataSourceMappingRemoveCommand command)
        {
            _characterPoseHandlerRegistry.TryRemoveDataSourceMapping(command.ActorId, command.DataSourceId, command.MotionDataType);
        }

        private void UpdateCharacterPose(int dataSourceId, BodyTrackingFrame bodyTrackingFrame)
        {
            // TODO: DataSourceId
            if (_characterPoseHandlerRegistry.TryGetBodyTrackingTargetPoseHandlers(new DataSourceId(dataSourceId), out var poseHandlers))
            {
                for (var i = 0; i < poseHandlers.Length; i++)
                {
                    poseHandlers[i]?.SetBodyTrackingPose(bodyTrackingFrame);
                }
            }
        }

        private void UpdateCharacterPose(int dataSourceId, FingerTrackingFrame fingerTrackingFrame)
        {
            // TODO: DataSourceId
            if (_characterPoseHandlerRegistry.TryGetFingerTrackingTargetPoseHandlers(new DataSourceId(dataSourceId), out var poseHandlers))
            {
                for (var i = 0; i < poseHandlers.Length; i++)
                {
                    poseHandlers[i]?.SetFingerTrackingPose(fingerTrackingFrame);
                }
            }
        }

        private void UpdateFaceExpression(int dataSourceId, FaceTrackingFrame faceTrackingFrame)
        {
            // TODO: DataSourceId
            if (_characterPoseHandlerRegistry.TryGetFaceExpressionHandlers(new DataSourceId(dataSourceId), out var faceExpressionHandlers))
            {
                for (var i = 0; i < faceExpressionHandlers.Length; i++)
                {
                    foreach (var faceExpressionKey in _faceExpressionKeys)
                    {
                        if (faceTrackingFrame.TryGetValue(faceExpressionKey, out var value))
                        {
                            faceExpressionHandlers[i]?.SetWeight(faceExpressionKey, value);
                        }
                    }
                }
            }
        }

        private void UpdateEyeTrackingPose(int dataSourceId, EyeTrackingFrame eyeTrackingFrame)
        {
            // TODO: DataSourceId
            if (_characterPoseHandlerRegistry.TryGetEyeTrackingHandlers(new DataSourceId(dataSourceId), out var eyeTrackingHandlers))
            {
                for (var i = 0; i < eyeTrackingHandlers.Length; i++)
                {
                    eyeTrackingHandlers[i]?.UpdateEyeDirection(eyeTrackingFrame.LeftEyeEulerAngles, eyeTrackingFrame.RightEyeEulerAngles);
                }
            }
        }
    }
}
