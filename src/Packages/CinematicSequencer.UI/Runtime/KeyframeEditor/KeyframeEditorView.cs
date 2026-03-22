using System;
using System.Collections.Generic;
using CinematicSequencer.Animation;
using CinematicSequencer.UI;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI.KeyframeEditor
{
    /// <summary>
    /// キーフレームエディタのメインView。
    /// Phase 3a共通コンポーネント（TimeRuler, Playhead, ScrollSync）を再利用し、
    /// v1 KeyframeEditorView (928行) をMVC分離された形で置き換える。
    /// </summary>
    public sealed class KeyframeEditorView : IDisposable
    {
        private readonly VisualElement _root;
        private readonly ZoomState _zoom;

        // Phase 3a共通コンポーネント
        private readonly TimeRulerElement _timeRuler;
        private readonly PlayheadElement _playhead;
        private readonly ScrollSyncGroup _scrollSync;

        // サブコンポーネント
        private readonly PropertyEditorPanel _propertyPanel;

        // UXMLコンテナ参照
        private readonly VisualElement _editorRoot;
        private readonly VisualElement _timelineArea;
        private readonly ScrollView _propertyScrollView;
        private readonly ScrollView _timelineScrollView;

        // 要素辞書
        private readonly Dictionary<int, KeyframeMarkerElement> _timeMarkers = new();
        private readonly Dictionary<KeyframeId, KeyframeMarkerElement> _propertyMarkers = new();
        private readonly Dictionary<string, VisualElement> _trackRows = new();

        // --- View → Controllerイベント ---
        public event Action<float> OnTimeClicked;
        public event Action<float> OnPlayheadDragged;
        public event Action OnAddKeyframeRequested;
        public event Action<KeyframeId> OnKeyframeClicked;
        public event Action<KeyframeId> OnKeyframeDeleteRequested;
        public event Action<string, float> OnPropertyValueChanged;
        public event Action OnPlayClicked;
        public event Action OnPauseClicked;
        public event Action OnStopClicked;
        public event Action OnCloseRequested;
        public event Action OnSaveRequested;

        public KeyframeEditorView(VisualElement root, ZoomState zoom)
        {
            _root = root;
            _zoom = zoom;

            // ルート要素
            _editorRoot = _root.Q("keyframe-editor-root") ?? _root;

            // UXMLコンテナ取得
            _timelineArea = _editorRoot.Q("timeline-area");
            _propertyScrollView = _editorRoot.Q<ScrollView>("property-scroll-view");
            _timelineScrollView = _editorRoot.Q<ScrollView>("timeline-scroll-view");

            // TimeRuler
            _timeRuler = new TimeRulerElement();
            var timeRulerContainer = _editorRoot.Q("time-ruler");
            timeRulerContainer?.Add(_timeRuler);
            _timeRuler.SetZoom(_zoom.PixelsPerSecond);
            _timeRuler.TimeClicked += time => OnTimeClicked?.Invoke(time);

            // Playhead
            _playhead = new PlayheadElement();
            (_timelineArea ?? _editorRoot).Add(_playhead);
            _playhead.SetZoom(_zoom.PixelsPerSecond);
            _playhead.TimeDragged += time => OnPlayheadDragged?.Invoke(time);

            // PropertyEditorPanel
            _propertyPanel = new PropertyEditorPanel();
            var propertyContainer = _editorRoot.Q("property-editor-container");
            (propertyContainer ?? _editorRoot).Add(_propertyPanel);
            _propertyPanel.ValueChanged += (name, value) =>
                OnPropertyValueChanged?.Invoke(name, value);

            // ScrollSync
            _scrollSync = new ScrollSyncGroup();
            if (_propertyScrollView != null && _timelineScrollView != null)
            {
                _scrollSync.Sync(_propertyScrollView, _timelineScrollView,
                    ScrollSyncGroup.SyncAxis.Vertical);
            }

            // ツールバーボタン
            SetupButton("play-button", () => OnPlayClicked?.Invoke());
            SetupButton("pause-button", () => OnPauseClicked?.Invoke());
            SetupButton("stop-button", () => OnStopClicked?.Invoke());
            SetupButton("add-keyframe-button", () => OnAddKeyframeRequested?.Invoke());
            SetupButton("close-button", () => OnCloseRequested?.Invoke());
            SetupButton("save-button", () => OnSaveRequested?.Invoke());

            // ズーム変更の購読
            _zoom.ZoomChanged += OnZoomChanged;
        }

        // --- 差分更新API（Controllerから呼ばれる） ---

        public void BindClipAsset(IAnimatableClipAsset clipAsset)
        {
            UnbindClipAsset();

            if (clipAsset == null) return;

            _editorRoot.style.display = DisplayStyle.Flex;
            _timeRuler.SetDuration(clipAsset.GetDuration());
            _propertyPanel.SetProperties(clipAsset.Properties);

            // プロパティ行の構築
            var timelineContainer = _timelineScrollView ?? _timelineArea ?? _editorRoot;
            foreach (var desc in clipAsset.Properties)
            {
                var row = new VisualElement();
                row.AddToClassList("keyframe-track-row");
                row.name = $"track-row-{desc.Name}";
                row.style.position = Position.Relative;
                timelineContainer.Add(row);
                _trackRows[desc.Name] = row;
            }

            // 全プロパティのキーフレームマーカー配置
            foreach (var desc in clipAsset.Properties)
            {
                var keyframes = clipAsset.GetKeyframes(desc.Name);
                foreach (var kf in keyframes)
                {
                    AddKeyframeMarkerInternal(desc.Name, kf.Time, kf.Value);
                }
            }
        }

        public void UnbindClipAsset()
        {
            foreach (var kvp in _propertyMarkers)
                kvp.Value.RemoveFromHierarchy();
            _propertyMarkers.Clear();

            foreach (var kvp in _timeMarkers)
                kvp.Value.RemoveFromHierarchy();
            _timeMarkers.Clear();

            foreach (var kvp in _trackRows)
                kvp.Value.RemoveFromHierarchy();
            _trackRows.Clear();

            _editorRoot.style.display = DisplayStyle.None;
        }

        public void AddKeyframeMarker(string propertyName, float time, float value)
        {
            AddKeyframeMarkerInternal(propertyName, time, value);
        }

        public void RemoveKeyframeMarker(KeyframeId id)
        {
            if (_propertyMarkers.TryGetValue(id, out var marker))
            {
                marker.RemoveFromHierarchy();
                _propertyMarkers.Remove(id);
            }
        }

        public void UpdatePropertyValues(AnimationFrame frame, bool editable)
        {
            _propertyPanel.UpdateValues(frame, editable);
        }

        public void UpdatePlayheadTime(float timeSeconds)
        {
            _playhead.SetTime(timeSeconds);
        }

        public void UpdatePlaybackState(bool isPlaying)
        {
            // Play/Pauseボタンの表示切り替え
            var playBtn = _editorRoot.Q("play-button");
            var pauseBtn = _editorRoot.Q("pause-button");
            if (playBtn != null) playBtn.style.display = isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
            if (pauseBtn != null) pauseBtn.style.display = isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetDuration(float duration)
        {
            _timeRuler.SetDuration(duration);
        }

        public void Dispose()
        {
            _zoom.ZoomChanged -= OnZoomChanged;
            UnbindClipAsset();
            _scrollSync.Dispose();
        }

        // --- 内部実装 ---

        private void AddKeyframeMarkerInternal(string propertyName, float time, float value)
        {
            var id = KeyframeId.FromSeconds(propertyName, time);
            if (_propertyMarkers.ContainsKey(id)) return;

            float pixelX = _zoom.TimeToPixels(time);
            var marker = new KeyframeMarkerElement(id, pixelX);
            marker.OnClicked += kfId => OnKeyframeClicked?.Invoke(kfId);
            marker.OnDeleteRequested += kfId => OnKeyframeDeleteRequested?.Invoke(kfId);

            _propertyMarkers[id] = marker;

            if (_trackRows.TryGetValue(propertyName, out var row))
            {
                row.Add(marker);
            }
        }

        private void SetupButton(string buttonName, Action callback)
        {
            var button = _editorRoot.Q<Button>(buttonName);
            button?.RegisterCallback<ClickEvent>(_ => callback());
        }

        private void OnZoomChanged(float pps)
        {
            _timeRuler.SetZoom(pps);
            _playhead.SetZoom(pps);

            // 全マーカーの位置を更新
            foreach (var kvp in _propertyMarkers)
            {
                float pixelX = _zoom.TimeToPixels(kvp.Key.Time);
                kvp.Value.SetPosition(pixelX);
            }
        }
    }
}
