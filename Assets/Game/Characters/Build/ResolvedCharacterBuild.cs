namespace ValorChronicle.Characters.Build
{
    public sealed class ResolvedCharacterBuild
    {
        internal ResolvedCharacterBuild(
            string characterId,
            int level,
            int awakening,
            long maxHp,
            long attack,
            double maxHpMultiplier,
            double attackMultiplier)
        {
            CharacterId = characterId;
            Level = level;
            Awakening = awakening;
            MaxHp = maxHp;
            Attack = attack;
            MaxHpMultiplier = maxHpMultiplier;
            AttackMultiplier = attackMultiplier;
        }

        public string CharacterId { get; }
        public int Level { get; }
        public int Awakening { get; }
        public long MaxHp { get; }
        public long Attack { get; }
        public double MaxHpMultiplier { get; }
        public double AttackMultiplier { get; }
    }
}
