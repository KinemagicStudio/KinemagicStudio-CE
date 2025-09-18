using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.Character;
using MessagePipe;
using R3;
using UnityEngine;
using VRMToolkit;
using VRMToolkit.UI;

namespace Kinemagic.Apps.Studio.UI.Character
{
    public sealed class CharacterModelPresenter : IDisposable, IInitializable, IStartable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly UIViewContext _context;
        private readonly CharacterModelListView _characterModelListView;
        private readonly CharacterLicenseView _characterLicenseView;
        private readonly ICharacterModelInfoRepository _characterModelInfoRepository;
        private readonly IPublisher<ICharacterCommand> _characterCommandPublisher;

        private int _maxModelCount = 100; // Maximum number of models to fetch.
        private CancellationTokenSource _cts;

        public CharacterModelPresenter(
            UIViewContext context,
            CharacterModelListView characterModelListView,
            CharacterLicenseView characterLicenseView,
            ICharacterModelInfoRepository characterModelInfoRepository,
            IPublisher<ICharacterCommand> characterCommandPublisher)
        {
            _context = context;
            _characterModelListView = characterModelListView;
            _characterLicenseView = characterLicenseView;
            _characterModelInfoRepository = characterModelInfoRepository;
            _characterCommandPublisher = characterCommandPublisher;
            
            _characterModelListView.RefreshButtonClicked += OnRefreshRequested;
            _characterModelListView.ModelSelected += OnModelListSelected;
            _characterModelListView.OpenFolderButtonClicked += OnOpenFolderRequested;
        }

        public void Dispose()
        {
            _characterModelListView.RefreshButtonClicked -= OnRefreshRequested;
            _characterModelListView.ModelSelected -= OnModelListSelected;
            _characterModelListView.OpenFolderButtonClicked -= OnOpenFolderRequested;
            _disposables.Dispose();
        }

        public void Initialize()
        {
            _context.CurrentPage
                .Skip(1)
                .Subscribe(pageType =>
                {
                    UnityEngine.Debug.Log($"[CharacterModelPresenter] CurrentPage changed: {pageType}");
                    if (pageType == UIPageType.Character)
                    {
                        UpdateCharacterModelList();
                        _characterModelListView.Show();
                    }
                    else
                    {
                        _characterModelListView.Hide();
                        _characterLicenseView.Hide();
                    }
                })
                .AddTo(_disposables);

            _context.CurrentPage
                .Skip(1)
                .Take(1)
                .Subscribe(pageType =>
                {
                    UpdateCharacterModelListAsync().Forget();
                })
                .AddTo(_disposables);
        }

        public void Start()
        {
            _characterModelListView.Hide();
            _characterLicenseView.Hide();
        }

        private void OnRefreshRequested()
        {
            UpdateCharacterModelListAsync().Forget();
        }

        private void OnModelListSelected(ICharacterModelInfo model)
        {
            LoadCharacterModelWithLicenseCheckAsync(model).Forget();
        }

        private void OnOpenFolderRequested()
        {
            FileBrowserUtility.OpenDirectory(Path.Combine(Application.persistentDataPath, Constants.PersistentDataDirectoryName));
        }

        private void UpdateCharacterModelList()
        {
            var characterModels = _characterModelInfoRepository.GetAll();

            _characterModelListView.ClearList();
            for (var i = 0; i < characterModels.Count; i++) // NOTE: Avoid allocation
            {
                _characterModelListView.AddCharacterModel(characterModels[i]);
            }
        }

        private async UniTask UpdateCharacterModelListAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var characterModels = await _characterModelInfoRepository.FetchAllAsync(_maxModelCount, _cts.Token);

            _characterModelListView.ClearList();
            for (var i = 0; i < characterModels.Count; i++) // NOTE: Avoid allocation
            {
                _characterModelListView.AddCharacterModel(characterModels[i]);
            }
        }

        private async UniTask LoadCharacterModelWithLicenseCheckAsync(ICharacterModelInfo modelInfo)
        {
            var accepted = await ShowLicenseConfirmationDialogAsync(modelInfo);
            if (!accepted) return;

            // TODO: Publish command to load the model
            var command = new CharacterInstanceCreateCommand(modelInfo.ResourceKey, modelInfo.StorageType);
            _characterCommandPublisher.Publish(command);

            UnityEngine.Debug.Log($"[CharacterModelPresenter] Published CharacterInstanceCreateCommand: {modelInfo.ResourceKey}");
        }

        private UniTask<bool> ShowLicenseConfirmationDialogAsync(ICharacterModelInfo modelInfo)
        {            
            var utcs = new UniTaskCompletionSource<bool>();

            _characterLicenseView.Show(modelInfo.Thumbnail, modelInfo, result =>
            {
                var accepted = result == CharacterLicenseView.DialogResult.Accept;
                utcs.TrySetResult(accepted);
            });

            return utcs.Task;
        }
    }
}
