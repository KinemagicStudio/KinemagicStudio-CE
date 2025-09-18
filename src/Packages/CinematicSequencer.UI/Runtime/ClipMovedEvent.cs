namespace CinematicSequencer.UI
{
    public sealed class ClipMovedEvent
    {
        public int ClipId { get; }
        public float NewStartTime { get; }
        public int NewTrackId { get; }
        public int OldTrackId { get; }

        public ClipMovedEvent(int clipId, float newStartTime, int newTrackId, int oldTrackId)
        {
            ClipId = clipId;
            NewStartTime = newStartTime;
            NewTrackId = newTrackId;
            OldTrackId = oldTrackId;
        }
    }
}