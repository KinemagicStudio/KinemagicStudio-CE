using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.AppCore
{
    public sealed class LicenseView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private TextAsset _mainLicenseAsset;
        [SerializeField] private TextAsset _thirdPartyNoticesAsset;

        private string _licenseText;
        private Label _licenseContentLabel;
 
        private void Awake()
        {
            _licenseText = $"{Application.productName} is developed based on Kinemagic Studio Community Edition." + Environment.NewLine
                            + "The source code of Community Edition is available on GitHub." + Environment.NewLine + Environment.NewLine
                            + _mainLicenseAsset.text + Environment.NewLine + Environment.NewLine
                            + $"{Application.productName} uses the following third-party libraries or resources." + Environment.NewLine + Environment.NewLine
                            + _thirdPartyNoticesAsset.text;
            Initialize();
        }

        public void Show()
        {
            _document.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _document.rootVisualElement.style.display = DisplayStyle.None;
        }

        private void Initialize()
        {
            var root = _document.rootVisualElement;
            _licenseContentLabel = root.Q<Label>("license-content");
            _licenseContentLabel.text = _licenseText;
        }
    }
}