using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    public sealed class ClipElementDragAndDropManipulator : PointerManipulator
    {
        private VisualElement _root;

        private bool _isDragging;
        private Vector3 _pointerStartPosition;
        private Vector2 _targetStartPosition;
        private float _oldClipStartTime;
        private DataType _availableDataType;

        public event Action<ClipMovedEvent> OnClipMoved;

        public bool Enabled { get; set; }
        public float TimelineZoomFactor { get; set; }

        public ClipElementDragAndDropManipulator(UIDocument document, VisualElement target, float zoomFactor = 1.0f)
        {
            if (zoomFactor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(zoomFactor), "Zoom factor must be greater than zero");
            }

            var data = target.userData as ClipElementData;
            if (data == null || data.Clip == null)
            {
                throw new InvalidOperationException("Target does not have clip data");
            }

            this.target = target;
            _root = document.rootVisualElement;

            Enabled = true;
            TimelineZoomFactor = zoomFactor;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(PointerDownHandler);
            target.RegisterCallback<PointerUpEvent>(PointerUpHandler);
            target.RegisterCallback<PointerMoveEvent>(PointerMoveHandler);
            target.RegisterCallback<PointerCaptureOutEvent>(PointerCaptureOutHandler);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(PointerDownHandler);
            target.UnregisterCallback<PointerUpEvent>(PointerUpHandler);
            target.UnregisterCallback<PointerMoveEvent>(PointerMoveHandler);
            target.UnregisterCallback<PointerCaptureOutEvent>(PointerCaptureOutHandler);
        }

        private void PointerDownHandler(PointerDownEvent evt)
        {
            // Dragging starts only when the left mouse button (button = 0) is pressed.
            if (!Enabled || evt.button != 0) return;

            target.CapturePointer(evt.pointerId);
            _isDragging = true;
            
            _targetStartPosition = target.transform.position;
            _pointerStartPosition = evt.position;

            var data = target.userData as ClipElementData;
            _oldClipStartTime = data.Clip.StartTime;
            _availableDataType = data.Clip.Type;
        }

        private void PointerUpHandler(PointerUpEvent evt)
        {
            if ((evt.pressedButtons & 1) == 0) // Left mouse button is not pressed.
            {
                if (_isDragging && target.HasPointerCapture(evt.pointerId))
                {
                    target.ReleasePointer(evt.pointerId);
                }
            }
        }

        private void PointerMoveHandler(PointerMoveEvent evt)
        {
            if ((evt.pressedButtons & 1) == 1) // Left mouse button is pressed.
            {
                if (_isDragging && target.HasPointerCapture(evt.pointerId))
                {
                    var pointerDelta = evt.position - _pointerStartPosition;
                    target.transform.position = new Vector2(
                        Mathf.Clamp(_targetStartPosition.x + pointerDelta.x, 0, target.panel.visualTree.worldBound.width),
                        Mathf.Clamp(_targetStartPosition.y + pointerDelta.y, 0, target.panel.visualTree.worldBound.height));
                }
            }
        }

        private void PointerCaptureOutHandler(PointerCaptureOutEvent evt)
        {
            if (_isDragging)
            {
                if (TryFindClosestTrackRow(out TrackElementData trackElementData, out Vector2 closestTrackRowPosition))
                {
                    var currentClipElementPosition = target.transform.position;
                    target.transform.position = new Vector2(currentClipElementPosition.x, closestTrackRowPosition.y);

                    var timeDelta = (currentClipElementPosition.x - _targetStartPosition.x) / TimelineZoomFactor;
                    var newClipStartTime = Mathf.Max(0, _oldClipStartTime + timeDelta);

                    var data = target.userData as ClipElementData;
                    var oldTrackId = data.TrackId;
                    var newTrackId = trackElementData.Track.Id;

                    // data.Clip.StartTime = newClipStartTime;
                    // data.Clip.TargetId = trackElementData.Track.TargetId;
                    data.TrackId = newTrackId;

                    OnClipMoved?.Invoke(new ClipMovedEvent(data.Clip.Id, newClipStartTime, newTrackId, oldTrackId));
                }
                else
                {
                    Debug.Log($"<color=orange>[ClipElementDragAndDropManipulator] No Closest Track Row Found</color>");
                    target.transform.position = _targetStartPosition;
                }

                _isDragging = false;
            }
        }

        private bool TryFindClosestTrackRow(out TrackElementData trackElementData, out Vector2 closestTrackRowPosition)
        {
            var trackContainer = _root.Q<VisualElement>(UIElementNames.TracksContainerName);
            
            var trackRows = trackContainer.Query<VisualElement>(className: UIElementNames.TrackRowClassName)
                .Where(trackRow =>
                {
                    var data = trackRow.userData as TrackElementData;
                    return data != null && data.Track.Type == _availableDataType;
                });

            var overlappingTrackRows = trackRows
                .Where(trackRow => target.worldBound.Overlaps(trackRow.worldBound))
                .ToList();

            trackElementData = null;
            closestTrackRowPosition = Vector2.zero;

            var bestDistanceSq = float.MaxValue;
            foreach (var trackRow in overlappingTrackRows)
            {
                var clipPosition = target.transform.position;
                var trackRowPosition = trackRow.layout.position;

                var diff = trackRowPosition - new Vector2(clipPosition.x, clipPosition.y);
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
    }
}