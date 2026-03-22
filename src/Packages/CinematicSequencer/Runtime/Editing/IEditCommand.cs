namespace CinematicSequencer.Editing
{
    /// <summary>
    /// 編集操作のコマンドインターフェース。Undo/Redo対応。
    /// </summary>
    public interface IEditCommand
    {
        /// <summary>コマンドの説明（Undo/Redoメニュー表示用）</summary>
        string Description { get; }

        void Execute();
        void Undo();
    }
}
