using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using ValorChronicle.Core.Logging;

namespace ValorChronicle.Core.Scene
{
    public sealed class SceneService
    {
        private bool isLoading;

        public event Action<bool> LoadingStateChanged;

        public bool IsLoading => isLoading;

        public async Task<bool> LoadAsync(GameScene scene)
        {
            if (isLoading)
            {
                GameLogger.Warning(
                    $"[SceneService] Scene load ignored because another load is active: {scene}");

                return false;
            }

            isLoading = true;

            try
            {
                LoadingStateChanged?.Invoke(true);
                string sceneName = GameSceneNames.GetName(scene);
                var operation = SceneManager.LoadSceneAsync(sceneName);

                if (operation == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to start loading scene: {sceneName}");
                }

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                return true;
            }
            finally
            {
                isLoading = false;
                LoadingStateChanged?.Invoke(false);
            }
        }
    }
}
