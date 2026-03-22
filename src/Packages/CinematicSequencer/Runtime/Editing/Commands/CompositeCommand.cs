namespace CinematicSequencer.Editing.Commands
{
    /// <summary>
    /// 複数のコマンドを1つのUndo単位として扱う。
    /// 例: 「全プロパティにキーフレーム追加」を1回のUndoで取り消す。
    /// </summary>
    public sealed class CompositeCommand : IEditCommand
    {
        private readonly IEditCommand[] _commands;

        public string Description { get; }

        public CompositeCommand(string description, params IEditCommand[] commands)
        {
            Description = description;
            _commands = commands;
        }

        public void Execute()
        {
            foreach (var cmd in _commands)
                cmd.Execute();
        }

        public void Undo()
        {
            for (int i = _commands.Length - 1; i >= 0; i--)
                _commands[i].Undo();
        }
    }
}
