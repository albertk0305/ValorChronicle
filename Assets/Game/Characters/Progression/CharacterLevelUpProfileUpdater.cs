using System;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Characters.Progression
{
    public static class CharacterLevelUpProfileUpdater
    {
        public static bool TryApply(
            ProfileSaveData profile,
            string characterId,
            int targetLevel,
            out CharacterLevelUpResult result,
            out CharacterLevelUpPersistenceStatus failureStatus)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            result = null;
            failureStatus = CharacterLevelUpPersistenceStatus.InvalidProfile;

            if (string.IsNullOrWhiteSpace(characterId))
            {
                failureStatus =
                    CharacterLevelUpPersistenceStatus.InvalidCharacterId;
                return false;
            }

            if (profile.Characters == null || profile.Currencies == null)
            {
                return false;
            }

            CharacterSaveData character = null;
            for (int i = 0; i < profile.Characters.Count; i++)
            {
                CharacterSaveData candidate = profile.Characters[i];
                if (candidate == null)
                {
                    return false;
                }

                if (!string.Equals(
                    candidate.CharacterId,
                    characterId,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                if (character != null)
                {
                    return false;
                }

                character = candidate;
            }

            if (character == null)
            {
                failureStatus =
                    CharacterLevelUpPersistenceStatus.CharacterNotOwned;
                return false;
            }

            int currentLevel = character.Level;
            if (currentLevel < CharacterLevelUpCostCalculator.MinimumLevel ||
                currentLevel > CharacterLevelUpCostCalculator.MaximumLevel ||
                profile.Currencies.BattleRecords < 0)
            {
                return false;
            }

            if (targetLevel < CharacterLevelUpCostCalculator.MinimumLevel ||
                targetLevel > CharacterLevelUpCostCalculator.MaximumLevel)
            {
                failureStatus =
                    CharacterLevelUpPersistenceStatus.InvalidTargetLevel;
                return false;
            }

            if (CharacterLevelUpCostCalculator.IsMaximumLevel(currentLevel))
            {
                failureStatus =
                    CharacterLevelUpPersistenceStatus.MaximumLevelReached;
                return false;
            }

            if (targetLevel <= currentLevel)
            {
                failureStatus = CharacterLevelUpPersistenceStatus
                    .TargetLevelNotHigher;
                return false;
            }

            long cost = CharacterLevelUpCostCalculator.GetTotalCost(
                currentLevel,
                targetLevel);
            if (profile.Currencies.BattleRecords < cost)
            {
                failureStatus = CharacterLevelUpPersistenceStatus
                    .InsufficientBattleRecords;
                return false;
            }

            long remainingBattleRecords = checked(
                profile.Currencies.BattleRecords - cost);

            profile.Currencies.BattleRecords = remainingBattleRecords;
            character.Level = targetLevel;
            result = new CharacterLevelUpResult(
                characterId,
                currentLevel,
                targetLevel,
                cost,
                remainingBattleRecords);
            failureStatus = CharacterLevelUpPersistenceStatus.Success;
            return true;
        }
    }
}
