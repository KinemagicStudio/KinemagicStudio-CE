using System.Numerics;

namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public sealed class CameraOutputModeUpdateCommand : ICameraSystemCommand
    {
        public CameraOutputMode OutputMode { get; }

        public CameraOutputModeUpdateCommand(CameraOutputMode outputMode)
        {
            OutputMode = outputMode;
        }
    }

    public enum CameraOutputMode
    {
        SplitViewOutput,
        SingleViewOutput,
        SceneViewOutput,
    }
}
