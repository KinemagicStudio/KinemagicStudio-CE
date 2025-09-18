using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Kinemagic.Apps.Studio.Contracts.SpatialEnvironment
{
    public interface IEnvironmentModelInfoRepository
    {
        UniTask<IReadOnlyList<EnvironmentModelInfo>> FetchAllAsync(CancellationToken cancellationToken = default);
        IReadOnlyList<EnvironmentModelInfo> GetAll();
        void ClearCache();
    }
}
