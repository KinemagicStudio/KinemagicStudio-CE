using System;

namespace CinematicSequencer.Serialization
{
    /// <summary>
    /// v2シーケンスデータのシリアライズ/デシリアライズインターフェース。
    /// </summary>
    public interface ISequenceSerializer
    {
        byte[] SerializeSequence(Sequence sequence);
        Sequence DeserializeSequence(byte[] data);
        byte[] SerializeClipAsset(IClipAsset clipAsset);
        IClipAsset DeserializeClipAsset(byte[] data);
        bool TryGetFormatVersion(byte[] data, out string version);
    }
}
