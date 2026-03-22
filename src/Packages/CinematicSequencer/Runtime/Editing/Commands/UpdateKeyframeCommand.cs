namespace CinematicSequencer.Editing.Commands
{
    public sealed class UpdateKeyframeCommand : IEditCommand
    {
        private readonly IAnimatableClipAsset _clipAsset;
        private readonly string _propertyName;
        private readonly float _time;
        private readonly float _oldValue;
        private readonly float _newValue;

        public string Description => $"Update Keyframe ({_propertyName})";

        public UpdateKeyframeCommand(IAnimatableClipAsset clipAsset, string propertyName,
            float time, float oldValue, float newValue)
        {
            _clipAsset = clipAsset;
            _propertyName = propertyName;
            _time = time;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Execute()
        {
            _clipAsset.UpdateKeyframeValue(_propertyName, _time, _newValue);
        }

        public void Undo()
        {
            _clipAsset.UpdateKeyframeValue(_propertyName, _time, _oldValue);
        }
    }
}
