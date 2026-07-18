namespace ValorChronicle.Save.Services
{
    /// <summary>Supplies identifiers for profiles that may need to be created.</summary>
    public interface IProfileIdProvider
    {
        /// <summary>Creates a non-empty stable-format profile identifier.</summary>
        string CreateProfileId();
    }
}
