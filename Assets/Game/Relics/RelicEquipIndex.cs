using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics
{
    /// <summary>
    /// Builds a deterministic reverse index from authoritative character slots.
    /// </summary>
    public sealed class RelicEquipIndex
    {
        private readonly Dictionary<string, RelicEquipLocation>
            locationsByInstanceId = new Dictionary<
                string,
                RelicEquipLocation>(StringComparer.Ordinal);

        public RelicEquipIndex(ProfileSaveData profile)
        {
            IList<CharacterSaveData> characters = profile?.Characters;
            if (characters == null)
            {
                return;
            }

            for (int characterIndex = 0;
                characterIndex < characters.Count;
                characterIndex++)
            {
                CharacterSaveData character = characters[characterIndex];
                if (character == null
                    || string.IsNullOrWhiteSpace(character.CharacterId)
                    || character.EquippedRelicInstanceIds == null)
                {
                    continue;
                }

                int slotCount = Math.Min(
                    character.EquippedRelicInstanceIds.Count,
                    SaveRules.RelicSlotCountPerCharacter);
                for (int slotIndex = 0;
                    slotIndex < slotCount;
                    slotIndex++)
                {
                    string instanceId =
                        character.EquippedRelicInstanceIds[slotIndex];
                    if (string.IsNullOrWhiteSpace(instanceId)
                        || locationsByInstanceId.ContainsKey(instanceId))
                    {
                        continue;
                    }

                    locationsByInstanceId.Add(
                        instanceId,
                        new RelicEquipLocation(
                            instanceId,
                            character.CharacterId,
                            slotIndex));
                }
            }
        }

        public bool TryGetLocation(
            string relicInstanceId,
            out RelicEquipLocation location)
        {
            if (string.IsNullOrWhiteSpace(relicInstanceId))
            {
                location = null;
                return false;
            }

            return locationsByInstanceId.TryGetValue(
                relicInstanceId,
                out location);
        }
    }
}
