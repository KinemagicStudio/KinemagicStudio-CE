using System;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace CinematicSequencer
{
    [Serializable]
    public sealed class TimelineClip
    {
        private static int _nextId = 0;

        private DataType _dataType;
        private Guid _clipDataId;
        private IClipData _clipData;

        [JsonIgnore]
        public int Id { get; }

        public DataType Type
        {
            get => _clipData != null ? _clipData.Type : _dataType;
            set => _dataType = value;
        }

        public int TargetId { get; set; }
        public float StartTime { get; set; }
        public float Duration { get; set; }
        public float TimeScale { get; set; }
        public float EndTime => StartTime + Duration * (1.0f / TimeScale);

        public Guid ClipDataId
        {
            get => _clipData != null ? _clipData.Id : _clipDataId;
            set => _clipDataId = value;
        }

        [JsonIgnore]
        public IClipData ClipData
        {
            get => _clipData;
            set
            {
                _clipData = value;
                if (_clipData != null)
                {
                    Type = _clipData.Type;
                    ClipDataId = _clipData.Id;
                    Duration = _clipData.GetDuration();
                }
                else
                {
                    Type = DataType.Unknown;
                    ClipDataId = Guid.Empty;
                    Duration = 0f;
                }
            }
        }

        public TimelineClip()
        {
            Id = _nextId++;
            TargetId = -1;
            StartTime = 0f;
            Duration = 0f;
            TimeScale = 1.0f;
            ClipData = null;
        }

        public TimelineClip(IClipData clipData, int targetId, float startTime)
        {
            Id = _nextId++;
            TargetId = targetId;
            StartTime = startTime;
            Duration = clipData.GetDuration();
            TimeScale = 1.0f;
            ClipData = clipData;
        }
        
        /// <summary>
        /// 指定した時間がクリップの範囲内にあるか確認
        /// </summary>
        public bool ContainsTime(float time)
        {
            return time >= StartTime && time <= EndTime;
        }
        
        /// <summary>
        /// タイムライン時間からクリップローカル時間に変換
        /// </summary>
        public float GetLocalTime(float globalTime)
        {
            if (!ContainsTime(globalTime))
            {
                return -1f; // 範囲外
            }
            
            // クリップ内での相対時間を計算し、タイムスケールを適用
            float localTime = (globalTime - StartTime) * TimeScale;
            return Math.Clamp(localTime, 0f, Duration);
        }
    }
}