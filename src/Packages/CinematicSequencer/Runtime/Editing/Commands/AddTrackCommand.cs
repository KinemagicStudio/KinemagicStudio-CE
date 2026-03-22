using System;

namespace CinematicSequencer.Editing.Commands
{
    public sealed class AddTrackCommand : IEditCommand
    {
        private readonly Sequence _sequence;
        private readonly string _name;
        private readonly TrackType _type;
        private Track _addedTrack;

        public string Description => $"Add {_type} Track";

        public AddTrackCommand(Sequence sequence, string name, TrackType type)
        {
            _sequence = sequence;
            _name = name;
            _type = type;
        }

        public void Execute()
        {
            if (_addedTrack == null)
            {
                _addedTrack = _sequence.AddTrack(_name, _type);
            }
            else
            {
                _sequence.InsertTrack(_addedTrack);
            }
        }

        public void Undo()
        {
            _sequence.RemoveTrack(_addedTrack.Id);
        }
    }
}
