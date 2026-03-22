using System;

namespace CinematicSequencer.Editing.Commands
{
    public sealed class RemoveTrackCommand : IEditCommand
    {
        private readonly Sequence _sequence;
        private readonly Guid _trackId;
        private Track _removedTrack;

        public string Description => "Remove Track";

        public RemoveTrackCommand(Sequence sequence, Guid trackId)
        {
            _sequence = sequence;
            _trackId = trackId;
        }

        public void Execute()
        {
            _removedTrack = _sequence.GetTrack(_trackId);
            _sequence.RemoveTrack(_trackId);
        }

        public void Undo()
        {
            if (_removedTrack != null)
            {
                _sequence.InsertTrack(_removedTrack);
            }
        }
    }
}
