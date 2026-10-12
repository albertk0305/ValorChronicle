using System;

namespace ValorChronicle.Relics.Acquisition
{
    /// <summary>Creates lowercase, separator-free GUID relic identifiers.</summary>
    public sealed class GuidRelicInstanceIdProvider
        : IRelicInstanceIdProvider
    {
        public string CreateRelicInstanceId() =>
            Guid.NewGuid().ToString("N");
    }
}
