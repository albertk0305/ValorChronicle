using ValorChronicle.Characters.Marea;

namespace ValorChronicle.Characters.Build
{
    public static class CharacterBuildResolverFactory
    {
        public static CharacterBuildResolver CreateDefault()
        {
            return new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new MareaBluefangBuildModifier()
                });
        }
    }
}
