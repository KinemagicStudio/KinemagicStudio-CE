using System;
using System.Threading;
using CinematicSequencer.Animation;
using CinematicSequencer.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Profiler = UnityEngine.Profiling.Profiler;

namespace CinematicSequencer.UI
{
    public sealed class TimelinePresenter : IDisposable
    {
        private readonly ITimelineRepository _timelineRepository;
        private readonly IClipDataRepository _clipDataRepository;
        private readonly KeyframeAnimationEditor _keyframeAnimationEditor;

        private readonly SaveConfirmationDialogView _saveConfirmationDialogView;
        private readonly SceneView _sceneView;
        private readonly TimelineEditorView _timelineEditorView;
        private readonly TimelinePlaybackControlView _timelinePlaybackControlView;
        private readonly TimelineTrackEditorView _timelineTrackEditorView;
        private readonly CinematicSequenceLibraryView _cinematicSequenceLibraryView;

        private readonly CancellationTokenSource _cts = new();
        private readonly TimelinePlayer _player;
        private Timeline _timeline;

        private float _defaultDuration = 30f;
        private float _totalDuration;
        private float _currentZoom = 100f;
        private TimelineClip _selectedClip;

        private bool _hasUnsavedChanges = false;
        private CinematicSequenceDataInfo _pendingTimelineToLoad = null;

        public TimelinePresenter(
            TimelinePlayer player,
            ITimelineRepository timelineRepository,
            IClipDataRepository clipDataRepository,
            KeyframeAnimationEditor keyframeAnimationEditor,
            TimelineEditorView timelineEditorView,
            CinematicSequenceLibraryView cinematicSequenceLibraryView,
            TimelinePlaybackControlView timelinePlaybackControlView,
            TimelineTrackEditorView timelineTrackEditorView)
        {
            _player = player;
            _timelineRepository = timelineRepository;
            _clipDataRepository = clipDataRepository;
            _keyframeAnimationEditor = keyframeAnimationEditor;
            _timelineEditorView = timelineEditorView;
            _cinematicSequenceLibraryView = cinematicSequenceLibraryView;
            _timelinePlaybackControlView = timelinePlaybackControlView;
            _timelineTrackEditorView = timelineTrackEditorView;
            _saveConfirmationDialogView = _timelineEditorView.SaveConfirmationDialogView;
            _sceneView = _timelineEditorView.SceneView;

            // Connect player events to presenter methods
            _player.OnTimelineStart += HandleTimelineStart;
            _player.OnTimelinePause += HandleTimelinePause;
            _player.OnTimelineStop += HandleTimelineStop;
            _player.OnTimeUpdate += HandleTimeUpdate;
            _player.OnTimelineComplete += HandleTimelineComplete;
            _player.OnAnimationEvaluate += OnAnimationEvaluated;

            // Connect view events to presenter methods
            _timelinePlaybackControlView.OnPlayButtonClicked += HandlePlayButtonClickedEvent;
            _timelinePlaybackControlView.OnPauseButtonClicked += HandlePauseButtonClickedEvent;
            _timelinePlaybackControlView.OnStopButtonClicked += HandleStopButtonClickedEvent;
            _timelinePlaybackControlView.OnTimeChanged += HandleTimeChanged;
            
            // Subscribe to editor view events
            _timelineEditorView.OnAddCameraTrackRequest += HandleAddCameraTrackRequest;
            _timelineEditorView.OnAddLightTrackRequest += HandleAddLightTrackRequest;
            _timelineEditorView.OnEditKeyframeRequest += HandleEditKeyframeRequest;
            _timelineEditorView.OnClipMoved += HandleClipMoved;
            _timelineEditorView.OnTimelineNameChanged += HandleTimelineNameChanged;
            _timelineEditorView.OnSaveTimelineRequest += HandleSaveTimelineRequest;
            _timelineEditorView.OnClipDeleteRequest += HandleClipDeleteRequest;

            // Subscribe to track editor view events
            _timelineTrackEditorView.OnAddTrackRequest += HandleAddTrackRequest;
            _timelineTrackEditorView.OnTrackRemoveRequested += HandleRemoveTrackRequest;
            
            // Subscribe to library view events
            _cinematicSequenceLibraryView.OnItemDropped += HandleLibraryItemDropped;
            _cinematicSequenceLibraryView.OnTimelineSelected += HandleTimelineSelected;
            _cinematicSequenceLibraryView.OnEditClipDataRequest += HandleEditKeyframeRequest2;
            _cinematicSequenceLibraryView.OnCreateNewTimelineRequest += HandleCreateNewTimelineRequest;
            _cinematicSequenceLibraryView.OnCreateNewClipDataRequest += HandleCreateNewClipDataRequest;
            _cinematicSequenceLibraryView.OnRefreshLibraryRequest += HandleRefreshLibraryRequest;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            
            // Disconnect view events
            _timelinePlaybackControlView.OnPlayButtonClicked -= HandlePlayButtonClickedEvent;
            _timelinePlaybackControlView.OnPauseButtonClicked -= HandlePauseButtonClickedEvent;
            _timelinePlaybackControlView.OnStopButtonClicked -= HandleStopButtonClickedEvent;
            _timelinePlaybackControlView.OnTimeChanged -= HandleTimeChanged;
            
            _timelineEditorView.OnAddCameraTrackRequest -= HandleAddCameraTrackRequest;
            _timelineEditorView.OnAddLightTrackRequest -= HandleAddLightTrackRequest;
            _timelineEditorView.OnEditKeyframeRequest -= HandleEditKeyframeRequest;
            _timelineEditorView.OnTimelineNameChanged -= HandleTimelineNameChanged;
            _timelineEditorView.OnSaveTimelineRequest -= HandleSaveTimelineRequest;
            _timelineEditorView.OnClipMoved -= HandleClipMoved;
            _timelineEditorView.OnClipDeleteRequest -= HandleClipDeleteRequest;

            // Disconnect track editor view events
            _timelineTrackEditorView.OnAddTrackRequest -= HandleAddTrackRequest;
            _timelineTrackEditorView.OnTrackRemoveRequested -= HandleRemoveTrackRequest;
            
            // Disconnect library view events
            _cinematicSequenceLibraryView.OnItemDropped -= HandleLibraryItemDropped;
            _cinematicSequenceLibraryView.OnTimelineSelected -= HandleTimelineSelected;
            _cinematicSequenceLibraryView.OnEditClipDataRequest -= HandleEditKeyframeRequest2;
            _cinematicSequenceLibraryView.OnCreateNewTimelineRequest -= HandleCreateNewTimelineRequest;
            _cinematicSequenceLibraryView.OnCreateNewClipDataRequest -= HandleCreateNewClipDataRequest;
            _cinematicSequenceLibraryView.OnRefreshLibraryRequest -= HandleRefreshLibraryRequest;
            
            // Disconnect player events
            _player.OnTimelineStart -= HandleTimelineStart;
            _player.OnTimelinePause -= HandleTimelinePause;
            _player.OnTimelineStop -= HandleTimelineStop;
            _player.OnTimeUpdate -= HandleTimeUpdate;
            _player.OnTimelineComplete -= HandleTimelineComplete;
            _player.OnAnimationEvaluate -= OnAnimationEvaluated;
        }

        public void Initialize()
        {
            Debug.Log($"<color=cyan>[TimelinePresenter] Initialize</color>");
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            // Load timeline data
            Debug.Log($"<color=cyan>GetKeyListAsync @Thread[{Environment.CurrentManagedThreadId}]</color>");
            var sequenceDataInfoList = await _timelineRepository.GetSequenceDataInfoListAsync(_cts.Token);
            
            Debug.Log($"<color=cyan>Timeline Data Count: {sequenceDataInfoList.Count} @Thread[{Environment.CurrentManagedThreadId}]</color>");
            
            // Load clip data
            UnityEngine.Debug.Log("------------------------------");
            var clipDataInfoList = await _clipDataRepository.GetClipDataInfoListAsync(_cts.Token);
            
            UnityEngine.Debug.Log($"ClipDataInfoList Count: {clipDataInfoList.Count}");
            foreach (var clipDataInfo in clipDataInfoList)
            {
                UnityEngine.Debug.Log($"ClipDataInfo - Id: {clipDataInfo.Id}, Name: {clipDataInfo.Name}, Type: {clipDataInfo.Type}");
            }
            UnityEngine.Debug.Log("------------------------------");

            // Initialize and configure library view
            await UniTask.WaitUntil(() => _cinematicSequenceLibraryView.IsInitialized);
            _cinematicSequenceLibraryView.SetTimelineItems(sequenceDataInfoList);
            _cinematicSequenceLibraryView.SetLibraryItems(clipDataInfoList);
        }

        private void SetTimeline(Timeline timeline)
        {
            _timeline = timeline;
            _player.Sequence = timeline;
            
            // Update total duration
            _totalDuration = Mathf.Max(_defaultDuration, _timeline.GetDuration());
            
            // Update the views
            _timelinePlaybackControlView.SetDuration(_totalDuration);
            _timelineEditorView.UpdateTimelineUI(_timeline);
        }

        // タイムラインを読み込むメソッド
        public async UniTask LoadTimelineAsync(string timelineId)
        {
            try
            {
                Debug.Log($"<color=cyan>[TimelinePresenter] Loading timeline: {timelineId}</color>");
                
                // タイムラインデータを読み込む
                _timeline = await _timelineRepository.LoadAsync(timelineId, _cts.Token);
                
                // クリップデータを読み込む
                foreach (var track in _timeline.Tracks)
                {
                    foreach (var clip in track.Clips)
                    {
                        if (clip.ClipData == null)
                        {
                            var key = clip.ClipDataId.ToString();
                            try
                            {
                                var clipData = await _clipDataRepository.LoadAsync(key, _cts.Token);
                                clip.ClipData = clipData;
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                            }
                        }
                    }
                }
                
                // タイムラインをセット
                SetTimeline(_timeline);
                _hasUnsavedChanges = false;
                Debug.Log($"<color=cyan>[TimelinePresenter] Timeline loaded successfully: {_timeline.Name}</color>");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        // Track management methods
        private void CreateTrack(DataType type)
        {
            if (_timeline == null) return;

            var newTrack = _timeline.CreateTrack($"New {type}", type);
            newTrack.Name = $"{type}_{newTrack.TargetId}";

            _timelineEditorView.UpdateTimelineUI(_timeline);
            _hasUnsavedChanges = true;
        }
        
        private void RemoveTrack(int trackId)
        {
            if (_timeline == null) return;
            _timeline.RemoveTrack(trackId);
            _timelineEditorView.UpdateTimelineUI(_timeline);
            _hasUnsavedChanges = true;
        }

        // Clip management methods
        public async void AddClip(ClipDataInfo clipDataInfo, int trackId, float startTime)
        {
            if (_timeline == null) return;
            
            var clipData = await _clipDataRepository.LoadAsync(clipDataInfo.Id.ToString(), _cts.Token);
            var newClip = _timeline.AddClip(trackId, startTime, clipData);
            _totalDuration = Mathf.Max(_totalDuration, _timeline.GetDuration());
            
            // Update the views
            _timelinePlaybackControlView.SetDuration(_totalDuration);
            _timelineEditorView.UpdateTimelineUI(_timeline);
            _hasUnsavedChanges = true;
        }
        
        public void RemoveClip(int trackId, int clipId)
        {
            if (_timeline == null) return;
            
            _timeline.RemoveClip(trackId, clipId);
            _totalDuration = Mathf.Max(_defaultDuration, _timeline.GetDuration());
            
            // Update the views
            _timelinePlaybackControlView.SetDuration(_totalDuration);
            _timelineEditorView.UpdateTimelineUI(_timeline);
            _hasUnsavedChanges = true;
        }
        
        public void SetTime(float time)
        {
            _player.SetTime(time);
        }
        
        public void SetZoom(float zoom)
        {
            _currentZoom = zoom;
            _timelinePlaybackControlView.SetZoom(zoom);
            _timelineEditorView.SetZoom(zoom);
        }
        
        public float GetCurrentZoom()
        {
            return _currentZoom;
        }
        
        public float GetTotalDuration()
        {
            return _totalDuration;
        }

        private void OnAnimationEvaluated(int targetId, AnimationFrame frame)
        {
            Profiler.BeginSample("TimelinePresenter.OnAnimationEvaluated");

            // Debug.Log($"<color=cyan>[TimelinePresenter] OnAnimationEvaluated - TargetId: {targetId}, Time: {frame.Time}</color>");
            if (frame.Type == DataType.CameraPose && targetId == _sceneView.CameraId)
            {
                var posX = frame.Properties[0].Value;
                var posY = frame.Properties[1].Value;
                var posZ = frame.Properties[2].Value;
                var eulerX = frame.Properties[3].Value;
                var eulerY = frame.Properties[4].Value;
                var eulerZ = frame.Properties[5].Value;
                // var fov = frame.Properties[6].Value;

                _sceneView.UpdateCameraTransform(new Vector3(posX, posY, posZ), Quaternion.Euler(eulerX, eulerY, eulerZ));
                // _sceneView.UpdateCameraParameters(fov);
            }

            Profiler.EndSample();
        }

        // Event handlers from the views
        private void HandlePlayButtonClickedEvent()
        {
            Debug.Log($"<color=cyan>[TimelinePresenter] HandlePlayButtonClickedEvent</color>");
            _player.Play();
        }
        
        private void HandlePauseButtonClickedEvent()
        {
            _player.Pause();
        }
        
        private void HandleStopButtonClickedEvent()
        {
            _player.Stop();
        }
        
        // ************************************************************
        // TODO: Code review

        private void HandleTimeChanged(float time)
        {
            SetTime(time);
        }
        
        private void HandleAddCameraTrackRequest()
        {
            CreateTrack(DataType.CameraPose);
        }
        
        private void HandleAddLightTrackRequest()
        {
            CreateTrack(DataType.LightProperties);
        }
        
        private void HandleEditKeyframeRequest(TimelineClip clip)
        {
            Debug.Log($"<color=cyan>[TimelinePresenter] HandleEditKeyframeRequest</color>");
            _keyframeAnimationEditor.LoadAsync(clip.ClipDataId.ToString(), _cts.Token).Forget();
        }
        
        private void HandleEditKeyframeRequest2(ClipDataInfo info)
        {
            Debug.Log($"<color=cyan>[TimelinePresenter] HandleEditKeyframeRequest2</color>");
            _keyframeAnimationEditor.LoadAsync(info.Id.ToString(), _cts.Token).Forget();
        }
        
        private void HandleClipDropped(IClipData clipData, int trackId, float startTime)
        {
            // AddClip(trackId, startTime, clipData);
        }

        private void HandleClipMoved(ClipMovedEvent movedEvent)
        {
            if (_timeline != null)
            {
                _timeline.UpdateClip(movedEvent.ClipId, movedEvent.NewStartTime, movedEvent.NewTrackId, movedEvent.OldTrackId);
                _timelineEditorView.UpdateTimelineUI(_timeline);
                _hasUnsavedChanges = true;
            }
        }

        private void HandleLibraryItemSelected(IClipData clipData)
        {
            // Handle library item selection
            Debug.Log($"Library item selected: {clipData.Name}");
        }
        
        private void HandleLibraryItemDropped(ClipDataInfo clipDataInfo, int trackId, float startTime)
        {
            AddClip(clipDataInfo, trackId, startTime);
        }
        
        // タイムラインが選択されたときのハンドラ
        private void HandleTimelineSelected(CinematicSequenceDataInfo sequenceDataInfo)
        {
            Debug.Log($"<color=cyan>[TimelinePresenter] Timeline selected: {sequenceDataInfo.Name}</color>");
            
            // 未保存の変更がある場合は確認ダイアログを表示
            if (_hasUnsavedChanges)
            {
                _pendingTimelineToLoad = sequenceDataInfo;
                ShowSaveConfirmationDialog();
            }
            else
            {
                LoadTimelineAsync(sequenceDataInfo.Id.ToString()).Forget();
            }
        }
        
        // Event handlers from the TimelinePlayer
        private void HandleTimelineStart()
        {
            Debug.Log($"<color=cyan>[TimelinePresenter] HandleTimelineStart</color>");
            _timelineEditorView.CanEdit = false;
            _timelineEditorView.DisableClipElementManipulators();
            _timelinePlaybackControlView.OnTimelineStart();
        }
        
        private void HandleTimelinePause()
        {
            _timelineEditorView.CanEdit = true;
            _timelineEditorView.EnableClipElementManipulators();
            _timelinePlaybackControlView.OnTimelinePause();
        }
        
        private void HandleTimelineStop()
        {
            _timelineEditorView.CanEdit = true;
            _timelineEditorView.EnableClipElementManipulators();
            _timelinePlaybackControlView.OnTimelineStop();
        }
        
        private void HandleTimelineComplete()
        {
            _timelineEditorView.CanEdit = true;
            _timelineEditorView.EnableClipElementManipulators();
            // Can add additional handling for timeline completion
        }
        
        private void HandleTimeUpdate(float time)
        {
            Profiler.BeginSample("TimelinePresenter.OnTimeUpdate");
            _timelinePlaybackControlView.UpdateTimeDisplay(time);
            Profiler.EndSample();
        }

        // ************************************************************
        // 新しいタイムラインの名前変更ハンドラ
        private void HandleTimelineNameChanged(string newName)
        {
            if (_timeline == null) return;
            
            if (string.IsNullOrWhiteSpace(newName))
            {
                Debug.LogWarning("Timeline name cannot be empty. Ignoring change.");
                // UI側に元の名前を表示
                _timelineEditorView.UpdateTimelineUI(_timeline);
                return;
            }
            
            if (_timeline.Name != newName)
            {
                Debug.Log($"Changing timeline name from '{_timeline.Name}' to '{newName}'");
                _timeline.Name = newName;
                _hasUnsavedChanges = true;
            }
        }

        // タイムラインの保存ハンドラ
        private void HandleSaveTimelineRequest()
        {
            SaveTimelineAsync().Forget();
        }

        // ライブラリビューのリフレッシュ処理
        private void HandleRefreshLibraryRequest()
        {
            RefreshLibraryAsync().Forget();
        }

        // リポジトリからデータを再取得してライブラリビューを更新
        private async UniTask RefreshLibraryAsync()
        {
            try
            {
                Debug.Log($"<color=cyan>[TimelinePresenter] Refreshing library...</color>");
                
                // 更新中に操作を受け付けないようにする場合は、UIを一時的に無効化することも考慮
                // _cinematicSequenceLibraryView.SetEnabled(false);
                
                // 既存のデータをクリア
                _cinematicSequenceLibraryView.ClearLibraryItems();
                
                // タイムラインとクリップデータのリストを取得
                var sequenceDataInfoList = await _timelineRepository.GetSequenceDataInfoListAsync(_cts.Token);
                var clipDataInfoList = await _clipDataRepository.GetClipDataInfoListAsync(_cts.Token);
                
                // ライブラリビューを更新
                _cinematicSequenceLibraryView.SetTimelineItems(sequenceDataInfoList);
                _cinematicSequenceLibraryView.SetLibraryItems(clipDataInfoList);
                
                Debug.Log($"<color=cyan>[TimelinePresenter] Library refreshed - Timelines: {sequenceDataInfoList.Count}, Clips: {clipDataInfoList.Count}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red>[TimelinePresenter] Failed to refresh library: {ex.Message}</color>");
            }
            finally
            {
                // UIの再有効化
                // _cinematicSequenceLibraryView.SetEnabled(true);
            }
        }

        // タイムラインの保存処理
        private async UniTask SaveTimelineAsync()
        {
            if (_timeline == null)
            {
                Debug.LogError("Cannot save: No timeline is currently loaded.");
                return;
            }

            try
            {
                Debug.Log($"<color=cyan>[TimelinePresenter] Saving timeline: {_timeline.Name} (ID: {_timeline.Id})</color>");
                
                // タイムラインを保存
                await _timelineRepository.SaveAsync(_timeline.Id.ToString(), _timeline, _cts.Token);
                
                // 保存フラグをリセット
                _hasUnsavedChanges = false;
                
                Debug.Log($"<color=cyan>[TimelinePresenter] Timeline saved successfully: {_timeline.Name}</color>");

                // 保存後にライブラリを更新
                await RefreshLibraryAsync();

                // 保留中のタイムライン読み込みがあれば実行
                if (_pendingTimelineToLoad != null)
                {
                    var pendingTimeline = _pendingTimelineToLoad;
                    _pendingTimelineToLoad = null;
                    LoadTimelineAsync(pendingTimeline.Id.ToString()).Forget();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red>[TimelinePresenter] Failed to save timeline: {ex.Message}</color>");
            }
        }
        
        // 保存確認ダイアログの表示
        private void ShowSaveConfirmationDialog()
        {
            _saveConfirmationDialogView.Show(HandleSaveConfirmationResult);
        }
        
        // 保存確認ダイアログの結果処理
        private void HandleSaveConfirmationResult(SaveConfirmationDialogView.DialogResult result)
        {
            _saveConfirmationDialogView.Hide();

            switch (result)
            {
                case SaveConfirmationDialogView.DialogResult.Save:
                    // 保存してから新しいタイムラインを読み込む
                    SaveTimelineAsync().Forget();
                    break;

                case SaveConfirmationDialogView.DialogResult.DontSave:
                    // 保存せずに新しいタイムラインを読み込む
                    if (_pendingTimelineToLoad != null)
                    {
                        var pendingTimeline = _pendingTimelineToLoad;
                        _pendingTimelineToLoad = null;
                        _hasUnsavedChanges = false;
                        LoadTimelineAsync(pendingTimeline.Id.ToString()).Forget();
                    }
                    break;

                case SaveConfirmationDialogView.DialogResult.Cancel:
                    // 何もしない
                    _pendingTimelineToLoad = null;
                    break;
            }
        }
        
        // アクションを実行する前に未保存の変更をチェック
        private void CheckUnsavedChangesBeforeAction(Action action)
        {
            if (_hasUnsavedChanges)
            {
                _saveConfirmationDialogView.Show(result => {
                    _saveConfirmationDialogView.Hide();
                    
                    switch (result)
                    {
                        case SaveConfirmationDialogView.DialogResult.Save:
                            SaveTimelineAsync().ContinueWith(() => action()).Forget();
                            break;
                            
                        case SaveConfirmationDialogView.DialogResult.DontSave:
                            _hasUnsavedChanges = false;
                            action();
                            break;
                            
                        case SaveConfirmationDialogView.DialogResult.Cancel:
                            // 何もしない
                            break;
                    }
                });
            }
            else
            {
                action();
            }
        }

        // Add a handler for clip deletion requests
        private void HandleClipDeleteRequest(int trackId, int clipId)
        {
            Debug.Log($"<color=cyan>[TimelinePresenter] Deleting clip {clipId} from track {trackId}</color>");
            RemoveClip(trackId, clipId);
        }

        private void HandleCreateNewTimelineRequest()
        {
            CheckUnsavedChangesBeforeAction(() => {
                var timeline = new Timeline($"Sequence_{DateTime.Now:yyyyMMdd_HHmmss}");
                SetTimeline(timeline);
                _hasUnsavedChanges = false;
            });
        }

        private void HandleCreateNewClipDataRequest(DataType dataType)
        {
            CheckUnsavedChangesBeforeAction(() => {
                _keyframeAnimationEditor.CreateNewClipData(dataType);
            });
        }
        
        // トラック追加要求のハンドラー
        private void HandleAddTrackRequest(DataType type)
        {
            Debug.Log($"<color=cyan>[TimelinePresenter] Adding track of type: {type}</color>");
            
            // タイムラインが読み込まれていない場合は警告を表示
            if (_timeline == null)
            {
                Debug.LogWarning("Cannot add track: No timeline is currently loaded. Please create or load a timeline first.");
                return;
            }

            // トラックタイプに応じて表示名を設定
            var trackTypeName = type.ToString();
            
            // 同じタイプのトラック数をカウントして番号付け
            var trackCount = 0;
            foreach (var track in _timeline.Tracks)
            {
                if (track.Type == type)
                {
                    trackCount++;
                }
            }
            
            // トラック作成
            var newTrack = _timeline.CreateTrack($"{trackTypeName}_{trackCount + 1}", type);
            
            // トラック作成後の処理
            _timelineEditorView.UpdateTimelineUI(_timeline);
            // _timelineEditorView.ScrollToTrack(newTrack.Id);  // 新しいトラックが表示されるようにスクロール
            _hasUnsavedChanges = true;
            
            Debug.Log($"<color=green>[TimelinePresenter] Track added: {newTrack.Name} (ID: {newTrack.Id})</color>");
        }
        
        // トラック削除要求のハンドラー
        private void HandleRemoveTrackRequest(int trackId)
        {
            Debug.Log($"<color=cyan>[TimelinePresenter] Removing track: {trackId}</color>");
            
            if (_timeline == null)
            {
                Debug.LogWarning("Cannot remove track: No timeline is currently loaded.");
                return;
            }
            
            // // トラックを取得
            // var trackToRemove = _timeline.GetTrack(trackId);
            // if (trackToRemove == null)
            // {
            //     Debug.LogError($"Cannot remove track: Track ID {trackId} not found.");
            //     return;
            // }
            
            // // クリップがあるかチェック
            // if (trackToRemove.Clips.Count > 0)
            // {
            //     // 確認ダイアログ表示（現在未実装なので、ログだけ表示）
            //     Debug.Log($"<color=yellow>[TimelinePresenter] Warning: Track {trackToRemove.Name} contains {trackToRemove.Clips.Count} clips that will also be removed.</color>");
            // }
            
            // トラック削除
            _timeline.RemoveTrack(trackId);
            
            // UIの更新とフラグの設定
            _timelineEditorView.UpdateTimelineUI(_timeline);
            _hasUnsavedChanges = true;
            
            Debug.Log($"<color=green>[TimelinePresenter] Track removed: ID {trackId}</color>");
        }
    }
}
