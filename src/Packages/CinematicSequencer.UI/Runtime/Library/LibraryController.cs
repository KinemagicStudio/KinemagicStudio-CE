using System;
using System.Threading;
using CinematicSequencer.IO;
using Cysharp.Threading.Tasks;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// ライブラリのController。リポジトリからデータ取得しViewに表示する。
    /// v1ではCinematicSequenceLibraryViewとTimelinePresenterに分散していたロジックを統合。
    /// </summary>
    public sealed class LibraryController : IDisposable
    {
        // DI
        private readonly ISequenceRepository _sequenceRepo;
        private readonly IClipAssetRepository _clipAssetRepo;

        // UI
        private readonly LibraryView _view;

        private CancellationTokenSource _cts;
        private bool _disposed;

        // --- 外部連携イベント（上位のアプリ層Controllerが購読） ---

        /// <summary>シーケンス読み込み要求。</summary>
        public event Action<Guid> OnSequenceLoadRequested;

        /// <summary>キーフレーム編集要求。</summary>
        public event Action<Guid> OnClipAssetEditRequested;

        /// <summary>ドロップ通知（トラック特定はアプリ層の責務）。</summary>
        public event Action<Guid, TrackType> OnClipAssetDropped;

        public LibraryController(
            ISequenceRepository sequenceRepo,
            IClipAssetRepository clipAssetRepo,
            LibraryView view)
        {
            _sequenceRepo = sequenceRepo ?? throw new ArgumentNullException(nameof(sequenceRepo));
            _clipAssetRepo = clipAssetRepo ?? throw new ArgumentNullException(nameof(clipAssetRepo));
            _view = view ?? throw new ArgumentNullException(nameof(view));

            _cts = new CancellationTokenSource();

            SubscribeViewEvents();
        }

        // --- 公開API ---

        /// <summary>
        /// 両リポジトリからリスト取得しViewに表示する。
        /// </summary>
        public async UniTask RefreshAsync()
        {
            var ct = ResetCts();

            var sequenceListTask = _sequenceRepo.GetSequenceListAsync(ct);
            var clipAssetListTask = _clipAssetRepo.GetClipAssetListAsync(ct);

            var sequences = await sequenceListTask;
            var clipAssets = await clipAssetListTask;

            _view.SetSequenceItems(sequences);
            _view.SetClipAssetItems(clipAssets);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            UnsubscribeViewEvents();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _view.Dispose();
        }

        // --- View → Command パターン ---

        private void HandleSequenceSelected(Guid sequenceId)
        {
            OnSequenceLoadRequested?.Invoke(sequenceId);
        }

        private void HandleEditClipAssetRequested(Guid clipAssetId)
        {
            OnClipAssetEditRequested?.Invoke(clipAssetId);
        }

        private void HandleClipAssetDropped(Guid clipAssetId, TrackType trackType)
        {
            OnClipAssetDropped?.Invoke(clipAssetId, trackType);
        }

        private void HandleCreateSequence()
        {
            // 将来実装: 新規Sequence作成
        }

        private void HandleCreateClipAsset(TrackType trackType)
        {
            // 将来実装: 新規ClipAsset作成
        }

        private void HandleRefresh()
        {
            RefreshAsync().Forget();
        }

        // --- イベント購読管理 ---

        private void SubscribeViewEvents()
        {
            _view.OnSequenceSelected += HandleSequenceSelected;
            _view.OnEditClipAssetRequested += HandleEditClipAssetRequested;
            _view.OnClipAssetDropped += HandleClipAssetDropped;
            _view.OnCreateSequenceRequested += HandleCreateSequence;
            _view.OnCreateClipAssetRequested += HandleCreateClipAsset;
            _view.OnRefreshRequested += HandleRefresh;
        }

        private void UnsubscribeViewEvents()
        {
            _view.OnSequenceSelected -= HandleSequenceSelected;
            _view.OnEditClipAssetRequested -= HandleEditClipAssetRequested;
            _view.OnClipAssetDropped -= HandleClipAssetDropped;
            _view.OnCreateSequenceRequested -= HandleCreateSequence;
            _view.OnCreateClipAssetRequested -= HandleCreateClipAsset;
            _view.OnRefreshRequested -= HandleRefresh;
        }

        private CancellationToken ResetCts()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }
    }
}
