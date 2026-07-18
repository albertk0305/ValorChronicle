using System;
using System.Globalization;
using Newtonsoft.Json;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Save.Serialization
{
    public sealed class NewtonsoftJsonSaveSerializer : ISaveSerializer
    {
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture,
            Formatting = Formatting.Indented,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None
        };

        public string Serialize(ProfileSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return JsonConvert.SerializeObject(data, settings);
        }

        public ProfileSaveData Deserialize(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Save JSON cannot be empty or whitespace.", nameof(json));
            }

            ProfileSaveData data = JsonConvert.DeserializeObject<ProfileSaveData>(json, settings);

            if (data == null)
            {
                throw new JsonSerializationException("Save JSON root cannot be null.");
            }

            return data;
        }
    }
}
