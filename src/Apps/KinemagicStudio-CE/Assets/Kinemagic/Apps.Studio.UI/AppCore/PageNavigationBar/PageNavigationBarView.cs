using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.AppCore
{
    public sealed class PageNavigationBarView : MonoBehaviour
    {
        const string NavigationBarName = "navigation-bar";
        const string NavButtonClassName = "nav-button";
        
        [SerializeField] private UIDocument _document;
        [SerializeField] private UIPageType _defaultPage = UIPageType.Character;

        private static readonly Dictionary<string, UIPageType> _buttonNameToPageTypeMap = new()
        {
            { "help", UIPageType.Help },
            { "character", UIPageType.Character },
            { "motion-capture", UIPageType.MotionCapture },
            { "spatial-environment", UIPageType.SpatialEnvironment },
            { "camera-control", UIPageType.CameraControl },
            { "camera-switcher", UIPageType.CameraSwitcher },
            { "cinematic-sequencer", UIPageType.CinematicSequencer },
            { "main-camera", UIPageType.Output },
        };

        private Button _selectedButton;
        
        private readonly Subject<UIPageType> _onItemSelected = new();
        public Observable<UIPageType> ItemSelected => _onItemSelected;
        
        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            OnNavigationButtonClicked(_selectedButton);
        }

        private void Initialize()
        {
            var root = _document.rootVisualElement;
            var navbar = root.Q(name: NavigationBarName);
            var buttons = navbar.Query<Button>(className: NavButtonClassName).ToList();

            _selectedButton = buttons[0];

            foreach (var button in buttons)
            {
                button.OnClickAsObservable()
                    .Subscribe(_ => OnNavigationButtonClicked(button))
                    .AddTo(this);

                if (_buttonNameToPageTypeMap.TryGetValue(button.name, out var pageType) && pageType == _defaultPage)
                {
                    _selectedButton = button;
                }
            }
        }

        private void OnNavigationButtonClicked(Button clickedButton)
        {
            _selectedButton?.RemoveFromClassList("selected");

            _selectedButton = clickedButton;
            _selectedButton.AddToClassList("selected");
            
            if (!_buttonNameToPageTypeMap.TryGetValue(clickedButton.name, out var pageType))
            {
                Debug.LogError($"Button name '{clickedButton.name}' does not map to a UIPageType.");
                return;
            }
            
            _onItemSelected.OnNext(pageType);
        }
    }
}
