using System;
using System.Collections.Generic;
using CinematicSequencer.Animation;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using FloatField = Unity.AppUI.UI.FloatField;
using Toggle = UnityEngine.UIElements.Toggle;
using ContextualMenuManipulator = ContextualMenuPlayer.ContextualMenuManipulator;
using Keyframe = CinematicSequencer.Animation.Keyframe;

namespace CinematicSequencer.UI
{
    // TODO: レビュー&コード整理
    public sealed class KeyframeEditorView : MonoBehaviour // KeyframeAnimationEditorView?
    {
        class KeyframeElementData
        {
            public KeyframeId Id { get; }
            public float Value { get; }

            public KeyframeElementData(KeyframeId id, float value)
            {
                Id = id;
                Value = value;
            }
        }

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private SceneView _sceneView;
        [SerializeField] private SaveConfirmationDialogView _saveConfirmationDialogView;
        [SerializeField] private float _pixelsPerFrame = 30f; // 1フレームあたりのピクセル数
        [SerializeField] private int _framesPerSecond = 30; // フレームレート（固定30fps）

        public SceneView SceneView => _sceneView;
        public SaveConfirmationDialogView SaveConfirmationDialogView => _saveConfirmationDialogView;

        private readonly Dictionary<string, VisualElement> _propertyFields = new();
        private readonly Dictionary<string, VisualElement> _tracks = new();
        private readonly Dictionary<int, VisualElement> _keyframeTimeMarkers = new();
        private readonly Dictionary<KeyframeId, VisualElement> _keyframeMarkers = new();

        private float _timeCursorOffset = 11.5f; // タイムカーソルのオフセット（ピクセル単位）
        private int _selectedKeyframeTimeMs = -1;

        // UI elements
        private VisualElement _root;
        private Label _titleLabel;

        private IconButton _closeButton;
        private Button _addKeyframeButton;
        private Button _saveButton;

        private IconButton _playButton;
        private IconButton _pauseButton;
        private IconButton _stopButton;

        private Label _currentTimeLabel;

        private VisualElement _tracksContentContainer;
        private VisualElement _tracksContainer;
        private VisualElement _trackHeadersContainer;

        private VisualElement _keyframeTimeTrack;
        private VisualElement _timeRuler;
        private VisualElement _timelineCursor;
        private VisualElement _tracksAreaPlayhead;

        private ScrollView _keyframeTimeScrollView;
        private ScrollView _timeRulerScrollView;
        private ScrollView _tracksScrollView;
        private ScrollView _trackHeadersScrollView;

        private ScrollView _propertyEditorPanel;

        // クリップ関連
        private float _currentZoom;
        private float _currentTime = 0f;
        // private bool _isDraggingTimeline = false; // TODO: Delete?
        // private KeyframeData _selectedKeyframe;

        private float _totalDuration = 30f;

        // New UI elements
        private IntegerField _targetIdField;
        
        // Events
        public event Action<float> OnTimeCursorMoved;
        // public event Action OnPlayButtonClicked;
        // public event Action OnPauseButtonClicked;
        // public event Action OnStopButtonClicked;

        public event Action OnAddKeyframeRequested;
        public event Action<KeyframeId> OnKeyframeDeleteRequested;
        public event Action<KeyframeId> OnKeyframeMarkerClicked;
        public event Action<KeyframeId, object> OnPropertyValueChanged;
        public event Action OnSaveRequested;
        public event Action OnPlayButtonClicked;
        public event Action OnPauseButtonClicked;
        public event Action OnStopButtonClicked;
        public event Action OnCloseButtonClicked;
        public event Action<int> OnTargetIdChanged;

        private void Awake()
        {
            _currentZoom = _pixelsPerFrame * _framesPerSecond; // Fixed
        }

        private void OnEnable()
        {
            InitializeUI();
            RegisterCallbacks();
        }
        
        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        public void UpdateTimeDisplay(float time)
        {
            if (!isActiveAndEnabled) return;

            _currentTime = time;
            _currentTimeLabel.text = $"Time: {_currentTime:F3}";

            var cursorPositionX = time * _currentZoom + _timeCursorOffset;
            _timelineCursor.transform.position = new Vector3(cursorPositionX, 0, 0);
            _tracksAreaPlayhead.transform.position = new Vector3(cursorPositionX, 0, 0);
        }

        public void Show()
        {
            Debug.Log($"<color=lime>OpenKeyframeEditor - ClipData.Name: </color>");
            _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            Debug.Log($"<color=cyan>CloseKeyframeEditor</color>");
            _uiDocument.rootVisualElement.style.display = DisplayStyle.None;

            // TODO
            ClearKeyframeMarkers();
            _keyframeTimeMarkers.Clear();
            _keyframeMarkers.Clear();
            _keyframeTimeTrack.Clear();
            // _trackHeadersContainer.Clear();
            // _tracksContainer.Clear();
            // _propertyFields.Clear();
        }

        public void ClearKeyframeMarkers()
        {
            _keyframeTimeMarkers.Clear();
            _keyframeMarkers.Clear();
            _keyframeTimeTrack.Clear();
            // TODO: Delete
            // _trackHeadersContainer.Clear();
            // _tracksContainer.Clear();
            // _propertyFields.Clear();
        }

        public void ClearPropertyEditor()
        {
            _propertyEditorPanel.Clear();
            _propertyFields.Clear();
            
            // 基本的なヘッダーだけを表示
            InitializePropertyPanel();
        }
        
        public void SetTitle(string title)
        {
            _titleLabel.text = title;
        }

        public void SetTotalDuration(float duration)
        {
            Debug.Log($"<color=cyan>SetTotalDuration - Duration: {duration}</color>");
            _totalDuration = duration;
            _timeRuler.style.width = duration * _currentZoom + _pixelsPerFrame; // _pixelsPerFrame * _framesPerSecond;
            _keyframeTimeTrack.style.width = duration * _currentZoom + _pixelsPerFrame; // _pixelsPerFrame * _framesPerSecond;
            _tracksContainer.style.width = duration * _currentZoom + _pixelsPerFrame; // _pixelsPerFrame * _framesPerSecond;
        }
        
        private void InitializeUI()
        {
            _root = _uiDocument.rootVisualElement.Q("keyframe-editor-root");
            _root.AddManipulator(new ContextualMenuManipulator());

            _titleLabel = _root.Q<Label>("Title");

            _closeButton = _root.Q<IconButton>("close-button");
            _addKeyframeButton = _root.Q<Button>("add-key-frame-button");
            _saveButton = _root.Q<Button>("save-button");

            _playButton = _root.Q<IconButton>("play-button");
            _pauseButton = _root.Q<IconButton>("pause-button");
            _stopButton = _root.Q<IconButton>("stop-button");

            _currentTimeLabel = _root.Q<Label>("time-value");

            // Initialize target ID controls
            _targetIdField = _root.Q<IntegerField>("target-id-field");

            _propertyEditorPanel = _root.Q<ScrollView>("property-editor-panel");
            _trackHeadersContainer = _root.Q("track-headers-container");
            _tracksContentContainer = _root.Q("tracks-content-container");
            _tracksContainer = _root.Q("tracks-container");
            _timeRuler = _root.Q("time-ruler");
            _timeRulerScrollView = _root.Q<ScrollView>("time-ruler-scroll-view");
            _timelineCursor = _root.Q("timeline-cursor");
            _tracksAreaPlayhead = _root.Q("tracks-area-playhead");
            _keyframeTimeTrack = _root.Q("keyframe-time-track");

            _keyframeTimeScrollView = _root.Q<ScrollView>("keyframe-time-scroll-view");
            _tracksScrollView = _root.Q<ScrollView>("tracks-scroll-view");
            _trackHeadersScrollView = _root.Q<ScrollView>("track-headers-scroll-view");
            
            // Clear sample data
            _trackHeadersContainer.Clear();
            _tracksContainer.Clear();
            _keyframeTimeTrack.Clear();

            // プロパティパネルを初期化
            InitializePropertyPanel();

            SetupScrollSynchronization();
            // EnableCursorDragging();
        }
        
        // プロパティパネルの初期化
        private void InitializePropertyPanel()
        {
            _propertyEditorPanel.Clear();
            
            var label = new Label("Property Values");
            label.AddToClassList("section-header");
            _propertyEditorPanel.Add(label);

            var separator = new VisualElement();
            separator.AddToClassList("separator");
            _propertyEditorPanel.Add(separator);
        }

        private void RegisterCallbacks()
        {
            _closeButton.clicked += OnCloseClicked;
            _addKeyframeButton.clicked += OnAddKeyframeClicked;
            _saveButton.clicked += OnSaveClicked;

            _playButton.clicked += OnPlayClicked;
            _pauseButton.clicked += OnPauseClicked;
            _stopButton.clicked += OnStopClicked;

            // TODO
            _targetIdField.RegisterValueChangedCallback(evt => 
            {
                OnTargetIdChanged?.Invoke(evt.newValue);
            });

            _timeRuler.RegisterCallback<PointerDownEvent>(OnTimeRulerPointerDown);
            _timeRuler.RegisterCallback<PointerUpEvent>(OnTimeRulerPointerUp);
            _timeRuler.RegisterCallback<PointerLeaveEvent>(OnTimeRulerPointerLeave);
            _timeRuler.RegisterCallback<PointerMoveEvent>(OnTimeRulerPointerMove);

            // Track area mouse events
            _tracksContentContainer.RegisterCallback<PointerDownEvent>(OnTimeRulerPointerDown);
            _tracksContentContainer.RegisterCallback<PointerUpEvent>(OnTimeRulerPointerUp);
            _tracksContentContainer.RegisterCallback<PointerLeaveEvent>(OnTimeRulerPointerLeave);
            _tracksContentContainer.RegisterCallback<PointerMoveEvent>(OnTimeRulerPointerMove);
        }
        
        private void UnregisterCallbacks()
        {
            _closeButton.clicked -= OnCloseClicked;
            _addKeyframeButton.clicked -= OnAddKeyframeClicked;
            _saveButton.clicked -= OnSaveClicked;

            _playButton.clicked -= OnPlayClicked;
            _pauseButton.clicked -= OnPauseClicked;
            _stopButton.clicked -= OnStopClicked;

            // TODO
            _targetIdField.UnregisterValueChangedCallback(evt => 
            {
                OnTargetIdChanged?.Invoke(evt.newValue);
            });

            _timeRuler.UnregisterCallback<PointerDownEvent>(OnTimeRulerPointerDown);
            _timeRuler.UnregisterCallback<PointerUpEvent>(OnTimeRulerPointerUp);
            _timeRuler.UnregisterCallback<PointerLeaveEvent>(OnTimeRulerPointerLeave);
            _timeRuler.UnregisterCallback<PointerMoveEvent>(OnTimeRulerPointerMove);
        }

        public void SetProperties(AnimationPropertyInfo[] properties)
        {
            Debug.Log($"<color=cyan>SetProperties - Properties: {properties.Length}</color>");

            _trackHeadersContainer.Clear();
            _tracksContainer.Clear();
            _tracks.Clear();
            _propertyFields.Clear();
            // _propertyEditorPanel.Clear();
            InitializePropertyPanel();

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];

                // Create track header
                var header = new VisualElement();
                header.AddToClassList("track-header");
                header.Add(new Label(property.Name));
                _trackHeadersContainer.Add(header);

                // Create track row
                var trackRow = new VisualElement();
                trackRow.AddToClassList("track-row");
                trackRow.style.width = _totalDuration * _currentZoom + _pixelsPerFrame; // Fixed
                trackRow.userData = property; // Store property reference
                _tracksContainer.Add(trackRow);
                _tracks[property.Name] = trackRow;

                // Create keyframe editor panel
                var field = CreatePropertyField(property, property.DefaultValue);
                if (field != null)
                {
                    var inputLabel = new InputLabel(property.Name);
                    inputLabel.direction = Direction.Horizontal;
                    inputLabel.Add(field);

                    _propertyEditorPanel.Add(inputLabel);
                    _propertyFields[property.Name] = field;
                }
            }
        }

        public void AddKeyframeTimeMarker(float time)
        {
            var keyframeId = KeyframeId.FromSeconds("TimeMarker", time);
            if (_keyframeTimeMarkers.ContainsKey(keyframeId.TimeMs))
            {
                // Debug.LogWarning($"Keyframe time marker already exists: {timeMs}");
                return;
            }

            var marker = new VisualElement();
            marker.AddToClassList("keyframe-marker");
            marker.transform.position = new Vector3(TimeToPixels(time), 0, 0);

            _keyframeTimeMarkers[keyframeId.TimeMs] = marker;
            _keyframeTimeTrack.Add(marker);

            // Register events
            marker.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return; // Only left button
                _selectedKeyframeTimeMs = keyframeId.TimeMs;
                OnKeyframeMarkerClicked?.Invoke(keyframeId); // OnKeyFrameTimeMarkerClicked?
                // evt.StopPropagation();
            });

            // Add context menu using ContextualMenuManipulator
            marker.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                // if (!CanEdit) return;
                evt.menu.AppendAction("Delete keyframes", action => 
                {
                    OnKeyframeDeleteRequested?.Invoke(keyframeId);
                });
            });
            
            // marker.RegisterCallback<ContextClickEvent>(evt => {
            //     ShowKeyframeContextMenu(keyframe);
            //     evt.StopPropagation();
            // });
        }

        public void AddKeyframeMarker(string propertyName, float time, float value)
        {
            var keyframeId = KeyframeId.FromSeconds(propertyName, time);
            if (_keyframeMarkers.ContainsKey(keyframeId))
            {
                // Debug.LogWarning($"Keyframe already exists: {keyframeId}");
                return;
            }

            var marker = new VisualElement();
            marker.AddToClassList("keyframe-marker");
            marker.transform.position = new Vector3(TimeToPixels(time), 0, 0);
            marker.userData = new KeyframeElementData(keyframeId, value);

            _keyframeMarkers[keyframeId] = marker;
            _tracks[propertyName].Add(marker);

            // Register events
            marker.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return; // Only left button

                var keyframeData = marker.userData as KeyframeElementData;
                if (keyframeData != null)
                {
                    _selectedKeyframeTimeMs = keyframeId.TimeMs;
                    Debug.Log($"<color=orange>Keyframe clicked - Property: {keyframeData.Id.PropertyName}, Time: {keyframeData.Id.TimeMs}, Value: {keyframeData.Value}</color>");
                    OnKeyframeMarkerClicked?.Invoke(keyframeId);
                }
                // evt.StopPropagation();
            });

            // Add context menu using ContextualMenuManipulator
            marker.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                // if (!CanEdit) return;
                evt.menu.AppendAction("Delete keyframe", action => 
                {
                    OnKeyframeDeleteRequested?.Invoke(keyframeId);
                });
            });
        }

        public void RemoveKeyframeMarker(KeyframeId keyframe)
        {
            if (_keyframeMarkers.Remove(keyframe, out var marker))
            {
                marker.RemoveFromHierarchy();
            }
        }
        
        public void RemoveKeyframeTimeMarker(float time)
        {
            var keyframeId = KeyframeId.FromSeconds("TimeMarker", time);
            if (_keyframeTimeMarkers.Remove(keyframeId.TimeMs, out var marker))
            {
                marker.RemoveFromHierarchy();
            }
        }

        // public void SelectKeyframe(KeyframeData keyframe, bool fireEvent = true)
        public void SelectKeyframe(bool fireEvent = true)
        {
            // // Deselect previous
            // if (_selectedKeyframe != null && _keyframeMarkers.ContainsKey(_selectedKeyframe))
            // {
            //     _keyframeMarkers[_selectedKeyframe].RemoveFromClassList("selected");
            // }
            //
            // _selectedKeyframe = keyframe;
            //
            // // Select new
            // if (_keyframeMarkers.ContainsKey(keyframe))
            // {
            //     _keyframeMarkers[keyframe].AddToClassList("selected");
            // }
            //
            // // イベント発火をフラグで制御
            // if (fireEvent)
            // {
            //     OnKeyframeSelected?.Invoke(keyframe);
            // }
        }

        /// <summary>
        /// すべてのキーフレームの選択を解除
        /// </summary>
        public void DeselectKeyframes()
        {
            // if (_selectedKeyframe != null && _keyframeMarkers.ContainsKey(_selectedKeyframe))
            // {
            //     _keyframeMarkers[_selectedKeyframe].RemoveFromClassList("selected");
            // }
            // _selectedKeyframe = null;
        }

        // *******************
        // Reviewed
        // *******************

        public void UpdatePropertyValues(IReadOnlyList<(string PropertyName, Keyframe? Keyframe)> keyframes, bool isEditable)
        {
            for (var i = 0; i < keyframes.Count; i++) // NOTE: Avoid allocation
            {
                var (propertyName, data) = keyframes[i];
                if (data == null) continue;

                var keyframe = data.Value;

                if (_propertyFields.TryGetValue(propertyName, out var field))
                {
                    UpdateFieldValue(field, keyframe.Value);
                    SetFieldEditable(field, isEditable);
                }
            }
        }

        public void UpdatePropertyValues(IReadOnlyList<(string Name, float Value)> properties, bool isEditable)
        {
            if (!isActiveAndEnabled) return;

            for (var i = 0; i < properties.Count; i++) // NOTE: Avoid allocation
            {
                var property = properties[i];
                if (_propertyFields.TryGetValue(property.Name, out var field))
                {
                    UpdateFieldValue(field, property.Value);
                    SetFieldEditable(field, isEditable);
                }
            }
        }

        private void UpdateFieldValue(VisualElement field, float value)
        {
            if (field is FloatField floatField)
            {
                floatField.SetValueWithoutNotify(Convert.ToSingle(value));
            }
            else if (field is IntegerField intField)
            {
                intField.SetValueWithoutNotify(Convert.ToInt32(value));
            }
            else if (field is Toggle toggle)
            {
                toggle.SetValueWithoutNotify(Convert.ToBoolean(value));
            }
        }

        private void SetFieldEditable(VisualElement field, bool editable)
        {
            if (field is BaseField<float> floatField)
            {
                floatField.SetEnabled(editable);
            }
            else if (field is BaseField<int> intField)
            {
                intField.SetEnabled(editable);
            }
            else if (field is BaseField<bool> boolField)
            {
                boolField.SetEnabled(editable);
            }
        }

        // *******************

        // KeyframePropertyEditorView
        private VisualElement CreatePropertyField(AnimationPropertyInfo property, object value)
        {
            // if (property.PropertyType == typeof(float))
            {
                var floatField = new FloatField() { value = Convert.ToSingle(value) };
                floatField.RegisterValueChangedCallback(evt => {
                    var keyframeId = new KeyframeId(property.Name, _selectedKeyframeTimeMs);
                    if (keyframeId.TimeMs == -1) return; // No keyframe selected
                    OnPropertyValueChanged?.Invoke(keyframeId, evt.newValue);
                });
                return floatField;
            }
            // else if (property.PropertyType == typeof(int))
            // {
            //     var intField = new IntegerField() { value = Convert.ToInt32(value) };
            //     intField.RegisterValueChangedCallback(evt => {
            //         var keyframeId = new KeyframeId(property.Name, _selectedKeyframeTimeMs);
            //         if (keyframeId.TimeMs == -1) return; // No keyframe selected
            //         OnPropertyValueChanged?.Invoke(keyframeId, evt.newValue);
            //     });
            //     return intField;
            // }
            // else if (property.PropertyType == typeof(bool))
            // {
            //     var boolField = new Toggle() { value = Convert.ToBoolean(value) };
            //     boolField.RegisterValueChangedCallback(evt => {
            //         var keyframeId = new KeyframeId(property.Name, _selectedKeyframeTimeMs);
            //         if (keyframeId.TimeMs == -1) return; // No keyframe selected
            //         OnPropertyValueChanged?.Invoke(keyframeId, evt.newValue);
            //     });
            //     return boolField;
            // }

            return null;
        }
        
        private float TimeToPixels(float time)
        {
            return time * _currentZoom; // Fixed
        }
        
        private float PixelsToTime(float pixels)
        {
            return pixels / _currentZoom; // Fixed
        }

        // Event handlers
        private void OnCloseClicked()
        {
            OnCloseButtonClicked?.Invoke();
        }
        
        private void OnPlayClicked()
        {
            OnPlayButtonClicked?.Invoke();
        }
        
        private void OnPauseClicked()
        {
            OnPauseButtonClicked?.Invoke();
        }
        
        private void OnStopClicked()
        {
            _currentTime = 0f;
            OnTimeCursorMoved?.Invoke(_currentTime);
            OnStopButtonClicked?.Invoke();
        }
        
        private void OnAddKeyframeClicked()
        {
            OnAddKeyframeRequested?.Invoke();
        }

        private void OnSaveClicked()
        {
            OnSaveRequested?.Invoke();
        }
        
        // ********************
        // WIP
        // ********************
        bool _isDraggingTimeRuler = false;

        private void OnTimeRulerPointerDown(PointerDownEvent evt)
        {
            Debug.Log($"<color=orange>OnTimeRulerPointerDown - Position: {evt.localPosition}</color>");
            _isDraggingTimeRuler = true;
            SetTimeFromPointerPosition(evt.localPosition);
        }
        
        private void OnTimeRulerPointerUp(PointerUpEvent evt)
        {
            Debug.Log($"<color=magenta>OnTimeRulerPointerUp - Position: {evt.localPosition}</color>");
            _isDraggingTimeRuler = false;
        }

        private void OnTimeRulerPointerMove(PointerMoveEvent evt)
        {
            // Debug.Log($"<color=cyan>OnTimeRulerPointerMove - Position: {evt.localPosition}, _isDraggingTimeRuler: {_isDraggingTimeRuler}</color>");
            if (_isDraggingTimeRuler)
            {
                SetTimeFromPointerPosition(evt.localPosition);
            }
        }

        private void OnTimeRulerPointerLeave(PointerLeaveEvent evt)
        {
            Debug.Log($"<color=purple>OnTimeRulerPointerLeave - Position: {evt.localPosition}</color>");
            _isDraggingTimeRuler = false;
        }

        // private void OnTracAreaPointerDown(PointerDownEvent evt)
        // {
        //     Debug.Log($"<color=yellow>OnTracAreaPointerDown - Position: {evt.localPosition}</color>");
        //     _isDraggingTimeRuler = true;
        //     SetTimeFromPointerPosition(evt.localPosition);
        //     // evt.StopPropagation();
        // }

        // private void OnTracAreaPointerUp(PointerUpEvent evt)
        // {
        //     Debug.Log($"<color=green>OnTracAreaPointerUp - Position: {evt.localPosition}</color>");
        //     _isDraggingTimeRuler = false;
        //     // evt.StopPropagation();
        // }

        // private void OnTracAreaPointerMove(PointerMoveEvent evt)
        // {
        //     if (_isDraggingTimeRuler)
        //     {
        //         SetTimeFromPointerPosition(evt.localPosition);
        //     }
        // }

        // private void OnTracAreaPointerLeave(PointerLeaveEvent evt)
        // {
        //     Debug.Log($"<color=red>OnTracAreaPointerLeave - Position: {evt.localPosition}, Phase: {evt.propagationPhase}</color>");
        //     _isDraggingTimeRuler = false;
        // }

        // private void OnTrackAreaMouseDown(MouseDownEvent evt)
        // {
        //     if (evt.button == 0) // Left button
        //     {
        //         _isDraggingTimeRuler = true;
        //         var localPosition = _tracksContainer.WorldToLocal(evt.mousePosition);
        //         SetTimeFromPointerPosition(localPosition);
        //     }
        // }

        // private void OnTrackAreaMouseMove(MouseMoveEvent evt)
        // {
        //     if (_isDraggingTimeRuler)
        //     {
        //         var localPosition = _tracksContainer.WorldToLocal(evt.mousePosition);
        //         SetTimeFromPointerPosition(localPosition);
        //     }
        // }

        // private void OnTrackAreaMouseUp(MouseUpEvent evt)
        // {
        //     _isDraggingTimeRuler = false;
        // }

        private void SetTimeFromPointerPosition(Vector2 pointerPosition)
        {
            // Convert mouse X position to time
            var time = Mathf.Clamp((pointerPosition.x - _timeCursorOffset) / _currentZoom, 0, _totalDuration);

            // Notify time change through event
            OnTimeCursorMoved?.Invoke(time);

            // Directly update local display
            UpdateTimeDisplay(time);
        }

        // TODO: Delete
        // private void UpdateTimeFromMousePosition(Vector2 mousePosition)
        // {
        //     Vector2 localPos = _tracksContainer.WorldToLocal(mousePosition);
        //     _currentTime = PixelsToTime(localPos.x);
        //     _currentTime = Mathf.Max(0, _currentTime); // Clamp to positive

        //     // if (_clipDataAdapter != null)
        //     // {
        //     //     _currentTime = Mathf.Min(_currentTime, _clipDataAdapter.Duration); // Clamp to duration
        //     // }

        //     // 時間変更イベントを発火
        //     OnTimeCursorMoved?.Invoke(_currentTime);
        // }

        // private void ShowKeyframeContextMenu(KeyframeData keyframe)
        // {
        //     Debug.Log($"<color=orange>********** ShowKeyframeContextMenu **********</color>");
        //     // var menu = new GenericMenu();
        //     // menu.AddItem(new GUIContent("Delete Keyframe"), false, () => {
        //     //     OnKeyframeDeleteRequested?.Invoke(keyframe);
        //     // });
        //     // menu.ShowAsContext();
        // }

        // ********************
        // WIP
        // ********************

        private void SetupScrollSynchronization()
        {
            _trackHeadersScrollView.verticalScroller.valueChanged += (value) =>
            {
                // When track headers scroll view is scrolled vertically, sync tracks content scroll view
                if (Mathf.Abs(_tracksScrollView.scrollOffset.y - value) > 0.01f)
                {
                    _tracksScrollView.scrollOffset = new Vector2(_tracksScrollView.scrollOffset.x, value);
                }
            };

            // Synchronize horizontal scrolling between time scroll view and tracks content
            _timeRulerScrollView.horizontalScroller.valueChanged += (value) =>
            {
                // When time ruler scroll view is scrolled horizontally, sync tracks scroll view
                if (Mathf.Abs(_tracksScrollView.scrollOffset.x - value) > 0.01f)
                {
                    _tracksScrollView.scrollOffset = new Vector2(value, _tracksScrollView.scrollOffset.y);
                }

                if (Mathf.Abs(_keyframeTimeScrollView.scrollOffset.x - value) > 0.01f)
                {
                    _keyframeTimeScrollView.scrollOffset = new Vector2(value, _keyframeTimeScrollView.scrollOffset.y);
                }
            };

            _keyframeTimeScrollView.horizontalScroller.valueChanged += (value) =>
            {
                // When keyframe time scroll view is scrolled horizontally, sync tracks scroll view
                if (Mathf.Abs(_tracksScrollView.scrollOffset.x - value) > 0.01f)
                {
                    _tracksScrollView.scrollOffset = new Vector2(value, _tracksScrollView.scrollOffset.y);
                }

                if (Mathf.Abs(_timeRulerScrollView.scrollOffset.x - value) > 0.01f)
                {
                    _timeRulerScrollView.scrollOffset = new Vector2(value, _timeRulerScrollView.scrollOffset.y);
                }
            };

            _tracksScrollView.horizontalScroller.valueChanged += (value) =>
            {
                // When tracks scroll view is scrolled horizontally, sync track headers scroll view
                if (Mathf.Abs(_timeRulerScrollView.scrollOffset.x - value) > 0.01f)
                {
                    _timeRulerScrollView.scrollOffset = new Vector2(value, _timeRulerScrollView.scrollOffset.y);
                }

                if (Mathf.Abs(_keyframeTimeScrollView.scrollOffset.x - value) > 0.01f)
                {
                    _keyframeTimeScrollView.scrollOffset = new Vector2(value, _keyframeTimeScrollView.scrollOffset.y);
                }
            };
        }

        bool _isDragging = false;

        // TODO: Delete?
        /// <summary>
        /// カーソルのドラッグ操作を設定
        /// </summary>
        private void EnableCursorDragging()
        {
            if (_timelineCursor == null) return;
            
            // カーソルヘッドのドラッグ操作
            var cursorHead = _timelineCursor.Q(null, "timeline-cursor-head");
            if (cursorHead != null)
            {
                // カーソルヘッド上でのマウスダウン時にドラッグを開始
                cursorHead.RegisterCallback<MouseDownEvent>(evt => 
                {
                    _isDragging = true;
                    evt.StopPropagation();
                });
                
                // カーソルヘッド上でマウス移動中のドラッグ処理
                cursorHead.RegisterCallback<MouseMoveEvent>(evt => 
                {
                    if (_isDragging && evt.pressedButtons == 1)
                    {
                        // カーソルの親要素（ルーラー）内での相対位置からタイム値を算出
                        Vector2 localPos = _timeRuler.WorldToLocal(evt.mousePosition);
                        SetTimeFromMousePosition(localPos);
                        evt.StopPropagation();
                    }
                });
                
                // カーソルヘッド上でマウスアップ時のドラッグ終了
                cursorHead.RegisterCallback<MouseUpEvent>(evt => 
                {
                    _isDragging = false;
                    evt.StopPropagation();
                });
            }
            
            // タイムカーソル線のドラッグ操作
            var cursorLine = _timelineCursor.Q(null, "timeline-cursor-line");
            if (cursorLine != null)
            {
                cursorLine.RegisterCallback<MouseDownEvent>(evt => 
                {
                    _isDragging = true;
                    evt.StopPropagation();
                });
            }
            
            // グローバルなマウスイベントを登録（ドラッグ中にカーソル外に出た場合でも継続するため）
            RegisterCursorDragGlobalEvents();
        }
        
        // TODO: Delete?
        /// <summary>
        /// カーソルドラッグ用のグローバルイベントを登録
        /// </summary>
        private void RegisterCursorDragGlobalEvents()
        {
            if (_root == null) return;

            // ドキュメント全体でのマウス移動を監視
            _root.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (_isDragging && evt.pressedButtons == 1)
                {
                    // ドキュメント内のどの位置でもカーソル位置を更新
                    Vector2 localPos = _timeRuler.WorldToLocal(evt.mousePosition);
                    SetTimeFromMousePosition(localPos);
                }
            });

            // ドキュメント全体でのマウスアップを監視
            _root.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (_isDragging)
                {
                    _isDragging = false;
                }
            });

            // マウスキャプチャを失った場合の処理（UIの外にマウスが出た場合など）
            _root.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (_isDragging)
                {
                    // ドラッグ継続中も一時的に追跡できなくなる可能性があるので、
                    // すぐにフラグをクリアするのではなく、マウス状態を確認
                    if (evt.pressedButtons == 0)
                    {
                        _isDragging = false;
                    }
                }
            });
        }

        private void SetTimeFromMousePosition(Vector2 mousePosition)
        {
            // Convert mouse X position to time
            float time = Mathf.Clamp(mousePosition.x / _currentZoom, 0, _totalDuration);
            Debug.Log($"<color=orange>SetTimeFromMousePosition - Time: {time}</color>");
        }

        // ********************

        // Set the target ID field value without triggering events
        public void SetTargetId(int targetId)
        {
            if (_targetIdField != null)
            {
                _targetIdField.SetValueWithoutNotify(targetId);
            }
        }
    }
}
