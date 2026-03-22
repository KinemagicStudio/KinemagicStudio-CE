using System;

namespace CinematicSequencer.Editing.Commands
{
    public sealed class ResizeClipCommand : IEditCommand
    {
        private readonly Sequence _sequence;
        private readonly Guid _trackId;
        private readonly Guid _clipId;
        private readonly TimeRange _oldPlacement;
        private readonly TimeRange _newPlacement;

        public string Description => "Resize Clip";

        public ResizeClipCommand(Sequence sequence, Guid trackId, Guid clipId,
            TimeRange oldPlacement, TimeRange newPlacement)
        {
            _sequence = sequence;
            _trackId = trackId;
            _clipId = clipId;
            _oldPlacement = oldPlacement;
            _newPlacement = newPlacement;
        }

        public void Execute()
        {
            SetPlacement(_newPlacement);
        }

        public void Undo()
        {
            SetPlacement(_oldPlacement);
        }

        private void SetPlacement(TimeRange placement)
        {
            var track = _sequence.GetTrack(_trackId);
            var clip = track?.GetClip(_clipId);
            if (clip != null) clip.Placement = placement;
        }
    }
}
