using System;
using System.Collections.Generic;
using CinematicSequencer.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// ライブラリのメインView。v1 CinematicSequenceLibraryView (637行) の置き換え。
    /// MonoBehaviourではなくPure C#設計。ListViewで仮想化対応。
    /// </summary>
    public sealed class LibraryView : IDisposable
    {
        public enum LibraryTab
        {
            Sequence,
            Camera,
            Light,
            Effect,
            Audio,
            Motion,
            PostEffect,
        }

        private static readonly Dictionary<TrackType, LibraryTab> TrackTypeToTab = new()
        {
            { TrackType.CameraPose, LibraryTab.Camera },
            { TrackType.CameraProperties, LibraryTab.Camera },
            { TrackType.LightPose, LibraryTab.Light },
            { TrackType.LightProperties, LibraryTab.Light },
            { TrackType.Effect, LibraryTab.Effect },
            { TrackType.Audio, LibraryTab.Audio },
            { TrackType.Motion, LibraryTab.Motion },
            { TrackType.PostEffect, LibraryTab.PostEffect },
        };

        private readonly VisualElement _root;

        // タブボタン
        private readonly Dictionary<LibraryTab, Button> _tabButtons = new();
        private readonly VisualElement _tabBar;

        // アイテムリスト
        private readonly ListView _itemListView;
        private readonly List<object> _currentItems = new(); // SequenceInfo or ClipAssetInfo

        // D&D
        private bool _isDragging;
        private VisualElement _dragPreview;
        private ClipAssetInfo _draggedClipAsset;
        private Vector2 _rootWorldOffset;

        private LibraryTab _currentTab = LibraryTab.Sequence;
        private IReadOnlyList<SequenceInfo> _sequenceItems;
        private IReadOnlyList<ClipAssetInfo> _clipAssetItems;
        private bool _disposed;

        // --- View → Controller イベント ---

        /// <summary>ダブルクリックでシーケンス選択。</summary>
        public event Action<Guid> OnSequenceSelected;

        /// <summary>D&Dドロップ（Controller側でトラック特定）。</summary>
        public event Action<Guid, TrackType> OnClipAssetDropped;

        /// <summary>コンテキストメニューからキーフレーム編集。</summary>
        public event Action<Guid> OnEditClipAssetRequested;

        /// <summary>新規ClipAsset作成。</summary>
        public event Action<TrackType> OnCreateClipAssetRequested;

        /// <summary>新規Sequence作成。</summary>
        public event Action OnCreateSequenceRequested;

        /// <summary>リスト再取得。</summary>
        public event Action OnRefreshRequested;

        public LibraryView(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));

            // Clear UXML preview content
            _root.Clear();

            // タブバー構築
            _tabBar = new VisualElement();
            _tabBar.AddToClassList("library-tab-bar");
            _tabBar.style.flexDirection = FlexDirection.Row;
            _root.Add(_tabBar);

            CreateTabButton(LibraryTab.Sequence, "Sequence");
            CreateTabButton(LibraryTab.Camera, "Camera");
            CreateTabButton(LibraryTab.Light, "Light");
            CreateTabButton(LibraryTab.Effect, "Effect");
            CreateTabButton(LibraryTab.Audio, "Audio");
            CreateTabButton(LibraryTab.Motion, "Motion");
            CreateTabButton(LibraryTab.PostEffect, "PostEffect");

            // アクションバー
            var actionsBar = new VisualElement();
            actionsBar.AddToClassList("library-actions");
            actionsBar.style.flexDirection = FlexDirection.Row;
            _root.Add(actionsBar);

            var createButton = new Button(OnCreateButtonClicked) { text = "Create" };
            createButton.AddToClassList("library-create-button");
            actionsBar.Add(createButton);

            var refreshButton = new Button(() => OnRefreshRequested?.Invoke()) { text = "Refresh" };
            refreshButton.AddToClassList("library-refresh-button");
            actionsBar.Add(refreshButton);

            // ListView（仮想化リスト）
            _itemListView = new ListView
            {
                makeItem = MakeItem,
                bindItem = BindItem,
                itemsSource = _currentItems,
                fixedItemHeight = 28f,
                selectionType = SelectionType.Single,
            };
            _itemListView.AddToClassList("library-list-view");
            _itemListView.style.flexGrow = 1f;
            _root.Add(_itemListView);

            // コンテキストメニュー
            _itemListView.RegisterCallback<ContextualMenuPopulateEvent>(OnListContextMenu);

            // D&Dグローバルイベント登録
            _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _root.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _root.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            // 初期タブ
            SwitchTab(LibraryTab.Sequence);
        }

        // --- 公開API ---

        public void SwitchTab(LibraryTab tab)
        {
            _currentTab = tab;

            // タブハイライト更新
            foreach (var kvp in _tabButtons)
            {
                kvp.Value.EnableInClassList("selected", kvp.Key == tab);
            }

            RebuildItemList();
        }

        public void SetSequenceItems(IReadOnlyList<SequenceInfo> items)
        {
            _sequenceItems = items;
            if (_currentTab == LibraryTab.Sequence)
                RebuildItemList();
        }

        public void SetClipAssetItems(IReadOnlyList<ClipAssetInfo> items)
        {
            _clipAssetItems = items;
            if (_currentTab != LibraryTab.Sequence)
                RebuildItemList();
        }

        public void ClearItems()
        {
            _sequenceItems = null;
            _clipAssetItems = null;
            _currentItems.Clear();
            _itemListView.RefreshItems();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _root.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _root.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);

            RemoveDragPreview();
        }

        // --- ListView callbacks ---

        private VisualElement MakeItem()
        {
            var element = new LibraryItemElement();
            element.RegisterCallback<PointerDownEvent>(OnItemPointerDown);
            return element;
        }

        private void BindItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _currentItems.Count) return;
            var item = (LibraryItemElement)element;
            var data = _currentItems[index];

            switch (data)
            {
                case SequenceInfo seq:
                    item.Bind(seq);
                    item.OnDoubleClicked = id => OnSequenceSelected?.Invoke(id);
                    break;
                case ClipAssetInfo clip:
                    item.Bind(clip);
                    item.OnDoubleClicked = id => OnEditClipAssetRequested?.Invoke(id);
                    break;
            }
        }

        // --- タブ・リスト構築 ---

        private void CreateTabButton(LibraryTab tab, string label)
        {
            var button = new Button(() => SwitchTab(tab)) { text = label };
            button.AddToClassList("library-tab-button");
            _tabBar.Add(button);
            _tabButtons[tab] = button;
        }

        private void RebuildItemList()
        {
            _currentItems.Clear();

            if (_currentTab == LibraryTab.Sequence)
            {
                if (_sequenceItems != null)
                {
                    foreach (var seq in _sequenceItems)
                        _currentItems.Add(seq);
                }
            }
            else if (_clipAssetItems != null)
            {
                foreach (var clip in _clipAssetItems)
                {
                    if (TrackTypeToTab.TryGetValue(clip.Type, out var tab) && tab == _currentTab)
                        _currentItems.Add(clip);
                }
            }

            _itemListView.RefreshItems();
        }

        // --- Create ボタン ---

        private void OnCreateButtonClicked()
        {
            if (_currentTab == LibraryTab.Sequence)
            {
                OnCreateSequenceRequested?.Invoke();
            }
            else
            {
                // 現在のタブに対応するTrackTypeで新規作成リクエスト
                foreach (var kvp in TrackTypeToTab)
                {
                    if (kvp.Value == _currentTab)
                    {
                        OnCreateClipAssetRequested?.Invoke(kvp.Key);
                        break;
                    }
                }
            }
        }

        // --- コンテキストメニュー ---

        private void OnListContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (_currentTab == LibraryTab.Sequence) return;

            foreach (var kvp in TrackTypeToTab)
            {
                if (kvp.Value == _currentTab)
                {
                    var trackType = kvp.Key;
                    evt.menu.AppendAction($"Create {trackType}", _ =>
                        OnCreateClipAssetRequested?.Invoke(trackType));
                }
            }
        }

        // --- Drag & Drop ---

        private void OnItemPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            var element = evt.currentTarget as LibraryItemElement;
            if (element == null || !element.ItemType.HasValue) return;

            // ClipAssetのみD&D対応
            var clipInfo = FindClipAssetInfo(element.ItemId);
            if (clipInfo == null) return;

            _draggedClipAsset = clipInfo;
            _isDragging = true;
            _rootWorldOffset = new Vector2(_root.worldBound.x, _root.worldBound.y);

            CreateDragPreview(clipInfo.Name, clipInfo.Type, evt.position);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || _dragPreview == null) return;
            _dragPreview.transform.position = evt.position - new Vector3(_rootWorldOffset.x, _rootWorldOffset.y, 0f);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging) return;

            if (_draggedClipAsset != null)
            {
                OnClipAssetDropped?.Invoke(_draggedClipAsset.Id, _draggedClipAsset.Type);
            }

            CancelDrag();
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (_isDragging)
                CancelDrag();
        }

        private void CreateDragPreview(string itemName, TrackType type, Vector3 position)
        {
            RemoveDragPreview();

            _dragPreview = new VisualElement();
            _dragPreview.AddToClassList("sequence-clip");
            _dragPreview.AddToClassList(GetDragPreviewClass(type));
            _dragPreview.style.position = Position.Absolute;
            _dragPreview.style.width = 100;
            _dragPreview.style.height = 30;
            _dragPreview.style.opacity = 0.7f;

            var label = new Label(itemName);
            label.AddToClassList("clip-label");
            _dragPreview.Add(label);

            _dragPreview.transform.position = position - new Vector3(_rootWorldOffset.x, _rootWorldOffset.y, 0f);
            _root.Add(_dragPreview);
        }

        private void RemoveDragPreview()
        {
            _dragPreview?.RemoveFromHierarchy();
            _dragPreview = null;
        }

        private void CancelDrag()
        {
            _isDragging = false;
            _draggedClipAsset = null;
            RemoveDragPreview();
        }

        private ClipAssetInfo FindClipAssetInfo(Guid id)
        {
            if (_clipAssetItems == null) return null;
            foreach (var clip in _clipAssetItems)
            {
                if (clip.Id == id) return clip;
            }
            return null;
        }

        private static string GetDragPreviewClass(TrackType type)
        {
            return type switch
            {
                TrackType.CameraPose => "clip-camera",
                TrackType.CameraProperties => "clip-camera",
                TrackType.LightPose => "clip-light",
                TrackType.LightProperties => "clip-light",
                TrackType.Effect => "clip-effect",
                TrackType.Audio => "clip-audio",
                TrackType.Motion => "clip-motion",
                TrackType.PostEffect => "clip-posteffect",
                _ => "clip-default",
            };
        }
    }
}
