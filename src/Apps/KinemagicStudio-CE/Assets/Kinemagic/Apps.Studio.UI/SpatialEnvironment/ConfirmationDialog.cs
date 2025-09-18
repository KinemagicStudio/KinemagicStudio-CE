using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.SpatialEnvironment
{
    public sealed class ConfirmationDialog : MonoBehaviour
    {
        public enum DialogResult
        {
            OK,
            Cancel
        }

        private const string DialogContainerName = "confirmation-dialog-container";
        private const string DialogTitleName = "confirmation-title";
        private const string DialogMessageName = "confirmation-message";
        private const string OkButtonName = "confirmation-ok-button";
        private const string CancelButtonName = "confirmation-cancel-button";

        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _dialogContainer;
        private Label _dialogTitle;
        private Label _dialogMessage;
        private Button _confirmButton;
        private Button _cancelButton;

        private Action<DialogResult> _onResultAction;

        private void Awake()
        {
            _uiDocument.sortingOrder = 1000; // Ensure the dialog is displayed in front of other UI elements
            
            var root = _uiDocument.rootVisualElement;
            _dialogContainer = root.Q<VisualElement>(DialogContainerName);
            _dialogTitle = root.Q<Label>(DialogTitleName);
            _dialogMessage = root.Q<Label>(DialogMessageName);
            _confirmButton = root.Q<Button>(OkButtonName);
            _cancelButton = root.Q<Button>(CancelButtonName);

            _confirmButton.clicked += OnConfirmButtonClicked;
            _cancelButton.clicked += OnCancelButtonClicked;

            Hide();
        }

        private void OnDestroy()
        {
            _confirmButton.clicked -= OnConfirmButtonClicked;
            _cancelButton.clicked -= OnCancelButtonClicked;
        }

        public void Show(string title, string message, Action<DialogResult> onResult)
        {
            _onResultAction = onResult;
            _dialogTitle.text = title;
            _dialogMessage.text = message;
            _dialogContainer.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _dialogContainer.style.display = DisplayStyle.None;
            _onResultAction = null;
        }

        private void OnConfirmButtonClicked()
        {
            _onResultAction?.Invoke(DialogResult.OK);
            Hide();
        }

        private void OnCancelButtonClicked()
        {
            _onResultAction?.Invoke(DialogResult.Cancel);
            Hide();
        }
    }
}