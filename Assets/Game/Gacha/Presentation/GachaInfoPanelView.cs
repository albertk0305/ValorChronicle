using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Gacha.Presentation
{
    public sealed class GachaInfoPanelView : MonoBehaviour
    {
        private Button characterInfoButton;
        private Button relicInfoButton;
        private Button exitButton;
        private GameObject panelRoot;
        private TMP_Text infoText;
        private bool subscribed;

        public event Action CharacterInfoRequested;
        public event Action RelicInfoRequested;
        public event Action CloseRequested;

        public GameObject PanelRoot => panelRoot;
        public TMP_Text InfoText => infoText;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public bool IsConfigured =>
            characterInfoButton != null
            && relicInfoButton != null
            && exitButton != null
            && panelRoot != null
            && infoText != null;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            Button configuredCharacterInfoButton,
            Button configuredRelicInfoButton,
            Button configuredExitButton,
            GameObject configuredPanelRoot,
            TMP_Text configuredInfoText)
        {
            Unsubscribe();
            characterInfoButton = configuredCharacterInfoButton
                ?? throw new ArgumentNullException(
                    nameof(configuredCharacterInfoButton));
            relicInfoButton = configuredRelicInfoButton
                ?? throw new ArgumentNullException(
                    nameof(configuredRelicInfoButton));
            exitButton = configuredExitButton
                ?? throw new ArgumentNullException(
                    nameof(configuredExitButton));
            panelRoot = configuredPanelRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredPanelRoot));
            infoText = configuredInfoText
                ?? throw new ArgumentNullException(
                    nameof(configuredInfoText));
            Close();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Open(string text)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "GachaInfoPanelView must be configured before use.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException(
                    "Info text cannot be empty.",
                    nameof(text));
            }

            infoText.text = text;
            panelRoot.transform.SetAsLastSibling();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (infoText != null)
            {
                infoText.text = string.Empty;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void Subscribe()
        {
            if (subscribed || !IsConfigured)
            {
                return;
            }

            characterInfoButton.onClick.AddListener(
                HandleCharacterInfoRequested);
            relicInfoButton.onClick.AddListener(
                HandleRelicInfoRequested);
            exitButton.onClick.AddListener(HandleCloseRequested);
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            characterInfoButton.onClick.RemoveListener(
                HandleCharacterInfoRequested);
            relicInfoButton.onClick.RemoveListener(
                HandleRelicInfoRequested);
            exitButton.onClick.RemoveListener(HandleCloseRequested);
            subscribed = false;
        }

        private void HandleCharacterInfoRequested()
        {
            CharacterInfoRequested?.Invoke();
        }

        private void HandleRelicInfoRequested()
        {
            RelicInfoRequested?.Invoke();
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }
    }
}
