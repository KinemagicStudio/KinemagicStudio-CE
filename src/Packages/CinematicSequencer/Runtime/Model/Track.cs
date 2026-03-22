using System;
using System.Collections.Generic;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace CinematicSequencer
{
    /// <summary>
    /// トラック。同一種類のクリップを時間軸上に配置するコンテナ。
    /// </summary>
    public sealed class Track : IObservableModel
    {
        private readonly List<Clip> _clips = new();
        private readonly Dictionary<Guid, Clip> _clipLookup = new();
        private string _name;
        private TimeRange _timeRange;
        private bool _timeRangeDirty = true;

        public Guid Id { get; }
        public TrackType Type { get; }

        /// <summary>
        /// シーンオブジェクト（カメラ、ライト等）とのバインディング用ID。
        /// シーケンスデータをシーンから独立させ、再利用可能にするための設計。
        /// 同一TrackType内で一意。アプリ側のアダプターがこのIDを使って
        /// 実際のオブジェクト（CameraActor等）にマッピングする。
        /// </summary>
        public int TargetId { get; }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                Changed?.Invoke(new ModelChangeEvent(
                    ModelChangeEvent.ChangeType.PropertyChanged, nameof(Name), this));
            }
        }

        public int SortOrder { get; set; }

        public IReadOnlyList<Clip> Clips => _clips;

        /// <summary>
        /// 全クリップを包含する時間範囲。
        /// </summary>
        [JsonIgnore]
        public TimeRange TimeRange
        {
            get
            {
                if (_timeRangeDirty) RecalculateTimeRange();
                return _timeRange;
            }
        }

        [JsonIgnore]
        public event Action<ModelChangeEvent> Changed;

        public Track(string name, TrackType type, int targetId)
        {
            Id = GuidExtensions.CreateVersion7();
            _name = name;
            Type = type;
            TargetId = targetId;
        }

        [JsonConstructor]
        public Track(Guid id, string name, TrackType type, int targetId, int sortOrder, List<Clip> clips)
        {
            Id = id;
            _name = name;
            Type = type;
            TargetId = targetId;
            SortOrder = sortOrder;
            if (clips != null)
            {
                foreach (var clip in clips)
                {
                    _clips.Add(clip);
                    _clipLookup[clip.Id] = clip;
                    clip.Changed += OnClipChanged;
                }
            }
            _timeRangeDirty = true;
        }

        public Clip AddClip(Guid clipAssetId, TimeRange placement)
        {
            var clip = new Clip(clipAssetId, placement);
            AddClipInternal(clip);
            return clip;
        }

        /// <summary>
        /// 既存のClipインスタンスを追加する（Undo/Redo用）。
        /// </summary>
        internal void InsertClip(Clip clip)
        {
            AddClipInternal(clip);
        }

        public bool RemoveClip(Guid clipId)
        {
            if (!_clipLookup.TryGetValue(clipId, out var clip)) return false;
            clip.Changed -= OnClipChanged;
            _clips.Remove(clip);
            _clipLookup.Remove(clipId);
            _timeRangeDirty = true;
            Changed?.Invoke(new ModelChangeEvent(
                ModelChangeEvent.ChangeType.ChildRemoved, nameof(Clips), this));
            return true;
        }

        public Clip GetClip(Guid clipId)
        {
            return _clipLookup.TryGetValue(clipId, out var clip) ? clip : null;
        }

        /// <summary>
        /// 指定時刻にアクティブなクリップを返す。
        /// 同一トラック上でクリップが重なっている場合、後ろのクリップが優先。
        /// </summary>
        public Clip GetActiveClipAt(int timeMs)
        {
            Clip active = null;
            foreach (var clip in _clips)
            {
                if (clip.ContainsTime(timeMs))
                {
                    active = clip;
                }
            }
            return active;
        }

        private void AddClipInternal(Clip clip)
        {
            _clips.Add(clip);
            _clipLookup[clip.Id] = clip;
            clip.Changed += OnClipChanged;
            _timeRangeDirty = true;
            Changed?.Invoke(new ModelChangeEvent(
                ModelChangeEvent.ChangeType.ChildAdded, nameof(Clips), this));
        }

        private void RecalculateTimeRange()
        {
            if (_clips.Count == 0)
            {
                _timeRange = default;
                _timeRangeDirty = false;
                return;
            }

            int minStartMs = int.MaxValue;
            int maxEndMs = 0;
            foreach (var clip in _clips)
            {
                if (clip.Placement.StartMs < minStartMs)
                    minStartMs = clip.Placement.StartMs;
                if (clip.Placement.EndMs > maxEndMs)
                    maxEndMs = clip.Placement.EndMs;
            }
            _timeRange = new TimeRange(minStartMs, maxEndMs - minStartMs);
            _timeRangeDirty = false;
        }

        private void OnClipChanged(ModelChangeEvent evt)
        {
            _timeRangeDirty = true;
            Changed?.Invoke(new ModelChangeEvent(
                ModelChangeEvent.ChangeType.ChildModified, nameof(Clips), this));
        }
    }
}
