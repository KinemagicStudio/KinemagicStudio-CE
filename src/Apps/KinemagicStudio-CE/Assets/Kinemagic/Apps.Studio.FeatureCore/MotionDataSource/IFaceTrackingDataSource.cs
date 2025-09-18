using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource
{
    public interface IFaceTrackingDataSource : IMotionDataSource
    {
        bool TryGetSample(float time, out FaceTrackingFrame value);
    }
}
