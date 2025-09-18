using System;
using System.Collections.Generic;
using System.Linq;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.MotionCapture
{
    public sealed class MotionDataSourceAdditionView : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private DropdownField _dataSourceTypeDropdown;
        private TextField _serverAddressField;
        private IntegerField _portField;
        private IntegerField _streamingDataIdField;
        private Button _addButton;

        private VisualElement _serverAddressContainer;
        private VisualElement _portContainer;
        private VisualElement _streamingDataIdContainer;

        private readonly Subject<(MotionDataSourceKey DataSourceKey, MotionDataSourceType DataSourceType)> _dataSourceAdditionNotifier = new();
        private IReadOnlyList<MotionDataSourceType> _dataSourceTypes;
        private MotionDataSourceType _currentDataSourceType;

        public Observable<(MotionDataSourceKey DataSourceKey, MotionDataSourceType DataSourceType)> DataSourceAdditionRequested => _dataSourceAdditionNotifier;

        private void Awake()
        {
            _root = _uiDocument.rootVisualElement.Q<VisualElement>("side-panel");
            _dataSourceTypeDropdown = _root.Q<DropdownField>("data-source-type-dropdown");
            _addButton = _root.Q<Button>("add-data-source-button");
            _serverAddressContainer = _root.Q<VisualElement>("server-address-container");
            _serverAddressField = _root.Q<TextField>("server-address-field");
            _portContainer = _root.Q<VisualElement>("port-container");
            _portField = _root.Q<IntegerField>("port-field");
            _streamingDataIdContainer = _root.Q<VisualElement>("streaming-data-id-container");
            _streamingDataIdField = _root.Q<IntegerField>("streaming-data-id-field");

            var dataSourceTypes = Enum.GetValues(typeof(MotionDataSourceType)).Cast<MotionDataSourceType>().ToList();
            UpdateDataSourceTypeDropdown(dataSourceTypes);
            
            _addButton.clicked += OnAddButtonClicked;
            _dataSourceTypeDropdown.RegisterValueChangedCallback(OnDropdownValueChanged);

            Clear();
            Debug.Log($"<color=lime>[{nameof(MotionDataSourceAdditionView)}] Initialized</color>");
        }

        private void OnDestroy()
        {
            _addButton.clicked -= OnAddButtonClicked;
            _dataSourceTypeDropdown.UnregisterValueChangedCallback(OnDropdownValueChanged);
            _dataSourceAdditionNotifier.Dispose();
        }

        public void Show()
        {
            _root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }

        private void Clear()
        {
            _portField.value = 30000;
            _serverAddressField.value = "127.0.0.1";
            _streamingDataIdField.value = 0;
        }

        private void UpdateDataSourceTypeDropdown(IReadOnlyList<MotionDataSourceType> dataSourceTypes)
        {
            _dataSourceTypes = dataSourceTypes;
            
            var choices = new List<string>();
            for (var i = 0; i < dataSourceTypes.Count; i++)
            {
                choices.Add(dataSourceTypes[i].ToString());
            }
            
            _dataSourceTypeDropdown.choices = choices;
            if (choices.Count > 0)
            {
                _dataSourceTypeDropdown.value = choices[0];
                _currentDataSourceType = dataSourceTypes[0];
                SetDefaultPortForDataSourceType(_currentDataSourceType);
            }
        }

        private void SetDefaultPortForDataSourceType(MotionDataSourceType dataSourceType)
        {
            switch (dataSourceType)
            {
                case MotionDataSourceType.VMCProtocolTypeA:
                case MotionDataSourceType.VMCProtocolTypeB:
                    _portField.value = 39539;
                    break;
                case MotionDataSourceType.iFacialMocap:
                    _portField.value = 49983;
                    break;
                case MotionDataSourceType.FaceMotion3d:
                    _portField.value = 50003;
                    break;
                case MotionDataSourceType.Mocopi:
                    _portField.value = 12351;
                    break;
                default:
                    _portField.value = 30000;
                    break;
            }
        }

        private void OnDropdownValueChanged(ChangeEvent<string> evt)
        {
            var selectedIndex = _dataSourceTypeDropdown.choices.IndexOf(evt.newValue);
            if (selectedIndex >= 0 && selectedIndex < _dataSourceTypes.Count)
            {
                _currentDataSourceType = _dataSourceTypes[selectedIndex];
            }
            else
            {
                _currentDataSourceType = MotionDataSourceType.Unknown;
            }
            
            SetDefaultPortForDataSourceType(_currentDataSourceType);
            
            if (_currentDataSourceType == MotionDataSourceType.iFacialMocap ||
                _currentDataSourceType == MotionDataSourceType.FaceMotion3d)
            {
                // Show server address and port fields
                _portContainer.style.display = DisplayStyle.Flex;
                _serverAddressContainer.style.display = DisplayStyle.Flex;
                _streamingDataIdContainer.style.display = DisplayStyle.None;
            }
            else
            {
                // Show only port field for other data sources
                _portContainer.style.display = DisplayStyle.Flex;
                _serverAddressContainer.style.display = DisplayStyle.None;
                _streamingDataIdContainer.style.display = DisplayStyle.None;
            }
        }

        private void OnAddButtonClicked()
        {
            var dataSourceType = _currentDataSourceType;
            
            var serverAddress = dataSourceType switch
            {
                // TODO
                MotionDataSourceType.iFacialMocap => _serverAddressField.value,
                MotionDataSourceType.FaceMotion3d => _serverAddressField.value,
                _ => "0.0.0.0"
            };
            var port = _portField.value;
            
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                return;
            }

            if (port <= 0 || port > 65535)
            {
                return;
            }

            _dataSourceAdditionNotifier.OnNext((new MotionDataSourceKey(serverAddress, port), dataSourceType));

            Clear();
        }
    }
}
