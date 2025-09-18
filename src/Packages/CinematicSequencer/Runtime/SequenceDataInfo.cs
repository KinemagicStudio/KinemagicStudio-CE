using System;

namespace CinematicSequencer
{
    public sealed class CinematicSequenceDataInfo
    {
        public const string IdPropertyKey = "Id";
        public const string NamePropertyKey = "Name";
        public const string TrackCountPropertyKey = "TrackCount";

        public Guid Id { get; set; }
        public string Name { get; set; }
        public int TrackCount { get; set; }

        public CinematicSequenceDataInfo(Guid id, string name, int trackCount)
        {
            Id = id;
            Name = name;
            TrackCount = trackCount;
        }
    }
}