using System.Numerics;

namespace Kinemagic.Apps.Studio.Contracts.Motion
{
    public sealed class EyeTrackingFrame
    {
        public Vector3 LeftEyeEulerAngles { get; set; }
        public Vector3 RightEyeEulerAngles { get; set; }
    }
}
