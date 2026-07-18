using System;

namespace ValorChronicle.Save.Services
{
    /// <summary>Creates lowercase, separator-free GUID profile identifiers.</summary>
    public sealed class GuidProfileIdProvider : IProfileIdProvider
    {
        /// <inheritdoc />
        public string CreateProfileId() => Guid.NewGuid().ToString("N");
    }
}
