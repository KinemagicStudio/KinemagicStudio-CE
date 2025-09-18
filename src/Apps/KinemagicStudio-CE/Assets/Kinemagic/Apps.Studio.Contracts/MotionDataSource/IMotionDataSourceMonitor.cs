using System;
using R3;

namespace Kinemagic.Apps.Studio.Contracts.MotionDataSource
{
    public interface IMotionDataSourceMonitor : IDisposable
    {
        Observable<MotionDataSourceStatus> StatusChanged { get; }
    }
}
