using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;

namespace ValorChronicle.Gacha.Presentation
{
    public sealed class GachaInfoPanelPresenter : MonoBehaviour
    {
        public event Action Opened;
        public event Action Closed;

        private GachaInfoPanelView view;
        private DefinitionDatabase database;
        private LocalizationService localization;
        private bool subscribed;
        private bool localizationSubscribed;
        private string openGachaId = string.Empty;
        private string openLocalizationKey = string.Empty;

        public bool IsConfigured => view != null && view.IsConfigured;
        public bool IsInitialized =>
            database != null && localization != null;
        public bool IsOpen => view != null && view.IsOpen;

        private void OnEnable()
        {
            Subscribe();
            SubscribeLocalization();
            RefreshOpenContent();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnsubscribeLocalization();
        }

        public void Configure(GachaInfoPanelView configuredView)
        {
            Unsubscribe();
            view = configuredView
                ?? throw new ArgumentNullException(nameof(configuredView));
            view.Close();
            ClearOpenContent();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Initialize(
            DefinitionDatabase initializedDatabase,
            LocalizationService initializedLocalization)
        {
            UnsubscribeLocalization();
            database = initializedDatabase
                ?? throw new ArgumentNullException(
                    nameof(initializedDatabase));
            localization = initializedLocalization
                ?? throw new ArgumentNullException(
                    nameof(initializedLocalization));
            if (isActiveAndEnabled)
            {
                SubscribeLocalization();
            }
        }

        public bool Open(string gachaId)
        {
            if (!IsConfigured || !IsInitialized)
            {
                Fail("Info panel services are not initialized.");
                return false;
            }

            if (!database.IsInitialized
                || !database.TryGetGacha(
                    gachaId,
                    out GachaDefinition definition))
            {
                Fail($"GachaDefinition '{gachaId}' was not found.");
                return false;
            }

            if (!TryBuildText(
                    definition,
                    localization,
                    out string text,
                    out string error))
            {
                Fail(error);
                return false;
            }

            view.Open(text);
            openGachaId = gachaId;
            openLocalizationKey = string.Empty;
            Opened?.Invoke();
            return true;
        }

        public bool ShowLocalizedMessage(string localizationKey)
        {
            if (!IsConfigured || !IsInitialized
                || string.IsNullOrWhiteSpace(localizationKey))
            {
                Fail("A localized Gacha message could not be shown.");
                return false;
            }

            view.Open(localization.GetText(localizationKey));
            openGachaId = string.Empty;
            openLocalizationKey = localizationKey;
            Opened?.Invoke();
            return true;
        }

        public bool ShowMessage(string message)
        {
            if (!IsConfigured
                || string.IsNullOrWhiteSpace(message))
            {
                Fail("A Gacha message could not be shown.");
                return false;
            }

            view.Open(message);
            ClearOpenContent();
            Opened?.Invoke();
            return true;
        }

        public void SetEntryButtonsInteractable(bool interactable)
        {
            view?.SetEntryButtonsInteractable(interactable);
        }

        public static bool TryBuildText(
            GachaDefinition definition,
            LocalizationService localization,
            out string text,
            out string error)
        {
            text = string.Empty;
            error = string.Empty;
            if (definition == null || localization == null)
            {
                error = "Gacha definition and localization are required.";
                return false;
            }

            if (definition.RewardEntries == null
                || definition.RewardEntries.Count == 0
                || definition.SinglePullCost <= 0
                || definition.TenPullCost <= 0
                || definition.PityPullCount <= 0)
            {
                error = $"GachaDefinition '{definition.Id}' is invalid.";
                return false;
            }

            string bannerKey = GetBannerNameKey(definition.Id);
            string pityRewardKey = GetPityRewardNameKey(
                definition.GachaKind);
            if (bannerKey == null || pityRewardKey == null)
            {
                error = $"GachaDefinition '{definition.Id}' is unsupported.";
                return false;
            }

            var builder = new StringBuilder(1024);
            builder.AppendLine(localization.GetText(bannerKey));
            builder.AppendLine();
            builder.AppendLine(localization.GetText(
                "gacha.info.rewards_heading"));

            int totalWeight = 0;
            for (int index = 0;
                index < definition.RewardEntries.Count;
                index++)
            {
                GachaRewardEntry reward =
                    definition.RewardEntries[index];
                if (reward == null
                    || reward.Quantity <= 0
                    || reward.Weight <= 0)
                {
                    error = $"Gacha reward entry {index} is invalid.";
                    return false;
                }

                totalWeight = checked(totalWeight + reward.Weight);
                string rewardNameKey = GetRewardNameKey(
                    reward.RewardKind);
                if (rewardNameKey == null)
                {
                    error = $"Gacha reward entry {index} has an invalid "
                        + "reward kind.";
                    return false;
                }

                decimal percentage =
                    reward.Weight * 100m
                    / GachaDefinition.RequiredTotalWeight;
                builder.AppendLine(localization.Format(
                    "gacha.info.reward_line",
                    Arguments(
                        ("reward", localization.GetText(
                            rewardNameKey)),
                        ("quantity", reward.Quantity.ToString(
                            "N0",
                            CultureInfo.InvariantCulture)),
                        ("percentage", percentage.ToString(
                            "0.###",
                            CultureInfo.InvariantCulture)))));
            }

            if (totalWeight != GachaDefinition.RequiredTotalWeight)
            {
                error = $"GachaDefinition '{definition.Id}' weight total "
                    + $"is {totalWeight}.";
                return false;
            }

            builder.AppendLine();
            builder.AppendLine(localization.Format(
                "gacha.info.costs",
                Arguments(
                    ("single", definition.SinglePullCost.ToString(
                        "N0",
                        CultureInfo.InvariantCulture)),
                    ("ten", definition.TenPullCost.ToString(
                        "N0",
                        CultureInfo.InvariantCulture)))));
            builder.AppendLine(localization.Format(
                "gacha.info.pity_count",
                Arguments(("count", definition.PityPullCount))));
            builder.AppendLine(localization.Format(
                "gacha.info.pity_condition",
                Arguments(
                    ("count", definition.PityPullCount),
                    ("reward", localization.GetText(pityRewardKey)))));
            builder.AppendLine(localization.Format(
                "gacha.info.pity_reset",
                Arguments(("reward", localization.GetText(
                    pityRewardKey)))));
            builder.Append(localization.GetText(
                "gacha.info.ten_pull_notice"));
            text = builder.ToString();
            return true;
        }

        private void Subscribe()
        {
            if (subscribed || view == null)
            {
                return;
            }

            view.CharacterInfoRequested += HandleCharacterInfoRequested;
            view.RelicInfoRequested += HandleRelicInfoRequested;
            view.CloseRequested += HandleCloseRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            view.CharacterInfoRequested -= HandleCharacterInfoRequested;
            view.RelicInfoRequested -= HandleRelicInfoRequested;
            view.CloseRequested -= HandleCloseRequested;
            subscribed = false;
        }

        private void HandleCharacterInfoRequested()
        {
            Open(GachaScreenController.CharacterGachaId);
        }

        private void HandleRelicInfoRequested()
        {
            Open(GachaScreenController.RelicGachaId);
        }

        private void HandleCloseRequested()
        {
            view.Close();
            ClearOpenContent();
            Closed?.Invoke();
        }

        private void SubscribeLocalization()
        {
            if (localizationSubscribed || localization == null)
            {
                return;
            }

            localization.LocaleChanged += HandleLocaleChanged;
            localizationSubscribed = true;
        }

        private void UnsubscribeLocalization()
        {
            if (!localizationSubscribed)
            {
                return;
            }

            localization.LocaleChanged -= HandleLocaleChanged;
            localizationSubscribed = false;
        }

        private void HandleLocaleChanged(string _)
        {
            RefreshOpenContent();
        }

        private void RefreshOpenContent()
        {
            if (view == null || !view.IsOpen)
            {
                return;
            }

            if (!string.IsNullOrEmpty(openGachaId))
            {
                RefreshOpenGachaInfo();
                return;
            }

            if (!string.IsNullOrEmpty(openLocalizationKey))
            {
                view.RefreshOpenText(localization.GetText(
                    openLocalizationKey));
            }
        }

        private void RefreshOpenGachaInfo()
        {
            string error = string.Empty;
            if (database == null
                || !database.IsInitialized
                || !database.TryGetGacha(
                    openGachaId,
                    out GachaDefinition definition)
                || !TryBuildText(
                    definition,
                    localization,
                    out string text,
                    out error))
            {
                GameLogger.Error(
                    "[GachaInfoPanelPresenter] Open Gacha information "
                        + "could not be relocalized. "
                        + (error ?? string.Empty),
                    this);
                return;
            }

            view.RefreshOpenText(text);
        }

        private void ClearOpenContent()
        {
            openGachaId = string.Empty;
            openLocalizationKey = string.Empty;
        }

        private void Fail(string message)
        {
            view?.Close();
            ClearOpenContent();
            GameLogger.Error("[GachaInfoPanelPresenter] " + message, this);
        }

        private static string GetBannerNameKey(string gachaId)
        {
            if (string.Equals(
                gachaId,
                GachaScreenController.CharacterGachaId,
                StringComparison.Ordinal))
            {
                return "gacha.banner.character_standard.name";
            }

            if (string.Equals(
                gachaId,
                GachaScreenController.RelicGachaId,
                StringComparison.Ordinal))
            {
                return "gacha.banner.relic_standard.name";
            }

            return null;
        }

        private static string GetPityRewardNameKey(GachaKind kind)
        {
            return kind switch
            {
                GachaKind.Character =>
                    "gacha.reward.random_character",
                GachaKind.Relic => "gacha.reward.random_relic",
                _ => null
            };
        }

        private static string GetRewardNameKey(GachaRewardKind kind)
        {
            return kind switch
            {
                GachaRewardKind.RandomCharacter =>
                    "gacha.reward.random_character",
                GachaRewardKind.RandomRelic =>
                    "gacha.reward.random_relic",
                GachaRewardKind.HeroTokens =>
                    "gacha.reward.hero_tokens",
                GachaRewardKind.RelicTokens =>
                    "gacha.reward.relic_tokens",
                GachaRewardKind.BattleRecords =>
                    "gacha.reward.battle_records",
                _ => null
            };
        }

        private static IReadOnlyDictionary<string, object> Arguments(
            params (string Key, object Value)[] entries)
        {
            var arguments = new Dictionary<string, object>(
                entries.Length,
                StringComparer.Ordinal);
            for (int index = 0; index < entries.Length; index++)
            {
                arguments.Add(entries[index].Key, entries[index].Value);
            }

            return arguments;
        }
    }
}
