using System;

namespace CinematicSequencer.Editing.Commands
{
    public sealed class MoveClipCommand : IEditCommand
    {
        private readonly Sequence _sequence;
        private readonly Guid _clipId;
        private readonly Guid _oldTrackId;
        private readonly Guid _newTrackId;
        private readonly TimeRange _oldPlacement;
        private readonly TimeRange _newPlacement;

        public string Description => "Move Clip";

        public MoveClipCommand(
            Sequence sequence, Guid clipId,
            Guid oldTrackId, Guid newTrackId,
            TimeRange oldPlacement, TimeRange newPlacement)
        {
            _sequence = sequence;
            _clipId = clipId;
            _oldTrackId = oldTrackId;
            _newTrackId = newTrackId;
            _oldPlacement = oldPlacement;
            _newPlacement = newPlacement;
        }

        public void Execute()
        {
            MoveClip(_oldTrackId, _newTrackId, _newPlacement);
        }

        public void Undo()
        {
            MoveClip(_newTrackId, _oldTrackId, _oldPlacement);
        }

        private void MoveClip(Guid fromTrackId, Guid toTrackId, TimeRange placement)
        {
            var fromTrack = _sequence.GetTrack(fromTrackId);
            if (fromTrack == null) return;

            var clip = fromTrack.GetClip(_clipId);
            if (clip == null) return;

            if (fromTrackId == toTrackId)
            {
                clip.Placement = placement;
            }
            else
            {
                var toTrack = _sequence.GetTrack(toTrackId);
                if (toTrack == null) return;

                fromTrack.RemoveClip(_clipId);
                clip.Placement = placement;
                toTrack.InsertClip(clip);
            }
        }
    }
}
