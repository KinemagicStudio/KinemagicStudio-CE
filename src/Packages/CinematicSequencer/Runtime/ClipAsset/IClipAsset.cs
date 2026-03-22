using System;

namespace CinematicSequencer
{
    /// <summary>
    /// 全クリップアセットの基底インターフェース。
    /// シーケンス上での配置管理に必要な最小限の契約。
    /// </summary>
    public interface IClipAsset
    {
        Guid Id { get; }
        string Name { get; set; }
        TrackType Type { get; }
        float GetDuration();
    }
}
