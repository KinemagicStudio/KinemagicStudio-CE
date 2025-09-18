using System;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.Motion;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource
{
    public sealed class FaceTrackingFrameInterpolator : IInterpolator<FaceTrackingFrame>
    {
        public FaceTrackingFrame Interpolate(FaceTrackingFrame startValue, FaceTrackingFrame endValue, float t)
        {
            var frame = new FaceTrackingFrame();
            var faceExpressionKeys = Enum.GetValues(typeof(FaceExpressionKey));

            foreach (FaceExpressionKey key in faceExpressionKeys)
            {
                if (startValue.TryGetValue(key, out float startVal) && endValue.TryGetValue(key, out float endVal))
                {
                    var value = startVal + (endVal - startVal) * t; // Linear interpolation
                    frame.SetValue(key, value);
                }
            }

            return frame;
        }
    }
}