using System;
using Kinemagic.Apps.Studio.Contracts.SpatialEnvironment;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.SpatialEnvironment
{
    public sealed class EnvironmentModelListView : MonoBehaviour
    {
        public const string SelectedClassName = "selected";
        public const string EnvironmentModelListContainerName = "environment-model-list-container";
        public const string RefreshButtonName = "refresh-button";
        public const string OpenFolderButtonName = "open-folder-button";
        public const string EnvironmentModelListItemClassName = "environment-model-list-item";
        public const string ModelInfoContainerClassName = "environment-model-info";
        public const string NameLabelClassName = "environment-name-label";
        public const string StorageTypeLabelClassName = "storage-type-label";

        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _environmentModelListContainer;
        private Button _refreshButton;
        private Button _openFolderButton;

        public event Action<EnvironmentModelInfo> ModelSelected;
        public event Action RefreshButtonClicked;
        public event Action OpenFolderButtonClicked;

        private void Awake()
        {
            var root = _uiDocument.rootVisualElement;
            _environmentModelListContainer = root.Q<VisualElement>(EnvironmentModelListContainerName);
            _refreshButton = root.Q<Button>(RefreshButtonName);
            _openFolderButton = root.Q<Button>(OpenFolderButtonName);
            _refreshButton.clicked += OnRefreshButtonClicked;
            _openFolderButton.clicked += OnOpenFolderButtonClicked;
        }

        private void OnDestroy()
        {
            _refreshButton.clicked -= OnRefreshButtonClicked;
            _openFolderButton.clicked -= OnOpenFolderButtonClicked;
        }

        public void Show()
        {
            _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }

        public void ClearList()
        {
            _environmentModelListContainer.Clear();
        }

        public void AddEnvironmentModel(EnvironmentModelInfo environmentModel)
        {
            // Create item container
            var listItem = new VisualElement();
            listItem.AddToClassList(EnvironmentModelListItemClassName);


            // Create info container
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList(ModelInfoContainerClassName);

            // Create name label
            var environmentNameLabel = new Label(environmentModel.DisplayName);
            environmentNameLabel.AddToClassList(NameLabelClassName);

            // Create storage type label
            var storageTypeLabel = new Label(environmentModel.StorageType);
            storageTypeLabel.AddToClassList(StorageTypeLabelClassName);

            // Add elements to containers
            infoContainer.Add(environmentNameLabel);
            infoContainer.Add(storageTypeLabel);
            listItem.Add(infoContainer);

            // Add item to the list container
            _environmentModelListContainer.Add(listItem);
            listItem.RegisterCallback<ClickEvent>(evt => OnItemSelected(listItem, environmentModel));
        }

        private void OnRefreshButtonClicked()
        {
            RefreshButtonClicked?.Invoke();
        }

        private void OnOpenFolderButtonClicked()
        {
            OpenFolderButtonClicked?.Invoke();
        }

        private void OnItemSelected(VisualElement selectedItem, EnvironmentModelInfo selectedModel)
        {
            foreach (var item in _environmentModelListContainer.Children())
            {
                item.RemoveFromClassList(SelectedClassName);
            }
            selectedItem.AddToClassList(SelectedClassName);

            ModelSelected?.Invoke(selectedModel);
        }
    }
}