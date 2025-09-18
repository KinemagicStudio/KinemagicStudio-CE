using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CinematicSequencer.IO
{
    public interface IClipDataRepository
    {
        UniTask<List<ClipDataInfo>> GetClipDataInfoListAsync(CancellationToken cancellationToken);
        UniTask<IClipData> LoadAsync(string key, CancellationToken cancellationToken);
        UniTask SaveAsync(string key, IClipData clipData, CancellationToken cancellationToken);
    }
}
