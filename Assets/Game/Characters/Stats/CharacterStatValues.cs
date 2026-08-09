namespace ValorChronicle.Characters.Stats
{
    public sealed class CharacterStatValues
    {
        internal CharacterStatValues(long maxHp, long attack)
        {
            MaxHp = maxHp;
            Attack = attack;
        }

        public long MaxHp { get; }
        public long Attack { get; }
    }
}
