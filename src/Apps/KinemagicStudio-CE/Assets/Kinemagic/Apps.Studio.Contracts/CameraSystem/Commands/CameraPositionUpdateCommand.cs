using System.Numerics;

namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public sealed class CameraPositionUpdateCommand : ICameraSystemCommand
    {
        public CameraId CameraId { get; set; }
        public Vector3 Position { get; set; }
        public CameraPositionUpdateCommandType Type { get; }

        public CameraPositionUpdateCommand(CameraPositionUpdateCommandType type)
        {
            Type = type;
        }
    }
    
    public enum CameraPositionUpdateCommandType
    {
        Movement,
        WorldSpacePosition,
        // LocalSpacePosition,
    }
}
