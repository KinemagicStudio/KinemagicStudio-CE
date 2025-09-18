using System;

namespace CinematicSequencer
{
    public sealed class ClipDataInfo
    {
        public const string IdPropertyKey = "Id";
        public const string NamePropertyKey = "Name";
        public const string TypePropertyKey = "Type";

        public Guid Id { get; set; }
        public string Name { get; set; }
        public DataType Type { get; set; }

        public ClipDataInfo(Guid id, string name, DataType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }
    }
}