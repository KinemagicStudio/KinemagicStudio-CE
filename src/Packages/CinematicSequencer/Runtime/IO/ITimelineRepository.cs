using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace CinematicSequencer.IO
{
    public interface ITimelineRepository
    {
        UniTask<List<CinematicSequenceDataInfo>> GetSequenceDataInfoListAsync(CancellationToken cancellationToken);
        UniTask<Timeline> LoadAsync(string key, CancellationToken cancellationToken);
        UniTask SaveAsync(string key, Timeline timeline, CancellationToken cancellationToken);
    }
}
