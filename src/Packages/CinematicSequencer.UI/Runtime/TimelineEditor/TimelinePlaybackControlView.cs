using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// Handles the timeline playback control UI elements and functionality.
    /// This class manages play/pause/stop buttons and time cursor interaction.
    /// </summary>
    public sealed class TimelinePlaybackControlView : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        
        [Header("Timeline Settings")]
        [SerializeField] private float _pixelsPerSecond = 100f;
        [SerializeField] private float _defaultDuration = 30f;
        
        // UI Element references
        private VisualElement _root;
        private IconButton _playButton;
        private IconButton _pauseButton;
        private IconButton _stopButton;
        private Label _timeDisplay;

        private VisualElement _timeRuler;
        private ScrollView _timeRulerScrollView;
        private VisualElement _timelineCursor;
        
        // Reference to tracks container for cursor
        private VisualElement _tracksContainer;
        private ScrollView _tracksScrollView;
        
        // State tracking
        private bool _isDragging = false;
        private bool _isPlaying = false;
        private float _currentTime = 0f;
        private float _totalDuration = 30f;
        private float _currentZoom = 100f;
        private bool _initialized = false;
        
        // Events
        public event Action OnPlayButtonClicked;
        public event Action OnPauseButtonClicked;
        public event Action OnStopButtonClicked;
        public event Action<float> OnTimeChanged;
        public event Action<float> OnZoomChanged;

        #region MonoBehaviour Callbacks

        private void OnDestroy()
        {
            _playButton.clicked -= HandlePlayButtonClickedEvent;
            _pauseButton.clicked -= HandlePauseButtonClickedEvent;
            _stopButton.clicked -= HandleStopButtonClickedEvent;
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            // TODO
            // // Unregister callbacks
            // _timeRuler.UnregisterCallback<MouseDownEvent>(OnTimeRulerMouseDown);
            // _timeRuler.UnregisterCallback<MouseUpEvent>(OnTimeRulerMouseUp);
            // _timeRuler.UnregisterCallback<MouseLeaveEvent>(OnTimeRulerMouseLeave);
            // _timeRuler.UnregisterCallback<MouseMoveEvent>(OnTimeRulerMouseMove);
            // _playButton.clicked -= HandlePlayButtonClickedEvent;
            // _pauseButton.clicked -= HandlePauseButtonClickedEvent;
            // _stopButton.clicked -= HandleStopButtonClickedEvent;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the total duration of the timeline
        /// </summary>
        public void SetDuration(float duration)
        {
            _totalDuration = Mathf.Max(_defaultDuration, duration);
            GenerateTimeRuler();
        }
        
        /// <summary>
        /// Sets the zoom level for the timeline
        /// </summary>
        public void SetZoom(float zoom)
        {
            _currentZoom = zoom;
            GenerateTimeRuler();
            UpdateTimeDisplay(_currentTime);
            
            // Notify zoom change to presenter
            OnZoomChanged?.Invoke(zoom);
        }
        
        /// <summary>
        /// Update the time display and cursor positions
        /// </summary>
        public void UpdateTimeDisplay(float time)
        {
            _currentTime = time;
            
            if (_timeDisplay != null)
            {
                _timeDisplay.text = FormatTime(time);
            }
            
            // 絶対位置を計算（ズームレベルを考慮）
            float cursorPosition = time * _currentZoom;
            
            // ルーラーのタイムカーソルを更新
            if (_timelineCursor != null)
            {
                _timelineCursor.style.left = cursorPosition;
            }
            else if (_timeRuler != null)
            {
                // カーソルが存在しなければ作成する
                CreateRulerCursor();
            }
            
            // トラックエリアのカーソルを更新
            var trackAreaCursor = _root.Q("track-area-cursor");
            if (trackAreaCursor != null)
            {
                trackAreaCursor.style.left = cursorPosition;
            }

            // カーソルがビュー外に出ないように表示領域を調整
            EnsureCursorVisible();
        }

        #endregion

        #region Private Methods

        private void Initialize()
        {
            if (_initialized) return;

            _root = _uiDocument.rootVisualElement.Q(UIElementNames.SequenceEditorRootName);
            _tracksContainer = _root.Q(UIElementNames.TracksContainerName);
            _tracksScrollView = _root.Q<ScrollView>(UIElementNames.TracksScrollViewName);

            // Get references to important UI elements
            _playButton = _root.Q<IconButton>("play-button");
            _pauseButton = _root.Q<IconButton>("pause-button");
            _stopButton = _root.Q<IconButton>("stop-button");
            _timeRuler = _root.Q("time-ruler");
            _timeRulerScrollView = _root.Q<ScrollView>("time-ruler-scroll-view");
            _timeDisplay = _root.Q<Label>("time-display");

            // Setup event handlers
            _playButton.clicked += HandlePlayButtonClickedEvent;
            _pauseButton.clicked += HandlePauseButtonClickedEvent;
            _stopButton.clicked += HandleStopButtonClickedEvent;

            // Set up time ruler interaction
            if (_timeRuler != null)
            {
                _timeRuler.RegisterCallback<MouseDownEvent>(OnTimeRulerMouseDown);
                _timeRuler.RegisterCallback<MouseUpEvent>(OnTimeRulerMouseUp);
                _timeRuler.RegisterCallback<MouseLeaveEvent>(OnTimeRulerMouseLeave);
                _timeRuler.RegisterCallback<MouseMoveEvent>(OnTimeRulerMouseMove);

                // Generate time ruler
                GenerateTimeRuler();

                // Create timeline cursor
                CreateTimelineCursor();

                SetupScrollSynchronization();
            }

            _initialized = true;
        }

        /// <summary>
        /// Generate time ruler with major and minor tick marks
        /// </summary>
        private void GenerateTimeRuler()
        {
            if (_timeRuler == null) return;

            // カーソルのリファレンスを保存
            var timelineCursor = _timeRuler.Q(null, "timeline-cursor");
            bool cursorExists = timelineCursor != null;

            _timeRuler.Clear();

            // Set the ruler width based on timeline duration and zoom level
            _timeRuler.style.width = _totalDuration * _currentZoom;

            // Add major and minor tick marks
            float majorTickInterval = 1.0f;
            float minorTickInterval = 0.25f;

            // For very zoomed out views, adjust the intervals
            if (_currentZoom < 80)
            {
                majorTickInterval = 5.0f;
                minorTickInterval = 1.0f;
            }

            for (float time = 0; time <= _totalDuration; time += minorTickInterval)
            {
                bool isMajor = Mathf.Approximately(time % majorTickInterval, 0f);

                // Create tick mark
                VisualElement tick = new VisualElement();
                tick.AddToClassList("ruler-mark");
                if (isMajor)
                {
                    tick.AddToClassList("major");

                    // Create label for major ticks
                    Label timeLabel = new Label(FormatTime(time));
                    timeLabel.AddToClassList("ruler-label");
                    timeLabel.style.left = time * _currentZoom - 10;
                    _timeRuler.Add(timeLabel);
                }

                tick.style.left = time * _currentZoom;
                _timeRuler.Add(tick);
            }

            // カーソルが既に存在した場合は再作成し、そうでなければ新規作成
            if (cursorExists)
            {
                CreateRulerCursor();
                UpdateTimeDisplay(_currentTime); // カーソル位置を現在の時間に合わせる
            }
            else if (_timelineCursor == null)
            {
                CreateTimelineCursor();
            }
        }
        
        /// <summary>
        /// Creates the timeline playhead cursor
        /// </summary>
        private void CreateTimelineCursor()
        {
            // ルーラー用のカーソルを作成
            CreateRulerCursor();
            
            // トラック領域用のカーソルを作成
            // CreateTrackAreaCursor();
            
            // カーソルのドラッグ操作を有効化
            // EnableCursorDragging(); // WIP
        }
        
        /// <summary>
        /// ルーラー用のカーソルを作成
        /// </summary>
        private void CreateRulerCursor()
        {
            if (_timeRuler == null) return;
            
            // ルーラー用のカーソル
            _timelineCursor = new VisualElement();
            _timelineCursor.AddToClassList("timeline-cursor");
            _timelineCursor.name = "timeline-cursor"; // 名前を設定してクエリを容易に
            
            // カーソルヘッド（上部の円形部分）を追加
            var cursorHead = new VisualElement();
            cursorHead.AddToClassList("timeline-cursor-head");
            _timelineCursor.Add(cursorHead);
            
            // カーソルライン（縦線部分）
            var cursorLine = new VisualElement();
            cursorLine.AddToClassList("timeline-cursor-line");
            _timelineCursor.Add(cursorLine);
            
            _timelineCursor.style.left = _currentTime * _currentZoom;
            _timeRuler.Add(_timelineCursor);
        }
        
        // /// <summary>
        // /// トラック領域用のカーソルを作成
        // /// </summary>
        // private void CreateTrackAreaCursor()
        // {
        //     if (_tracksContainer == null) return;
            
        //     // トラック領域用のカーソル
        //     var trackAreaCursor = new VisualElement();
        //     trackAreaCursor.AddToClassList("timeline-cursor");
        //     trackAreaCursor.name = "track-area-cursor"; // 名前を設定してクエリを容易に
            
        //     // トラック領域用のカーソル線
        //     var trackCursorLine = new VisualElement();
        //     trackCursorLine.AddToClassList("timeline-cursor-line");
        //     trackAreaCursor.Add(trackCursorLine);
            
        //     trackAreaCursor.style.left = _currentTime * _currentZoom;
        //     _tracksContainer.Add(trackAreaCursor);
        // }
        
        /// <summary>
        /// カーソルが見えるようにスクロール位置を調整
        /// </summary>
        private void EnsureCursorVisible()
        {
            if (_tracksScrollView == null) return;
            
            // カーソル位置
            float cursorPos = _currentTime * _currentZoom;
            
            // 現在の水平スクロール位置
            float scrollLeft = _tracksScrollView.scrollOffset.x;
            float viewportWidth = _tracksScrollView.contentViewport.resolvedStyle.width;
            
            // カーソルが左に隠れる場合
            if (cursorPos < scrollLeft + 20)
            {
                _tracksScrollView.scrollOffset = new Vector2(Mathf.Max(0, cursorPos - 20), _tracksScrollView.scrollOffset.y);
            }
            // カーソルが右に隠れる場合
            else if (cursorPos > scrollLeft + viewportWidth - 20)
            {
                _tracksScrollView.scrollOffset = new Vector2(cursorPos - viewportWidth + 20, _tracksScrollView.scrollOffset.y);
            }
        }

        private void SetupScrollSynchronization()
        {
            if (_timeRulerScrollView == null || _tracksScrollView == null) return;

            // Synchronize horizontal scrolling between track headers and tracks content
            _timeRulerScrollView.horizontalScroller.valueChanged += (value) =>
            {
                // When track headers scrollview is scrolled horizontally, sync tracks scrollview
                if (Mathf.Abs(_tracksScrollView.scrollOffset.x - value) > 0.01f)
                {
                    _tracksScrollView.scrollOffset = new Vector2(value, _tracksScrollView.scrollOffset.y);
                }
            };

            _tracksScrollView.horizontalScroller.valueChanged += (value) =>
            {
                // When tracks scrollview is scrolled horizontally, sync track headers scrollview
                if (Mathf.Abs(_timeRulerScrollView.scrollOffset.x - value) > 0.01f)
                {
                    _timeRulerScrollView.scrollOffset = new Vector2(value, _timeRulerScrollView.scrollOffset.y);
                }
            };
        }
        
        /// <summary>
        /// Formats a time value (in seconds) into a readable string
        /// </summary>
        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000) % 1000);
            
            return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        }

        #endregion

        #region Event Handlers

        private void HandlePlayButtonClickedEvent()
        {
            OnPlayButtonClicked?.Invoke();
        }
        
        private void HandlePauseButtonClickedEvent()
        {
            OnPauseButtonClicked?.Invoke();
        }
        
        private void HandleStopButtonClickedEvent()
        {
            OnStopButtonClicked?.Invoke();
        }
        
        private void OnTimeRulerMouseDown(MouseDownEvent evt)
        {
            _isDragging = true;
            SetTimeFromMousePosition(evt.localMousePosition);
        }
        
        private void OnTimeRulerMouseUp(MouseUpEvent evt)
        {
            _isDragging = false;
        }

        private void OnTimeRulerMouseLeave(MouseLeaveEvent evt)
        {
            _isDragging = false;
        }
        
        private void OnTimeRulerMouseMove(MouseMoveEvent evt)
        {
            if (_isDragging)
            {
                SetTimeFromMousePosition(evt.localMousePosition);
            }
        }
        
        private void SetTimeFromMousePosition(Vector2 mousePosition)
        {
            // Convert mouse X position to time
            float time = Mathf.Clamp(mousePosition.x / _currentZoom, 0, _totalDuration);
            
            // Notify time change through event
            OnTimeChanged?.Invoke(time);
            
            // Directly update local display
            UpdateTimeDisplay(time);
        }
        
        // Methods that can be called by the presenter
        public void OnTimelineStart()
        {
            _isPlaying = true;
        }
        
        public void OnTimelinePause()
        {
            _isPlaying = false;
        }
        
        public void OnTimelineStop()
        {
            _isPlaying = false;
            UpdateTimeDisplay(0f);
        }
        
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
        
        #endregion
    }
}
