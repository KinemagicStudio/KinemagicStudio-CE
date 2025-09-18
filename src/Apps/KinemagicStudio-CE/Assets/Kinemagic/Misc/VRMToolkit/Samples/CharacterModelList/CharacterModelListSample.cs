using VRMToolkit.UI;
using UnityEngine;

namespace VRMToolkit.Samples
{
    public sealed class CharacterModelListSample : MonoBehaviour
    {
        [SerializeField] private CharacterModelListView _characterModelListView;
        [SerializeField] private CharacterLicenseView _characterLicenseView;

        private ICharacterModelInfoRepository _repository;
        private CharacterModelsPresenter _presenter;

        void Awake()
        {
            _repository = new CharacterModelInfoLocalRepository();
            _presenter = new CharacterModelsPresenter(_characterModelListView, _characterLicenseView, _repository);
        }

        void Start()
        {
            _presenter.Initialize();
        }

        void OnDestroy()
        {
            _presenter.Dispose();
            _repository.Dispose();
        }
    }
}
