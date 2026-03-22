using System;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// 右パネルのトラックコンテンツ行。ClipElementの親コンテナ。
    /// v1のflat clips-containerと異なり、トラック毎にスコープされる。
    /// </summary>
    public sealed class TrackContentView : VisualElement
    {
        public Guid TrackId { get; }
        public TrackType TrackType { get; }

        public TrackContentView(Track track)
        {
            TrackId = track.Id;
            TrackType = track.Type;

            AddToClassList("track-row");
            style.position = Position.Relative;
        }
    }
}
