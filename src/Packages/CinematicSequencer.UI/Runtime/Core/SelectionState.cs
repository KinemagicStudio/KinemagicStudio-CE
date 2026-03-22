using System;
using System.Collections.Generic;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// エディタ全体の選択状態を一元管理。
    /// トラック・クリップ・キーフレームの選択を集約し、整合性を担保する。
    /// </summary>
    public sealed class SelectionState
    {
        private readonly HashSet<Guid> _selectedTrackIds = new();
        private readonly HashSet<Guid> _selectedClipIds = new();
        private readonly HashSet<KeyframeId> _selectedKeyframeIds = new();

        private readonly List<Guid> _trackIdsList = new();
        private readonly List<Guid> _clipIdsList = new();
        private readonly List<KeyframeId> _keyframeIdsList = new();

        public IReadOnlyList<Guid> SelectedTrackIds => _trackIdsList;
        public IReadOnlyList<Guid> SelectedClipIds => _clipIdsList;
        public IReadOnlyList<KeyframeId> SelectedKeyframeIds => _keyframeIdsList;

        public event Action SelectionChanged;

        public void SelectTrack(Guid trackId, bool addToSelection = false)
        {
            if (!addToSelection) ClearInternal();
            if (_selectedTrackIds.Add(trackId))
            {
                _trackIdsList.Add(trackId);
                SelectionChanged?.Invoke();
            }
        }

        public void SelectClip(Guid clipId, bool addToSelection = false)
        {
            if (!addToSelection) ClearInternal();
            if (_selectedClipIds.Add(clipId))
            {
                _clipIdsList.Add(clipId);
                SelectionChanged?.Invoke();
            }
        }

        public void SelectKeyframe(KeyframeId id, bool addToSelection = false)
        {
            if (!addToSelection) ClearInternal();
            if (_selectedKeyframeIds.Add(id))
            {
                _keyframeIdsList.Add(id);
                SelectionChanged?.Invoke();
            }
        }

        public void ClearSelection()
        {
            if (_selectedTrackIds.Count == 0 && _selectedClipIds.Count == 0 && _selectedKeyframeIds.Count == 0)
                return;

            ClearInternal();
            SelectionChanged?.Invoke();
        }

        public bool IsTrackSelected(Guid trackId) => _selectedTrackIds.Contains(trackId);
        public bool IsClipSelected(Guid clipId) => _selectedClipIds.Contains(clipId);
        public bool IsKeyframeSelected(KeyframeId id) => _selectedKeyframeIds.Contains(id);

        private void ClearInternal()
        {
            _selectedTrackIds.Clear();
            _selectedClipIds.Clear();
            _selectedKeyframeIds.Clear();
            _trackIdsList.Clear();
            _clipIdsList.Clear();
            _keyframeIdsList.Clear();
        }
    }
}
