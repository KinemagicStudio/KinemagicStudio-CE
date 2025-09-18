using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EngineLooper;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kinemagic.Apps.Studio
{
    /// <summary>
    /// Entry point
    /// </summary>
    public sealed class AppMain : IInitializable
    {
        private const string ActiveSceneName = SceneNames.Stage;

        private readonly List<string> _sceneNames = new List<string>
        {
            SceneNames.CameraSystem,
            SceneNames.Stage,
            SceneNames.UIView,
        };

        public void Initialize()
        {
            Application.targetFrameRate = 120;
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            await LoadScenesAsync();
        }

        private async UniTask LoadScenesAsync()
        {
            foreach (var sceneName in _sceneNames)
            {
                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(ActiveSceneName));
        }
    }
}
