using CinematicSequencer.Animation;

namespace CinematicSequencer.Editing.Commands
{
    public sealed class RemoveKeyframeCommand : IEditCommand
    {
        private readonly IAnimatableClipAsset _clipAsset;
        private readonly string _propertyName;
        private readonly Keyframe _keyframe;

        public string Description => $"Remove Keyframe ({_propertyName})";

        public RemoveKeyframeCommand(IAnimatableClipAsset clipAsset, string propertyName, Keyframe keyframe)
        {
            _clipAsset = clipAsset;
            _propertyName = propertyName;
            _keyframe = keyframe;
        }

        public void Execute()
        {
            _clipAsset.RemoveKeyframe(_propertyName, _keyframe.Time);
        }

        public void Undo()
        {
            _clipAsset.AddKeyframe(_propertyName, _keyframe);
        }
    }
}
