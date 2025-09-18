using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.AppCore
{
    public sealed class AppBarView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private Label _fpsLabel;
        private Label _ipLabel;
        private VisualElement _ipAddressContainer;

        private readonly Subject<Unit> _onLocalIpUpdateButtonClicked = new();
        public Observable<Unit> LocalIpUpdateButtonClicked => _onLocalIpUpdateButtonClicked;

        private void Awake()
        {
            var updateButton = _document.rootVisualElement.Q<Button>("update-ip-address-button");
            updateButton.clicked += () =>
            {
                _onLocalIpUpdateButtonClicked.OnNext(Unit.Default);
            };
        }

        public void SetFrameRateValue(float fps)
        {
            if (_fpsLabel == null)
            {
                _fpsLabel = _document.rootVisualElement.Q<Label>("fps-display");
            }
            _fpsLabel.text = $"FPS: {fps:0.0}";
        }

        public void SetLocalIpAddress(string ipAddress)
        {
            if (_ipLabel == null)
            {
                _ipLabel = _document.rootVisualElement.Q<Label>("ip-address-display");
            }
            _ipLabel.text = $"Local IP: {ipAddress}";
        }

        public void SetLocalIpAddressVisibility(bool isVisible)
        {
            if (_ipAddressContainer == null)
            {
                _ipAddressContainer = _document.rootVisualElement.Q<VisualElement>("ip-address-container");
            }
            _ipAddressContainer.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}