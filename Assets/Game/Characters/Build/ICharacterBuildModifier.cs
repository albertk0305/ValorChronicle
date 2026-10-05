namespace ValorChronicle.Characters.Build
{
    public interface ICharacterBuildModifier
    {
        string CharacterId { get; }

        CharacterBuildStatMultipliers GetStatMultipliers(int awakening);
    }
}
