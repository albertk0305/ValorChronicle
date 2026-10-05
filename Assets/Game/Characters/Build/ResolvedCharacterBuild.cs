namespace ValorChronicle.Characters.Build
{
    public sealed class ResolvedCharacterBuild
    {
        internal ResolvedCharacterBuild(
            string characterId,
            int level,
            int awakening,
            long maxHp,
            long attack)
        {
            CharacterId = characterId;
            Level = level;
            Awakening = awakening;
            MaxHp = maxHp;
            Attack = attack;
        }

        public string CharacterId { get; }
        public int Level { get; }
        public int Awakening { get; }
        public long MaxHp { get; }
        public long Attack { get; }
    }
}
