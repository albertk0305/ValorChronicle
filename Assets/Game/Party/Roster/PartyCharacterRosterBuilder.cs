using System;
using System.Collections.Generic;
using ValorChronicle.Characters.Build;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Party.Roster
{
    public sealed class PartyCharacterRosterBuilder
    {
        private readonly DefinitionDatabase definitionDatabase;
        private readonly CharacterBuildResolver buildResolver;
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
            this.warningReporter = warningReporter;
            contentOrderByCharacterId = BuildContentOrderLookup(
                definitionDatabase.Characters);
        }

        public IReadOnlyList<CharacterRosterEntry> Build(
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

                ResolvedCharacterBuild build = buildResolver.Resolve(
                    definition,
                    owned.Level,
                    owned.Awakening);
                entries.Add(new CharacterRosterEntry(
                    owned.CharacterId,
                    definition.Element,
                    owned.Level,
                    owned.Awakening,
                    build.MaxHp,
                    build.Attack,
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
