using System;
using System.Collections.Generic;
using System.Linq;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace CinematicSequencer
{
    [Serializable]
    public sealed class Timeline // CinematicSequence
    {
        private float _duration = 0f;
        private List<TimelineTrack> _tracks = new();
        private Dictionary<TimelineTrackId, TimelineClip> _activeClips = new();

        public string CinematicSequencerFormatVersion => Constants.CinematicSequencerFormatVersion;

        public Guid Id { get; }
        public string Name { get; set; }
        public int TrackCount => _tracks.Count;
        public IReadOnlyList<TimelineTrack> Tracks => _tracks.AsReadOnly();

        [JsonConstructor]
        public Timeline(string id, string name, List<TimelineTrack> tracks)
        {
            Id = Guid.Parse(id);
            Name = name;
            _tracks = tracks;
            UpdateDuration();
        }

        public Timeline(string name)
        {
            Id = GuidExtensions.CreateVersion7();
            Name = name;
        }

        /// <summary>
        /// Frequently called method
        /// </summary>
        public float GetDuration() => _duration;

        /// <summary>
        /// Frequently called method
        /// </summary>
        public Dictionary<TimelineTrackId, TimelineClip>.ValueCollection GetActiveClipsAtTime(float time)
        {
            _activeClips.Clear();

            foreach (var track in _tracks)
            {
                for (var i = 0; i < track.Clips.Count; i++) // NOTE: Avoid allocation
                {
                    var clip = track.Clips[i];
                    if (!clip.ContainsTime(time)) continue;
                    // 複数のクリップがある場合は最後に追加されたものが優先される
                    var key = new TimelineTrackId(clip.Type, clip.TargetId);
                    _activeClips[key] = clip;
                }
            }

            return _activeClips.Values;
        }

        // AddNewTrack?
        public TimelineTrack CreateTrack(string name, DataType type)
        {
            var targetTypeTracks = _tracks.FindAll(t => t.Type == type);

            var nextTargetId = 1;
            targetTypeTracks.Sort((x, y) => x.TargetId.CompareTo(y.TargetId));
            foreach (var track in targetTypeTracks)
            {
                if (track.TargetId == nextTargetId)
                {
                    nextTargetId++;
                }
            }

            var newTrack = new TimelineTrack(name, type, nextTargetId);
            _tracks.Add(newTrack);
            
            UpdateDuration();
            
            return newTrack;
        }

        public bool RemoveTrack(int trackId)
        {
            var result = _tracks.RemoveAll(t => t.Id == trackId) > 0;
            UpdateDuration();
            return result;
        }

        public TimelineClip AddClip(int trackId, float startTime, IClipData sourceClip)
        {
            // var track = _tracks.Find(t => t.Id == trackId && t.Type == sourceClip.Type);
            var track = _tracks.Find(t => t.Id == trackId);
            if (track == null)
            {
                throw new ArgumentException($"Track not found. Id: {trackId}, Type: {sourceClip.Type}");
            }

            var timelineClip = new TimelineClip(sourceClip, track.TargetId, startTime);
            track.TryAddClip(timelineClip);
            
            UpdateDuration();
            
            return timelineClip;
        }

        public bool RemoveClip(int trackId, int clipId)
        {
            var track = _tracks.Find(t => t.Id == trackId);
            if (track == null)
            {
                throw new ArgumentException($"Track not found. Id: {trackId}");
            }
            
            UpdateDuration();
            
            return track.RemoveClip(clipId);
        }

        public void UpdateClip(int clipId, float startTime, int newTrackId, int oldTrackId)
        {
            var srcTrack = _tracks.Find(t => t.Id == oldTrackId);
            if (srcTrack == null)
            {
                throw new ArgumentException($"Track not found. Id: {oldTrackId}");
            }

            var clip = srcTrack.GetClip(clipId);
            if (clip == null)
            {
                throw new ArgumentException($"Clip not found in track. TrackId: {oldTrackId}, ClipId: {clipId}");
            }

            clip.StartTime = startTime;

            if (newTrackId != oldTrackId)
            {
                var dstTrack = _tracks.Find(t => t.Id == newTrackId);
                if (dstTrack == null)
                {
                    throw new ArgumentException($"Track not found. Id: {newTrackId}");
                }

                dstTrack.TryAddClip(clip);
                srcTrack.RemoveClip(clipId);
            }
            
            UpdateDuration();
        }

        private void UpdateDuration()
        {
            _duration = _tracks.Select(track => track.EndTime).Prepend(0f).Max();
        }

        // TimelineTrackId?
        public readonly struct TimelineTrackId : IEquatable<TimelineTrackId>
        {
            public readonly DataType Type;
            public readonly int TargetId;

            public TimelineTrackId(DataType type, int targetId)
            {
                Type = type;
                TargetId = targetId;
            }

            public bool Equals(TimelineTrackId other)
            {
                return Type == other.Type && TargetId == other.TargetId;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Type, TargetId);
            }
        }
    }
}