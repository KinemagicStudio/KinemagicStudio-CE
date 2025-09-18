using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRMToolkit.UI
{
    public sealed class CharacterModelListView : MonoBehaviour
    {
        public const string SelectedClassName = "selected";
        public const string CharacterModelListContainerName = "character-model-list-container";
        public const string RefreshButtonName = "refresh-button";
        public const string OpenFolderButtonName = "open-folder-button";
        public const string CharacterModelListItemClassName = "character-model-list-item";
        public const string ThumbnailClassName = "character-model-thumbnail";
        public const string ModelInfoContainerClassName = "character-model-info";
        public const string NameLabelClassName = "character-name-label";
        public const string StorageTypeLabelClassName = "storage-type-label";

        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _characterModelListContainer;
        private Button _refreshButton;
        private Button _openFolderButton;

        public event Action<ICharacterModelInfo> ModelSelected;
        public event Action RefreshButtonClicked;
        public event Action OpenFolderButtonClicked;

        private void Awake()
        {
            var root = _uiDocument.rootVisualElement;
            _characterModelListContainer = root.Q<VisualElement>(CharacterModelListContainerName);
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
            _characterModelListContainer.Clear();
        }

        public void AddCharacterModel(ICharacterModelInfo characterModel)
        {
            // Create item container
            var listItem = new VisualElement();
            listItem.AddToClassList(CharacterModelListItemClassName);

            // Create thumbnail image
            var thumbnailImage = new Image();
            thumbnailImage.AddToClassList(ThumbnailClassName);
            if (characterModel.Thumbnail != null)
            {
                thumbnailImage.image = characterModel.Thumbnail;
            }

            // Create info container
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList(ModelInfoContainerClassName);

            // Create name label
            var characterNameLabel = new Label(characterModel.DisplayName);
            characterNameLabel.AddToClassList(NameLabelClassName);

            // Create storage type label
            var storageTypeLabel = new Label(characterModel.StorageType);
            storageTypeLabel.AddToClassList(StorageTypeLabelClassName);

            // Add elements to containers
            infoContainer.Add(characterNameLabel);
            infoContainer.Add(storageTypeLabel);
            listItem.Add(thumbnailImage);
            listItem.Add(infoContainer);

            // Add item to the list container
            _characterModelListContainer.Add(listItem);
            listItem.RegisterCallback<ClickEvent>(evt => OnItemSelected(listItem, characterModel));
        }

        private void OnRefreshButtonClicked()
        {
            RefreshButtonClicked?.Invoke();
        }

        private void OnOpenFolderButtonClicked()
        {
            OpenFolderButtonClicked?.Invoke();
        }

        private void OnItemSelected(VisualElement selectedItem, ICharacterModelInfo selectedModel)
        {
            foreach (var item in _characterModelListContainer.Children())
            {
                item.RemoveFromClassList(SelectedClassName);
            }
            selectedItem.AddToClassList(SelectedClassName);

            ModelSelected?.Invoke(selectedModel);
        }
    }
}
