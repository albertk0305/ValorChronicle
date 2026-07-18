using ValorChronicle.Save.DTO;

namespace ValorChronicle.Save.Serialization
{
    public interface ISaveSerializer
    {
        string Serialize(ProfileSaveData data);
        ProfileSaveData Deserialize(string json);
    }
}
