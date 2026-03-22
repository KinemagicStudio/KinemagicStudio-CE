using System;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace CinematicSequencer
{
    /// <summary>
    /// 不変の時間範囲。開始時刻とDurationのペア。
    /// 内部はミリ秒int精度、APIはfloat秒で公開。
    /// </summary>
    public readonly struct TimeRange : IEquatable<TimeRange>
    {
        public int StartMs { get; }
        public int DurationMs { get; }

#if USE_NEWTONSOFT_JSON
        [JsonIgnore]
#endif
        public int EndMs => StartMs + DurationMs;

#if USE_NEWTONSOFT_JSON
        [JsonIgnore]
#endif
        public float Start => StartMs * 0.001f;

#if USE_NEWTONSOFT_JSON
        [JsonIgnore]
#endif
        public float Duration => DurationMs * 0.001f;

#if USE_NEWTONSOFT_JSON
        [JsonIgnore]
#endif
        public float End => EndMs * 0.001f;

        public TimeRange(float startSeconds, float durationSeconds)
        {
            StartMs = (int)MathF.Round(startSeconds * 1000f);
            DurationMs = (int)MathF.Round(durationSeconds * 1000f);
        }

#if USE_NEWTONSOFT_JSON
        [JsonConstructor]
#endif
        public TimeRange(int startMs, int durationMs)
        {
            StartMs = startMs;
            DurationMs = durationMs;
        }

        public bool Contains(int timeMs) => timeMs >= StartMs && timeMs <= EndMs;

        public bool Overlaps(TimeRange other)
        {
            return StartMs < other.EndMs && EndMs > other.StartMs;
        }

        public TimeRange WithStart(float newStart)
        {
            int newStartMs = (int)MathF.Round(newStart * 1000f);
            return new TimeRange(newStartMs, DurationMs);
        }

        public TimeRange WithDuration(float newDuration)
        {
            int newDurationMs = (int)MathF.Round(newDuration * 1000f);
            return new TimeRange(StartMs, newDurationMs);
        }

        public TimeRange Offset(float deltaSeconds)
        {
            int deltaMs = (int)MathF.Round(deltaSeconds * 1000f);
            return new TimeRange(StartMs + deltaMs, DurationMs);
        }

        public bool Equals(TimeRange other)
        {
            return StartMs == other.StartMs && DurationMs == other.DurationMs;
        }

        public override bool Equals(object obj)
        {
            return obj is TimeRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartMs, DurationMs);
        }

        public static bool operator ==(TimeRange left, TimeRange right) => left.Equals(right);
        public static bool operator !=(TimeRange left, TimeRange right) => !left.Equals(right);

        public override string ToString() => $"TimeRange({Start:F3}s - {End:F3}s, Duration: {Duration:F3}s)";
    }
}
