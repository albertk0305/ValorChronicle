using System;
using System.Collections.Generic;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Party.Battle
{
    public sealed class BattlePartyResolver
    {
        private readonly DefinitionDatabase definitionDatabase;

        public BattlePartyResolver(DefinitionDatabase definitionDatabase)
        {
            this.definitionDatabase = definitionDatabase
                ?? throw new ArgumentNullException(
                    nameof(definitionDatabase));
        }

        public BattlePartyResolutionResult Resolve(ProfileSaveData profile)
        {
            if (!definitionDatabase.IsInitialized)
            {
                return BattlePartyResolutionResult.InvalidParty(
                    "DefinitionDatabase is not initialized.");
            }

            if (profile == null)
            {
                return BattlePartyResolutionResult.InvalidParty(
                    "Profile snapshot is missing.");
            }

            if (!TryGetActiveSlots(profile.Party, out var slots, out var error))
            {
                return BattlePartyResolutionResult.InvalidParty(error);
            }

            if (!TryBuildOwnedCharacterLookup(
                profile.Characters,
                out var ownedCharacters,
                out error))
            {
                return BattlePartyResolutionResult.InvalidParty(error);
            }

            var seenCharacterIds = new HashSet<string>(
                StringComparer.Ordinal);
            var members = new List<BattlePartyMemberInput>(
                SaveRules.PartySlotCount);

            for (int slotIndex = 0;
                slotIndex < SaveRules.PartySlotCount;
                slotIndex++)
            {
                string characterId = slots[slotIndex];
                if (characterId == string.Empty)
                {
                    continue;
                }

                if (characterId == null)
                {
                    return BattlePartyResolutionResult.InvalidParty(
                        $"Active party slot {slotIndex} is null.");
                }

                if (!seenCharacterIds.Add(characterId))
                {
                    return BattlePartyResolutionResult.InvalidParty(
                        $"Active party contains duplicate character "
                            + $"'{characterId}'.");
                }

                if (!ownedCharacters.TryGetValue(
                    characterId,
                    out var characterSaveData))
                {
                    return BattlePartyResolutionResult.InvalidParty(
                        $"Active party character '{characterId}' is not "
                            + "owned by the profile.");
                }

                if (!definitionDatabase.TryGetCharacter(
                    characterId,
                    out CharacterDefinition definition))
                {
                    return BattlePartyResolutionResult.InvalidParty(
                        $"CharacterDefinition '{characterId}' is missing.");
                }

                if (characterSaveData.Level < SaveRules.CharacterMinLevel
                    || characterSaveData.Level
                        > SaveRules.CharacterMaxLevel
                    || characterSaveData.Awakening
                        < SaveRules.CharacterMinAwakening
                    || characterSaveData.Awakening
                        > SaveRules.CharacterMaxAwakening)
                {
                    return BattlePartyResolutionResult.InvalidParty(
                        $"Owned character '{characterId}' has invalid "
                            + "progress values.");
                }

                members.Add(new BattlePartyMemberInput(
                    characterId,
                    slotIndex,
                    characterSaveData.Level,
                    characterSaveData.Awakening,
                    definition));
            }

            return members.Count == 0
                ? BattlePartyResolutionResult.EmptyParty()
                : BattlePartyResolutionResult.Succeeded(members);
        }

        private static bool TryGetActiveSlots(
            PartySaveData party,
            out IReadOnlyList<string> slots,
            out string error)
        {
            slots = null;

            if (party == null)
            {
                error = "Profile Party data is missing.";
                return false;
            }

            if (party.Presets == null
                || party.Presets.Count != SaveRules.PartyPresetCount)
            {
                error = $"Profile Party must contain exactly "
                    + $"{SaveRules.PartyPresetCount} presets.";
                return false;
            }

            if (party.ActivePresetIndex < 0
                || party.ActivePresetIndex >= party.Presets.Count)
            {
                error = $"ActivePresetIndex {party.ActivePresetIndex} is "
                    + "outside the available preset range.";
                return false;
            }

            PartyPresetSaveData activePreset =
                party.Presets[party.ActivePresetIndex];
            if (activePreset?.CharacterSlotIds == null
                || activePreset.CharacterSlotIds.Count
                    != SaveRules.PartySlotCount)
            {
                error = $"The active preset must contain exactly "
                    + $"{SaveRules.PartySlotCount} slots.";
                return false;
            }

            slots = activePreset.CharacterSlotIds;
            error = string.Empty;
            return true;
        }

        private static bool TryBuildOwnedCharacterLookup(
            IReadOnlyList<CharacterSaveData> characters,
            out Dictionary<string, CharacterSaveData> ownedCharacters,
            out string error)
        {
            ownedCharacters = null;

            if (characters == null)
            {
                error = "Profile Characters data is missing.";
                return false;
            }

            var lookup = new Dictionary<string, CharacterSaveData>(
                characters.Count,
                StringComparer.Ordinal);

            for (int index = 0; index < characters.Count; index++)
            {
                CharacterSaveData character = characters[index];
                if (character == null
                    || string.IsNullOrEmpty(character.CharacterId))
                {
                    error = $"Profile character entry {index} is invalid.";
                    return false;
                }

                if (!lookup.TryAdd(character.CharacterId, character))
                {
                    error = $"Profile Characters contains duplicate "
                        + $"character '{character.CharacterId}'.";
                    return false;
                }
            }

            ownedCharacters = lookup;
            error = string.Empty;
            return true;
        }
    }
}
