using System.Numerics;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.Motion;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource
{
    public sealed class EyeTrackingFrameInterpolator : IInterpolator<EyeTrackingFrame>
    {
        public EyeTrackingFrame Interpolate(EyeTrackingFrame startValue, EyeTrackingFrame endValue, float t)
        {
            var frame = new EyeTrackingFrame
            {
                LeftEyeEulerAngles = Vector3.Lerp(startValue.LeftEyeEulerAngles, endValue.LeftEyeEulerAngles, t),
                RightEyeEulerAngles = Vector3.Lerp(startValue.RightEyeEulerAngles, endValue.RightEyeEulerAngles, t)
            };

            return frame;
        }
    }
}