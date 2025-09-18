using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource
{
    public interface IEyeTrackingDataSource : IMotionDataSource
    {
        bool TryGetSample(float time, out EyeTrackingFrame value);
    }
}
