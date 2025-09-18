using System;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace CinematicSequencer.Animation
{
    public struct Keyframe : IComparable<Keyframe>
    {
        [JsonIgnore]
        public readonly int TimeMs;

        public float Time => TimeMs * 0.001f;
        public float Value { get; set; }
        public float InTangent { get; set; }
        public float OutTangent { get; set; }
        public TangentMode TangentMode { get; set; }

        [JsonConstructor]
        public Keyframe(float time, float value, float inTangent = 0f, float outTangent = 0f, TangentMode tangentMode = TangentMode.Free)
        {
            TimeMs= (int)MathF.Round(time * 1000f);
            Value = value;
            InTangent = inTangent;
            OutTangent = outTangent;
            TangentMode = tangentMode;
        }

        public int CompareTo(Keyframe other) => TimeMs.CompareTo(other.TimeMs);

        public override string ToString() => $"TimeMs: {TimeMs}, Value: {Value}, InTangent: {InTangent}, OutTangent: {OutTangent}, TangentMode: {TangentMode}";
    }
}
