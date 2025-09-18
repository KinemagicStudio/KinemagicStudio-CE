using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class CameraOutputView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private Image _cameraOutputImage;
        private Image _mainCameraOutputImage;
        private VisualElement _cameraSwitcherLables;
        private VisualElement _cameraOutputControl;
        private DropdownField _numOfCamera;
        private VisualElement _cameraLabels2;
        private VisualElement _cameraLabels8;

        private void Awake()
        {
            var root = _document.rootVisualElement;
            _cameraOutputImage = root.Q<Image>("camera-output-image");
            _mainCameraOutputImage = root.Q<Image>("main-camera-output-image");
            _cameraSwitcherLables = root.Q<VisualElement>("camera-switcher-labels");
            _cameraOutputControl = root.Q<VisualElement>("camera-output-control");

            _cameraLabels2 = _document.rootVisualElement.Q<VisualElement>("camera-labels-2");
            _cameraLabels8 = _document.rootVisualElement.Q<VisualElement>("camera-labels-8");

            _numOfCamera = _document.rootVisualElement.Q<DropdownField>("num-of-camera-dropdown");
            _numOfCamera.RegisterValueChangedCallback(evt =>
            {
                if (int.Parse(evt.newValue) == 2)
                {
                    _cameraLabels2.style.display = DisplayStyle.Flex;
                    _cameraLabels8.style.display = DisplayStyle.None;
                }
                else
                {
                    _cameraLabels2.style.display = DisplayStyle.None;
                    _cameraLabels8.style.display = DisplayStyle.Flex;
                }
            });
        }

        public void SetCameraSwitcherView(Texture mainCameraOutput, Texture multiCameraOutput)
        {
            _mainCameraOutputImage.image = mainCameraOutput;
            _mainCameraOutputImage.style.display = DisplayStyle.Flex;

            _cameraOutputImage.image = multiCameraOutput;
            _cameraOutputImage.style.display = DisplayStyle.Flex;

            _cameraSwitcherLables.style.display = DisplayStyle.Flex;
            _cameraOutputControl.style.display = DisplayStyle.Flex;
        }

        public void SetSingleCameraOutputView(Texture cameraOutput)
        {
            _cameraOutputImage.image = cameraOutput;
            _cameraOutputImage.style.display = DisplayStyle.Flex;

            _cameraSwitcherLables.style.display = DisplayStyle.None;
            _mainCameraOutputImage.style.display = DisplayStyle.None;
            _cameraOutputControl.style.display = DisplayStyle.None;
        }
    }
}
