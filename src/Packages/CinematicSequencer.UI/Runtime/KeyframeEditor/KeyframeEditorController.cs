using System;
using System.Threading;
using CinematicSequencer.Animation;
using CinematicSequencer.Editing;
using CinematicSequencer.Editing.Commands;
using CinematicSequencer.IO;
using CinematicSequencer.Playback;
using CinematicSequencer.UI;
using Cysharp.Threading.Tasks;

namespace CinematicSequencer.UI.KeyframeEditor
{
    /// <summary>
    /// キーフレームエディタのController。v1 KeyframeAnimationEditorPresenter の置き換え。
    /// v1の「一時Sequence」設計を廃止し、SequenceEditor/SequencePlayerをそのまま使用する。
    /// キーフレーム操作はコマンド経由でUndo/Redo対応。
    /// </summary>
    public sealed class KeyframeEditorController : IDisposable
    {
        // DI
        private readonly SequenceEditor _editor;
        private readonly SequencePlayer _player;
        private readonly IClipAssetRepository _clipAssetRepo;

        // UI
        private readonly KeyframeEditorView _view;
        private readonly SelectionState _selection;
        private readonly ZoomState _zoom;

        // State
        private Guid _currentClipId;
        private Guid _currentTrackId;
        private IAnimatableClipAsset _currentClipAsset;

        private CancellationTokenSource _cts;
        private bool _disposed;

        public KeyframeEditorController(
            SequenceEditor editor,
            SequencePlayer player,
            IClipAssetRepository clipAssetRepo,
            KeyframeEditorView view,
            SelectionState selection,
            ZoomState zoom)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _clipAssetRepo = clipAssetRepo ?? throw new ArgumentNullException(nameof(clipAssetRepo));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _zoom = zoom ?? throw new ArgumentNullException(nameof(zoom));

            _cts = new CancellationTokenSource();

            SubscribeViewEvents();
            SubscribeModelEvents();
        }

        // --- 公開API ---

        /// <summary>
        /// IAnimatableClipAssetを非同期ロードしてViewに表示する。
        /// IExternalClipAssetの場合は何もしない。
        /// </summary>
        public async UniTask OpenClipAsset(Guid clipId, Guid trackId)
        {
            var ct = ResetCts();

            var clipAsset = await _clipAssetRepo.LoadClipAssetAsync(clipId, ct);
            if (clipAsset is not IAnimatableClipAsset animatable)
                return;

            _currentClipId = clipId;
            _currentTrackId = trackId;
            _currentClipAsset = animatable;

            _view.BindClipAsset(animatable);

            // 現在時刻の値を表示
            var frame = animatable.Evaluate(_player.CurrentTime);
            _view.UpdatePropertyValues(frame, editable: !_player.IsPlaying);
            _view.UpdatePlayheadTime(_player.CurrentTime);
            _view.UpdatePlaybackState(_player.IsPlaying);
        }

        public void Close()
        {
            _currentClipAsset = null;
            _currentClipId = Guid.Empty;
            _currentTrackId = Guid.Empty;
            _view.UnbindClipAsset();
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

        private void HandleAddKeyframe()
        {
            if (_currentClipAsset == null) return;

            float time = _player.CurrentTime;
            var frame = _currentClipAsset.Evaluate(time);
            var properties = _currentClipAsset.Properties;

            // 全プロパティにキーフレーム追加をCompositeCommandで1 Undo単位にまとめる
            var commands = new IEditCommand[properties.Count];
            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var (_, value) = frame.GetProperty(i);
                var keyframe = new Keyframe(time, value);
                commands[i] = new AddKeyframeCommand(_currentClipAsset, prop.Name, keyframe);
            }

            var composite = new CompositeCommand("Add keyframes (all properties)", commands);
            _editor.ExecuteCommand(composite);

            // Viewにマーカーを追加
            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var (_, value) = frame.GetProperty(i);
                _view.AddKeyframeMarker(prop.Name, time, value);
            }
        }

        private void HandleKeyframeDeleteRequested(KeyframeId id)
        {
            if (_currentClipAsset == null) return;

            var keyframes = _currentClipAsset.GetKeyframes(id.PropertyName);
            Keyframe? target = null;
            foreach (var kf in keyframes)
            {
                if (KeyframeId.FromSeconds(id.PropertyName, kf.Time).Equals(id))
                {
                    target = kf;
                    break;
                }
            }

            if (target == null) return;

            _editor.RemoveKeyframe(_currentClipAsset, id.PropertyName, target.Value);
            _view.RemoveKeyframeMarker(id);
        }

        private void HandlePropertyValueChanged(string propertyName, float newValue)
        {
            if (_currentClipAsset == null) return;

            float time = _player.CurrentTime;

            // 現在のキーフレーム値を取得
            var keyframes = _currentClipAsset.GetKeyframes(propertyName);
            float? oldValue = null;
            foreach (var kf in keyframes)
            {
                if (Math.Abs(kf.Time - time) < 0.001f)
                {
                    oldValue = kf.Value;
                    break;
                }
            }

            if (oldValue.HasValue)
            {
                _editor.UpdateKeyframeValue(_currentClipAsset, propertyName, time,
                    oldValue.Value, newValue);
            }
        }

        private void HandleTimeClicked(float time)
        {
            _player.Seek(time);
            UpdateCurrentFrame();
        }

        private void HandlePlayheadDragged(float time)
        {
            _player.Seek(time);
            UpdateCurrentFrame();
        }

        private void HandleKeyframeClicked(KeyframeId id)
        {
            _selection.SelectKeyframe(id);
        }

        private void HandlePlayClicked() => _player.Play();
        private void HandlePauseClicked() => _player.Pause();
        private void HandleStopClicked() => _player.Stop();

        private void HandleSaveRequested()
        {
            SaveAsync().Forget();
        }

        private async UniTaskVoid SaveAsync()
        {
            if (_currentClipAsset == null) return;
            var ct = ResetCts();
            await _clipAssetRepo.SaveClipAssetAsync(_currentClipAsset, ct);
            _editor.MarkSaved();
        }

        private void HandleCloseRequested()
        {
            // 未保存チェック → Close
            if (_editor.HasUnsavedChanges)
            {
                // 将来: 確認ダイアログ表示
                // 現時点ではそのままCloseする
            }
            Close();
        }

        // --- Model → View 更新 ---

        private void OnPlayerTimeChanged(float time)
        {
            _view.UpdatePlayheadTime(time);
            UpdateCurrentFrame();
        }

        private void OnPlayerPlay()
        {
            _view.UpdatePlaybackState(true);
        }

        private void OnPlayerPause()
        {
            _view.UpdatePlaybackState(false);
            UpdateCurrentFrame();
        }

        private void OnPlayerStop()
        {
            _view.UpdatePlaybackState(false);
            _view.UpdatePlayheadTime(0f);
            UpdateCurrentFrame();
        }

        private void UpdateCurrentFrame()
        {
            if (_currentClipAsset == null) return;
            var frame = _currentClipAsset.Evaluate(_player.CurrentTime);
            _view.UpdatePropertyValues(frame, editable: !_player.IsPlaying);
        }

        // --- イベント購読管理 ---

        private void SubscribeViewEvents()
        {
            _view.OnTimeClicked += HandleTimeClicked;
            _view.OnPlayheadDragged += HandlePlayheadDragged;
            _view.OnAddKeyframeRequested += HandleAddKeyframe;
            _view.OnKeyframeClicked += HandleKeyframeClicked;
            _view.OnKeyframeDeleteRequested += HandleKeyframeDeleteRequested;
            _view.OnPropertyValueChanged += HandlePropertyValueChanged;
            _view.OnPlayClicked += HandlePlayClicked;
            _view.OnPauseClicked += HandlePauseClicked;
            _view.OnStopClicked += HandleStopClicked;
            _view.OnCloseRequested += HandleCloseRequested;
            _view.OnSaveRequested += HandleSaveRequested;
        }

        private void UnsubscribeViewEvents()
        {
            _view.OnTimeClicked -= HandleTimeClicked;
            _view.OnPlayheadDragged -= HandlePlayheadDragged;
            _view.OnAddKeyframeRequested -= HandleAddKeyframe;
            _view.OnKeyframeClicked -= HandleKeyframeClicked;
            _view.OnKeyframeDeleteRequested -= HandleKeyframeDeleteRequested;
            _view.OnPropertyValueChanged -= HandlePropertyValueChanged;
            _view.OnPlayClicked -= HandlePlayClicked;
            _view.OnPauseClicked -= HandlePauseClicked;
            _view.OnStopClicked -= HandleStopClicked;
            _view.OnCloseRequested -= HandleCloseRequested;
            _view.OnSaveRequested -= HandleSaveRequested;
        }

        private void SubscribeModelEvents()
        {
            _player.OnTimeChanged += OnPlayerTimeChanged;
            _player.OnPlay += OnPlayerPlay;
            _player.OnPause += OnPlayerPause;
            _player.OnStop += OnPlayerStop;
        }

        private void UnsubscribeModelEvents()
        {
            _player.OnTimeChanged -= OnPlayerTimeChanged;
            _player.OnPlay -= OnPlayerPlay;
            _player.OnPause -= OnPlayerPause;
            _player.OnStop -= OnPlayerStop;
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
