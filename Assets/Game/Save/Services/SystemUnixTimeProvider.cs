using System;

namespace ValorChronicle.Save.Services
{
    /// <summary>Provides UTC Unix seconds from the system clock.</summary>
    public sealed class SystemUnixTimeProvider : IUnixTimeProvider
    {
        /// <inheritdoc />
        public long GetUtcUnixTimeSeconds() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
