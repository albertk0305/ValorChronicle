using System;
using TMPro;
using UnityEngine;
using ValorChronicle.Core.Bootstrap;

namespace ValorChronicle.Localization
{
    public sealed class LocalizedTMPText : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private string localizationKey = string.Empty;

        private LocalizationService localizationService;
        private GameBootstrapper subscribedBootstrapper;
        private bool localeSubscribed;

        public TMP_Text TargetText => targetText;
        public string LocalizationKey => localizationKey;

        private void Awake()
        {
            ResolveTargetText();
        }

        private void OnEnable()
        {
            ResolveTargetText();
            TryResolveLocalizationService();
            SubscribeLocalization();
            ApplyCurrentLocale();
        }

        private void Start()
        {
            if (localizationService != null)
                return;

            TryResolveLocalizationService();
            SubscribeLocalization();
            ApplyCurrentLocale();
        }

        private void OnDisable()
        {
            UnsubscribeLocalization();
            UnsubscribeBootstrapper();
        }

        private void OnDestroy()
        {
            UnsubscribeLocalization();
            UnsubscribeBootstrapper();
        }

        public void Initialize(LocalizationService service)
        {
            UnsubscribeLocalization();
            UnsubscribeBootstrapper();
            localizationService = service;

            if (!isActiveAndEnabled)
                return;

            SubscribeLocalization();
            ApplyCurrentLocale();
        }

        public void SetLocalizationKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "A localization key is required.",
                    nameof(key));
            }

            localizationKey = key;
            ApplyCurrentLocale();
        }

        private void TryResolveLocalizationService()
        {
            if (localizationService != null)
                return;

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
                return;

            if (bootstrapper.LocalizationService != null)
            {
                localizationService = bootstrapper.LocalizationService;
                UnsubscribeBootstrapper();
                return;
            }

            if (ReferenceEquals(subscribedBootstrapper, bootstrapper))
                return;

            UnsubscribeBootstrapper();
            subscribedBootstrapper = bootstrapper;
            subscribedBootstrapper.InitializationCompleted +=
                HandleBootstrapInitializationCompleted;
        }

        private void HandleBootstrapInitializationCompleted(
            BootstrapInitializationResult _)
        {
            GameBootstrapper bootstrapper = subscribedBootstrapper;
            UnsubscribeBootstrapper();
            if (bootstrapper?.LocalizationService == null)
                return;

            localizationService = bootstrapper.LocalizationService;
            SubscribeLocalization();
            ApplyCurrentLocale();
        }

        private void SubscribeLocalization()
        {
            if (localeSubscribed || localizationService == null)
                return;

            localizationService.LocaleChanged += HandleLocaleChanged;
            localeSubscribed = true;
        }

        private void UnsubscribeLocalization()
        {
            if (!localeSubscribed || localizationService == null)
                return;

            localizationService.LocaleChanged -= HandleLocaleChanged;
            localeSubscribed = false;
        }

        private void UnsubscribeBootstrapper()
        {
            if (subscribedBootstrapper == null)
                return;

            subscribedBootstrapper.InitializationCompleted -=
                HandleBootstrapInitializationCompleted;
            subscribedBootstrapper = null;
        }

        private void HandleLocaleChanged(string _)
        {
            ApplyCurrentLocale();
        }

        private void ApplyCurrentLocale()
        {
            if (targetText == null
                || localizationService == null
                || string.IsNullOrWhiteSpace(localizationKey))
            {
                return;
            }

            targetText.text = localizationService.GetText(localizationKey);
        }

        private void ResolveTargetText()
        {
            if (targetText == null)
                targetText = GetComponent<TMP_Text>();
        }

        private void Reset()
        {
            ResolveTargetText();
        }
    }
}
