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
        private SceneService subscribedSceneService;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
            TryBindSceneService();
        }

        private void OnEnable()
        {
            TryBindSceneService();
        }

        private void OnDisable()
        {
            UnbindSceneService();
        }

        private void OnDestroy()
        {
            UnbindSceneService();
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

            BindSceneService(sceneService);
            if (sceneService.IsLoading)
            {
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
                    button.interactable = !sceneService.IsLoading;
                }
            }
        }

        private void TryBindSceneService()
        {
            BindSceneService(GameBootstrapper.Instance?.SceneService);
        }

        private void BindSceneService(SceneService sceneService)
        {
            if (ReferenceEquals(subscribedSceneService, sceneService))
            {
                if (sceneService != null)
                {
                    HandleLoadingStateChanged(sceneService.IsLoading);
                }

                return;
            }

            UnbindSceneService();
            subscribedSceneService = sceneService;
            if (subscribedSceneService == null)
            {
                return;
            }

            subscribedSceneService.LoadingStateChanged +=
                HandleLoadingStateChanged;
            HandleLoadingStateChanged(subscribedSceneService.IsLoading);
        }

        private void UnbindSceneService()
        {
            if (subscribedSceneService == null)
            {
                return;
            }

            subscribedSceneService.LoadingStateChanged -=
                HandleLoadingStateChanged;
            subscribedSceneService = null;
        }

        private void HandleLoadingStateChanged(bool isLoading)
        {
            if (button != null)
            {
                button.interactable = !isLoading;
            }
        }
    }
}
