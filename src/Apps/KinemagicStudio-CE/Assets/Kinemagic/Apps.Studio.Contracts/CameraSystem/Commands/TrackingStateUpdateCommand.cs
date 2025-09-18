// TODO:
// using System.Numerics;
using UnityEngine;

namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public sealed class TrackingStateUpdateCommand : ICameraSystemCommand
    {
        public CameraId CameraId { get; }
        public bool IsTrackingEnabled { get; }

        public TrackingStateUpdateCommand(CameraId cameraId, bool isTrackingEnabled)
        {
            CameraId = cameraId;
            IsTrackingEnabled = isTrackingEnabled;
        }
    }

    public sealed class TrackingTargetUpdateCommand : ICameraSystemCommand
    {
        public CameraId CameraId { get; }
        public Vector3 NormalizedScreenPoint { get; }

        public TrackingTargetUpdateCommand(CameraId cameraId, Vector3 normalizedScreenPoint)
        {
            CameraId = cameraId;
            NormalizedScreenPoint = normalizedScreenPoint;
        }
    }

    public sealed class RotationComposerUpdateCommand : ICameraSystemCommand
    {
        public CameraId CameraId { get; }
        public Vector2 ScreenPosition { get; }

        public RotationComposerUpdateCommand(CameraId cameraId, Vector2 screenPosition)
        {
            CameraId = cameraId;
            ScreenPosition = screenPosition;
        }
    }
}