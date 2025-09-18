using System;
using System.Collections.Generic;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace CinematicSequencer
{
    [Serializable]
    public sealed class TimelineTrack
    {
        private static int _nextId = 0;

        private float _endTime = 0f;
        private List<TimelineClip> _clips = new();

        [JsonIgnore]
        public int Id { get; }
        
        public string Name { get; set; }
        public DataType Type { get; }
        public int TargetId { get; }
        public int ClipCount => Clips.Count;
        public IReadOnlyList<TimelineClip> Clips => _clips;
        public float EndTime => _endTime;
        
        [JsonConstructor]
        public TimelineTrack(string name, DataType type, int targetId, List<TimelineClip> clips)
        {
            Id = _nextId++;
            Name = name;
            Type = type;
            TargetId = targetId;

            _clips = clips;
            UpdateEndTime();
        }
        
        public TimelineTrack(string name, DataType type, int targetId)
        {
            Id = _nextId++;
            Name = name;
            Type = type;
            TargetId = targetId;
        }
        
        public TimelineClip GetClip(int clipId)
        {
            return _clips.Find(c => c.Id == clipId);
        }
        
        public bool TryAddClip(TimelineClip clip)
        {
            if (clip.Type != Type) return false;
            
            clip.TargetId = TargetId;
            _clips.Add(clip);
            UpdateEndTime();
            
            return true;
        }
        
        public bool RemoveClip(int clipId)
        {
            var result = _clips.RemoveAll(clip => clip.Id == clipId) > 0;
            UpdateEndTime();
            return result;
        }
        
        private void UpdateEndTime()
        {
            foreach (var clip in Clips)
            {
                _endTime = Math.Max(_endTime, clip.EndTime);
            }
        }
    }
}