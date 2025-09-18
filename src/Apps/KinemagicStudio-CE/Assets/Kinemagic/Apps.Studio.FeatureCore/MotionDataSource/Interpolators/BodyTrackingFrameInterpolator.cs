using System.Numerics;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.Motion;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource
{
    public sealed class BodyTrackingFrameInterpolator : IInterpolator<BodyTrackingFrame>
    {
        public BodyTrackingFrame Interpolate(BodyTrackingFrame startValue, BodyTrackingFrame endValue, float t)
        {
            var frame = new BodyTrackingFrame
            {
                Scale = Vector3.Lerp(startValue.Scale, endValue.Scale, t),
                RootPosition = Vector3.Lerp(startValue.RootPosition, endValue.RootPosition, t),
                RootRotation = Quaternion.Slerp(startValue.RootRotation, endValue.RootRotation, t)
            };

            for (var i = 0; i < frame.BoneCount; i++)
            {
                frame.BonePositions[i] = Vector3.Lerp(startValue.BonePositions[i], endValue.BonePositions[i], t);
                frame.BoneRotations[i] = Quaternion.Slerp(startValue.BoneRotations[i], endValue.BoneRotations[i], t);
            }

            return frame;
        }
    }
}