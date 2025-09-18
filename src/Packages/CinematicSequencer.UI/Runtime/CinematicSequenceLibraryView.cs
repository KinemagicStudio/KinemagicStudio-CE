using System;
using System.Collections.Generic;
using CinematicSequencer.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    // public sealed class LibraryView : MonoBehaviour
    public class CinematicSequenceLibraryView : MonoBehaviour
    {
        // TODO: Refactor
        class LibraryItemData
        {
            public ClipDataInfo ClipDataInfo { get; set; }
        }

        // TODO: Refactor
        class TimelineItemData
        {
            public CinematicSequenceDataInfo SequenceDataInfo { get; set; }
        }

        enum LibraryTabType
        {
            Timeline, // SequenceData?
            Camera,
            Light,
            Effect,
            Audio,
        }
        
        // Dictionary mapping each tab type to available data types
        private readonly Dictionary<LibraryTabType, DataTypeInfo[]> _tabDataTypeMapping = new Dictionary<LibraryTabType, DataTypeInfo[]>
        {
            {
                LibraryTabType.Camera, new[]
                {
                    new DataTypeInfo(DataType.CameraPose, "Camera Pose Animation"),
                    new DataTypeInfo(DataType.CameraProperties, "Camera Properties Animation")
                }
            },
            {
                LibraryTabType.Light, new[]
                {
                    new DataTypeInfo(DataType.LightPose, "Light Pose Animation"),
                    new DataTypeInfo(DataType.LightProperties, "Light Properties Animation")
                }
            },
            {
                LibraryTabType.Effect, new[]
                {
                    new DataTypeInfo(DataType.Effect, "Effect Animation")
                }
            },
            {
                LibraryTabType.Audio, new[]
                {
                    new DataTypeInfo(DataType.Audio, "Audio Clip")
                }
            }
        };

        // Helper class for data type information
        private class DataTypeInfo
        {
            public DataType Type { get; }
            public string DisplayName { get; }
            
            public DataTypeInfo(DataType type, string displayName)
            {
                Type = type;
                DisplayName = displayName;
            }
        }

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private TimelineTrackEditorView _timelineTrackEditorView;

        // private readonly Dictionary<Guid, LibraryItemElementManipulator> _itemElementManipulators = new();

        // Remove direct dependency on model/repositories
        private bool _initialized;
        private float _currentZoom = 100f;
        private LibraryTabType _currentTabType = LibraryTabType.Timeline;

        private VisualElement _root;
        private ScrollView _libraryItemsScrollView;
        private VisualElement _libraryActionsContainer;
        private Button _timelineTabButton;
        private Button _cameraTabButton;
        
        // TODO
        // private Button _lightTabButton;
        // private Button _effectTabButton;
        // private Button _audioTabButton;

        private Button _createNewItemButton;
        private Button _refreshLibraryButton;
        private VisualElement _timelineTabContent;
        private VisualElement _cameraTabContent;
        private VisualElement _lightTabContent;
        private VisualElement _effectTabContent;
        private VisualElement _audioTabContent;

        private bool _isDraggingItem = false;
        private VisualElement _dragPreview;
        private ClipDataInfo _draggedItemInfo;
        private Vector2? _rootVisualElementPosition = null;
        
        public bool IsInitialized => _initialized;
        
        public event Action<ClipDataInfo, int, float> OnItemDropped;
        public event Action<IClipData> OnItemSelected;
        public event Action<CinematicSequenceDataInfo> OnTimelineSelected;

        // Add new event for clip edit from library
        public event Action<ClipDataInfo> OnEditClipDataRequest;
        
        // 新規作成イベント
        public event Action<DataType> OnCreateNewClipDataRequest;
        public event Action OnCreateNewTimelineRequest;
        
        // リフレッシュリクエストイベント
        public event Action OnRefreshLibraryRequest;
        
        private void OnEnable()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            if (_initialized) return;

            _root = _uiDocument.rootVisualElement.Q(UIElementNames.SequenceEditorRootName);
            _libraryItemsScrollView = _root.Q<ScrollView>("library-items-scroll-view");
            _libraryActionsContainer = _root.Q("library-actions");

            _timelineTabButton = _root.Q<Button>("timeline-tab");
            _cameraTabButton = _root.Q<Button>("camera-tab");
            // _lightTabButton = _root.Q<Button>("light-tab");
            // _effectTabButton = _root.Q<Button>("effect-tab");
            // _audioTabButton = _root.Q<Button>("audio-tab");
            _createNewItemButton = _root.Q<Button>("create-new-item-button");
            _refreshLibraryButton = _root.Q<Button>("refresh-library-button");

            _timelineTabContent = _root.Q("timeline-tab-content");
            _cameraTabContent = _root.Q("camera-tab-content");
            _lightTabContent = _root.Q("light-tab-content");
            _effectTabContent = _root.Q("effect-tab-content");
            _audioTabContent = _root.Q("audio-tab-content"); 

            _timelineTabContent.Clear();
            _cameraTabContent.Clear();
            _lightTabContent.Clear();
            _effectTabContent.Clear();
            _audioTabContent.Clear();

            if (_timelineTabButton != null) _timelineTabButton.clicked += () => SwitchLibraryTab(LibraryTabType.Timeline);
            if (_cameraTabButton != null) _cameraTabButton.clicked += () => SwitchLibraryTab(LibraryTabType.Camera);
            // if (_lightTabButton != null) _lightTabButton.clicked += () => SwitchLibraryTab(LibraryTabType.Light);
            // if (_effectTabButton != null) _effectTabButton.clicked += () => SwitchLibraryTab(LibraryTabType.Effect);
            // if (_audioTabButton != null) _audioTabButton.clicked += () => SwitchLibraryTab(LibraryTabType.Audio);
            
            // 新規作成ボタンとリフレッシュボタンのイベントハンドラー
            if (_createNewItemButton != null) 
            {
                _createNewItemButton.clicked += OnCreateNewButtonClicked;
            }
            if (_refreshLibraryButton != null) _refreshLibraryButton.clicked += OnRefreshButtonClicked;
            
            // 右クリックメニューを追加
            _libraryItemsScrollView.RegisterCallback<ContextualMenuPopulateEvent>(BuildContextMenu);
            _libraryActionsContainer.RegisterCallback<ContextualMenuPopulateEvent>(BuildContextMenu);

            // Set up global drag events
            RegisterLibraryDragGlobalEvents();

            // 初期タブを設定
            SwitchLibraryTab(LibraryTabType.Timeline);

            _initialized = true;
        }

        // リフレッシュボタンのクリックハンドラー
        private void OnRefreshButtonClicked()
        {
            Debug.Log("<color=cyan>[CinematicSequenceLibraryView] Refresh button clicked</color>");
            OnRefreshLibraryRequest?.Invoke();
        }
        
        // 右クリックメニュー構築
        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            // Timelineタブでは何も表示しない
            if (_currentTabType == LibraryTabType.Timeline)
            {
                return;
            }
            
            // 現在のタブタイプに応じたクリップ作成オプションを表示
            AddCreateClipMenuItems(evt.menu);
        }

        // クリップ作成メニュー項目を追加するヘルパーメソッド
        private void AddCreateClipMenuItems(DropdownMenu menu)
        {
            if (_tabDataTypeMapping.TryGetValue(_currentTabType, out var dataTypeOptions) && dataTypeOptions.Length > 0)
            {
                foreach (var option in dataTypeOptions)
                {
                    var dataType = option.Type;
                    menu.AppendAction($"Create/{option.DisplayName}", action =>
                    {
                        Debug.Log($"<color=cyan>[CinematicSequenceLibraryView] Creating new clip of type: {dataType}</color>");
                        OnCreateNewClipDataRequest?.Invoke(dataType);
                    });
                }
            }
        }

        // 新規作成ボタンクリック時のハンドラー
        private void OnCreateNewButtonClicked()
        {
            // タイムラインタブの場合のみ処理する（他のタブではボタンが非表示）
            if (_currentTabType == LibraryTabType.Timeline)
            {
                OnCreateNewTimelineRequest?.Invoke();
            }
        }

        // TODO: Code review
        // public void SetZoom(float zoom)
        // {
        //     _currentZoom = zoom;
        // }

        public void SetTimelineItems(List<CinematicSequenceDataInfo> list)
        {
            _timelineTabContent.Clear();

            foreach (var sequenceDataInfo in list)
            {
                var item = new VisualElement();
                item.AddToClassList("library-item");

                // Store the sequence data info for later use
                item.userData = new TimelineItemData
                {
                    SequenceDataInfo = sequenceDataInfo
                };

                var label = new Label(sequenceDataInfo.Name);
                label.name = $"item-label";
                label.AddToClassList("library-item-label");
                item.Add(label);

                _timelineTabContent.Add(item);
                
                // Add double-click event to load timeline
                item.RegisterCallback<MouseDownEvent>(evt => 
                {
                    if (evt.clickCount == 2) // Double click
                    {
                        var timelineData = item.userData as TimelineItemData;
                        if (timelineData != null)
                        {
                            OnTimelineSelected?.Invoke(timelineData.SequenceDataInfo);
                        }
                        evt.StopPropagation();
                    }
                });
            }
        }

        public void SetLibraryItems(List<ClipDataInfo> list)
        {
            _cameraTabContent.Clear();
            _lightTabContent.Clear();
            _effectTabContent.Clear();
            _audioTabContent.Clear();
            // _itemElementManipulators.Clear();

            foreach (var clipDataInfo in list)
            {
                var item = CreateLibraryItemElement(clipDataInfo);

                switch (clipDataInfo.Type)
                {
                    case DataType.CameraPose:
                    case DataType.CameraProperties:
                        _cameraTabContent.Add(item);
                        break;
                    case DataType.LightPose:
                    case DataType.LightProperties:
                        _lightTabContent.Add(item);
                        break;
                    case DataType.Effect:
                        _effectTabContent.Add(item);
                        break;
                    case DataType.Audio:
                        _audioTabContent.Add(item);
                        break;
                }
                
                // Set up drag and drop for this item
                SetupLibraryItemDrag(item);
            }
        }

        /// <summary>
        /// Switches between different library tabs
        /// </summary>
        private void SwitchLibraryTab(LibraryTabType tabType)
        {
            _currentTabType = tabType;
            
            // Hide all tab contents
            _timelineTabContent.style.display = DisplayStyle.None;
            _cameraTabContent.style.display = DisplayStyle.None;
            _lightTabContent.style.display = DisplayStyle.None;
            _audioTabContent.style.display = DisplayStyle.None;
            _effectTabContent.style.display = DisplayStyle.None;
            
            // Deselect all tab buttons
            _timelineTabButton.RemoveFromClassList("selected");
            _cameraTabButton.RemoveFromClassList("selected");
            // TODO
            // _lightTabButton.RemoveFromClassList("selected");
            // _audioTabButton.RemoveFromClassList("selected");
            // _effectTabButton.RemoveFromClassList("selected");
            
            // Show the selected tab content and select the corresponding button
            switch (tabType)
            {
                case LibraryTabType.Timeline:
                    _timelineTabContent.style.display = DisplayStyle.Flex;
                    _timelineTabButton.AddToClassList("selected");
                    _createNewItemButton.text = "Create New Timeline";
                    _createNewItemButton.style.display = DisplayStyle.Flex; // タイムラインタブでのみボタンを表示
                    break;
                case LibraryTabType.Camera:
                    _cameraTabContent.style.display = DisplayStyle.Flex;
                    _cameraTabButton.AddToClassList("selected");
                    _createNewItemButton.style.display = DisplayStyle.None; // 他のタブではボタンを非表示
                    break;
                // TODO
                // case LibraryTabType.Light:
                //     _lightTabContent.style.display = DisplayStyle.Flex;
                //     _lightTabButton.AddToClassList("selected");
                //     _createNewItemButton.style.display = DisplayStyle.None; // 他のタブではボタンを非表示
                //     break;
                // case LibraryTabType.Audio:
                //     _audioTabContent.style.display = DisplayStyle.Flex;
                //     _audioTabButton.AddToClassList("selected");
                //     _createNewItemButton.style.display = DisplayStyle.None; // 他のタブではボタンを非表示
                //     break;
                // case LibraryTabType.Effect:
                //     _effectTabContent.style.display = DisplayStyle.Flex;
                //     _effectTabButton.AddToClassList("selected");
                //     _createNewItemButton.style.display = DisplayStyle.None; // 他のタブではボタンを非表示
                //     break;
            }
        }

        /// <summary>
        /// Notifies when a library item is dropped onto a track
        /// </summary>
        private void NotifyItemDropped(ClipDataInfo clipDataInfo, int trackId, float startTime)
        {
            Debug.Log($"<color=cyan>Item dropped: {clipDataInfo.Name} on track {trackId} at time {startTime}</color>");
            OnItemDropped?.Invoke(clipDataInfo, trackId, startTime);
        }
        
        /// <summary>
        /// Notifies when a library item is selected
        /// </summary>
        private void NotifyItemSelected(ClipDataInfo info)
        {
            // var clipData = CreateClipDataFromInfo(info);
            // if (clipData != null)
            // {
            //     OnItemSelected?.Invoke(clipData);
            // }
        }

        /// <summary>
        /// Sets up drag and drop for a library item
        /// </summary>
        private void SetupLibraryItemDrag(VisualElement item)
        {
            // Register mouse down event to start drag
            item.RegisterCallback<PointerDownEvent>(evt => 
            {
                if (evt.button != 0) return; // Only left mouse button
                
                var itemData = item.userData as LibraryItemData;
                if (itemData == null) return;
                
                // Store the data for the item being dragged
                _draggedItemInfo = itemData.ClipDataInfo;
                _isDraggingItem = true;
                
                // Create visual drag preview
                CreateDragPreviewElement(itemData.ClipDataInfo.Name, itemData.ClipDataInfo.Type, evt.position);
                
                // Notify selection
                NotifyItemSelected(itemData.ClipDataInfo);
                
                evt.StopPropagation();
            });
            
            // Also support click to select without dragging
            item.RegisterCallback<ClickEvent>(evt => 
            {
                var itemData = item.userData as LibraryItemData;
                if (itemData != null)
                {
                    NotifyItemSelected(itemData.ClipDataInfo);
                }
            });
        }

        private bool _hasRegisteredLibraryDragEvents = false;

        /// <summary>
        /// Registers global events for drag and drop
        /// </summary>
        private void RegisterLibraryDragGlobalEvents()
        {
            if (_hasRegisteredLibraryDragEvents) return;
            
            // Pointer move - update drag preview position
            _root.RegisterCallback<PointerMoveEvent>(evt => 
            {
                if (_isDraggingItem && _dragPreview != null)
                {
                    UpdateDragPreviewPosition(evt.position);
                }
            });
            
            // Pointer up - handle drop
            _root.RegisterCallback<PointerUpEvent>(evt => 
            {
                if (_isDraggingItem)
                {
                    HandleLibraryItemDrop(evt.position);
                    _isDraggingItem = false;
                    _draggedItemInfo = null;
                    RemoveDragPreview();
                }
            });
            
            // Mouse leave - cancel drag if mouse leaves window
            _root.RegisterCallback<MouseLeaveEvent>(evt => 
            {
                if (_isDraggingItem && evt.pressedButtons == 0)
                {
                    _isDraggingItem = false;
                    _draggedItemInfo = null;
                    RemoveDragPreview();
                }
            });

            _hasRegisteredLibraryDragEvents = true;
        }

        /// <summary>
        /// Creates a visual element for drag preview
        /// </summary>
        private void CreateDragPreviewElement(string itemName, DataType dataType, Vector2 worldPosition)
        {
            RemoveDragPreview();

            _dragPreview = new VisualElement();
            _root.Add(_dragPreview);

            // Style
            _dragPreview.AddToClassList("timeline-clip");
            // _dragPreview.AddToClassList("drag-preview"); // TODO: Delete

            if (dataType == DataType.CameraPose || dataType == DataType.CameraProperties)
            {
                _dragPreview.AddToClassList("camera-clip");
            }
            else if (dataType == DataType.LightPose || dataType == DataType.LightProperties)
            {
                _dragPreview.AddToClassList("light-clip");
            }
            else if (dataType == DataType.Audio)
            {
                _dragPreview.AddToClassList("audio-clip");
            }
            else if (dataType == DataType.Effect)
            {
                _dragPreview.AddToClassList("effect-clip");
            }

            _dragPreview.style.width = 100;
            _dragPreview.style.height = 30;
            _dragPreview.style.opacity = 0.7f;

            var label = new Label(itemName);
            label.AddToClassList("clip-label");
            _dragPreview.Add(label);

            // Position
            if (_rootVisualElementPosition == null)
            {
                _rootVisualElementPosition = new Vector2(_root.worldBound.x, _root.worldBound.y);
            }
            _dragPreview.style.position = Position.Absolute;
            _dragPreview.transform.position = worldPosition - _rootVisualElementPosition.Value;
        }
        
        /// <summary>
        /// Updates position of drag preview element
        /// </summary>
        private void UpdateDragPreviewPosition(Vector2 position)
        {
            _dragPreview.transform.position = position - _rootVisualElementPosition.Value;
        }

        /// <summary>
        /// Removes drag preview element
        /// </summary>
        private void RemoveDragPreview()
        {
            if (_dragPreview != null)
            {
                _dragPreview.RemoveFromHierarchy();
                _dragPreview = null;
            }
        }

        /// <summary>
        /// Handles drop of library item
        /// </summary>
        private void HandleLibraryItemDrop(Vector2 position)
        {
            if (_draggedItemInfo == null || _timelineTrackEditorView == null) return;
            
            // If track type is None, we can't proceed
            if (_draggedItemInfo.Type == DataType.Unknown) return;
            
            // Find the closest compatible track
            if (_timelineTrackEditorView.TryFindClosestTrackRow(_dragPreview, _draggedItemInfo.Type, 
                    out var trackElementData, out var trackPosition))
            {
                // Calculate time position from X coordinate and zoom
                var startTime = Mathf.Max(0, (_dragPreview.worldBound.x - trackPosition.x) / _currentZoom);
                                
                // Create the clip data based on the library item
                // var clipData = CreateClipDataFromInfo(_draggedItemInfo);

                NotifyItemDropped(_draggedItemInfo, trackElementData.Track.Id, startTime);
            }
        }
        
        /// <summary>
        /// Creates a library item element with context menu
        /// </summary>
        private VisualElement CreateLibraryItemElement(ClipDataInfo clipDataInfo)
        {
            var element = new VisualElement();
            element.AddToClassList("library-item");
            element.AddToClassList(clipDataInfo.Type.ToString().ToLower() + "-item");
            
            // Store the clip data info for later use
            element.userData = new LibraryItemData
            {
                ClipDataInfo = clipDataInfo
            };

            var label = new Label(clipDataInfo.Name);
            label.name = $"item-label";
            label.AddToClassList("library-item-label");
            element.Add(label);

            // Add context menu using ContextualMenuManipulator
            element.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.AppendAction("Edit Keyframes", action => 
                {
                    OnEditClipDataRequest?.Invoke(clipDataInfo);
                });
            });

            return element;
        }

        public void ClearLibraryItems()
        {
            _timelineTabContent.Clear();
            _cameraTabContent.Clear();
            _lightTabContent.Clear();
            _effectTabContent.Clear();
            _audioTabContent.Clear();
        }

        // TODO: Delete
        // /// <summary>
        // /// Creates a clip data instance from library item info
        // /// </summary>
        // private IClipData CreateClipDataFromInfo(ClipDataInfo info)
        // {
        //     switch (info.Type)
        //     {
        //         case ClipType.Camera:
        //             return new CameraAnimationClip(info.Name);
        //         case ClipType.Light:
        //             return new LightAnimationClip(info.Name);
        //         case ClipType.Audio:
        //             // Implement when audio clips are supported
        //             Debug.LogWarning("Audio clips not yet implemented");
        //             return null;
        //         case ClipType.Effect:
        //             // Implement when effect clips are supported
        //             Debug.LogWarning("Effect clips not yet implemented");
        //             return null;
        //         default:
        //             Debug.LogError($"Unsupported clip type: {info.Type}");
        //             return null;
        //     }
        // }
    }
    
    // /// <summary>
    // /// Data class for library items
    // /// </summary>
    // public class LibraryItemData
    // {
    //     public ClipDataInfo ClipDataInfo { get; set; }
    // }
}

