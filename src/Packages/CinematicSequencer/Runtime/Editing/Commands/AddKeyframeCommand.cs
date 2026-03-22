using CinematicSequencer.Animation;

namespace CinematicSequencer.Editing.Commands
{
    public sealed class AddKeyframeCommand : IEditCommand
    {
        private readonly IAnimatableClipAsset _clipAsset;
        private readonly string _propertyName;
        private readonly Keyframe _keyframe;

        public string Description => $"Add Keyframe ({_propertyName})";

        public AddKeyframeCommand(IAnimatableClipAsset clipAsset, string propertyName, Keyframe keyframe)
        {
            _clipAsset = clipAsset;
            _propertyName = propertyName;
            _keyframe = keyframe;
        }

        public void Execute()
        {
            _clipAsset.AddKeyframe(_propertyName, _keyframe);
        }

        public void Undo()
        {
            _clipAsset.RemoveKeyframe(_propertyName, _keyframe.Time);
        }
    }
}
