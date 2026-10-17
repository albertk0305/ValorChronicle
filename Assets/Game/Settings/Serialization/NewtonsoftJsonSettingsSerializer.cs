using System;
using System.Globalization;
using Newtonsoft.Json;
using ValorChronicle.Settings.DTO;

namespace ValorChronicle.Settings.Serialization
{
    public sealed class NewtonsoftJsonSettingsSerializer :
        ISettingsSerializer
    {
        private readonly JsonSerializerSettings settings =
            new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture,
                Formatting = Formatting.Indented,
                MetadataPropertyHandling =
                    MetadataPropertyHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                TypeNameHandling = TypeNameHandling.None
            };

        public string Serialize(GameSettingsData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return JsonConvert.SerializeObject(data, settings);
        }

        public GameSettingsData Deserialize(string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException(
                    "Settings JSON cannot be empty or whitespace.",
                    nameof(json));
            }

            GameSettingsData data =
                JsonConvert.DeserializeObject<GameSettingsData>(
                    json,
                    settings);
            if (data == null)
            {
                throw new JsonSerializationException(
                    "Settings JSON root cannot be null.");
            }

            return data;
        }
    }
}
