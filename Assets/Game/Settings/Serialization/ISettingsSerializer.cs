using ValorChronicle.Settings.DTO;

namespace ValorChronicle.Settings.Serialization
{
    public interface ISettingsSerializer
    {
        string Serialize(GameSettingsData data);
        GameSettingsData Deserialize(string json);
    }
}
