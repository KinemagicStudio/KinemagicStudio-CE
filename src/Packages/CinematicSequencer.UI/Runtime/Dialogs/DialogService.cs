using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// ダイアログの統一管理サービス。
    /// v1では各Presenterが直接SaveConfirmationDialogViewを参照していたのを解消。
    /// </summary>
    public sealed class DialogService
    {
        private readonly VisualElement _dialogLayer;
        private readonly SaveConfirmationDialog _saveConfirmation;

        public DialogService(VisualElement dialogLayer)
        {
            _dialogLayer = dialogLayer ?? throw new ArgumentNullException(nameof(dialogLayer));
            _saveConfirmation = new SaveConfirmationDialog();
            _dialogLayer.Add(_saveConfirmation);
        }

        /// <summary>
        /// 保存確認ダイアログを表示して結果を返す。
        /// </summary>
        public UniTask<SaveConfirmationDialog.SaveDialogResult> ShowSaveConfirmationAsync()
        {
            return _saveConfirmation.ShowAsync();
        }

        /// <summary>
        /// 汎用Yes/No確認ダイアログ。
        /// 現時点ではSaveConfirmationDialogのSave=Yes, DontSave/Cancel=Noとして再利用。
        /// </summary>
        public async UniTask<bool> ShowConfirmationAsync(string title, string message)
        {
            // 将来的には汎用ConfirmationDialogを実装する。
            // 現時点ではSaveConfirmationDialogで代用。
            var result = await _saveConfirmation.ShowAsync();
            return result == SaveConfirmationDialog.SaveDialogResult.Save;
        }
    }
}
