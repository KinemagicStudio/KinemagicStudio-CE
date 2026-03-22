using System;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// 左パネルのトラックヘッダー行。トラック名とコンテキストメニューを表示する。
    /// </summary>
    public sealed class TrackHeaderView : VisualElement
    {
        private readonly Label _nameLabel;

        public Guid TrackId { get; }

        public event Action<Guid> OnDeleteRequested;

        public TrackHeaderView(Track track)
        {
            TrackId = track.Id;

            AddToClassList("track-header");
            AddToClassList(GetTrackTypeClass(track.Type));

            _nameLabel = new Label(track.Name);
            _nameLabel.AddToClassList("track-header__name");
            Add(_nameLabel);

            RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenu);
        }

        public void UpdateName(string name)
        {
            _nameLabel.text = name;
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Delete Track", _ => OnDeleteRequested?.Invoke(TrackId));
        }

        private static string GetTrackTypeClass(TrackType type)
        {
            return type switch
            {
                TrackType.CameraPose => "track-camera",
                TrackType.CameraProperties => "track-camera",
                TrackType.LightPose => "track-light",
                TrackType.LightProperties => "track-light",
                TrackType.Effect => "track-effect",
                TrackType.Audio => "track-audio",
                TrackType.Motion => "track-motion",
                TrackType.PostEffect => "track-posteffect",
                _ => "track-default",
            };
        }
    }
}
