using System;
using System.Collections.Generic;
using System.Threading;
using CinematicSequencer.Editing;
using CinematicSequencer.IO;
using CinematicSequencer.Playback;
using Cysharp.Threading.Tasks;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// シーケンスエディタのController。v1 TimelinePresenter の置き換え。
    /// Model→View差分更新とView→Commandパターンを担う。
    /// </summary>
    public sealed class SequenceEditorController : IDisposable
    {
        // DI
        private readonly SequenceEditor _editor;
        private readonly SequencePlayer _player;
        private readonly IClipAssetRepository _clipAssetRepo;
        private readonly ISequenceRepository _sequenceRepo;

        // UI
        private readonly SequenceEditorView _view;
        private readonly SelectionState _selection;
        private readonly ZoomState _zoom;
        private readonly SnappingService _snapping;
        private readonly KeyboardShortcutHandler _shortcuts;

        private CancellationTokenSource _cts;
        private bool _disposed;

        public SequenceEditorController(
            SequenceEditor editor,
            SequencePlayer player,
            IClipAssetRepository clipAssetRepo,
            ISequenceRepository sequenceRepo,
            SequenceEditorView view,
            SelectionState selection,
            ZoomState zoom,
            SnappingService snapping,
            KeyboardShortcutHandler shortcuts)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _clipAssetRepo = clipAssetRepo ?? throw new ArgumentNullException(nameof(clipAssetRepo));
            _sequenceRepo = sequenceRepo ?? throw new ArgumentNullException(nameof(sequenceRepo));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _zoom = zoom ?? throw new ArgumentNullException(nameof(zoom));
            _snapping = snapping ?? throw new ArgumentNullException(nameof(snapping));
            _shortcuts = shortcuts ?? throw new ArgumentNullException(nameof(shortcuts));

            _cts = new CancellationTokenSource();

            SubscribeViewEvents();
            SubscribeModelEvents();
        }

        // --- 非同期操作 ---

        public async UniTask LoadSequenceAsync(Guid sequenceId)
        {
            var ct = ResetCts();
            var sequence = await _sequenceRepo.LoadSequenceAsync(sequenceId, ct);
            if (sequence == null) return;

            _editor.SetSequence(sequence);
            _player.Sequence = sequence;
            _view.BindSequence(sequence);
        }

        public async UniTask SaveAsync()
        {
            if (_editor.Sequence == null || !_editor.HasUnsavedChanges) return;

            var ct = ResetCts();
            await _sequenceRepo.SaveSequenceAsync(_editor.Sequence, ct);
            _editor.MarkSaved();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            UnsubscribeViewEvents();
            UnsubscribeModelEvents();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _view.Dispose();
        }

        // --- View → Command パターン ---

        private void HandleClipMoveCompleted(Guid clipId, Guid oldTrackId, Guid newTrackId, float rawNewStartTime)
        {
            var sequence = _editor.Sequence;
            if (sequence == null) return;

            // スナッピング適用
            float snappedTime = _snapping.Snap(
                rawNewStartTime, sequence, clipId, _zoom.PixelsPerSecond);

            // 元のクリップの配置を取得
            var oldTrack = sequence.GetTrack(oldTrackId);
            var clip = oldTrack?.GetClip(clipId);
            if (clip == null) return;

            var oldPlacement = clip.Placement;
            var newPlacement = new TimeRange(snappedTime, oldPlacement.Duration);

            _editor.MoveClip(clipId, oldTrackId, newTrackId, oldPlacement, newPlacement);
        }

        private void HandleClipResizeCompleted(Guid clipId, Guid trackId, float rawNewDuration)
        {
            var sequence = _editor.Sequence;
            if (sequence == null) return;

            var track = sequence.GetTrack(trackId);
            var clip = track?.GetClip(clipId);
            if (clip == null) return;

            // リサイズ後のend位置をスナッピング
            float rawEndTime = clip.Placement.Start + rawNewDuration;
            float snappedEnd = _snapping.Snap(
                rawEndTime, sequence, clipId, _zoom.PixelsPerSecond);
            float snappedDuration = snappedEnd - clip.Placement.Start;

            var oldPlacement = clip.Placement;
            var newPlacement = oldPlacement.WithDuration(snappedDuration);

            _editor.ResizeClip(trackId, clipId, oldPlacement, newPlacement);
        }

        private void HandleClipSelected(Guid clipId)
        {
            _selection.SelectClip(clipId);
        }

        private void HandleClipDoubleClicked(Guid clipId)
        {
            // ダブルクリック時の動作は将来拡張用（キーフレームエディタ表示等）
        }

        private void HandleTrackDeleteRequested(Guid trackId)
        {
            _editor.RemoveTrack(trackId);
        }

        private void HandlePlayClicked()
        {
            _player.Play();
        }

        private void HandlePauseClicked()
        {
            _player.Pause();
        }

        private void HandleStopClicked()
        {
            _player.Stop();
        }

        private void HandleTimeRulerClicked(float time)
        {
            _player.Seek(time);
        }

        private void HandlePlayheadDragged(float time)
        {
            _player.Seek(time);
        }

        private void HandleSaveRequested()
        {
            SaveAsync().Forget();
        }

        // --- Model → View 差分更新 ---

        private void OnSequenceChanged(ModelChangeEvent evt)
        {
            var sequence = _editor.Sequence;
            if (sequence == null) return;

            switch (evt.Type)
            {
                case ModelChangeEvent.ChangeType.ChildAdded when evt.PropertyName == "Tracks":
                    SyncAddedTracks(sequence);
                    break;

                case ModelChangeEvent.ChangeType.ChildRemoved when evt.PropertyName == "Tracks":
                    SyncRemovedTracks(sequence);
                    break;

                case ModelChangeEvent.ChangeType.ChildModified when evt.PropertyName == "Tracks":
                    SyncModifiedTracks(sequence);
                    break;

                case ModelChangeEvent.ChangeType.PropertyChanged:
                    // Sequence自身のプロパティ変更（Duration再計算等）
                    break;
            }
        }

        /// <summary>
        /// Viewに無いTrackIdを見つけてAddTrackUI。
        /// ModelChangeEventのChildAddedはSourceがSequence自身で、
        /// 追加されたオブジェクトは含まれないため、差分比較で特定する。
        /// </summary>
        private void SyncAddedTracks(Sequence sequence)
        {
            foreach (var track in sequence.Tracks)
            {
                if (!_view.HasTrack(track.Id))
                {
                    _view.AddTrackUI(track);
                }
            }
        }

        /// <summary>
        /// Modelに無いTrackIdを見つけてRemoveTrackUI。
        /// </summary>
        private void SyncRemovedTracks(Sequence sequence)
        {
            var modelTrackIds = new HashSet<Guid>();
            foreach (var track in sequence.Tracks)
            {
                modelTrackIds.Add(track.Id);
            }

            var viewTrackIds = _view.GetTrackIds();
            foreach (var trackId in viewTrackIds)
            {
                if (!modelTrackIds.Contains(trackId))
                {
                    _view.RemoveTrackUI(trackId);
                }
            }
        }

        /// <summary>
        /// Track内のClip変更を差分同期。
        /// </summary>
        private void SyncModifiedTracks(Sequence sequence)
        {
            foreach (var track in sequence.Tracks)
            {
                SyncClipsForTrack(track);
            }
        }

        private void SyncClipsForTrack(Track track)
        {
            var modelClipIds = new HashSet<Guid>();
            foreach (var clip in track.Clips)
            {
                modelClipIds.Add(clip.Id);
            }

            // Viewにあるがモデルにないクリップを削除
            var viewClipIds = _view.GetClipIdsForTrack(track.Id);
            foreach (var clipId in viewClipIds)
            {
                if (!modelClipIds.Contains(clipId))
                {
                    _view.RemoveClipUI(clipId);
                }
            }

            // モデルにあるがViewにないクリップを追加、既存は更新
            foreach (var clip in track.Clips)
            {
                if (!_view.HasClip(clip.Id))
                {
                    _view.AddClipUI(clip, track);
                }
                else
                {
                    _view.UpdateClipUI(clip);
                }
            }
        }

        // --- Player Events ---

        private void OnPlayerTimeChanged(float time)
        {
            _view.UpdatePlayheadTime(time);
        }

        private void OnPlayerPlay()
        {
            _view.UpdatePlaybackState(true);
            _view.SetManipulatorsEnabled(false);
        }

        private void OnPlayerPause()
        {
            _view.UpdatePlaybackState(false);
            _view.SetManipulatorsEnabled(true);
        }

        private void OnPlayerStop()
        {
            _view.UpdatePlaybackState(false);
            _view.UpdatePlayheadTime(0f);
            _view.SetManipulatorsEnabled(true);
        }

        private void OnSelectionChanged()
        {
            _view.UpdateSelectionHighlight(_selection.SelectedClipIds);
        }

        // --- イベント購読管理 ---

        private void SubscribeViewEvents()
        {
            _view.OnClipMoveCompleted += HandleClipMoveCompleted;
            _view.OnClipResizeCompleted += HandleClipResizeCompleted;
            _view.OnClipSelected += HandleClipSelected;
            _view.OnClipDoubleClicked += HandleClipDoubleClicked;
            _view.OnTrackDeleteRequested += HandleTrackDeleteRequested;
            _view.OnPlayClicked += HandlePlayClicked;
            _view.OnPauseClicked += HandlePauseClicked;
            _view.OnStopClicked += HandleStopClicked;
            _view.OnTimeRulerClicked += HandleTimeRulerClicked;
            _view.OnPlayheadDragged += HandlePlayheadDragged;
        }

        private void UnsubscribeViewEvents()
        {
            _view.OnClipMoveCompleted -= HandleClipMoveCompleted;
            _view.OnClipResizeCompleted -= HandleClipResizeCompleted;
            _view.OnClipSelected -= HandleClipSelected;
            _view.OnClipDoubleClicked -= HandleClipDoubleClicked;
            _view.OnTrackDeleteRequested -= HandleTrackDeleteRequested;
            _view.OnPlayClicked -= HandlePlayClicked;
            _view.OnPauseClicked -= HandlePauseClicked;
            _view.OnStopClicked -= HandleStopClicked;
            _view.OnTimeRulerClicked -= HandleTimeRulerClicked;
            _view.OnPlayheadDragged -= HandlePlayheadDragged;
        }

        private void SubscribeModelEvents()
        {
            _editor.SequenceChanged += OnSequenceChanged;
            _player.OnTimeChanged += OnPlayerTimeChanged;
            _player.OnPlay += OnPlayerPlay;
            _player.OnPause += OnPlayerPause;
            _player.OnStop += OnPlayerStop;
            _selection.SelectionChanged += OnSelectionChanged;
            _shortcuts.SaveRequested += HandleSaveRequested;
        }

        private void UnsubscribeModelEvents()
        {
            _editor.SequenceChanged -= OnSequenceChanged;
            _player.OnTimeChanged -= OnPlayerTimeChanged;
            _player.OnPlay -= OnPlayerPlay;
            _player.OnPause -= OnPlayerPause;
            _player.OnStop -= OnPlayerStop;
            _selection.SelectionChanged -= OnSelectionChanged;
            _shortcuts.SaveRequested -= HandleSaveRequested;
        }

        private CancellationToken ResetCts()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }
    }
}
