using System;

namespace CinematicSequencer.Playback
{
    /// <summary>
    /// 再生中のクリップの情報。SequencePlayerからアプリ側アダプターに通知される。
    /// クリップの種類（IAnimatableClipAsset / IExternalClipAsset）に応じて
    /// アダプター側で適切な処理を行う。
    /// </summary>
    public readonly struct ClipPlaybackInfo
    {
        public Guid TrackId { get; }
        public int TargetId { get; }
        public TrackType Type { get; }
        public Guid ClipId { get; }
        public IClipAsset ClipAsset { get; }
        public float LocalTime { get; }

        public ClipPlaybackInfo(Guid trackId, int targetId, TrackType type,
            Guid clipId, IClipAsset clipAsset, float localTime)
        {
            TrackId = trackId;
            TargetId = targetId;
            Type = type;
            ClipId = clipId;
            ClipAsset = clipAsset;
            LocalTime = localTime;
        }
    }
}
