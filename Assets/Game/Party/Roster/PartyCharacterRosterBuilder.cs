using System;
using System.Collections.Generic;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Relics.Stats;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Party.Roster
{
    public sealed class PartyCharacterRosterBuilder
    {
        private readonly DefinitionDatabase definitionDatabase;
        private readonly CharacterBuildResolver buildResolver;
        private readonly RelicStatBonusResolver relicStatBonusResolver;
        private readonly Action<string> warningReporter;
        private readonly Dictionary<string, int> contentOrderByCharacterId;

        public PartyCharacterRosterBuilder(
            DefinitionDatabase definitionDatabase,
            Action<string> warningReporter = null,
            CharacterBuildResolver characterBuildResolver = null)
        {
            this.definitionDatabase = definitionDatabase
                ?? throw new ArgumentNullException(
                    nameof(definitionDatabase));
            buildResolver = characterBuildResolver
                ?? CharacterBuildResolverFactory.CreateDefault();
            relicStatBonusResolver = new RelicStatBonusResolver(
                definitionDatabase);
            this.warningReporter = warningReporter;
            contentOrderByCharacterId = BuildContentOrderLookup(
                definitionDatabase.Characters);
        }

        public IReadOnlyList<CharacterRosterEntry> Build(
            IReadOnlyList<CharacterSaveData> ownedCharacters)
        {
            return Build(profile: null, ownedCharacters);
        }

        public IReadOnlyList<CharacterRosterEntry> Build(
            ProfileSaveData profile)
        {
            if (profile?.Characters == null)
            {
                throw new ArgumentException(
                    "A profile with characters is required.",
                    nameof(profile));
            }

            return Build(profile, profile.Characters);
        }

        private IReadOnlyList<CharacterRosterEntry> Build(
            ProfileSaveData profile,
            IReadOnlyList<CharacterSaveData> ownedCharacters)
        {
            if (ownedCharacters == null)
            {
                throw new ArgumentNullException(nameof(ownedCharacters));
            }

            var entries = new List<CharacterRosterEntry>(
                ownedCharacters.Count);
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < ownedCharacters.Count; index++)
            {
                CharacterSaveData owned = ownedCharacters[index];
                if (owned == null || string.IsNullOrEmpty(owned.CharacterId))
                {
                    warningReporter?.Invoke(
                        $"Owned character entry {index} has no usable ID.");
                    continue;
                }

                if (!seenIds.Add(owned.CharacterId))
                {
                    warningReporter?.Invoke(
                        $"Owned character '{owned.CharacterId}' is duplicated.");
                    continue;
                }

                if (!definitionDatabase.TryGetCharacter(
                    owned.CharacterId,
                    out CharacterDefinition definition))
                {
                    warningReporter?.Invoke(
                        $"Owned character definition is missing: "
                            + owned.CharacterId);
                    continue;
                }

                RelicStatBonusSummary relicBonuses = profile == null
                    ? RelicStatBonusSummary.None
                    : relicStatBonusResolver.Resolve(profile, owned);
                ResolvedCharacterBuild build = buildResolver.Resolve(
                    definition,
                    owned.Level,
                    owned.Awakening,
                    relicBonuses);
                long displayedAttack =
                    CharacterStatCalculator.RoundFinalStat(
                        build.Attack
                            * (1d
                                + relicBonuses
                                    .AttackIncreaseRateSum));
                entries.Add(new CharacterRosterEntry(
                    owned.CharacterId,
                    definition.Element,
                    owned.Level,
                    owned.Awakening,
                    build.MaxHp,
                    displayedAttack,
                    contentOrderByCharacterId[owned.CharacterId]));
            }

            return entries;
        }

        private static Dictionary<string, int> BuildContentOrderLookup(
            IReadOnlyList<CharacterDefinition> definitions)
        {
            var result = new Dictionary<string, int>(
                definitions.Count,
                StringComparer.Ordinal);
            for (int index = 0; index < definitions.Count; index++)
            {
                CharacterDefinition definition = definitions[index];
                if (definition != null
                    && !string.IsNullOrEmpty(definition.Id))
                {
                    result[definition.Id] = index;
                }
            }

            return result;
        }
    }
}
