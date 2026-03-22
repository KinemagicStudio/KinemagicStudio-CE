using System;

namespace CinematicSequencer.Editing.Commands
{
    public sealed class AddClipCommand : IEditCommand
    {
        private readonly Sequence _sequence;
        private readonly Guid _trackId;
        private readonly Guid _clipAssetId;
        private readonly TimeRange _placement;
        private Clip _addedClip;

        public string Description => "Add Clip";

        public AddClipCommand(Sequence sequence, Guid trackId, Guid clipAssetId, TimeRange placement)
        {
            _sequence = sequence;
            _trackId = trackId;
            _clipAssetId = clipAssetId;
            _placement = placement;
        }

        public void Execute()
        {
            var track = _sequence.GetTrack(_trackId);
            if (track == null) return;

            if (_addedClip == null)
            {
                _addedClip = track.AddClip(_clipAssetId, _placement);
            }
            else
            {
                track.InsertClip(_addedClip);
            }
        }

        public void Undo()
        {
            var track = _sequence.GetTrack(_trackId);
            track?.RemoveClip(_addedClip.Id);
        }
    }
}
