using System;
using ValorChronicle.Characters.Progression;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterLevelUpPreviewSession
    {
        public CharacterLevelUpPreviewSession(
            string characterId,
            int startingLevel,
            long startingBattleRecords,
            int awakening)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                throw new ArgumentException(
                    "A character ID is required.",
                    nameof(characterId));
            }

            CharacterLevelUpCostCalculator.IsMaximumLevel(startingLevel);
            if (startingBattleRecords < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingBattleRecords),
                    startingBattleRecords,
                    "Battle records cannot be negative.");
            }

            CharacterId = characterId;
            StartingLevel = startingLevel;
            PreviewTargetLevel = startingLevel;
            StartingBattleRecords = startingBattleRecords;
            PreviewRemainingBattleRecords = startingBattleRecords;
            Awakening = awakening;
        }

        public string CharacterId { get; }
        public int StartingLevel { get; }
        public int PreviewTargetLevel { get; private set; }
        public long StartingBattleRecords { get; }
        public long PreviewRemainingBattleRecords { get; private set; }
        public int Awakening { get; }

        public bool HasLevelChange => PreviewTargetLevel > StartingLevel;

        public bool TryAdvance()
        {
            if (CharacterLevelUpCostCalculator.IsMaximumLevel(
                PreviewTargetLevel))
            {
                return false;
            }

            int candidateTarget = checked(PreviewTargetLevel + 1);
            long totalCost = CharacterLevelUpCostCalculator.GetTotalCost(
                StartingLevel,
                candidateTarget);
            if (totalCost > StartingBattleRecords)
            {
                return false;
            }

            PreviewTargetLevel = candidateTarget;
            PreviewRemainingBattleRecords = checked(
                StartingBattleRecords - totalCost);
            return true;
        }
    }
}
