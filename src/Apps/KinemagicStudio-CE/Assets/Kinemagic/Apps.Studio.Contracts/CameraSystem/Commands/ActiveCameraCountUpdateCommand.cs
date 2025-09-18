namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public sealed class ActiveCameraCountUpdateCommand : ICameraSystemCommand
    {
        public int Count { get; }

        public ActiveCameraCountUpdateCommand(int count)
        {
            Count = count;
        }
    }
}
