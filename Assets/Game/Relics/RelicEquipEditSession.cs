using System;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics
{
    public enum RelicEquipSessionCommandType
    {
        EquipPreview = 0,
        ClearTargetSlot = 1
    }

    /// <summary>Describes a persistence request without executing it.</summary>
    public sealed class RelicEquipSessionCommand
    {
        internal RelicEquipSessionCommand(
            RelicEquipSessionCommandType commandType,
            string characterId,
            int targetSlotIndex,
            string expectedOriginalInstanceId,
            string relicInstanceId)
        {
            CommandType = commandType;
            CharacterId = characterId;
            TargetSlotIndex = targetSlotIndex;
            ExpectedOriginalInstanceId = expectedOriginalInstanceId;
            RelicInstanceId = relicInstanceId;
        }

        public RelicEquipSessionCommandType CommandType { get; }
        public string CharacterId { get; }
        public int TargetSlotIndex { get; }
        public string ExpectedOriginalInstanceId { get; }
        public string RelicInstanceId { get; }
    }

    /// <summary>
    /// Holds an isolated preview while editing one character relic slot.
    /// </summary>
    public sealed class RelicEquipEditSession
    {
        private readonly RelicEquipEditor editor;
        private readonly ProfileSaveData profileSnapshot;

        public RelicEquipEditSession(
            RelicEquipEditor editor,
            ProfileSaveData profile,
            string characterId,
            int targetSlotIndex)
        {
            this.editor = editor
                ?? throw new ArgumentNullException(nameof(editor));
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profileSnapshot = new SaveDataCloner().Clone(profile);
            if (!RelicEquipEditor.TryGetTargetCharacter(
                    profileSnapshot,
                    characterId,
                    targetSlotIndex,
                    out CharacterSaveData character,
                    out RelicEquipAvailabilityReason failure))
            {
                if (failure
                    == RelicEquipAvailabilityReason.InvalidTargetSlot)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(targetSlotIndex),
                        targetSlotIndex,
                        "The target relic slot is invalid.");
                }

                throw new ArgumentException(
                    "The character does not exist in the profile.",
                    nameof(characterId));
            }

            CharacterId = characterId;
            TargetSlotIndex = targetSlotIndex;
            OriginalInstanceId =
                character.EquippedRelicInstanceIds[targetSlotIndex];
            PreviewInstanceId = OriginalInstanceId;
        }

        public string CharacterId { get; }
        public int TargetSlotIndex { get; }
        public string OriginalInstanceId { get; }
        public string PreviewInstanceId { get; private set; }

        public RelicEquipAvailability TrySetPreview(
            string candidateRelicInstanceId)
        {
            RelicEquipAvailability availability = editor.Evaluate(
                profileSnapshot,
                CharacterId,
                TargetSlotIndex,
                candidateRelicInstanceId);
            if (availability.CanSelect)
            {
                PreviewInstanceId = candidateRelicInstanceId;
            }

            return availability;
        }

        public RelicEquipSessionCommand CreateEquipCommand()
        {
            return new RelicEquipSessionCommand(
                RelicEquipSessionCommandType.EquipPreview,
                CharacterId,
                TargetSlotIndex,
                OriginalInstanceId,
                PreviewInstanceId);
        }

        public RelicEquipSessionCommand CreateClearCommand()
        {
            return new RelicEquipSessionCommand(
                RelicEquipSessionCommandType.ClearTargetSlot,
                CharacterId,
                TargetSlotIndex,
                OriginalInstanceId,
                SaveRules.EmptyId);
        }
    }
}
