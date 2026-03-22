using System;
using CinematicSequencer.Editing;
using CinematicSequencer.Playback;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// キーボードショートカットの処理。
    /// SequenceEditorとSequencePlayerへの操作を一元的にハンドリングする。
    /// </summary>
    public sealed class KeyboardShortcutHandler
    {
        private const float FrameDuration = 1f / 30f;

        private readonly SequenceEditor _editor;
        private readonly SequencePlayer _player;
        private readonly SelectionState _selection;

        /// <summary>保存リクエスト。保存はController側で処理する。</summary>
        public event Action SaveRequested;

        public KeyboardShortcutHandler(
            SequenceEditor editor,
            SequencePlayer player,
            SelectionState selection)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        }

        public void RegisterTo(VisualElement root)
        {
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public void UnregisterFrom(VisualElement root)
        {
            root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            bool ctrl = evt.ctrlKey || evt.commandKey;
            bool shift = evt.shiftKey;

            switch (evt.keyCode)
            {
                // Ctrl+Z: Undo
                case KeyCode.Z when ctrl && !shift:
                    _editor.Undo();
                    evt.StopPropagation();
                    break;

                // Ctrl+Shift+Z or Ctrl+Y: Redo
                case KeyCode.Z when ctrl && shift:
                case KeyCode.Y when ctrl:
                    _editor.Redo();
                    evt.StopPropagation();
                    break;

                // Space: Play/Pause toggle
                case KeyCode.Space:
                    if (_player.IsPlaying) _player.Pause();
                    else _player.Play();
                    evt.StopPropagation();
                    break;

                // Delete / Backspace: 選択中のクリップ削除
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    DeleteSelection();
                    evt.StopPropagation();
                    break;

                // Ctrl+S: 保存
                case KeyCode.S when ctrl:
                    SaveRequested?.Invoke();
                    evt.StopPropagation();
                    break;

                // Ctrl+A: 全選択
                case KeyCode.A when ctrl:
                    SelectAll();
                    evt.StopPropagation();
                    break;

                // Escape: 選択解除
                case KeyCode.Escape:
                    _selection.ClearSelection();
                    evt.StopPropagation();
                    break;

                // Home: 先頭にシーク
                case KeyCode.Home:
                    _player.Seek(0f);
                    evt.StopPropagation();
                    break;

                // End: 末尾にシーク
                case KeyCode.End:
                    if (_editor.Sequence != null)
                        _player.Seek(_editor.Sequence.Duration.End);
                    evt.StopPropagation();
                    break;

                // Left: 1フレーム戻る
                case KeyCode.LeftArrow:
                    _player.Seek(Mathf.Max(0f, _player.CurrentTime - FrameDuration));
                    evt.StopPropagation();
                    break;

                // Right: 1フレーム進む
                case KeyCode.RightArrow:
                    _player.Seek(_player.CurrentTime + FrameDuration);
                    evt.StopPropagation();
                    break;
            }
        }

        private void DeleteSelection()
        {
            if (_editor.Sequence == null) return;

            foreach (var clipId in _selection.SelectedClipIds)
            {
                foreach (var track in _editor.Sequence.Tracks)
                {
                    if (track.GetClip(clipId) != null)
                    {
                        _editor.RemoveClip(track.Id, clipId);
                        break;
                    }
                }
            }

            _selection.ClearSelection();
        }

        private void SelectAll()
        {
            if (_editor.Sequence == null) return;

            _selection.ClearSelection();
            foreach (var track in _editor.Sequence.Tracks)
            {
                foreach (var clip in track.Clips)
                {
                    _selection.SelectClip(clip.Id, addToSelection: true);
                }
            }
        }
    }
}
