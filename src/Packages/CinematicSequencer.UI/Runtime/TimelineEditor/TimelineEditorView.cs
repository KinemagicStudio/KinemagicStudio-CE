using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ContextualMenuManipulator = ContextualMenuPlayer.ContextualMenuManipulator;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// The main view class for the sequence editor UI.
    /// </summary>
    public class TimelineEditorView : MonoBehaviour
    // public sealed class SequenceEditorView : MonoBehaviour // TODO: Rename
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private SceneView _sceneView;
        [SerializeField] private SaveConfirmationDialogView _saveConfirmationDialogView;
        [SerializeField] private CinematicSequenceLibraryView _cinematicSequenceLibraryView; // TODO: Rename to _libraryView
        [SerializeField] private TimelineTrackEditorView _timelineTrackEditorView; // WIP
        [SerializeField] private TimelinePlaybackControlView _timelinePlaybackControlView; // TODO: Rename to _playbackControlView

        [Header("Timeline Settings")]
        [SerializeField] private float _defaultDuration = 30f;
        [SerializeField] private float _pixelsPerSecond = 100f;
        [SerializeField] private float _minZoom = 50f;
        [SerializeField] private float _maxZoom = 200f;
        [SerializeField] private float _zoomStep = 10f;

        public SceneView SceneView => _sceneView;
        public SaveConfirmationDialogView SaveConfirmationDialogView => _saveConfirmationDialogView;

        private readonly Dictionary<int, ClipElementDragAndDropManipulator> _clipElementManipulators = new();

        private bool _initialized;
        private Timeline _currentTimeline;
        private TimelineClip _selectedClip;

        // UI Element references
        private VisualElement _root;
        private TextField _timelineNameField;
        private Button _saveTimelineButton;
        private VisualElement _clipsContainer;
        private VisualElement _tracksContainer; // TrackRowsContainer
        private VisualElement _trackHeadersContainer;
        private ScrollView _trackHeadersScrollView;
        private ScrollView _tracksScrollView;
        private Button _addCameraTrackButton;
        private Button _addLightTrackButton;
        private VisualElement _timeRuler;
        private Label _timeDisplay;
        private Button _zoomInButton;
        private Button _zoomOutButton;

        private float _currentZoom;
        
        // Events to notify the presenter about user actions
        public event Action OnAddCameraTrackRequest;
        public event Action OnAddLightTrackRequest;
        public event Action<TimelineClip> OnEditKeyframeRequest;
        public event Action<ClipMovedEvent> OnClipMoved;
        public event Action<string> OnTimelineNameChanged;
        public event Action OnSaveTimelineRequest;
        
        // TODO: Code review
        public event Action<TimelineClip> OnClipSelected;
        public event Action<int, int> OnClipRemoved;
        public event Action<float> OnZoomChanged;
        // Add new event for clip deletion
        public event Action<int, int> OnClipDeleteRequest;

        public bool CanEdit { get; set; } = true;

        #region MonoBehaviour Methods

        private void Awake()
        {
            _currentZoom = _pixelsPerSecond;
            // _totalDuration = _defaultDuration;
        }

        private void OnEnable()
        {
            // Initialize UI once it's ready
            InitializeUI();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_addCameraTrackButton != null) _addCameraTrackButton.clicked -= OnAddCameraTrackButtonClicked;
            if (_addLightTrackButton != null) _addLightTrackButton.clicked -= OnAddLightTrackButtonClicked;
            
            Debug.Log("<color=orange>TimelineEditorView destroyed</color>");
        }

        #endregion

        #region Public Methods

        public void Show()
        {
            _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }

        // TODO: Code review
        public void SetZoom(float zoom)
        {
            _currentZoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
            RegenerateTimelineUI();
            
            // Notify about zoom change
            OnZoomChanged?.Invoke(_currentZoom);
        }
        
        // // TODO: Code review
        // public void SetSelectedClip(TimelineClip clip)
        // {
        //     _selectedClip = clip;
            
        //     // Update UI to highlight the selected clip
        //     var clipElements = _root.Query<VisualElement>(className: "timeline-clip").ToList();
        //     foreach (var clipElement in clipElements)
        //     {
        //         var clipData = clipElement.userData as ClipElementData;
        //         if (clipData != null)
        //         {
        //             bool isSelected = clipData.Clip == clip;
        //             if (isSelected)
        //             {
        //                 clipElement.AddToClassList("selected");
        //             }
        //             else
        //             {
        //                 clipElement.RemoveFromClassList("selected");
        //             }
        //         }
        //     }
        // }
        
        /// <summary>
        /// Update the UI to reflect the new timeline data
        /// </summary>
        public void UpdateTimelineUI(Timeline timeline)
        {
            _currentTimeline = timeline;
            
            // Update timeline name in text field
            if (_timelineNameField != null && timeline != null)
            {
                _timelineNameField.value = timeline.Name;
            }
            
            RegenerateTimelineUI();
        }
        
        public void EnableClipElementManipulators()
        {
            foreach (var manipulator in _clipElementManipulators.Values)
            {
                manipulator.Enabled = true;
            }
        }
        
        public void DisableClipElementManipulators()
        {
            foreach (var manipulator in _clipElementManipulators.Values)
            {
                manipulator.Enabled = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize the UI by getting references to important elements and setting up event handlers
        /// </summary>
        private void InitializeUI()
        {
            if (_initialized) return;

            // Get the root element
            _root = _uiDocument.rootVisualElement.Q(UIElementNames.SequenceEditorRootName);
            if (_root == null) return;

            _root.AddManipulator(new ContextualMenuManipulator());

            // Get references to timeline name and save UI elements
            _timelineNameField = _root.Q<TextField>("timeline-name-field");
            _saveTimelineButton = _root.Q<Button>("save-timeline-button");

            // Setup event handlers for timeline name and save
            if (_timelineNameField != null)
            {
                _timelineNameField.RegisterValueChangedCallback(evt =>
                {
                    OnTimelineNameChanged?.Invoke(evt.newValue);
                });
            }

            if (_saveTimelineButton != null)
            {
                _saveTimelineButton.clicked += () =>
                {
                    OnSaveTimelineRequest?.Invoke();
                };
            }

            InitializeTrackUI();

            // TODO: Code review

            // // Get references to important UI elements
            // _timeRuler = _root.Q("time-ruler");

            // // Get the track headers title element
            // var trackHeadersTitle = _root.Q("track-headers-title");

            // // Setup header height synchronization
            // if (_timeRuler != null && trackHeadersTitle != null)
            // {
            //     SetupHeaderHeightSynchronization(_timeRuler, trackHeadersTitle);
            // }

            // // Get the edit keyframes button
            // Button editKeyframesButton = _root.Q<Button>("edit-keyframes-button");
            // if (editKeyframesButton != null)
            // {
            //     editKeyframesButton.clicked += OnEditKeyframesButtonClicked;
            // }

            // // Get zoom buttons
            // _zoomInButton = _root.Q<Button>("zoom-in-button");
            // _zoomOutButton = _root.Q<Button>("zoom-out-button");

            // if (_zoomInButton != null)
            // {
            //     _zoomInButton.clicked += () => 
            //     {
            //         SetZoom(_currentZoom + _zoomStep);
            //     };
            // }

            // if (_zoomOutButton != null)
            // {
            //     _zoomOutButton.clicked += () => 
            //     {
            //         SetZoom(_currentZoom - _zoomStep);
            //     };
            // }

            _initialized = true;
        }

        // CODE REVIEW: DONE
        private void InitializeTrackUI()
        {
            Debug.Log("<color=lime>Initialize Track UI</color>");

            _clipsContainer = _root.Q(UIElementNames.ClipsContainerName);
            _tracksContainer = _root.Q(UIElementNames.TracksContainerName);
            _trackHeadersContainer = _root.Q(UIElementNames.TrackHeadersContainerName);
            _tracksScrollView = _root.Q<ScrollView>(UIElementNames.TracksScrollViewName);
            _trackHeadersScrollView = _root.Q<ScrollView>(UIElementNames.TrackHeadersScrollViewName);
            _addCameraTrackButton = _root.Q<Button>("add-camera-track-button");
            _addLightTrackButton = _root.Q<Button>("add-light-track-button");

            // Clear existing tracks and clips
            _clipsContainer.Clear();
            _tracksContainer.Clear();
            _trackHeadersContainer.Clear();

            // Setup scroll synchronization between track headers and tracks content
            SetupScrollSynchronization();
            SetupScrollViewHeightSynchronization();

            // Set up button click handlers
            if (_addCameraTrackButton != null)
            {
                _addCameraTrackButton.clicked -= OnAddCameraTrackButtonClicked; // Avoid duplicate handlers
                _addCameraTrackButton.clicked += OnAddCameraTrackButtonClicked;
            }
            
            if (_addLightTrackButton != null)
            {
                _addLightTrackButton.clicked -= OnAddLightTrackButtonClicked; // Avoid duplicate handlers
                _addLightTrackButton.clicked += OnAddLightTrackButtonClicked;
            }
        }

        // CODE REVIEW: DONE
        private void SetupScrollSynchronization()
        {
            if (_trackHeadersScrollView == null || _tracksScrollView == null) return;

            // Synchronize vertical scrolling between track headers and tracks content
            _trackHeadersScrollView.verticalScroller.valueChanged += (value) =>
            {
                // When track headers scrollview is scrolled vertically, sync tracks scrollview
                if (Mathf.Abs(_tracksScrollView.scrollOffset.y - value) > 0.01f)
                {
                    _tracksScrollView.scrollOffset = new Vector2(_tracksScrollView.scrollOffset.x, value);
                }
            };

            _tracksScrollView.verticalScroller.valueChanged += (value) =>
            {
                // When tracks scrollview is scrolled vertically, sync track headers scrollview
                if (Mathf.Abs(_trackHeadersScrollView.scrollOffset.y - value) > 0.01f)
                {
                    _trackHeadersScrollView.scrollOffset = new Vector2(_trackHeadersScrollView.scrollOffset.x, value);
                }
            };
        }

        // CODE REVIEW: TODO
        private void SetupScrollViewHeightSynchronization()
        {
            if (_trackHeadersScrollView == null || _tracksScrollView == null) return;

            // 初期高さを同期
            SynchronizeScrollViewHeights();

            // レイアウト変更時に高さを同期
            _tracksScrollView.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                SynchronizeScrollViewHeights();
            });

            // 親要素のレイアウト変更時にも高さを同期
            VisualElement leftPanel = _root.Q("left-panel");
            VisualElement rightPanel = _root.Q("right-panel");

            if (leftPanel != null && rightPanel != null)
            {
                leftPanel.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    SynchronizeScrollViewHeights();
                });

                rightPanel.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    SynchronizeScrollViewHeights();
                });
            }

            // ウィンドウリサイズなどのタイミングでも同期を行うためにrootにもイベント登録
            _root.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                SynchronizeScrollViewHeights();
            });
        }

        // CODE REVIEW: TODO
        private void SynchronizeScrollViewHeights()
        {
            if (_trackHeadersScrollView == null || _tracksScrollView == null) return;

            // tracks-scroll-viewを基準にして、track-headers-scroll-viewの高さを合わせる
            float tracksScrollViewHeight = _tracksScrollView.resolvedStyle.height;
            if (tracksScrollViewHeight > 0)
            {
                _trackHeadersScrollView.style.height = tracksScrollViewHeight;

                // コンテンツサイズも同期させる
                if (_tracksContainer != null)
                {
                    float contentHeight = _tracksContainer.resolvedStyle.height;
                    if (contentHeight > _trackHeadersScrollView.contentContainer.resolvedStyle.height)
                    {
                        _trackHeadersScrollView.contentContainer.style.minHeight = contentHeight;
                    }
                }
            }
        }

        /// <summary>
        /// Setup synchronization for header heights (time-ruler and track-headers-title)
        /// </summary>
        private void SetupHeaderHeightSynchronization(VisualElement timeRuler, VisualElement trackHeadersTitle)
        {
            // 初期高さを同期
            SynchronizeHeaderHeights(timeRuler, trackHeadersTitle);

            // レイアウト変更時に高さを同期
            timeRuler.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                SynchronizeHeaderHeights(timeRuler, trackHeadersTitle);
            });

            trackHeadersTitle.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                SynchronizeHeaderHeights(timeRuler, trackHeadersTitle);
            });
        }

        /// <summary>
        /// Synchronize the heights of the headers
        /// </summary>
        private void SynchronizeHeaderHeights(VisualElement timeRuler, VisualElement trackHeadersTitle)
        {
            // time-rulerを基準にして、track-headers-titleの高さを合わせる
            float rulerHeight = timeRuler.resolvedStyle.height;
            if (rulerHeight > 0)
            {
                trackHeadersTitle.style.height = rulerHeight;
            }
        }

        /// <summary>
        /// Completely regenerates the timeline UI based on the current timeline data
        /// </summary>
        private void RegenerateTimelineUI()
        {
            if (!_initialized)
            {
                InitializeUI();
            }

            // Clear existing UI
            _clipsContainer.Clear();
            _tracksContainer.Clear();
            _trackHeadersContainer.Clear();
            _clipElementManipulators.Clear();

            if (_currentTimeline == null) return;

            // Create track UI for each track
            foreach (var track in _currentTimeline.Tracks)
            {
                CreateTrackUI(track);
            }
        }

        private void CreateTrackUI(TimelineTrack track)
        {
            // Create track header (left side)
            var trackHeader = new VisualElement();
            trackHeader.AddToClassList("track-header");
            trackHeader.userData = new TrackElementData()
            {
                Track = track,
                // TrackId = track.Id,
                // TrackType = track.Type,
                // TargetId = track.TargetId,
            };

            var trackName = new Label(track.Name);
            trackName.AddToClassList("track-name");
            trackHeader.Add(trackName);

            _trackHeadersContainer.Add(trackHeader);

            // Create track content row (right side)
            var trackRow = new VisualElement();
            trackRow.AddToClassList(UIElementNames.TrackRowClassName);
            trackRow.userData = new TrackElementData()
            {
                Track = track,
                // TrackId = track.Id,
                // TrackType = track.Type,
                // TargetId = track.TargetId,
            };

            _tracksContainer.Add(trackRow);

            // Add clip elements
            foreach (var clip in track.Clips)
            {
                var clipElement = CreateClipUI(clip, track);
                _clipsContainer.Add(clipElement);
                
                // Add drag and drop manipulator
                var manipulator = new ClipElementDragAndDropManipulator(_uiDocument, clipElement, _currentZoom);
                manipulator.OnClipMoved += (eventArgs) =>
                {
                    OnClipMoved?.Invoke(eventArgs);
                };
                _clipElementManipulators.Add(clip.Id, manipulator);
                

                // TODO: Code review
                // // Setup click handler
                // clipElement.RegisterCallback<MouseDownEvent>(evt => 
                // {
                //     OnClipSelected?.Invoke(clip);
                //     evt.StopPropagation();
                // });
            }

            // Register the callback to set the position of clip elements
            trackRow.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var clipElements = _root.Query<VisualElement>(className: "timeline-clip").ToList();

                foreach (var clipElement in clipElements)
                {
                    var clipData = clipElement.userData as ClipElementData;
                    if (clipData != null && clipData.TrackId == track.Id)
                    {
                        var trackRowLocalBound = trackRow.localBound;
                        clipElement.transform.position = new Vector2(clipData.Clip.StartTime * _currentZoom, trackRowLocalBound.y);
                    }
                }
            });
        }

        private VisualElement CreateClipUI(TimelineClip clip, TimelineTrack track)
        {
            var clipElement = new VisualElement();
            clipElement.AddToClassList("timeline-clip");

            clipElement.style.width = (clip.EndTime - clip.StartTime) * _currentZoom;

            clipElement.userData = new ClipElementData()
            {
                TrackId = track.Id,
                Clip = clip
            };

            var clipLabel = new Label(clip.ClipData.Name);
            clipLabel.AddToClassList("clip-label");
            clipElement.Add(clipLabel);

            if (track.Type == DataType.CameraPose || track.Type == DataType.CameraProperties)
            {
                clipElement.AddToClassList("camera-clip");
            }
            else if (track.Type == DataType.LightPose || track.Type == DataType.LightProperties)
            {
                clipElement.AddToClassList("light-clip");
            }
            // else if (track.Type == TrackType.Effect)
            // {
            //     clipElement.AddToClassList("effect-clip");
            // }
            // else if (track.Type == TrackType.Audio)
            // {
            //     clipElement.AddToClassList("audio-clip");
            // }

            // Add double click handler for keyframe editing
            clipElement.RegisterCallback<MouseDownEvent>(evt => 
            {
                if (evt.button == 0 && evt.clickCount == 2 && _timelineTrackEditorView != null)
                {
                    _timelineTrackEditorView.NotifyClipEditRequested(clip);
                    evt.StopPropagation(); // TODO
                }
                else if (evt.button == 0 && evt.clickCount == 1 && _timelineTrackEditorView != null)
                {
                    _timelineTrackEditorView.NotifyClipSelected(clip);
                    evt.StopPropagation(); // TODO
                }
            });
            
            // Add context menu using ContextualMenuManipulator
            clipElement.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                if (!CanEdit) return;
                
                evt.menu.AppendAction("Edit Keyframes", action => 
                {
                    OnEditKeyframeRequest?.Invoke(clip);
                });
                
                evt.menu.AppendAction("Delete Clip", action => 
                {
                    OnClipDeleteRequest?.Invoke(track.Id, clip.Id);
                });
            });

            return clipElement;
        }

        #endregion

        #region Event Handlers

        private void OnAddCameraTrackButtonClicked()
        {
            if (CanEdit)
            {
                OnAddCameraTrackRequest?.Invoke();
            }
        }
        
        private void OnAddLightTrackButtonClicked()
        {
            if (CanEdit)
            {
                OnAddLightTrackRequest?.Invoke();
            }
        }

        private void OnEditKeyframesButtonClicked()
        {
            if (CanEdit)
            {
            }
            // TODO: Code review
            // if (_selectedClip != null)
            // {
            //     OnEditKeyframeRequest?.Invoke(_selectedClip);
            // }
            // else
            // {
            //     Debug.LogWarning("No clip selected for keyframe editing");
            // }
        }

        #endregion

        #region UI Switching Methods

        /// <summary>
        /// Show the timeline editor UI
        /// </summary>
        public void ShowTimelineEditor()
        {
            if (_uiDocument != null)
            {
                _uiDocument.enabled = true;

                // UIDocumentの有効化後、初回表示時に必要なUI要素の初期化を行う
                if (_root == null || _timeRuler == null)
                {
                    InitializeUI();
                }

                Debug.Log("Timeline Editor UI shown");
            }
        }

        /// <summary>
        /// Hide the timeline editor UI
        /// </summary>
        public void HideTimelineEditor()
        {
            if (_uiDocument != null)
            {
                _uiDocument.enabled = false;
            }
        }

        #endregion

        // /// <summary>
        // /// 指定されたトラックIDのトラックが表示されるようにスクロールする
        // /// </summary>
        // public void ScrollToTrack(int trackId)
        // {
        //     // 対象のトラック要素を検索
        //     var trackRow = _tracksContainer.Query<VisualElement>()
        //         .Where(e => {
        //             if (e.userData is TrackElementData data)
        //             {
        //                 return data.Track.Id == trackId;
        //             }
        //             return false;
        //         })
        //         .First();
            
        //     if (trackRow != null)
        //     {
        //         // トラックの縦位置までスクロール
        //         _tracksScrollView.scrollOffset = new Vector2(
        //             _tracksScrollView.scrollOffset.x,
        //             trackRow.worldBound.y - _tracksScrollView.worldBound.y + _tracksScrollView.scrollOffset.y
        //         );
        //     }
        // }
    }
}
