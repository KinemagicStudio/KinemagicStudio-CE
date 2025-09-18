using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniVRM10;
using VRMToolkit.UI;

namespace VRMToolkit.Samples
{
    public sealed class CharacterModelsPresenter : IDisposable
    {
        private readonly CharacterModelListView _characterModelListView;
        private readonly CharacterLicenseView _characterLicenseView;
        private readonly ICharacterModelInfoRepository _characterModelInfoRepository;

        private int _maxModelCount = 100; // Maximum number of models to fetch.
        private CancellationTokenSource _cts;

        public CharacterModelsPresenter(
            CharacterModelListView characterModelListView,
            CharacterLicenseView characterLicenseView,
            ICharacterModelInfoRepository characterModelInfoRepository)
        {
            _characterModelListView = characterModelListView;
            _characterLicenseView = characterLicenseView;
            _characterModelInfoRepository = characterModelInfoRepository;
            _characterModelListView.RefreshButtonClicked += OnRefreshRequested;
            _characterModelListView.ModelSelected += OnModelListSelected;
            _characterModelListView.OpenFolderButtonClicked += OnOpenFolderRequested;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _characterModelListView.RefreshButtonClicked -= OnRefreshRequested;
            _characterModelListView.ModelSelected -= OnModelListSelected;
            _characterModelListView.OpenFolderButtonClicked -= OnOpenFolderRequested;
        }

        public void Initialize()
        {
            UpdateCharacterModelListAsync().Forget();
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
            Debug.Log("Open Folder button clicked");
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

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            Vrm10Instance vrm10Instance = null;
            if (modelInfo.StorageType == "StreamingAssetsDirectory")
            {
                vrm10Instance = await Vrm10.LoadPathAsync(
                    Path.Combine(Application.streamingAssetsPath, modelInfo.ResourceKey),
                    canLoadVrm0X: true,
                    showMeshes: true,
                    ct: _cts.Token);
            }
            else if (modelInfo.StorageType == "PersistentDataDirectory")
            {
                vrm10Instance = await Vrm10.LoadPathAsync(
                    Path.Combine(Application.persistentDataPath, modelInfo.ResourceKey),
                    canLoadVrm0X: true,
                    showMeshes: true,
                    ct: _cts.Token);
            }
            else
            {
                Debug.LogError($"Unsupported storage type: {modelInfo.StorageType}");
            }

            if (vrm10Instance != null)
            {
                Debug.Log($"Successfully loaded VRM model: {modelInfo.DisplayName}");
            }
            else
            {
                Debug.LogError($"Failed to load VRM model: {modelInfo.ResourceKey}");
            }
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
