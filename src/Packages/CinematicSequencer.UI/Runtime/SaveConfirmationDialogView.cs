using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    public sealed class SaveConfirmationDialogView : MonoBehaviour
    {
        public enum DialogResult
        {
            Save,
            DontSave,
            Cancel
        }

        private const string DialogContainerName = "dialog-container";
        private const string SaveButtonName = "save-button";
        private const string DontSaveButtonName = "dont-save-button";
        private const string CancelButtonName = "cancel-button";

        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _dialogContainer;
        private Button _saveButton;
        private Button _dontSaveButton;
        private Button _cancelButton;
        private bool _initialized;

        private Action<DialogResult> _onResult;

        private void OnEnable()
        {
            if (_initialized) return;

            _uiDocument.sortingOrder = 1000; // Ensure the dialog is displayed in front of other UI elements

            var rootElement = _uiDocument.rootVisualElement;

            _dialogContainer = rootElement.Q<VisualElement>(DialogContainerName);
            _saveButton = rootElement.Q<Button>(SaveButtonName);
            _dontSaveButton = rootElement.Q<Button>(DontSaveButtonName);
            _cancelButton = rootElement.Q<Button>(CancelButtonName);

            _saveButton.clicked += () => _onResult?.Invoke(DialogResult.Save);
            _dontSaveButton.clicked += () => _onResult?.Invoke(DialogResult.DontSave);
            _cancelButton.clicked += () => _onResult?.Invoke(DialogResult.Cancel);

            Hide();

            _initialized = true;
        }

        public void Show(Action<DialogResult> onResult)
        {
            _onResult = onResult;
            _dialogContainer.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _dialogContainer.style.display = DisplayStyle.None;
        }
    }
}
