using System;
using System.Collections.Generic;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Party.Roster
{
    public sealed class PartyCharacterRosterBuilder
    {
        private readonly DefinitionDatabase definitionDatabase;
        private readonly Action<string> warningReporter;

        public PartyCharacterRosterBuilder(
            DefinitionDatabase definitionDatabase,
            Action<string> warningReporter = null)
        {
            this.definitionDatabase = definitionDatabase
                ?? throw new ArgumentNullException(
                    nameof(definitionDatabase));
            this.warningReporter = warningReporter;
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

                CharacterStatValues stats =
                    CharacterStatCalculator.Calculate(
                        definition,
                        owned.Level);
                entries.Add(new CharacterRosterEntry(
                    owned.CharacterId,
                    definition.Element,
                    owned.Level,
                    owned.Awakening,
                    stats.MaxHp,
                    stats.Attack));
            }

            return entries;
        }
    }
}
