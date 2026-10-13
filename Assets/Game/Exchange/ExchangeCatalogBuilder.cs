using System;
using System.Collections.Generic;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Exchange
{
    public static class ExchangeCatalogBuilder
    {
        public static ExchangeCatalogData Build(
            DefinitionDatabase database,
            ExchangeCatalogDefinition catalog,
            ProfileSaveData profileSnapshot)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            if (!database.IsInitialized)
            {
                throw new ArgumentException(
                    "Definition database must be initialized.",
                    nameof(database));
            }
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (profileSnapshot == null)
                throw new ArgumentNullException(nameof(profileSnapshot));
            if (profileSnapshot.Currencies == null
                || profileSnapshot.Characters == null
                || profileSnapshot.RelicInstances == null)
            {
                throw new ArgumentException(
                    "Profile currencies, characters, and relic instances "
                        + "are required.",
                    nameof(profileSnapshot));
            }

            ValidateCost(catalog.CharacterExchangeCost, "character");
            ValidateCost(catalog.RelicExchangeCost, "relic");
            if (profileSnapshot.Currencies.HeroTokens < 0L
                || profileSnapshot.Currencies.RelicTokens < 0L)
            {
                throw new ArgumentException(
                    "Exchange currency amounts cannot be negative.",
                    nameof(profileSnapshot));
            }

            CharacterExchangeListItem[] characters = BuildCharacters(
                database,
                catalog,
                profileSnapshot);
            RelicExchangeListItem[] relics = BuildRelics(
                database,
                catalog,
                profileSnapshot);
            return new ExchangeCatalogData(characters, relics);
        }

        private static CharacterExchangeListItem[] BuildCharacters(
            DefinitionDatabase database,
            ExchangeCatalogDefinition catalog,
            ProfileSaveData profile)
        {
            List<string> ids = ValidateAndSortIds(
                catalog.CharacterIds,
                id => database.TryGetCharacter(id, out _),
                "character");
            var ownedById = new Dictionary<string, CharacterSaveData>(
                StringComparer.Ordinal);
            for (int index = 0; index < profile.Characters.Count; index++)
            {
                CharacterSaveData character = profile.Characters[index];
                if (character == null
                    || string.IsNullOrWhiteSpace(character.CharacterId)
                    || character.Awakening
                        < SaveRules.CharacterMinAwakening
                    || character.Awakening
                        > SaveRules.CharacterMaxAwakening
                    || !ownedById.TryAdd(character.CharacterId, character))
                {
                    throw new ArgumentException(
                        "Profile character data is invalid.",
                        nameof(profile));
                }
            }

            var results = new CharacterExchangeListItem[ids.Count];
            for (int index = 0; index < ids.Count; index++)
            {
                string id = ids[index];
                bool isOwned = ownedById.TryGetValue(
                    id,
                    out CharacterSaveData owned);
                int awakening = isOwned
                    ? owned.Awakening
                    : SaveRules.CharacterMinAwakening;
                bool isMaximum = isOwned
                    && awakening >= SaveRules.CharacterMaxAwakening;
                bool canAfford = profile.Currencies.HeroTokens
                    >= catalog.CharacterExchangeCost;
                ExchangeUnavailableReason unavailable =
                    GetUnavailableReasons(canAfford, isMaximum);
                ExchangeProgressDisplayData progress = isOwned
                    ? new ExchangeProgressDisplayData(
                        isMaximum
                            ? ExchangeProgressKind.MaximumAwakening
                            : ExchangeProgressKind.AwakeningIncrease,
                        true,
                        awakening,
                        isMaximum ? (int?)null : awakening + 1,
                        SaveRules.CharacterMaxAwakening)
                    : new ExchangeProgressDisplayData(
                        ExchangeProgressKind.Unowned,
                        false,
                        null,
                        SaveRules.CharacterMinAwakening,
                        SaveRules.CharacterMaxAwakening);
                results[index] = new CharacterExchangeListItem(
                    id,
                    isOwned,
                    awakening,
                    catalog.CharacterExchangeCost,
                    canAfford,
                    unavailable,
                    progress);
            }

            return results;
        }

        private static RelicExchangeListItem[] BuildRelics(
            DefinitionDatabase database,
            ExchangeCatalogDefinition catalog,
            ProfileSaveData profile)
        {
            List<string> ids = ValidateAndSortIds(
                catalog.RelicIds,
                id => database.TryGetRelic(id, out _),
                "relic");
            var results = new RelicExchangeListItem[ids.Count];
            for (int index = 0; index < ids.Count; index++)
            {
                string id = ids[index];
                if (!RelicAcquisitionTargetResolver.TryResolve(
                        profile.RelicInstances,
                        id,
                        out RelicAcquisitionTarget target))
                {
                    throw new ArgumentException(
                        "Profile relic instance data is invalid.",
                        nameof(profile));
                }

                bool canAfford = profile.Currencies.RelicTokens
                    >= catalog.RelicExchangeCost;
                ExchangeUnavailableReason unavailable = canAfford
                    ? ExchangeUnavailableReason.None
                    : ExchangeUnavailableReason.InsufficientCurrency;
                ExchangeProgressDisplayData progress;
                if (!target.HasMatchingInstance)
                {
                    progress = new ExchangeProgressDisplayData(
                        ExchangeProgressKind.Unowned,
                        false,
                        null,
                        SaveRules.RelicMinAwakening,
                        SaveRules.RelicMaxAwakening);
                }
                else if (target.CanIncreaseAwakening)
                {
                    progress = new ExchangeProgressDisplayData(
                        ExchangeProgressKind.AwakeningIncrease,
                        true,
                        target.CurrentAwakening,
                        target.CurrentAwakening + 1,
                        SaveRules.RelicMaxAwakening);
                }
                else
                {
                    progress = new ExchangeProgressDisplayData(
                        ExchangeProgressKind
                            .NewInstanceAfterMaximumAwakening,
                        true,
                        SaveRules.RelicMaxAwakening,
                        SaveRules.RelicMinAwakening,
                        SaveRules.RelicMaxAwakening);
                }

                results[index] = new RelicExchangeListItem(
                    id,
                    target.HasMatchingInstance,
                    target.AwakeningTargetInstanceId,
                    target.CurrentAwakening,
                    target.WillCreateNewInstance,
                    catalog.RelicExchangeCost,
                    canAfford,
                    unavailable,
                    progress);
            }

            return results;
        }

        private static List<string> ValidateAndSortIds(
            IReadOnlyList<string> source,
            Func<string, bool> definitionExists,
            string contentType)
        {
            var ids = new List<string>(source.Count);
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < source.Count; index++)
            {
                string id = source[index];
                if (string.IsNullOrWhiteSpace(id)
                    || !definitionExists(id))
                {
                    throw new InvalidOperationException(
                        $"Exchange {contentType} ID is not registered: "
                            + $"{(string.IsNullOrWhiteSpace(id) ? "<empty>" : id)}.");
                }

                if (!uniqueIds.Add(id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate Exchange {contentType} ID: {id}.");
                }

                ids.Add(id);
            }

            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        private static void ValidateCost(long cost, string contentType)
        {
            if (cost <= 0L)
            {
                throw new InvalidOperationException(
                    $"Exchange {contentType} cost must be greater than zero.");
            }
        }

        private static ExchangeUnavailableReason GetUnavailableReasons(
            bool canAfford,
            bool isMaximumAwakening)
        {
            ExchangeUnavailableReason result =
                ExchangeUnavailableReason.None;
            if (!canAfford)
                result |= ExchangeUnavailableReason.InsufficientCurrency;
            if (isMaximumAwakening)
                result |= ExchangeUnavailableReason.MaximumAwakening;
            return result;
        }
    }
}
