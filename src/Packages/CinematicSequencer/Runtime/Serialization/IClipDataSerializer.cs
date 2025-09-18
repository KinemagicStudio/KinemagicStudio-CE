namespace CinematicSequencer.Serialization
{
    public interface IClipDataSerializer
    {
        byte[] Serialize<T>(T data);
        T Deserialize<T>(byte[] data);
        bool TryGetFormatVersion(byte[] data, out string value);
        bool TryGetClipDataInfo(byte[] data, out ClipDataInfo info);
    }
}