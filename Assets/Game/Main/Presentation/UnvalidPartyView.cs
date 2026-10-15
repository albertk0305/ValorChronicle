using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Logging;
using ValorChronicle.Localization;

namespace ValorChronicle.Main.Presentation
{
    public sealed class UnvalidPartyView : MonoBehaviour
    {
        public const string EmptyPartyMessageKey =
            "main.party.empty_required";
        public const string EmptyPartyMessage =
            "At least one character is required in your party.";

        [SerializeField] private TMP_Text messageText = null;
        [SerializeField] private Button dismissButton = null;

        private bool dismissListenerRegistered;

        public TMP_Text MessageText => messageText;
        public Button DismissButton => dismissButton;
        public bool IsConfigured =>
            messageText != null && dismissButton != null;

        public void Configure(
            TMP_Text configuredMessageText,
            Button configuredDismissButton)
        {
            RemoveDismissListener();
            messageText = configuredMessageText;
            dismissButton = configuredDismissButton;
            AddDismissListener();
        }

        public void ShowEmptyParty(LocalizationService localization)
        {
            messageText.text = ResolveEmptyPartyMessage(localization);
            gameObject.SetActive(true);
        }

        public string ResolveEmptyPartyMessage(
            LocalizationService localization)
        {
            if (localization != null)
            {
                string localized = localization.GetText(
                    EmptyPartyMessageKey);
                if (!string.Equals(
                        localized,
                        LocalizationService.MissingKey(
                            EmptyPartyMessageKey),
                        StringComparison.Ordinal))
                {
                    return localized;
                }
            }

            GameLogger.Error(
                "[UnvalidPartyView] Empty-party localization is missing. "
                    + "Using the English fallback.",
                this);
            return EmptyPartyMessage;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            AddDismissListener();
        }

        private void OnDestroy()
        {
            RemoveDismissListener();
        }

        private void AddDismissListener()
        {
            if (dismissListenerRegistered || dismissButton == null)
            {
                return;
            }

            dismissButton.onClick.AddListener(Hide);
            dismissListenerRegistered = true;
        }

        private void RemoveDismissListener()
        {
            if (!dismissListenerRegistered || dismissButton == null)
            {
                dismissListenerRegistered = false;
                return;
            }

            dismissButton.onClick.RemoveListener(Hide);
            dismissListenerRegistered = false;
        }
    }
}
