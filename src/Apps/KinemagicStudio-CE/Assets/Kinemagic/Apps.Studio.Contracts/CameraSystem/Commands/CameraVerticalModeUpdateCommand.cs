namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public sealed class CameraVerticalModeUpdateCommand : ICameraSystemCommand
    {
        public bool IsVerticalMode { get; }

        public CameraVerticalModeUpdateCommand(bool isVerticalMode)
        {
            IsVerticalMode = isVerticalMode;
        }
    }
}
