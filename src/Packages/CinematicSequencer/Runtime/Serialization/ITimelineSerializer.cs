namespace CinematicSequencer.Serialization
{
    public interface ITimelineSerializer // ISequenceDataSerializer
    {
        byte[] Serialize<T>(T data);
        T Deserialize<T>(byte[] data);
        bool TryGetFormatVersion(byte[] data, out string value);
        bool TryGetSequenceDataInfo(byte[] data, out CinematicSequenceDataInfo info);
    }
}