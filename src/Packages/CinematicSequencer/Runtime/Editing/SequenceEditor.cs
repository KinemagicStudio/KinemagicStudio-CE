using System;
using CinematicSequencer.Animation;
using CinematicSequencer.Editing.Commands;

namespace CinematicSequencer.Editing
{
    /// <summary>
    /// シーケンスの編集セッションを管理する。
    /// UIからのコマンド発行窓口であり、Undo/Redo、変更追跡、保存状態を統括する。
    /// </summary>
    public sealed class SequenceEditor : IDisposable
    {
        private readonly EditHistory _editHistory;
        private Sequence _sequence;
        private bool _hasUnsavedChanges;

        public Sequence Sequence => _sequence;
        public EditHistory History => _editHistory;
        public bool HasUnsavedChanges => _hasUnsavedChanges;

        public event Action<ModelChangeEvent> SequenceChanged;
        public event Action UnsavedChangesStateChanged;

        public SequenceEditor(int maxHistorySize = 100)
        {
            _editHistory = new EditHistory(maxHistorySize);
        }

        public void SetSequence(Sequence sequence)
        {
            if (_sequence != null)
            {
                _sequence.Changed -= OnSequenceChanged;
            }

            _sequence = sequence;
            _hasUnsavedChanges = false;
            _editHistory.Clear();

            if (_sequence != null)
            {
                _sequence.Changed += OnSequenceChanged;
            }
        }

        // --- シーケンス操作（全てEditHistory経由） ---

        public void AddTrack(string name, TrackType type)
        {
            if (_sequence == null) return;
            _editHistory.Execute(new AddTrackCommand(_sequence, name, type));
            MarkDirty();
        }

        public void RemoveTrack(Guid trackId)
        {
            if (_sequence == null) return;
            _editHistory.Execute(new RemoveTrackCommand(_sequence, trackId));
            MarkDirty();
        }

        public void AddClip(Guid trackId, Guid clipAssetId, TimeRange placement)
        {
            if (_sequence == null) return;
            _editHistory.Execute(new AddClipCommand(_sequence, trackId, clipAssetId, placement));
            MarkDirty();
        }

        public void RemoveClip(Guid trackId, Guid clipId)
        {
            if (_sequence == null) return;
            _editHistory.Execute(new RemoveClipCommand(_sequence, trackId, clipId));
            MarkDirty();
        }

        public void MoveClip(Guid clipId, Guid oldTrackId, Guid newTrackId,
            TimeRange oldPlacement, TimeRange newPlacement)
        {
            if (_sequence == null) return;
            _editHistory.Execute(new MoveClipCommand(
                _sequence, clipId, oldTrackId, newTrackId, oldPlacement, newPlacement));
            MarkDirty();
        }

        public void ResizeClip(Guid trackId, Guid clipId, TimeRange oldPlacement, TimeRange newPlacement)
        {
            if (_sequence == null) return;
            _editHistory.Execute(new ResizeClipCommand(
                _sequence, trackId, clipId, oldPlacement, newPlacement));
            MarkDirty();
        }

        // --- キーフレーム操作 ---

        public void AddKeyframe(IAnimatableClipAsset clipAsset, string propertyName, Keyframe keyframe)
        {
            _editHistory.Execute(new AddKeyframeCommand(clipAsset, propertyName, keyframe));
            MarkDirty();
        }

        public void RemoveKeyframe(IAnimatableClipAsset clipAsset, string propertyName, Keyframe keyframe)
        {
            _editHistory.Execute(new RemoveKeyframeCommand(clipAsset, propertyName, keyframe));
            MarkDirty();
        }

        public void UpdateKeyframeValue(IAnimatableClipAsset clipAsset, string propertyName,
            float time, float oldValue, float newValue)
        {
            _editHistory.Execute(new UpdateKeyframeCommand(
                clipAsset, propertyName, time, oldValue, newValue));
            MarkDirty();
        }

        /// <summary>
        /// 任意のコマンドを実行する。
        /// </summary>
        public void ExecuteCommand(IEditCommand command)
        {
            _editHistory.Execute(command);
            MarkDirty();
        }

        // --- Undo/Redo ---

        public void Undo()
        {
            _editHistory.Undo();
            MarkDirty();
        }

        public void Redo()
        {
            _editHistory.Redo();
            MarkDirty();
        }

        /// <summary>
        /// 保存完了を記録。
        /// </summary>
        public void MarkSaved()
        {
            if (!_hasUnsavedChanges) return;
            _hasUnsavedChanges = false;
            UnsavedChangesStateChanged?.Invoke();
        }

        private void MarkDirty()
        {
            if (_hasUnsavedChanges) return;
            _hasUnsavedChanges = true;
            UnsavedChangesStateChanged?.Invoke();
        }

        private void OnSequenceChanged(ModelChangeEvent evt)
        {
            SequenceChanged?.Invoke(evt);
        }

        public void Dispose()
        {
            if (_sequence != null)
            {
                _sequence.Changed -= OnSequenceChanged;
            }
        }
    }
}
