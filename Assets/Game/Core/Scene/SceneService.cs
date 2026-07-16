using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using ValorChronicle.Core.Logging;

namespace ValorChronicle.Core.Scene
{
    public sealed class SceneService
    {
        private bool isLoading;

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
            }
        }
    }
}
