using System;

namespace CinematicSequencer.Editing.Commands
{
    public sealed class RemoveClipCommand : IEditCommand
    {
        private readonly Sequence _sequence;
        private readonly Guid _trackId;
        private readonly Guid _clipId;
        private Clip _removedClip;

        public string Description => "Remove Clip";

        public RemoveClipCommand(Sequence sequence, Guid trackId, Guid clipId)
        {
            _sequence = sequence;
            _trackId = trackId;
            _clipId = clipId;
        }

        public void Execute()
        {
            var track = _sequence.GetTrack(_trackId);
            if (track == null) return;
            _removedClip = track.GetClip(_clipId);
            track.RemoveClip(_clipId);
        }

        public void Undo()
        {
            if (_removedClip == null) return;
            var track = _sequence.GetTrack(_trackId);
            track?.InsertClip(_removedClip);
        }
    }
}
