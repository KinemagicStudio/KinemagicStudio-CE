using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class ToneMappingParametersView : MonoBehaviour, IPostProcessingEffectView
    {
        [SerializeField] private UIDocument _document;
        
        private bool _initialized;
        private VisualElement _container;
        private Toggle _enabledToggle;
        private DropdownField _modeDropdown;

        private readonly Subject<TonemappingParameters> _onValueChanged = new();
        public Observable<TonemappingParameters> ValueChanged => _onValueChanged;

        private void Awake()
        {
            Initialize();
        }

        public void UpdateParameters(TonemappingParameters parameters)
        {
            _enabledToggle.value = parameters.IsEnabled;
            if ((int)parameters.Mode < _modeDropdown.choices.Count)
            {
                _modeDropdown.value = _modeDropdown.choices[(int)parameters.Mode];
            }
        }

        public void SetActive(bool isActive)
        {
            _container.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Initialize()
        {
            if (_initialized) return;
            
            var root = _document.rootVisualElement;
            _container = root.Q<VisualElement>("tone-mapping-container");
            _enabledToggle = root.Q<Toggle>("tone-mapping-enabled");
            _modeDropdown = root.Q<DropdownField>("tone-mapping-mode");

            _modeDropdown.choices = new System.Collections.Generic.List<string>
            {
                nameof(TonemappingMode.None),
                nameof(TonemappingMode.Neutral),
                nameof(TonemappingMode.ACES),
            };
            _modeDropdown.value = nameof(TonemappingMode.None);
            
            _enabledToggle.RegisterValueChangedCallback(evt => NotifyValueChanged());
            _modeDropdown.RegisterValueChangedCallback(evt => NotifyValueChanged());

            _initialized = true;
        }

        private void NotifyValueChanged()
        {
            _onValueChanged.OnNext(new TonemappingParameters
            {
                IsEnabled = _enabledToggle.value,
                Mode = (TonemappingMode)_modeDropdown.choices.IndexOf(_modeDropdown.value)
            });
        }
    }
}
