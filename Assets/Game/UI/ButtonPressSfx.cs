using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Audio.Core;
using ValorChronicle.Core.Bootstrap;

namespace ValorChronicle.UI.Audio
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPressSfx : MonoBehaviour
    {
        private Button button;
        private bool isSubscribed;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (isSubscribed || button == null)
                return;

            button.onClick.AddListener(HandleClick);
            isSubscribed = true;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void HandleClick()
        {
            if (!isActiveAndEnabled
                || button == null
                || !button.isActiveAndEnabled
                || !button.interactable)
            {
                return;
            }

            AudioService audioService =
                GameBootstrapper.Instance?.AudioService;
            if (audioService == null || !audioService.IsInitialized)
                return;

            audioService.PlayUiSfx(SfxCue.ButtonPress);
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || button == null)
                return;

            button.onClick.RemoveListener(HandleClick);
            isSubscribed = false;
        }
    }
}
