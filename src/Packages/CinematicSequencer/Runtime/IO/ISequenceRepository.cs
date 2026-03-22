using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CinematicSequencer.IO
{
    /// <summary>
    /// v2シーケンスデータのリポジトリインターフェース。
    /// </summary>
    public interface ISequenceRepository
    {
        UniTask<Sequence> LoadSequenceAsync(Guid id, CancellationToken ct);
        UniTask SaveSequenceAsync(Sequence sequence, CancellationToken ct);
        UniTask<List<SequenceInfo>> GetSequenceListAsync(CancellationToken ct);
    }

    /// <summary>
    /// シーケンスの一覧表示用の軽量情報。
    /// </summary>
    public sealed class SequenceInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int TrackCount { get; set; }

        public SequenceInfo(Guid id, string name, int trackCount)
        {
            Id = id;
            Name = name;
            TrackCount = trackCount;
        }
    }
}
