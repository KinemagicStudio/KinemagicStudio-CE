using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VRMToolkit
{
    public interface ICharacterModelInfoRepository : IDisposable
    {
        UniTask<IReadOnlyList<ICharacterModelInfo>> FetchAllAsync(int limitCount, CancellationToken cancellationToken = default);
        IReadOnlyList<ICharacterModelInfo> GetAll();
        void ClearCache();
    }
}
