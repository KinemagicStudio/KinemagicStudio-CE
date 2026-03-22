using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// シーケンスエディタのメインView。
    /// Phase 3a共通コンポーネントとTier 0/1コンポーネントを統合する。
    /// MonoBehaviourではない（app層の責務）。
    /// </summary>
    public sealed class SequenceEditorView : IDisposable
    {
        private readonly VisualElement _root;
        private readonly ZoomState _zoom;
        private readonly SnappingService _snapping;

        // Phase 3a共通コンポーネント
        private readonly TimeRulerElement _timeRuler;
        private readonly PlayheadElement _playhead;
        private readonly PlaybackToolbar _toolbar;
        private readonly ScrollSyncGroup _scrollSync;

        // UXMLコンテナ参照
        private readonly VisualElement _trackHeadersContainer;
        private readonly VisualElement _tracksContainer;
        private readonly ScrollView _trackHeadersScrollView;
        private readonly ScrollView _tracksScrollView;

        // 要素辞書
        private readonly Dictionary<Guid, TrackHeaderView> _trackHeaders = new();
        private readonly Dictionary<Guid, TrackContentView> _trackContents = new();
        private readonly Dictionary<Guid, ClipElement> _clipElements = new();
        private readonly Dictionary<Guid, ClipManipulator> _clipManipulators = new();

        private bool _manipulatorsEnabled = true;

        // --- View → Controllerイベント ---

        /// <summary>クリップ移動完了。(clipId, oldTrackId, newTrackId, rawNewStartTime)</summary>
        public event Action<Guid, Guid, Guid, float> OnClipMoveCompleted;

        /// <summary>クリップリサイズ完了。(clipId, trackId, rawNewDuration)</summary>
        public event Action<Guid, Guid, float> OnClipResizeCompleted;

        /// <summary>クリップ選択。(clipId)</summary>
        public event Action<Guid> OnClipSelected;

        /// <summary>クリップダブルクリック。(clipId)</summary>
        public event Action<Guid> OnClipDoubleClicked;

        /// <summary>トラック削除リクエスト。(trackId)</summary>
        public event Action<Guid> OnTrackDeleteRequested;

        public event Action OnPlayClicked;
        public event Action OnPauseClicked;
        public event Action OnStopClicked;

        /// <summary>タイムルーラークリック。(timeSeconds)</summary>
        public event Action<float> OnTimeRulerClicked;

        /// <summary>プレイヘッドドラッグ。(timeSeconds)</summary>
        public event Action<float> OnPlayheadDragged;

        public SequenceEditorView(VisualElement root, ZoomState zoom, SnappingService snapping)
        {
            _root = root;
            _zoom = zoom;
            _snapping = snapping;

            // UXMLコンテナ取得（UIElementNamesの既存定数を利用）
            _trackHeadersContainer = _root.Q(UIElementNames.TrackHeadersContainerName);
            _tracksContainer = _root.Q(UIElementNames.TracksContainerName);
            _trackHeadersScrollView = _root.Q<ScrollView>(UIElementNames.TrackHeadersScrollViewName);
            _tracksScrollView = _root.Q<ScrollView>(UIElementNames.TracksScrollViewName);

            // Clear UXML preview content before adding dynamic elements
            ClearPreviewContent(_root);

            // TimeRuler
            _timeRuler = new TimeRulerElement();
            var timeRulerContainer = _root.Q("time-ruler");
            timeRulerContainer?.Add(_timeRuler);
            _timeRuler.SetZoom(_zoom.PixelsPerSecond);
            _timeRuler.TimeClicked += time => OnTimeRulerClicked?.Invoke(time);

            // Playhead
            _playhead = new PlayheadElement();
            _tracksContainer?.Add(_playhead);
            _playhead.SetZoom(_zoom.PixelsPerSecond);
            _playhead.TimeDragged += time => OnPlayheadDragged?.Invoke(time);

            // PlaybackToolbar
            _toolbar = new PlaybackToolbar();
            var toolbarContainer = _root.Q("playback-toolbar-container");
            toolbarContainer?.Clear();
            if (toolbarContainer != null)
                toolbarContainer.Add(_toolbar);
            else
                _root.Insert(0, _toolbar);

            _toolbar.OnPlayClicked += () => OnPlayClicked?.Invoke();
            _toolbar.OnPauseClicked += () => OnPauseClicked?.Invoke();
            _toolbar.OnStopClicked += () => OnStopClicked?.Invoke();

            // ScrollSync
            _scrollSync = new ScrollSyncGroup();
            if (_trackHeadersScrollView != null && _tracksScrollView != null)
            {
                _scrollSync.Sync(_trackHeadersScrollView, _tracksScrollView,
                    ScrollSyncGroup.SyncAxis.Vertical);
            }

            // ズーム変更の購読
            _zoom.ZoomChanged += OnZoomChanged;
        }

        // --- 差分更新API（Controllerから呼ばれる） ---

        /// <summary>
        /// 初回読み込み・切り替え時の全構築。
        /// </summary>
        public void BindSequence(Sequence sequence)
        {
            ClearAllUI();

            if (sequence == null) return;

            _timeRuler.SetDuration(sequence.Duration.Duration);

            foreach (var track in sequence.Tracks)
            {
                AddTrackUIInternal(track);
                foreach (var clip in track.Clips)
                {
                    AddClipUIInternal(clip, track);
                }
            }
        }

        public void AddTrackUI(Track track)
        {
            AddTrackUIInternal(track);
        }

        public void RemoveTrackUI(Guid trackId)
        {
            if (_trackHeaders.TryGetValue(trackId, out var header))
            {
                header.RemoveFromHierarchy();
                _trackHeaders.Remove(trackId);
            }

            if (_trackContents.TryGetValue(trackId, out var content))
            {
                content.RemoveFromHierarchy();
                _trackContents.Remove(trackId);
            }

            // トラックに属するクリップもすべて除去
            var clipsToRemove = new List<Guid>();
            foreach (var kvp in _clipElements)
            {
                if (kvp.Value.TrackId == trackId)
                    clipsToRemove.Add(kvp.Key);
            }
            foreach (var clipId in clipsToRemove)
            {
                RemoveClipUIInternal(clipId);
            }
        }

        public void AddClipUI(Clip clip, Track track)
        {
            AddClipUIInternal(clip, track);
        }

        public void UpdateClipUI(Clip clip)
        {
            if (_clipElements.TryGetValue(clip.Id, out var element))
            {
                element.UpdateFromModel(clip);
            }
        }

        public void RemoveClipUI(Guid clipId)
        {
            RemoveClipUIInternal(clipId);
        }

        public void UpdatePlayheadTime(float timeSeconds)
        {
            _playhead.SetTime(timeSeconds);
            _toolbar.UpdateTime(timeSeconds);
        }

        public void UpdatePlaybackState(bool isPlaying)
        {
            _toolbar.UpdatePlaybackState(isPlaying);
        }

        public void UpdateZoom(float pixelsPerSecond)
        {
            _zoom.SetZoom(pixelsPerSecond);
        }

        public void UpdateSelectionHighlight(IReadOnlyList<Guid> selectedClipIds)
        {
            // まず全てのselectedクラスを外す
            foreach (var kvp in _clipElements)
            {
                kvp.Value.SetSelected(false);
            }
            // 選択中のクリップにselectedクラスを付ける
            foreach (var clipId in selectedClipIds)
            {
                if (_clipElements.TryGetValue(clipId, out var element))
                {
                    element.SetSelected(true);
                }
            }
        }

        public void SetManipulatorsEnabled(bool enabled)
        {
            _manipulatorsEnabled = enabled;
            foreach (var kvp in _clipManipulators)
            {
                kvp.Value.Enabled = enabled;
            }
        }

        // --- 差分同期用クエリ（Controllerから使用） ---

        public bool HasTrack(Guid trackId) => _trackHeaders.ContainsKey(trackId);
        public bool HasClip(Guid clipId) => _clipElements.ContainsKey(clipId);

        public IReadOnlyCollection<Guid> GetTrackIds() => _trackHeaders.Keys;

        public List<Guid> GetClipIdsForTrack(Guid trackId)
        {
            var result = new List<Guid>();
            foreach (var kvp in _clipElements)
            {
                if (kvp.Value.TrackId == trackId)
                    result.Add(kvp.Key);
            }
            return result;
        }

        public void Dispose()
        {
            _zoom.ZoomChanged -= OnZoomChanged;
            ClearAllUI();
            _scrollSync.Dispose();
        }

        // --- 内部実装 ---

        private void AddTrackUIInternal(Track track)
        {
            // TrackHeaderView
            var header = new TrackHeaderView(track);
            header.OnDeleteRequested += id => OnTrackDeleteRequested?.Invoke(id);
            _trackHeaders[track.Id] = header;
            _trackHeadersContainer?.Add(header);

            // TrackContentView
            var content = new TrackContentView(track);
            _trackContents[track.Id] = content;
            _tracksContainer?.Add(content);
        }

        private void AddClipUIInternal(Clip clip, Track track)
        {
            if (_clipElements.ContainsKey(clip.Id)) return;

            var element = new ClipElement(clip, track, _zoom);
            element.OnClicked += id => OnClipSelected?.Invoke(id);
            element.OnDoubleClicked += id => OnClipDoubleClicked?.Invoke(id);

            // ClipManipulator
            var manipulator = new ClipManipulator(_snapping, _zoom, FindTrackAtPosition);
            manipulator.Enabled = _manipulatorsEnabled;
            manipulator.MoveCompleted += (clipId, oldTrackId, newTrackId, rawStartTime) =>
                OnClipMoveCompleted?.Invoke(clipId, oldTrackId, newTrackId, rawStartTime);
            manipulator.ResizeCompleted += (clipId, trackId, rawDuration) =>
                OnClipResizeCompleted?.Invoke(clipId, trackId, rawDuration);
            element.AttachManipulator(manipulator);

            _clipElements[clip.Id] = element;
            _clipManipulators[clip.Id] = manipulator;

            // トラックのContentViewに追加
            if (_trackContents.TryGetValue(track.Id, out var content))
            {
                content.Add(element);
            }
        }

        private void RemoveClipUIInternal(Guid clipId)
        {
            if (_clipElements.TryGetValue(clipId, out var element))
            {
                element.RemoveFromHierarchy();
                _clipElements.Remove(clipId);
            }
            _clipManipulators.Remove(clipId);
        }

        /// <summary>
        /// Remove UXML preview-content elements so they don't duplicate runtime UI.
        /// </summary>
        private static void ClearPreviewContent(VisualElement root)
        {
            var previews = root.Query(className: "preview-content").ToList();
            foreach (var el in previews)
                el.RemoveFromHierarchy();
        }

        private void ClearAllUI()
        {
            foreach (var kvp in _clipElements)
                kvp.Value.RemoveFromHierarchy();
            _clipElements.Clear();
            _clipManipulators.Clear();

            foreach (var kvp in _trackHeaders)
                kvp.Value.RemoveFromHierarchy();
            _trackHeaders.Clear();

            foreach (var kvp in _trackContents)
                kvp.Value.RemoveFromHierarchy();
            _trackContents.Clear();
        }

        private void OnZoomChanged(float pps)
        {
            _timeRuler.SetZoom(pps);
            _playhead.SetZoom(pps);

            foreach (var kvp in _clipElements)
            {
                kvp.Value.UpdateZoom();
            }
        }

        /// <summary>
        /// ドラッグ中のクリップの位置から、最も近い互換トラックを検索する。
        /// ClipManipulatorのtrackFinderデリゲートとして使用。
        /// </summary>
        private (Guid trackId, float trackY)? FindTrackAtPosition(
            VisualElement draggedElement, TrackType trackType)
        {
            Guid bestTrackId = default;
            float bestTrackY = 0f;
            float bestDistance = float.MaxValue;

            foreach (var kvp in _trackContents)
            {
                var content = kvp.Value;
                if (content.TrackType != trackType) continue;

                // ドラッグ中要素と各トラック行のworldBound重なりを確認
                if (!draggedElement.worldBound.Overlaps(content.worldBound)) continue;

                var dragCenter = draggedElement.worldBound.center;
                var trackCenter = content.worldBound.center;
                float dist = Mathf.Abs(dragCenter.y - trackCenter.y);

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestTrackId = kvp.Key;
                    bestTrackY = content.worldBound.y;
                }
            }

            return bestDistance < float.MaxValue
                ? (bestTrackId, bestTrackY)
                : null;
        }
    }
}
