using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.SpatialEnvironment;
using MessagePipe;
using R3;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Kinemagic.Apps.Studio.UI.SpatialEnvironment
{
    public sealed class EnvironmentModelsPresenter : IInitializable, IDisposable
    {
        private readonly UIViewContext _context;
        private readonly EnvironmentModelListView _modelListView;
        private readonly ConfirmationDialog _confirmationDialog;
        private readonly IEnvironmentModelInfoRepository _modelInfoRepository;
        private readonly IPublisher<IEnvironmentSystemCommand> _commandPublisher;
        private readonly CompositeDisposable _disposables = new();

        private CancellationTokenSource _cts;

        public EnvironmentModelsPresenter(
            UIViewContext context,
            EnvironmentModelListView modelListView,
            ConfirmationDialog confirmationDialog,
            IEnvironmentModelInfoRepository modelInfoRepository,
            IPublisher<IEnvironmentSystemCommand> commandPublisher)
        {
            _context = context;
            _modelListView = modelListView;
            _confirmationDialog = confirmationDialog;
            _modelInfoRepository = modelInfoRepository;
            _commandPublisher = commandPublisher;

            _modelListView.RefreshButtonClicked += OnRefreshRequested;
            _modelListView.ModelSelected += OnModelSelected;
            _modelListView.OpenFolderButtonClicked += OnOpenFolderRequested;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _modelListView.RefreshButtonClicked -= OnRefreshRequested;
            _modelListView.ModelSelected -= OnModelSelected;
            _modelListView.OpenFolderButtonClicked -= OnOpenFolderRequested;
            _disposables.Dispose();
        }

        public void Initialize()
        {
            _context.CurrentPage
                .Skip(1)
                .Subscribe(pageType =>
                {
                    if (pageType == UIPageType.SpatialEnvironment)
                    {
                        _modelListView.Show();
                    }
                    else
                    {
                        _modelListView.Hide();
                        _confirmationDialog.Hide();
                    }
                })
                .AddTo(_disposables);

            _context.CurrentPage
                .Skip(1)
                .Take(1)
                .Subscribe(pageType =>
                {
                    RefreshModelListAsync().Forget();
                })
                .AddTo(_disposables);
        }

        private async UniTask RefreshModelListAsync()
        {
            try
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                var models = await _modelInfoRepository.FetchAllAsync(_cts.Token);

                _modelListView.ClearList();
                foreach (var model in models)
                {
                    _modelListView.AddEnvironmentModel(model);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"<color=yellow>[EnvironmentModelsPresenter] Environment models loading was cancelled</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnvironmentModelsPresenter] Failed to fetch environment models: {ex}");
            }
        }

        private void OnRefreshRequested()
        {
            RefreshModelListAsync().Forget();
        }

        private void OnOpenFolderRequested()
        {
            FileBrowserUtility.OpenDirectory(Path.Combine(Application.persistentDataPath, Constants.PersistentDataDirectoryName));
        }

        private void OnModelSelected(EnvironmentModelInfo modelInfo)
        {
            var title = "Loading Environment Model";
            var message = $"Do you want to load '{modelInfo.DisplayName}'?\n\nThe current model will be replaced.";

            _confirmationDialog.Show(title, message, result =>
            {
                if (result == ConfirmationDialog.DialogResult.OK)
                {
                    _commandPublisher.Publish(new SceneInstanceCreateCommand(modelInfo.ResourceKey, modelInfo.StorageType));
                }
            });
        }
    }
}