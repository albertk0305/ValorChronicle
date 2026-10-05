namespace ValorChronicle.Characters.Progression
{
    public sealed class CharacterLevelUpResult
    {
        internal CharacterLevelUpResult(
            string characterId,
            int previousLevel,
            int newLevel,
            long spentBattleRecords,
            long remainingBattleRecords)
        {
            CharacterId = characterId;
            PreviousLevel = previousLevel;
            NewLevel = newLevel;
            SpentBattleRecords = spentBattleRecords;
            RemainingBattleRecords = remainingBattleRecords;
        }

        public string CharacterId { get; }
        public int PreviousLevel { get; }
        public int NewLevel { get; }
        public long SpentBattleRecords { get; }
        public long RemainingBattleRecords { get; }
    }
}
