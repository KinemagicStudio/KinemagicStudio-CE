using System;
using System.Collections.Generic;

namespace CinematicSequencer.Editing
{
    /// <summary>
    /// Undo/Redoスタックを管理するクラス。
    /// </summary>
    public sealed class EditHistory
    {
        private readonly List<IEditCommand> _undoStack = new();
        private readonly List<IEditCommand> _redoStack = new();
        private readonly int _maxHistorySize;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public string UndoDescription => CanUndo ? _undoStack[_undoStack.Count - 1].Description : null;
        public string RedoDescription => CanRedo ? _redoStack[_redoStack.Count - 1].Description : null;

        public event Action HistoryChanged;

        public EditHistory(int maxHistorySize = 100)
        {
            _maxHistorySize = maxHistorySize;
        }

        /// <summary>
        /// コマンドを実行し、Undoスタックに積む。Redoスタックはクリアされる。
        /// </summary>
        public void Execute(IEditCommand command)
        {
            command.Execute();
            _undoStack.Add(command);
            _redoStack.Clear();

            if (_undoStack.Count > _maxHistorySize)
                _undoStack.RemoveAt(0);

            HistoryChanged?.Invoke();
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var command = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            command.Undo();
            _redoStack.Add(command);
            HistoryChanged?.Invoke();
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var command = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            command.Execute();
            _undoStack.Add(command);
            HistoryChanged?.Invoke();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            HistoryChanged?.Invoke();
        }
    }
}
