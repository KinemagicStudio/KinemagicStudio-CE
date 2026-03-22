using System;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace CinematicSequencer
{
    /// <summary>
    /// クリップ。ClipAssetのシーケンス上の配置を表すインスタンス。
    /// </summary>
    public sealed class Clip : IObservableModel
    {
        private TimeRange _placement;
        private float _playbackRate = 1.0f;
        private TimeRange _sourceRange;
        private IClipAsset _clipAsset;

        public Guid Id { get; }

        /// <summary>
        /// このClipが参照するClipAssetのID。
        /// ClipAssetは遅延ロードされ、再生時またはエディタで開いた時に読み込まれる。
        /// </summary>
        public Guid ClipAssetId { get; }

        /// <summary>
        /// シーケンス上の配置（開始時刻と表示Duration）。
        /// </summary>
        public TimeRange Placement
        {
            get => _placement;
            set
            {
                if (_placement == value) return;
                _placement = value;
                Changed?.Invoke(new ModelChangeEvent(
                    ModelChangeEvent.ChangeType.PropertyChanged, nameof(Placement), this));
            }
        }

        /// <summary>
        /// 再生速度。1.0が等速、2.0が2倍速。
        /// </summary>
        public float PlaybackRate
        {
            get => _playbackRate;
            set
            {
                if (Math.Abs(_playbackRate - value) < float.Epsilon) return;
                _playbackRate = value;
                Changed?.Invoke(new ModelChangeEvent(
                    ModelChangeEvent.ChangeType.PropertyChanged, nameof(PlaybackRate), this));
            }
        }

        /// <summary>
        /// ClipAsset内のどの範囲を使用するか（トリミング）。
        /// </summary>
        public TimeRange SourceRange
        {
            get => _sourceRange;
            set
            {
                if (_sourceRange == value) return;
                _sourceRange = value;
                Changed?.Invoke(new ModelChangeEvent(
                    ModelChangeEvent.ChangeType.PropertyChanged, nameof(SourceRange), this));
            }
        }

        /// <summary>
        /// 遅延ロード可能なClipAsset参照。
        /// </summary>
        [JsonIgnore]
        public IClipAsset ClipAsset
        {
            get => _clipAsset;
            set => _clipAsset = value;
        }

        [JsonIgnore]
        public event Action<ModelChangeEvent> Changed;

        public Clip(Guid clipAssetId, TimeRange placement)
        {
            Id = GuidExtensions.CreateVersion7();
            ClipAssetId = clipAssetId;
            _placement = placement;
            _sourceRange = new TimeRange(0, placement.DurationMs);
        }

        [JsonConstructor]
        public Clip(Guid id, Guid clipAssetId, TimeRange placement, float playbackRate, TimeRange sourceRange)
        {
            Id = id;
            ClipAssetId = clipAssetId;
            _placement = placement;
            _playbackRate = playbackRate;
            _sourceRange = sourceRange;
        }

        public bool ContainsTime(int timeMs)
        {
            return _placement.Contains(timeMs);
        }

        /// <summary>
        /// グローバル時刻からクリップ内のローカル時刻に変換。
        /// </summary>
        public float GetLocalTime(float globalTime)
        {
            var localTime = (globalTime - _placement.Start) * _playbackRate;
            return Math.Clamp(localTime, 0f, _sourceRange.Duration);
        }
    }
}
