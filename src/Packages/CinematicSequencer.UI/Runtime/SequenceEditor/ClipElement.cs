using System;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// タイムライン上のクリップUI要素。
    /// Position.Absoluteで親TrackContentView内に配置され、ズーム対応。
    /// </summary>
    public sealed class ClipElement : VisualElement
    {
        private readonly Label _nameLabel;
        private readonly ZoomState _zoom;

        private float _startSeconds;
        private float _durationSeconds;

        public Guid ClipId { get; }
        public Guid TrackId { get; set; }
        public TrackType TrackType { get; }

        public event Action<Guid> OnClicked;
        public event Action<Guid> OnDoubleClicked;

        public ClipElement(Clip clip, Track track, ZoomState zoom)
        {
            ClipId = clip.Id;
            TrackId = track.Id;
            TrackType = track.Type;
            _zoom = zoom;

            _startSeconds = clip.Placement.Start;
            _durationSeconds = clip.Placement.Duration;

            AddToClassList("sequence-clip");
            AddToClassList(GetTrackTypeClass(track.Type));
            style.position = Position.Absolute;

            _nameLabel = new Label(clip.ClipAsset?.ToString() ?? clip.ClipAssetId.ToString("N")[..8]);
            _nameLabel.AddToClassList("sequence-clip__name");
            Add(_nameLabel);

            UpdateLayout();

            RegisterCallback<ClickEvent>(OnClick);
        }

        public void UpdateFromModel(Clip clip)
        {
            _startSeconds = clip.Placement.Start;
            _durationSeconds = clip.Placement.Duration;
            _nameLabel.text = clip.ClipAsset?.ToString() ?? clip.ClipAssetId.ToString("N")[..8];
            UpdateLayout();
        }

        public void UpdateZoom()
        {
            UpdateLayout();
        }

        public void SetSelected(bool selected)
        {
            EnableInClassList("selected", selected);
        }

        public void AttachManipulator(ClipManipulator manipulator)
        {
            this.AddManipulator(manipulator);
        }

        private void UpdateLayout()
        {
            float pps = _zoom.PixelsPerSecond;
            style.left = _startSeconds * pps;
            style.width = _durationSeconds * pps;
        }

        private void OnClick(ClickEvent evt)
        {
            if (evt.clickCount == 2)
                OnDoubleClicked?.Invoke(ClipId);
            else
                OnClicked?.Invoke(ClipId);
            evt.StopPropagation();
        }

        private static string GetTrackTypeClass(TrackType type)
        {
            return type switch
            {
                TrackType.CameraPose => "clip-camera",
                TrackType.CameraProperties => "clip-camera",
                TrackType.LightPose => "clip-light",
                TrackType.LightProperties => "clip-light",
                TrackType.Effect => "clip-effect",
                TrackType.Audio => "clip-audio",
                TrackType.Motion => "clip-motion",
                TrackType.PostEffect => "clip-posteffect",
                _ => "clip-default",
            };
        }
    }
}
