using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Save.Migration
{
    /// <summary>
    /// Moves version 2 relic equip relationships to ordered character slots.
    /// </summary>
    public sealed class V2ToV3RelicEquipMigration : ISaveMigrationStep
    {
        public int FromVersion => 2;
        public int ToVersion => 3;

        public ProfileSaveData Migrate(ProfileSaveData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source.SaveVersion != FromVersion)
            {
                throw new ArgumentException(
                    $"Migration requires save version {FromVersion}.",
                    nameof(source));
            }

            source.Characters ??= new List<CharacterSaveData>();
            source.RelicInstances ??= new List<RelicInstanceSaveData>();

            var charactersById = new Dictionary<string, CharacterSaveData>(
                StringComparer.Ordinal);
            var definitionsByCharacterId = new Dictionary<
                string,
                HashSet<string>>(StringComparer.Ordinal);
            for (int characterIndex = 0;
                characterIndex < source.Characters.Count;
                characterIndex++)
            {
                CharacterSaveData character =
                    source.Characters[characterIndex];
                if (character == null)
                {
                    continue;
                }

                character.EquippedRelicInstanceIds =
                    SaveRules.CreateEmptyRelicInstanceIdSlots();
                if (!string.IsNullOrWhiteSpace(character.CharacterId)
                    && !charactersById.ContainsKey(character.CharacterId))
                {
                    charactersById.Add(character.CharacterId, character);
                    definitionsByCharacterId.Add(
                        character.CharacterId,
                        new HashSet<string>(StringComparer.Ordinal));
                }
            }

            var equippedInstanceIds = new HashSet<string>(
                StringComparer.Ordinal);
            for (int relicIndex = 0;
                relicIndex < source.RelicInstances.Count;
                relicIndex++)
            {
                RelicInstanceSaveData relic =
                    source.RelicInstances[relicIndex];
                if (relic == null)
                {
                    continue;
                }

                string characterId = relic.EquippedCharacterId;
                int slotIndex = relic.EquippedSlotIndex;
                relic.EquippedCharacterId = SaveRules.EmptyId;
                relic.EquippedSlotIndex =
                    SaveRules.UnequippedRelicSlotIndex;

                if (string.IsNullOrEmpty(characterId)
                    || slotIndex == SaveRules.UnequippedRelicSlotIndex
                    || slotIndex < SaveRules.EquippedRelicSlotMinIndex
                    || slotIndex > SaveRules.EquippedRelicSlotMaxIndex
                    || string.IsNullOrWhiteSpace(relic.InstanceId)
                    || !charactersById.TryGetValue(
                        characterId,
                        out CharacterSaveData character))
                {
                    continue;
                }

                if (character.EquippedRelicInstanceIds[slotIndex]
                        != SaveRules.EmptyId
                    || equippedInstanceIds.Contains(relic.InstanceId))
                {
                    continue;
                }

                HashSet<string> definitions =
                    definitionsByCharacterId[characterId];
                string definitionId =
                    relic.RelicDefinitionId ?? SaveRules.EmptyId;
                if (!definitions.Add(definitionId))
                {
                    continue;
                }

                character.EquippedRelicInstanceIds[slotIndex] =
                    relic.InstanceId;
                equippedInstanceIds.Add(relic.InstanceId);
            }

            source.SaveVersion = ToVersion;
            return source;
        }
    }
}
