using System;
using System.Numerics;
using System.Threading;
using CinematicSequencer;
using CinematicSequencer.Animation;
using Cysharp.Threading.Tasks;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using Kinemagic.Apps.Studio.Contracts.CinematicSequencer;
using Kinemagic.Apps.Studio.Contracts.SpatialEnvironment;
using MessagePipe;

namespace Kinemagic.Apps.Studio.FeatureCore.CinematicSequencer
{
    public sealed class CinematicSequenceSystemAdapter : IDisposable, IInitializable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly TimelinePlayer _sequencePlayer;
        private readonly IPublisher<ICameraSystemCommand> _cameraSystemCommandPublisher;
        private readonly IPublisher<IEnvironmentSystemCommand> _spatialEnvironmentCommandPublisher;

        private CameraPositionUpdateCommand _positionUpdateCommand = new(CameraPositionUpdateCommandType.WorldSpacePosition);
        private CameraRotationUpdateCommand _rotationUpdateCommand = new(CameraRotationUpdateCommandType.WorldSpaceRotation);

        public CinematicSequenceSystemAdapter(
            IPublisher<ICameraSystemCommand> cameraSystemCommandPublisher,
            IPublisher<IEnvironmentSystemCommand> spatialEnvironmentCommandPublisher)
        {
            _sequencePlayer = CinematicSequenceSystem.SequencePlayer;
            _cameraSystemCommandPublisher = cameraSystemCommandPublisher;
            _spatialEnvironmentCommandPublisher = spatialEnvironmentCommandPublisher;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            if (_sequencePlayer != null)
            {
                _sequencePlayer.OnAnimationEvaluate -= OnAnimationEvaluated;
            }
        }

        public void Initialize()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            await StreamingAssetsSynchronizer.SynchronizeFilesAsync(
                Contracts.CinematicSequencer.Constants.PersistentDataDirectoryName, 
                "*.json",
                _cancellationTokenSource.Token);

            _sequencePlayer.OnAnimationEvaluate += OnAnimationEvaluated;
        }

        /// <summary>
        /// Frequently called method
        /// </summary>
        private void OnAnimationEvaluated(int targetId, AnimationFrame frame)
        {
            if (frame.Type == DataType.CameraPose)
            {
                _positionUpdateCommand.CameraId = new CameraId(targetId);
                _positionUpdateCommand.Position = new Vector3(
                    frame.Properties[0].Value,
                    frame.Properties[1].Value,
                    frame.Properties[2].Value);

                _rotationUpdateCommand.CameraId = new CameraId(targetId);
                _rotationUpdateCommand.EulerAngles = new Vector3(
                    frame.Properties[3].Value,
                    frame.Properties[4].Value,
                    frame.Properties[5].Value);

                _cameraSystemCommandPublisher.Publish(_positionUpdateCommand);
                _cameraSystemCommandPublisher.Publish(_rotationUpdateCommand);
            }
        }
    }
}