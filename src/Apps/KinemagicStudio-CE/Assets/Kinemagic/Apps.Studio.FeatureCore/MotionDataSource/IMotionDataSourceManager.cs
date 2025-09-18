using System;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource
{
    public interface IMotionDataSourceManager : IDisposable
    {
        UniTask<IMotionDataSource> CreateAsync(MotionDataSourceKey dataSourceKey, MotionDataSourceType dataSourceType);
    }
}
