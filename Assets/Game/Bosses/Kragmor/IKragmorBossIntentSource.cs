namespace ValorChronicle.Bosses.Kragmor
{
    public interface IKragmorBossIntentSource
    {
        KragmorBossIntent NextIntent { get; }
    }
}
