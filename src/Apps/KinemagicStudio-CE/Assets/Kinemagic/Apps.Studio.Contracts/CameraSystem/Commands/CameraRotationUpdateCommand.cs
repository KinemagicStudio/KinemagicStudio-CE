using System.Numerics;

namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public sealed class CameraRotationUpdateCommand : ICameraSystemCommand
    {

        public CameraId CameraId { get; set; }
        public Vector3 EulerAngles { get; set; }
        public CameraRotationUpdateCommandType Type { get; }

        public CameraRotationUpdateCommand(CameraRotationUpdateCommandType type)
        {
            Type = type;
        }
    }

    public enum CameraRotationUpdateCommandType
    {
        RotateAroundWorldUp,
        RotateAroundLocalUp,
        WorldSpaceRotation,
        // LocalSpaceRotation,
    }
}
