namespace CinematicSequencer
{
    /// <summary>
    /// モデル変更イベントの情報。
    /// </summary>
    public readonly struct ModelChangeEvent
    {
        public enum ChangeType
        {
            PropertyChanged,
            ChildAdded,
            ChildRemoved,
            ChildModified,
            Reordered,
        }

        public ChangeType Type { get; }
        public string PropertyName { get; }
        public object Source { get; }

        public ModelChangeEvent(ChangeType type, string propertyName, object source)
        {
            Type = type;
            PropertyName = propertyName;
            Source = source;
        }
    }
}
