using System;
using System.Collections.Generic;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Rewards;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Characters.Acquisition
{
    public sealed class CharacterAcquisitionProfileUpdater
    {
        public const long MaxAwakeningDuplicateHeroTokens = 60L;

        private readonly DefinitionDatabase database;

        public CharacterAcquisitionProfileUpdater(
            DefinitionDatabase database)
        {
            this.database = database
                ?? throw new ArgumentNullException(nameof(database));
            if (!database.IsInitialized)
            {
                throw new ArgumentException(
                    "Definition database must be initialized.",
                    nameof(database));
            }
        }

        public CharacterAcquisitionResult Apply(
            ProfileSaveData profile,
            string characterId)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "Character ID cannot be null or whitespace.",
                    nameof(characterId));
            }

            if (!database.TryGetCharacter(
                    characterId,
                    out CharacterDefinition _))
            {
                throw new KeyNotFoundException(
                    $"Character definition was not found: {characterId}.");
            }

            if (profile.Characters == null)
            {
                throw new ArgumentException(
                    "Profile character collection is required.",
                    nameof(profile));
            }

            CharacterSaveData owned = null;
            var ownedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < profile.Characters.Count; index++)
            {
                CharacterSaveData character = profile.Characters[index];
                ValidateOwnedCharacter(character, index, profile);
                if (!ownedIds.Add(character.CharacterId))
                {
                    throw new ArgumentException(
                        "Profile contains duplicate character IDs.",
                        nameof(profile));
                }

                if (string.Equals(
                        character.CharacterId,
                        characterId,
                        StringComparison.Ordinal))
                {
                    owned = character;
                }
            }

            if (owned == null)
            {
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = characterId,
                    Level = SaveRules.CharacterMinLevel,
                    Awakening = SaveRules.CharacterMinAwakening,
                    IsFavorite = false,
                    IsNew = true
                });
                return new CharacterAcquisitionResult(
                    CharacterAcquisitionKind.NewCharacter,
                    characterId,
                    null,
                    SaveRules.CharacterMinAwakening,
                    null);
            }

            int previousAwakening = owned.Awakening;
            if (previousAwakening < SaveRules.CharacterMaxAwakening)
            {
                owned.Awakening = checked(previousAwakening + 1);
                return new CharacterAcquisitionResult(
                    CharacterAcquisitionKind.AwakeningIncreased,
                    characterId,
                    previousAwakening,
                    owned.Awakening,
                    null);
            }

            CurrencyGrantResult currencyGrant = CurrencyProfileUpdater.Add(
                profile,
                RewardCurrencyKind.HeroTokens,
                MaxAwakeningDuplicateHeroTokens);
            return new CharacterAcquisitionResult(
                CharacterAcquisitionKind
                    .MaxAwakeningConvertedToHeroTokens,
                characterId,
                previousAwakening,
                previousAwakening,
                currencyGrant);
        }

        private static void ValidateOwnedCharacter(
            CharacterSaveData character,
            int index,
            ProfileSaveData profile)
        {
            if (character == null
                || string.IsNullOrWhiteSpace(character.CharacterId)
                || character.Level < SaveRules.CharacterMinLevel
                || character.Level > SaveRules.CharacterMaxLevel
                || character.Awakening
                    < SaveRules.CharacterMinAwakening
                || character.Awakening
                    > SaveRules.CharacterMaxAwakening
                || character.EquippedRelicInstanceIds == null
                || character.EquippedRelicInstanceIds.Count
                    != SaveRules.RelicSlotCountPerCharacter)
            {
                throw new ArgumentException(
                    $"Profile character at index {index} is invalid.",
                    nameof(profile));
            }
        }
    }
}
