using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CinematicSequencer.IO
{
    /// <summary>
    /// v2クリップアセットのリポジトリインターフェース。
    /// </summary>
    public interface IClipAssetRepository
    {
        UniTask<IClipAsset> LoadClipAssetAsync(Guid id, CancellationToken ct);
        UniTask SaveClipAssetAsync(IClipAsset asset, CancellationToken ct);
        UniTask<List<ClipAssetInfo>> GetClipAssetListAsync(CancellationToken ct);
    }

    /// <summary>
    /// クリップアセットの一覧表示用の軽量情報。
    /// </summary>
    public sealed class ClipAssetInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public TrackType Type { get; set; }

        public ClipAssetInfo(Guid id, string name, TrackType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }
    }
}
