using System;
using ValorChronicle.Settings.Repository;
using ValorChronicle.Settings.Serialization;
using ValorChronicle.Settings.Validation;

namespace ValorChronicle.Settings.Services
{
    public static class SettingsSystemFactory
    {
        public static SettingsService Create(string persistentDataPath)
        {
            if (string.IsNullOrWhiteSpace(persistentDataPath))
            {
                throw new ArgumentException(
                    "Persistent data path cannot be empty or whitespace.",
                    nameof(persistentDataPath));
            }

            return new SettingsService(
                new SettingsRepository(new SettingsPaths(persistentDataPath)),
                new NewtonsoftJsonSettingsSerializer(),
                new SettingsValidator());
        }
    }
}
