using System;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using MessagePipe;
using R3;
using UnityEngine;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class CameraPoseController : IDisposable, IInitializable
    {
        private readonly CompositeDisposable _disposables = new();

        private readonly UIViewContext _context;
        private readonly CameraSystemInputActions _inputActions;
        private readonly IPublisher<ICameraSystemCommand> _commandPublisher;

        private bool _initialized;
        private CameraId _targetCameraId = new(1);

        public CameraPoseController(
            UIViewContext context,
            CameraSystemInputActions inputActions,
            IPublisher<ICameraSystemCommand> commandPublisher)
        {
            _context = context;
            _inputActions = inputActions;
            _commandPublisher = commandPublisher;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void Initialize()
        {
            if (_initialized) return;


            _inputActions.ControlTargetCameraSwitched
                .Subscribe(cameraId =>
                {
                    if (_context.CurrentPage.Value != UIPageType.CameraControl) return;
                    _targetCameraId = cameraId;
                })
                .AddTo(_disposables);

            _inputActions.Moved
                .Subscribe(deltaPosition =>
                {
                    if (_context.CurrentPage.Value == UIPageType.CameraSwitcher) return;

                    var cameraId = _context.CurrentPage.Value == UIPageType.CameraControl
                                    ? _targetCameraId
                                    : new CameraId(0); // Scene view camera

                    _commandPublisher.Publish(new CameraPositionUpdateCommand(CameraPositionUpdateCommandType.Movement)
                    {
                        CameraId = cameraId,
                        Position = new System.Numerics.Vector3(deltaPosition.x, deltaPosition.y, deltaPosition.z),
                    });
                })
                .AddTo(_disposables);

            _inputActions.Rotated
                .Subscribe(deltaRotation =>
                {
                    if (_context.CurrentPage.Value == UIPageType.CameraSwitcher) return;

                    var cameraId = _context.CurrentPage.Value == UIPageType.CameraControl
                                    ? _targetCameraId
                                    : new CameraId(0); // Scene view camera

                    _commandPublisher.Publish(new CameraRotationUpdateCommand(CameraRotationUpdateCommandType.RotateAroundWorldUp)
                    {
                        CameraId = cameraId,
                        EulerAngles = new System.Numerics.Vector3(deltaRotation.x, deltaRotation.y, 0f),
                    });
                })
                .AddTo(_disposables);

            Debug.Log($"[{nameof(CameraPoseController)}] Initialized");
            _initialized = true;
        }
    }
}