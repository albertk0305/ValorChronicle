namespace ValorChronicle.Relics.Acquisition
{
    /// <summary>Supplies stable identifiers for newly acquired relic instances.</summary>
    public interface IRelicInstanceIdProvider
    {
        string CreateRelicInstanceId();
    }
}
