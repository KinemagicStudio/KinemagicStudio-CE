using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    // TimelineEditorView?
    public sealed class TimelineTrackEditorView : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private bool _initialized;
        private VisualElement _root;
        private VisualElement _tracksContainer; // TrackRowsContainer
        private VisualElement _leftPanel;
        private ScrollView _trackHeadersScrollView;
        private VisualElement _trackHeadersContainer;

        // イベントの追加: トラックタイプ追加リクエスト
        public event Action<DataType> OnAddTrackRequest;

        // Events for communicating with presenter
        public event Action<IClipData, int, float> OnClipDropped;
        public event Action<int, int, float> OnClipMoved;
        public event Action<int> OnTrackSelected;
        public event Action<int> OnTrackRemoveRequested;
        public event Action<TimelineClip> OnClipEditRequested;
        public event Action<TimelineClip> OnClipSelected;
        public event Action<int, int> OnClipRemoveRequested;

        void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;

            _root = _uiDocument.rootVisualElement;
            _tracksContainer = _root.Q<VisualElement>(UIElementNames.TracksContainerName);

            // 左パネル要素を取得
            _leftPanel = _root.Q<VisualElement>("left-panel");
            _trackHeadersScrollView = _root.Q<ScrollView>("track-headers-scroll-view");
            _trackHeadersContainer = _trackHeadersScrollView?.Q<VisualElement>("track-headers-container");

            // 左パネルにコンテキストメニューを追加
            if (_leftPanel != null)
            {
                _leftPanel.RegisterCallback<ContextualMenuPopulateEvent>(BuildLeftPanelContextMenu);
            }

            // // トラックヘッダーコンテナにもコンテキストメニューを追加
            // if (_trackHeadersContainer != null)
            // {
            //     _trackHeadersContainer.RegisterCallback<ContextualMenuPopulateEvent>(BuildTrackHeadersContextMenu);
            // }
            //
            // // トラックヘッダーのスクロールビューにもコンテキストメニューを追加
            // if (_trackHeadersScrollView != null)
            // {
            //     _trackHeadersScrollView.RegisterCallback<ContextualMenuPopulateEvent>(BuildTrackHeadersContextMenu);
            // }

            _initialized = true;
        }

        // 左パネル全体のコンテキストメニュー
        private void BuildLeftPanelContextMenu(ContextualMenuPopulateEvent evt)
        {
            // 「トラック追加」サブメニュー
            evt.menu.AppendAction("Add Track/Camera Pose", action => OnAddTrackRequest?.Invoke(DataType.CameraPose));
            // evt.menu.AppendAction("Add Track/Camera Properties", action => OnAddTrackRequest?.Invoke(DataType.CameraProperties));
            evt.menu.AppendAction("Add Track/Light Pose", action => OnAddTrackRequest?.Invoke(DataType.LightPose));
            evt.menu.AppendAction("Add Track/Light Properties", action => OnAddTrackRequest?.Invoke(DataType.LightProperties));
        }

        // // トラックヘッダーコンテナのコンテキストメニュー
        // private void BuildTrackHeadersContextMenu(ContextualMenuPopulateEvent evt)
        // {
        //     // 「トラック追加」サブメニュー
        //     evt.menu.AppendAction("Add Track/Camera Track", action => OnAddTrackRequest?.Invoke(DataType.CameraPose));
        //     evt.menu.AppendAction("Add Track/Light Track", action => OnAddTrackRequest?.Invoke(DataType.LightProperties));
        //     //
        //     // // ターゲット要素がトラックヘッダーかどうかを確認
        //     // if (evt.target is VisualElement targetElement)
        //     // {
        //     //     // クリックされた要素から最も近いトラックヘッダーを探す
        //     //     VisualElement trackHeader = targetElement.GetFirstAncestorOfType<VisualElement>(e => 
        //     //         e.ClassListContains("track-header") && e.userData is TrackElementData);
        //     //     
        //     //     // トラックヘッダーが見つかった場合は削除オプションを追加
        //     //     if (trackHeader != null && trackHeader.userData is TrackElementData trackData)
        //     //     {
        //     //         evt.menu.AppendSeparator();
        //     //         evt.menu.AppendAction("Remove Track", action => 
        //     //         {
        //     //             OnTrackRemoveRequested?.Invoke(trackData.Track.Id);
        //     //         });
        //     //     }
        //     // }
        // }

        public void SetTimeline(Timeline timeline)
        {
            // This view doesn't need to store the timeline directly
            // It's managed by the presenter, and this method is called when the timeline changes

            // Potentially update UI elements based on the new timeline data
            // But most of the UI updates are handled by TimelineEditorView
        }

        // TODO: TrackRow -> Track?
        public bool TryFindClosestTrackRow(VisualElement target, DataType availableDataType,
            out TrackElementData trackElementData, out Vector2 closestTrackRowPosition)
        {
            if (_tracksContainer == null)
            {
                Debug.LogError($"[{nameof(TimelineTrackEditorView)}] Tracks container is null.");
                trackElementData = null;
                closestTrackRowPosition = Vector2.zero;
                return false;
            }

            var trackRows = _tracksContainer.Query<VisualElement>(className: UIElementNames.TrackRowClassName)
                .Where(trackRow =>
                {
                    var data = trackRow.userData as TrackElementData;
                    return data != null && data.Track.Type == availableDataType;
                });

            var overlappingTrackRows = trackRows
                .Where(trackRow => target.worldBound.Overlaps(trackRow.worldBound))
                .ToList();

            trackElementData = null;
            closestTrackRowPosition = Vector2.zero;

            var bestDistanceSq = float.MaxValue;
            foreach (var trackRow in overlappingTrackRows)
            {
                var clipPosition = target.worldBound.position;
                var trackRowPosition = trackRow.worldBound.position;

                var diff = trackRowPosition - clipPosition;
                var distanceSq = diff.sqrMagnitude;
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    trackElementData = trackRow.userData as TrackElementData;
                    closestTrackRowPosition = trackRowPosition;
                }
            }

            return bestDistanceSq < float.MaxValue && trackElementData != null;
        }

        // ************************************************************
        // TODO: Code Review

        // Methods that will trigger events instead of directly calling the presenter

        public void NotifyClipDropped(IClipData clipData, int trackId, float startTime)
        {
            OnClipDropped?.Invoke(clipData, trackId, startTime);
        }

        public void NotifyClipMoved(int trackId, int clipId, float newStartTime)
        {
            OnClipMoved?.Invoke(trackId, clipId, newStartTime);
        }

        public void NotifyTrackSelected(int trackId)
        {
            OnTrackSelected?.Invoke(trackId);
        }

        public void NotifyTrackRemoveRequested(int trackId)
        {
            OnTrackRemoveRequested?.Invoke(trackId);
        }

        public void NotifyClipEditRequested(TimelineClip clip)
        {
            OnClipEditRequested?.Invoke(clip);
        }

        public void NotifyClipSelected(TimelineClip clip)
        {
            OnClipSelected?.Invoke(clip);
        }

        public void NotifyClipRemoveRequested(int trackId, int clipId)
        {
            OnClipRemoveRequested?.Invoke(trackId, clipId);
        }

        // ************************************************************
    }
}