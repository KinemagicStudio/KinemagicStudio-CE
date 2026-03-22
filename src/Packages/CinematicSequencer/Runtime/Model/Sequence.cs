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
    /// シーケンスのルートモデル。トラックのコンテナであり、全体の再生管理の単位。
    /// </summary>
    public sealed class Sequence : IObservableModel
    {
        private readonly List<Track> _tracks = new();
        private readonly Dictionary<Guid, Track> _trackLookup = new();
        private string _name;
        private TimeRange _duration;
        private bool _durationDirty = true;

        public Guid Id { get; }

        [JsonIgnore]
        public string FormatVersion => "2.0.0";

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

        /// <summary>
        /// 全トラック中の最大EndTimeから自動計算。変更時のみ再計算。
        /// </summary>
        [JsonIgnore]
        public TimeRange Duration
        {
            get
            {
                if (_durationDirty) RecalculateDuration();
                return _duration;
            }
        }

        public IReadOnlyList<Track> Tracks => _tracks;

        [JsonIgnore]
        public event Action<ModelChangeEvent> Changed;

        public Sequence(string name)
        {
            Id = GuidExtensions.CreateVersion7();
            _name = name;
        }

        [JsonConstructor]
        public Sequence(Guid id, string name, List<Track> tracks)
        {
            Id = id;
            _name = name;
            if (tracks != null)
            {
                foreach (var track in tracks)
                {
                    _tracks.Add(track);
                    _trackLookup[track.Id] = track;
                    track.Changed += OnTrackChanged;
                }
            }
            _durationDirty = true;
        }

        /// <summary>
        /// トラックを追加。TargetIdは同一TrackType内で自動採番される。
        /// </summary>
        public Track AddTrack(string name, TrackType type)
        {
            return AddTrack(name, type, GetNextTargetId(type));
        }

        /// <summary>
        /// トラックを追加。TargetIdを明示的に指定する。
        /// </summary>
        public Track AddTrack(string name, TrackType type, int targetId)
        {
            var track = new Track(name, type, targetId);
            AddTrackInternal(track);
            return track;
        }

        /// <summary>
        /// 既存のTrackインスタンスを追加する（Undo/Redo、デシリアライズ用）。
        /// </summary>
        internal void InsertTrack(Track track)
        {
            AddTrackInternal(track);
        }

        public bool RemoveTrack(Guid trackId)
        {
            if (!_trackLookup.TryGetValue(trackId, out var track)) return false;
            track.Changed -= OnTrackChanged;
            _tracks.Remove(track);
            _trackLookup.Remove(trackId);
            _durationDirty = true;
            Changed?.Invoke(new ModelChangeEvent(
                ModelChangeEvent.ChangeType.ChildRemoved, nameof(Tracks), this));
            return true;
        }

        public Track GetTrack(Guid trackId)
        {
            return _trackLookup.TryGetValue(trackId, out var track) ? track : null;
        }

        public void ReorderTrack(Guid trackId, int newIndex)
        {
            if (!_trackLookup.TryGetValue(trackId, out var track)) return;
            _tracks.Remove(track);
            _tracks.Insert(Math.Clamp(newIndex, 0, _tracks.Count), track);
            Changed?.Invoke(new ModelChangeEvent(
                ModelChangeEvent.ChangeType.Reordered, nameof(Tracks), this));
        }

        private void AddTrackInternal(Track track)
        {
            _tracks.Add(track);
            _trackLookup[track.Id] = track;
            track.Changed += OnTrackChanged;
            _durationDirty = true;
            Changed?.Invoke(new ModelChangeEvent(
                ModelChangeEvent.ChangeType.ChildAdded, nameof(Tracks), this));
        }

        private int GetNextTargetId(TrackType type)
        {
            var nextTargetId = 1;
            var usedIds = new List<int>();
            foreach (var t in _tracks)
            {
                if (t.Type == type) usedIds.Add(t.TargetId);
            }
            usedIds.Sort();
            foreach (var id in usedIds)
            {
                if (id == nextTargetId) nextTargetId++;
            }
            return nextTargetId;
        }

        private void RecalculateDuration()
        {
            int maxEndMs = 0;
            foreach (var track in _tracks)
            {
                if (track.TimeRange.EndMs > maxEndMs)
                    maxEndMs = track.TimeRange.EndMs;
            }
            _duration = new TimeRange(0, maxEndMs);
            _durationDirty = false;
        }

        private void OnTrackChanged(ModelChangeEvent evt)
        {
            _durationDirty = true;
            Changed?.Invoke(new ModelChangeEvent(
                ModelChangeEvent.ChangeType.ChildModified, nameof(Tracks), this));
        }
    }
}
