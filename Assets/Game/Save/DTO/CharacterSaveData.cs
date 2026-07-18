namespace ValorChronicle.Save.DTO
{
    public sealed class CharacterSaveData
    {
        public string CharacterId { get; set; }
        public int Level { get; set; }
        public int Awakening { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsNew { get; set; }
    }
}
