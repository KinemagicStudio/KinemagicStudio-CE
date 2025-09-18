using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class PostProcessingView : MonoBehaviour
    {
        enum PostProcessingEffectType
        {
            None,
            ColorAdjustment,
            ToneMapping,
            DepthOfField,
            Bloom,
            ScreenSpaceLensFlare,
        }

        [SerializeField] private UIDocument _document;

        [Header("Effect Views")]
        [SerializeField] private ColorAdjustmentParametersView _colorAdjustmentView;
        [SerializeField] private ToneMappingParametersView _toneMappingView;
        [SerializeField] private DepthOfFieldParametersView _depthOfFieldView;
        [SerializeField] private BloomParametersView _bloomView;
        [SerializeField] private ScreenSpaceLensFlareParametersView _screenSpaceLensFlareView;

        private readonly Dictionary<PostProcessingEffectType, IPostProcessingEffectView> _effectViews = new();

        private bool _initialized;
        private DropdownField _effectTypeDropdown;

        public ColorAdjustmentParametersView ColorAdjustmentView => _colorAdjustmentView;
        public ToneMappingParametersView ToneMappingView => _toneMappingView;
        public DepthOfFieldParametersView DepthOfFieldView => _depthOfFieldView;
        public BloomParametersView BloomView => _bloomView;
        public ScreenSpaceLensFlareParametersView ScreenSpaceLensFlareView => _screenSpaceLensFlareView;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;

            var root = _document.rootVisualElement;
            _effectTypeDropdown = root.Q<DropdownField>("effect-type-dropdown");

            _effectViews.Add(PostProcessingEffectType.ColorAdjustment, _colorAdjustmentView);
            _effectViews.Add(PostProcessingEffectType.ToneMapping, _toneMappingView);
            _effectViews.Add(PostProcessingEffectType.DepthOfField, _depthOfFieldView);
            _effectViews.Add(PostProcessingEffectType.Bloom, _bloomView);
            _effectViews.Add(PostProcessingEffectType.ScreenSpaceLensFlare, _screenSpaceLensFlareView);
            foreach (var view in _effectViews.Values)
            {
                view.SetActive(false);
            }

            InitializeDropdown();

            _initialized = true;
        }

        private void InitializeDropdown()
        {
            var options = new List<string>();
            foreach (var effectType in Enum.GetValues(typeof(PostProcessingEffectType)))
            {
                options.Add(effectType.ToString());
            }

            _effectTypeDropdown.choices = options;
            _effectTypeDropdown.value = options[0];

            _effectTypeDropdown.RegisterValueChangedCallback(evt =>
            {
                var selectedIndex = _effectTypeDropdown.choices.IndexOf(evt.newValue);
                OnEffectTypeChanged(selectedIndex);
            });

            OnEffectTypeChanged(0); // Initialize with None selected
        }

        private void OnEffectTypeChanged(int index)
        {
            var effectType = (PostProcessingEffectType)index;

            foreach (var view in _effectViews.Values)
            {
                view.SetActive(false);
            }

            if (_effectViews.TryGetValue(effectType, out var selectedEffectView))
            {
                selectedEffectView.SetActive(true);
            }
        }
    }
}
