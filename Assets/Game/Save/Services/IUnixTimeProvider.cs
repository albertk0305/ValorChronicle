namespace ValorChronicle.Save.Services
{
    /// <summary>Provides the current UTC time as Unix seconds.</summary>
    public interface IUnixTimeProvider
    {
        /// <summary>Gets the current UTC Unix timestamp in seconds.</summary>
        long GetUtcUnixTimeSeconds();
    }
}
