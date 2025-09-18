using System.Numerics;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.Motion;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource
{
    public sealed class FingerTrackingFrameInterpolator : IInterpolator<FingerTrackingFrame>
    {
        public FingerTrackingFrame Interpolate(FingerTrackingFrame startValue, FingerTrackingFrame endValue, float t)
        {
            var frame = new FingerTrackingFrame();

            for (var i = 0; i < frame.BoneCount; i++)
            {
                frame.BoneRotations[i] = Quaternion.Slerp(startValue.BoneRotations[i], endValue.BoneRotations[i], t);
            }

            return frame;
        }
    }
}