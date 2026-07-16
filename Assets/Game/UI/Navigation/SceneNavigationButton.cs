using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Scene;

namespace ValorChronicle.UI.Navigation
{
    [RequireComponent(typeof(Button))]
    public sealed class SceneNavigationButton : MonoBehaviour
    {
        [SerializeField]
        private GameScene destination;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        private async void HandleClick()
        {
            var bootstrapper = GameBootstrapper.Instance;

            if (bootstrapper == null)
            {
                GameLogger.Error(
                    "[SceneNavigationButton] GameBootstrapper is missing.",
                    this);
                return;
            }

            var sceneService = bootstrapper.SceneService;

            if (sceneService == null)
            {
                GameLogger.Error(
                    "[SceneNavigationButton] SceneService is not initialized.",
                    this);
                return;
            }

            button.interactable = false;
            bool loadSucceeded = false;

            try
            {
                loadSucceeded = await sceneService.LoadAsync(destination);
            }
            catch (System.Exception exception)
            {
                GameLogger.Exception(exception, this);
            }
            finally
            {
                if (!loadSucceeded && button != null)
                {
                    button.interactable = true;
                }
            }
        }
    }
}
