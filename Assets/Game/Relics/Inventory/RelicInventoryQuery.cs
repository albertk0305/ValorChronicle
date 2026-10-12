using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics.Inventory
{
    /// <summary>Builds sorted, UI-ready relic inventory entries.</summary>
    public sealed class RelicInventoryQuery
    {
        private readonly DefinitionDatabase definitionDatabase;
        private readonly LocalizationService localizationService;
        private readonly RelicEquipEditor equipEditor;

        public RelicInventoryQuery(
            DefinitionDatabase definitionDatabase,
            LocalizationService localizationService,
            RelicEquipEditor equipEditor)
        {
            this.definitionDatabase = definitionDatabase
                ?? throw new ArgumentNullException(
                    nameof(definitionDatabase));
            this.localizationService = localizationService
                ?? throw new ArgumentNullException(
                    nameof(localizationService));
            this.equipEditor = equipEditor
                ?? throw new ArgumentNullException(nameof(equipEditor));
            if (!definitionDatabase.IsInitialized)
            {
                throw new InvalidOperationException(
                    "DefinitionDatabase must be initialized before "
                        + "building a relic inventory query.");
            }
        }

        public IReadOnlyList<RelicInventoryEntry> BuildEntries(
            ProfileSaveData profile,
            string characterId,
            int targetSlotIndex,
            string locale)
        {
            CultureInfo culture = LocalizationCultureResolver.Resolve(
                locale);
            if (profile?.RelicInstances == null)
            {
                return Array.Empty<RelicInventoryEntry>();
            }

            string targetDefinitionId = FindTargetDefinitionId(
                profile,
                characterId,
                targetSlotIndex);
            var equipIndex = new RelicEquipIndex(profile);
            var entries = new List<RelicInventoryEntry>(
                profile.RelicInstances.Count);
            for (int index = 0;
                index < profile.RelicInstances.Count;
                index++)
            {
                RelicInstanceSaveData relic =
                    profile.RelicInstances[index];
                if (relic == null
                    || string.IsNullOrWhiteSpace(relic.InstanceId)
                    || string.IsNullOrWhiteSpace(
                        relic.RelicDefinitionId)
                    || !definitionDatabase.TryGetRelic(
                        relic.RelicDefinitionId,
                        out RelicDefinition definition))
                {
                    continue;
                }

                RelicEquipAvailability availability =
                    equipEditor.Evaluate(
                        profile,
                        characterId,
                        targetSlotIndex,
                        relic.InstanceId);
                bool isEquipped = equipIndex.TryGetLocation(
                    relic.InstanceId,
                    out RelicEquipLocation location);
                bool isEquippedInTargetSlot = isEquipped
                    && string.Equals(
                        location.CharacterId,
                        characterId,
                        StringComparison.Ordinal)
                    && location.SlotIndex == targetSlotIndex;
                bool matchesTargetDefinition =
                    !string.IsNullOrEmpty(targetDefinitionId)
                    && string.Equals(
                        relic.RelicDefinitionId,
                        targetDefinitionId,
                        StringComparison.Ordinal);

                entries.Add(new RelicInventoryEntry(
                    relic.InstanceId,
                    relic.RelicDefinitionId,
                    relic.Awakening,
                    localizationService.GetText(
                        definition.DisplayNameKey,
                        locale),
                    relic.IsLocked,
                    relic.IsNew,
                    isEquipped,
                    location?.CharacterId ?? SaveRules.EmptyId,
                    location?.SlotIndex
                        ?? SaveRules.UnequippedRelicSlotIndex,
                    isEquippedInTargetSlot,
                    !availability.CanSelect
                        || matchesTargetDefinition,
                    availability.CanSelect,
                    availability.Reason));
            }

            entries.Sort((left, right) => Compare(
                left,
                right,
                culture));
            return entries.AsReadOnly();
        }

        private string FindTargetDefinitionId(
            ProfileSaveData profile,
            string characterId,
            int targetSlotIndex)
        {
            if (!RelicEquipEditor.TryGetTargetCharacter(
                    profile,
                    characterId,
                    targetSlotIndex,
                    out CharacterSaveData character,
                    out _))
            {
                return SaveRules.EmptyId;
            }

            string targetInstanceId =
                character.EquippedRelicInstanceIds[targetSlotIndex];
            if (string.IsNullOrEmpty(targetInstanceId))
            {
                return SaveRules.EmptyId;
            }

            for (int index = 0;
                index < profile.RelicInstances.Count;
                index++)
            {
                RelicInstanceSaveData relic =
                    profile.RelicInstances[index];
                if (relic != null
                    && string.Equals(
                        relic.InstanceId,
                        targetInstanceId,
                        StringComparison.Ordinal))
                {
                    return relic.RelicDefinitionId
                        ?? SaveRules.EmptyId;
                }
            }

            return SaveRules.EmptyId;
        }

        private static int Compare(
            RelicInventoryEntry left,
            RelicInventoryEntry right,
            CultureInfo culture)
        {
            int comparison = right.Awakening.CompareTo(
                left.Awakening);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = culture.CompareInfo.Compare(
                left.DisplayName,
                right.DisplayName,
                CompareOptions.StringSort);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(
                    left.InstanceId,
                    right.InstanceId);
        }
    }
}
