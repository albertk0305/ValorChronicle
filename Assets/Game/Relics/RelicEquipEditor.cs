using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics
{
    /// <summary>
    /// Evaluates and calculates relic slot edits without mutating profile data.
    /// </summary>
    public sealed class RelicEquipEditor
    {
        public RelicEquipAvailability Evaluate(
            ProfileSaveData profile,
            string characterId,
            int targetSlotIndex,
            string candidateRelicInstanceId)
        {
            if (!TryGetTargetCharacter(
                    profile,
                    characterId,
                    targetSlotIndex,
                    out CharacterSaveData character,
                    out RelicEquipAvailabilityReason targetFailure))
            {
                return CreateAvailability(targetFailure, null);
            }

            if (!TryGetRelic(
                    profile?.RelicInstances,
                    candidateRelicInstanceId,
                    out RelicInstanceSaveData candidate))
            {
                return CreateAvailability(
                    RelicEquipAvailabilityReason.RelicInstanceNotFound,
                    null);
            }

            var index = new RelicEquipIndex(profile);
            if (index.TryGetLocation(
                    candidateRelicInstanceId,
                    out RelicEquipLocation location))
            {
                if (!string.Equals(
                        location.CharacterId,
                        characterId,
                        StringComparison.Ordinal))
                {
                    return CreateAvailability(
                        RelicEquipAvailabilityReason
                            .EquippedByOtherCharacter,
                        location);
                }

                if (location.SlotIndex != targetSlotIndex)
                {
                    return CreateAvailability(
                        RelicEquipAvailabilityReason.EquippedInOtherSlot,
                        location);
                }

                return CreateAvailability(
                    RelicEquipAvailabilityReason.Allowed,
                    location);
            }

            for (int slotIndex = 0;
                slotIndex < character.EquippedRelicInstanceIds.Count;
                slotIndex++)
            {
                if (slotIndex == targetSlotIndex)
                {
                    continue;
                }

                string equippedInstanceId =
                    character.EquippedRelicInstanceIds[slotIndex];
                if (!TryGetRelic(
                        profile.RelicInstances,
                        equippedInstanceId,
                        out RelicInstanceSaveData equipped))
                {
                    continue;
                }

                if (string.Equals(
                        equipped.RelicDefinitionId,
                        candidate.RelicDefinitionId,
                        StringComparison.Ordinal))
                {
                    return CreateAvailability(
                        RelicEquipAvailabilityReason
                            .DuplicateDefinitionInOtherSlot,
                        null);
                }
            }

            return CreateAvailability(
                RelicEquipAvailabilityReason.Allowed,
                null);
        }

        public RelicEquipEditResult Equip(
            ProfileSaveData profile,
            string characterId,
            int targetSlotIndex,
            string candidateRelicInstanceId)
        {
            RelicEquipAvailability availability = Evaluate(
                profile,
                characterId,
                targetSlotIndex,
                candidateRelicInstanceId);
            if (!TryGetTargetCharacter(
                    profile,
                    characterId,
                    targetSlotIndex,
                    out CharacterSaveData character,
                    out _))
            {
                return new RelicEquipEditResult(
                    RelicEquipEditOperationType.None,
                    availability,
                    Array.Empty<string>());
            }

            string[] slots = CopySlots(
                character.EquippedRelicInstanceIds);
            if (!availability.CanSelect)
            {
                return new RelicEquipEditResult(
                    RelicEquipEditOperationType.None,
                    availability,
                    slots);
            }

            RelicEquipEditOperationType operationType =
                string.Equals(
                    slots[targetSlotIndex],
                    candidateRelicInstanceId,
                    StringComparison.Ordinal)
                    ? RelicEquipEditOperationType.NoChange
                    : RelicEquipEditOperationType.Equip;
            slots[targetSlotIndex] = candidateRelicInstanceId;
            return new RelicEquipEditResult(
                operationType,
                availability,
                slots);
        }

        public RelicEquipEditResult Clear(
            ProfileSaveData profile,
            string characterId,
            int targetSlotIndex)
        {
            if (!TryGetTargetCharacter(
                    profile,
                    characterId,
                    targetSlotIndex,
                    out CharacterSaveData character,
                    out RelicEquipAvailabilityReason failure))
            {
                return new RelicEquipEditResult(
                    RelicEquipEditOperationType.None,
                    CreateAvailability(failure, null),
                    Array.Empty<string>());
            }

            string[] slots = CopySlots(
                character.EquippedRelicInstanceIds);
            RelicEquipEditOperationType operationType =
                slots[targetSlotIndex] == SaveRules.EmptyId
                    ? RelicEquipEditOperationType.NoChange
                    : RelicEquipEditOperationType.Clear;
            slots[targetSlotIndex] = SaveRules.EmptyId;
            return new RelicEquipEditResult(
                operationType,
                CreateAvailability(
                    RelicEquipAvailabilityReason.Allowed,
                    null),
                slots);
        }

        internal static bool TryGetTargetCharacter(
            ProfileSaveData profile,
            string characterId,
            int targetSlotIndex,
            out CharacterSaveData character,
            out RelicEquipAvailabilityReason failure)
        {
            character = FindCharacter(profile?.Characters, characterId);
            if (character == null)
            {
                failure = RelicEquipAvailabilityReason.CharacterNotFound;
                return false;
            }

            if (targetSlotIndex < 0
                || targetSlotIndex
                    >= SaveRules.RelicSlotCountPerCharacter
                || character.EquippedRelicInstanceIds == null
                || character.EquippedRelicInstanceIds.Count
                    != SaveRules.RelicSlotCountPerCharacter)
            {
                failure = RelicEquipAvailabilityReason.InvalidTargetSlot;
                return false;
            }

            failure = RelicEquipAvailabilityReason.Allowed;
            return true;
        }

        private static CharacterSaveData FindCharacter(
            IList<CharacterSaveData> characters,
            string characterId)
        {
            if (characters == null || string.IsNullOrEmpty(characterId))
            {
                return null;
            }

            for (int index = 0; index < characters.Count; index++)
            {
                CharacterSaveData character = characters[index];
                if (character != null
                    && string.Equals(
                        character.CharacterId,
                        characterId,
                        StringComparison.Ordinal))
                {
                    return character;
                }
            }

            return null;
        }

        private static bool TryGetRelic(
            IList<RelicInstanceSaveData> relics,
            string instanceId,
            out RelicInstanceSaveData relic)
        {
            if (relics != null && !string.IsNullOrEmpty(instanceId))
            {
                for (int index = 0; index < relics.Count; index++)
                {
                    RelicInstanceSaveData candidate = relics[index];
                    if (candidate != null
                        && string.Equals(
                            candidate.InstanceId,
                            instanceId,
                            StringComparison.Ordinal))
                    {
                        relic = candidate;
                        return true;
                    }
                }
            }

            relic = null;
            return false;
        }

        private static string[] CopySlots(IReadOnlyList<string> slots)
        {
            var copy = new string[slots.Count];
            for (int index = 0; index < slots.Count; index++)
            {
                copy[index] = slots[index];
            }

            return copy;
        }

        private static RelicEquipAvailability CreateAvailability(
            RelicEquipAvailabilityReason reason,
            RelicEquipLocation location)
        {
            return new RelicEquipAvailability(reason, location);
        }
    }
}
