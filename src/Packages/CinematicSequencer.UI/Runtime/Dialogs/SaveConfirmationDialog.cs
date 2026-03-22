using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// 保存確認ダイアログ。UniTaskベースの非同期APIを提供する。
    /// v1 SaveConfirmationDialogView の置き換え。
    /// </summary>
    public sealed class SaveConfirmationDialog : VisualElement
    {
        public enum SaveDialogResult
        {
            Save,
            DontSave,
            Cancel,
        }

        private readonly VisualElement _window;
        private readonly Button _saveButton;
        private readonly Button _dontSaveButton;
        private readonly Button _cancelButton;

        private UniTaskCompletionSource<SaveDialogResult> _tcs;

        public SaveConfirmationDialog()
        {
            AddToClassList("dialog-container");
            style.display = DisplayStyle.None;

            // 半透明オーバーレイ背景
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            style.backgroundColor = new UnityEngine.Color(0f, 0f, 0f, 0.5f);
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            // ダイアログウィンドウ
            _window = new VisualElement();
            _window.AddToClassList("dialog-window");
            Add(_window);

            var title = new Label("Unsaved Changes");
            title.AddToClassList("dialog-title");
            _window.Add(title);

            var message = new Label(
                "There are unsaved changes in the current sequence. Do you want to save before proceeding?");
            message.AddToClassList("dialog-message");
            _window.Add(message);

            // ボタンコンテナ
            var buttons = new VisualElement();
            buttons.AddToClassList("dialog-buttons");
            buttons.style.flexDirection = FlexDirection.Row;
            _window.Add(buttons);

            _saveButton = new Button { text = "Save" };
            _saveButton.AddToClassList("dialog-button");
            _saveButton.clicked += () => Complete(SaveDialogResult.Save);
            buttons.Add(_saveButton);

            _dontSaveButton = new Button { text = "Don't Save" };
            _dontSaveButton.AddToClassList("dialog-button");
            _dontSaveButton.clicked += () => Complete(SaveDialogResult.DontSave);
            buttons.Add(_dontSaveButton);

            _cancelButton = new Button { text = "Cancel" };
            _cancelButton.AddToClassList("dialog-button");
            _cancelButton.clicked += () => Complete(SaveDialogResult.Cancel);
            buttons.Add(_cancelButton);
        }

        public UniTask<SaveDialogResult> ShowAsync()
        {
            _tcs = new UniTaskCompletionSource<SaveDialogResult>();
            style.display = DisplayStyle.Flex;
            return _tcs.Task;
        }

        public void Hide()
        {
            style.display = DisplayStyle.None;
        }

        private void Complete(SaveDialogResult result)
        {
            Hide();
            _tcs?.TrySetResult(result);
        }
    }
}
