using System;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Characters.Acquisition;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Gacha.Persistence;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Rewards;

namespace ValorChronicle.Gacha.Presentation
{
    public sealed class GachaResultPresenter : MonoBehaviour
    {
        public event Action Opened;
        public event Action Closed;

        private GachaResultView view;
        private CharacterPresentationCatalog characterCatalog;
        private Sprite heroTokenSprite;
        private Sprite relicTokenSprite;
        private Sprite battleRecordsSprite;
        private Sprite[] awakeningSprites = Array.Empty<Sprite>();
        private DefinitionDatabase database;
        private bool subscribed;

        public GachaResultView View => view;
        public bool IsConfigured =>
            view != null
            && view.IsConfigured
            && characterCatalog != null
            && heroTokenSprite != null
            && relicTokenSprite != null
            && battleRecordsSprite != null
            && awakeningSprites.Length == 6
            && Array.TrueForAll(
                awakeningSprites,
                sprite => sprite != null);
        public bool IsInitialized =>
            database != null
            && database.IsInitialized
            && characterCatalog != null
            && characterCatalog.IsInitialized;
        public bool IsOpen => view != null && view.IsOpen;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            GachaResultView configuredView,
            CharacterPresentationCatalog configuredCharacterCatalog,
            Sprite configuredHeroTokenSprite,
            Sprite configuredRelicTokenSprite,
            Sprite configuredBattleRecordsSprite,
            Sprite[] configuredAwakeningSprites)
        {
            Unsubscribe();
            view = configuredView
                ?? throw new ArgumentNullException(nameof(configuredView));
            characterCatalog = configuredCharacterCatalog
                ?? throw new ArgumentNullException(
                    nameof(configuredCharacterCatalog));
            heroTokenSprite = configuredHeroTokenSprite
                ?? throw new ArgumentNullException(
                    nameof(configuredHeroTokenSprite));
            relicTokenSprite = configuredRelicTokenSprite
                ?? throw new ArgumentNullException(
                    nameof(configuredRelicTokenSprite));
            battleRecordsSprite = configuredBattleRecordsSprite
                ?? throw new ArgumentNullException(
                    nameof(configuredBattleRecordsSprite));
            if (configuredAwakeningSprites == null
                || configuredAwakeningSprites.Length != 6
                || Array.Exists(
                    configuredAwakeningSprites,
                    sprite => sprite == null))
            {
                throw new ArgumentException(
                    "Awakening sprites 1 through 6 are required.",
                    nameof(configuredAwakeningSprites));
            }

            awakeningSprites =
                (Sprite[])configuredAwakeningSprites.Clone();
            view.Close();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Initialize(DefinitionDatabase initializedDatabase)
        {
            database = initializedDatabase
                ?? throw new ArgumentNullException(
                    nameof(initializedDatabase));
            if (!database.IsInitialized)
            {
                throw new ArgumentException(
                    "DefinitionDatabase must be initialized.",
                    nameof(initializedDatabase));
            }

            if (!characterCatalog.IsInitialized)
            {
                characterCatalog.Initialize();
            }
        }

        public bool ShowResult(GachaTransactionResult transaction)
        {
            if (!IsConfigured || !IsInitialized)
            {
                Fail("Result presentation is not initialized.");
                return false;
            }

            if (transaction == null
                || transaction.Pulls == null
                || transaction.PullCount != transaction.Pulls.Count
                || (transaction.PullCount != 1
                    && transaction.PullCount
                        != GachaResultView.MaximumResultCount))
            {
                Fail("Transaction result must contain one or ten pulls.");
                return false;
            }

            var entries = new GachaResultDisplayEntry[
                transaction.Pulls.Count];
            for (int index = 0; index < transaction.Pulls.Count; index++)
            {
                GachaTransactionPullResult pull =
                    transaction.Pulls[index];
                string error = string.Empty;
                if (pull == null
                    || pull.Index != index
                    || !TryCreateDisplayEntry(
                        pull,
                        out entries[index],
                        out error))
                {
                    Fail(string.IsNullOrEmpty(error)
                        ? "Transaction pull order is invalid."
                        : error);
                    return false;
                }
            }

            try
            {
                view.Show(entries);
                Opened?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                Fail("Result view rejected the transaction results.");
                return false;
            }
        }

        public void SetCloseInputEnabled(bool enabled)
        {
            view?.SetCloseInputEnabled(enabled);
        }

        public void Close(bool notify = true)
        {
            if (view == null)
            {
                return;
            }

            bool wasOpen = view.IsOpen;
            view.Close();
            if (notify && wasOpen)
            {
                Closed?.Invoke();
            }
        }

        public bool TryCreateIndividualDisplayEntry(
            GachaTransactionPullResult pull,
            out GachaIndividualDisplayEntry entry,
            out string error)
        {
            entry = null;
            if (!TryCreateDisplayEntry(
                pull,
                out GachaResultDisplayEntry summary,
                out error))
            {
                return false;
            }

            Sprite primarySprite = summary.PrimarySprite;
            Sprite overlaySprite = summary.AwakeningSprite;
            string overlayText = string.Empty;
            if (pull.RewardKind == GachaRewardKind.RandomCharacter)
            {
                if (!characterCatalog.TryGet(
                    pull.ContentId,
                    out CharacterPresentationDefinition presentation)
                    || presentation.PreviewSprite == null)
                {
                    error = "Character preview presentation is missing for "
                        + $"pull {pull.Index}: {pull.ContentId}.";
                    return false;
                }

                primarySprite = presentation.PreviewSprite;
                if (pull.GrantKind
                    == RewardGrantKind.CharacterAwakeningIncreased)
                {
                    overlayText = $"{pull.PreviousAwakening.Value} \u2192 "
                        + pull.NewAwakening.Value;
                }
                else if (pull.GrantKind == RewardGrantKind
                    .MaxAwakeningCharacterConvertedToHeroTokens)
                {
                    if (pull.GrantedCurrencyAmount
                        != CharacterAcquisitionProfileUpdater
                            .MaxAwakeningDuplicateHeroTokens)
                    {
                        error = $"Max-awakening reward at pull "
                            + $"{pull.Index} must grant 60 Hero Tokens.";
                        return false;
                    }

                    overlayText = "+" + pull.GrantedCurrencyAmount;
                }
            }
            else if (pull.RewardKind == GachaRewardKind.RandomRelic
                && pull.GrantKind
                    == RewardGrantKind.RelicAwakeningIncreased)
            {
                overlayText = $"{pull.PreviousAwakening.Value} \u2192 "
                    + pull.NewAwakening.Value;
            }

            entry = new GachaIndividualDisplayEntry(
                pull.Index,
                summary.Kind,
                primarySprite,
                summary.ItemAmount,
                overlaySprite,
                overlayText);
            return true;
        }

        public bool TryCreateDisplayEntry(
            GachaTransactionPullResult pull,
            out GachaResultDisplayEntry entry,
            out string error)
        {
            entry = null;
            error = string.Empty;
            if (!IsConfigured || !IsInitialized || pull == null)
            {
                error = "Result presentation or pull data is unavailable.";
                return false;
            }

            switch (pull.RewardKind)
            {
                case GachaRewardKind.RandomCharacter:
                    return TryCreateCharacterEntry(
                        pull,
                        out entry,
                        out error);
                case GachaRewardKind.RandomRelic:
                    return TryCreateRelicEntry(
                        pull,
                        out entry,
                        out error);
                case GachaRewardKind.HeroTokens:
                    return TryCreateCurrencyEntry(
                        pull,
                        RewardCurrencyKind.HeroTokens,
                        heroTokenSprite,
                        out entry,
                        out error);
                case GachaRewardKind.RelicTokens:
                    return TryCreateCurrencyEntry(
                        pull,
                        RewardCurrencyKind.RelicTokens,
                        relicTokenSprite,
                        out entry,
                        out error);
                case GachaRewardKind.BattleRecords:
                    return TryCreateCurrencyEntry(
                        pull,
                        RewardCurrencyKind.BattleRecords,
                        battleRecordsSprite,
                        out entry,
                        out error);
                default:
                    error = $"Pull {pull.Index} has an invalid reward kind.";
                    return false;
            }
        }

        public bool TryValidateResources(
            IReadOnlyList<GachaDefinition> definitions,
            out string error)
        {
            error = string.Empty;
            if (!IsConfigured || !IsInitialized)
            {
                error = "Result presentation is not initialized.";
                return false;
            }

            if (definitions == null || definitions.Count == 0)
            {
                error = "Gacha definitions are required for result UI "
                    + "validation.";
                return false;
            }

            for (int definitionIndex = 0;
                definitionIndex < definitions.Count;
                definitionIndex++)
            {
                GachaDefinition definition = definitions[definitionIndex];
                if (definition == null)
                {
                    error = $"Gacha definition {definitionIndex} is null.";
                    return false;
                }

                for (int index = 0;
                    index < definition.CharacterPoolIds.Count;
                    index++)
                {
                    string characterId =
                        definition.CharacterPoolIds[index];
                    if (!database.TryGetCharacter(characterId, out _)
                        || !characterCatalog.TryGet(
                            characterId,
                            out CharacterPresentationDefinition presentation)
                        || presentation.FaceSprite == null)
                    {
                        error = "Character result presentation is missing: "
                            + characterId;
                        return false;
                    }
                }

                for (int index = 0;
                    index < definition.RelicPoolIds.Count;
                    index++)
                {
                    string relicId = definition.RelicPoolIds[index];
                    if (!database.TryGetRelic(relicId, out _)
                        || !database.TryGetRelicPresentation(
                            relicId,
                            out RelicPresentationDefinition presentation)
                        || presentation.Icon == null)
                    {
                        error = "Relic result presentation is missing: "
                            + relicId;
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TryValidateIndividualResources(
            IReadOnlyList<GachaDefinition> definitions,
            out string error)
        {
            if (!TryValidateResources(definitions, out error))
            {
                return false;
            }

            for (int definitionIndex = 0;
                definitionIndex < definitions.Count;
                definitionIndex++)
            {
                GachaDefinition definition = definitions[definitionIndex];
                for (int index = 0;
                    index < definition.CharacterPoolIds.Count;
                    index++)
                {
                    string characterId = definition.CharacterPoolIds[index];
                    if (!characterCatalog.TryGet(
                        characterId,
                        out CharacterPresentationDefinition presentation)
                        || presentation.PreviewSprite == null)
                    {
                        error = "Character individual presentation is "
                            + "missing: " + characterId;
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private bool TryCreateCharacterEntry(
            GachaTransactionPullResult pull,
            out GachaResultDisplayEntry entry,
            out string error)
        {
            entry = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(pull.ContentId)
                || !database.TryGetCharacter(pull.ContentId, out _)
                || !characterCatalog.TryGet(
                    pull.ContentId,
                    out CharacterPresentationDefinition presentation)
                || presentation.FaceSprite == null)
            {
                error = $"Character presentation is missing for pull "
                    + $"{pull.Index}: {pull.ContentId}.";
                return false;
            }

            Sprite awakeningSprite;
            switch (pull.GrantKind)
            {
                case RewardGrantKind.NewCharacter:
                    if (pull.PreviousAwakening.HasValue
                        || pull.NewAwakening != 0)
                    {
                        error = $"New character result at pull "
                            + $"{pull.Index} is inconsistent.";
                        return false;
                    }

                    awakeningSprite = null;
                    break;
                case RewardGrantKind.CharacterAwakeningIncreased:
                    if (!TryGetAwakeningSprite(
                        pull,
                        maximumStage: 6,
                        out awakeningSprite,
                        out error))
                    {
                        return false;
                    }
                    break;
                case RewardGrantKind
                    .MaxAwakeningCharacterConvertedToHeroTokens:
                    if (pull.PreviousAwakening != 6
                        || pull.NewAwakening != 6
                        || pull.GrantedCurrencyKind
                            != RewardCurrencyKind.HeroTokens
                        || pull.GrantedCurrencyAmount <= 0)
                    {
                        error = $"Max-awakening character result at pull "
                            + $"{pull.Index} is inconsistent.";
                        return false;
                    }

                    awakeningSprite = heroTokenSprite;
                    break;
                default:
                    error = $"Character pull {pull.Index} has an invalid "
                        + "grant kind.";
                    return false;
            }

            entry = new GachaResultDisplayEntry(
                pull.Index,
                GachaResultDisplayKind.Character,
                presentation.FaceSprite,
                0,
                awakeningSprite);
            return true;
        }

        private bool TryCreateRelicEntry(
            GachaTransactionPullResult pull,
            out GachaResultDisplayEntry entry,
            out string error)
        {
            entry = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(pull.ContentId)
                || !database.TryGetRelic(pull.ContentId, out _)
                || !database.TryGetRelicPresentation(
                    pull.ContentId,
                    out RelicPresentationDefinition presentation)
                || presentation.Icon == null)
            {
                error = $"Relic presentation is missing for pull "
                    + $"{pull.Index}: {pull.ContentId}.";
                return false;
            }

            Sprite awakeningSprite;
            switch (pull.GrantKind)
            {
                case RewardGrantKind.NewRelicInstanceCreated:
                    if (pull.PreviousAwakening.HasValue
                        || pull.NewAwakening != 0)
                    {
                        error = $"New relic result at pull {pull.Index} "
                            + "is inconsistent.";
                        return false;
                    }

                    awakeningSprite = null;
                    break;
                case RewardGrantKind.RelicAwakeningIncreased:
                    if (!TryGetAwakeningSprite(
                        pull,
                        maximumStage: 5,
                        out awakeningSprite,
                        out error))
                    {
                        return false;
                    }
                    break;
                default:
                    error = $"Relic pull {pull.Index} has an invalid grant "
                        + "kind.";
                    return false;
            }

            entry = new GachaResultDisplayEntry(
                pull.Index,
                GachaResultDisplayKind.Relic,
                presentation.Icon,
                0,
                awakeningSprite);
            return true;
        }

        private static bool TryCreateCurrencyEntry(
            GachaTransactionPullResult pull,
            RewardCurrencyKind expectedKind,
            Sprite sprite,
            out GachaResultDisplayEntry entry,
            out string error)
        {
            entry = null;
            error = string.Empty;
            if (pull.GrantKind != RewardGrantKind.CurrencyGranted
                || pull.GrantedCurrencyKind != expectedKind
                || pull.GrantedCurrencyAmount <= 0
                || pull.Amount != pull.GrantedCurrencyAmount
                || sprite == null)
            {
                error = $"Currency result at pull {pull.Index} is invalid.";
                return false;
            }

            entry = new GachaResultDisplayEntry(
                pull.Index,
                GachaResultDisplayKind.Item,
                sprite,
                pull.GrantedCurrencyAmount,
                null);
            return true;
        }

        private bool TryGetAwakeningSprite(
            GachaTransactionPullResult pull,
            int maximumStage,
            out Sprite sprite,
            out string error)
        {
            sprite = null;
            error = string.Empty;
            if (!pull.PreviousAwakening.HasValue
                || !pull.NewAwakening.HasValue
                || pull.NewAwakening.Value
                    != pull.PreviousAwakening.Value + 1
                || pull.NewAwakening.Value < 1
                || pull.NewAwakening.Value > maximumStage)
            {
                error = $"Awakening result at pull {pull.Index} is invalid.";
                return false;
            }

            sprite = awakeningSprites[pull.NewAwakening.Value - 1];
            if (sprite == null)
            {
                error = $"Awakening sprite {pull.NewAwakening.Value} is "
                    + "missing.";
                return false;
            }

            return true;
        }

        private void Subscribe()
        {
            if (subscribed || view == null)
            {
                return;
            }

            view.CloseRequested += HandleCloseRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            view.CloseRequested -= HandleCloseRequested;
            subscribed = false;
        }

        private void HandleCloseRequested()
        {
            Close();
        }

        private void Fail(string message)
        {
            view?.Close();
            GameLogger.Error("[GachaResultPresenter] " + message, this);
        }
    }
}
