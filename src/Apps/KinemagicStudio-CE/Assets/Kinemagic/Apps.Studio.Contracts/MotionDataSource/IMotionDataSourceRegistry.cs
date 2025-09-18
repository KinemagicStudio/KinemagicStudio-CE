using System;
using R3;

namespace Kinemagic.Apps.Studio.Contracts.MotionDataSource
{
    public interface IMotionDataSourceRegistry : IDisposable
    {
        Observable<MotionDataSourceInfo> Added { get; }
        Observable<MotionDataSourceInfo> Removed { get; }
    }
}
