namespace ValorChronicle.Battle.Combat.Effects
{
    public enum EffectStackPolicy
    {
        RefreshDuration = 0,
        SeparateInstance = 1,
        StackMagnitude = 2,
        ReplaceWithStronger = 3,
        ReplaceWithNewest = 4,
        Unique = 5,
        StackCount = 6
    }
}
