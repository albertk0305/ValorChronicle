using ValorChronicle.Characters.Runtime;

namespace ValorChronicle.Characters.Build
{
    public static class CharacterBuildResolverFactory
    {
        public static CharacterBuildResolver CreateDefault()
        {
            return CharacterRuntimeRegistrationCatalog
                .CreateDefault()
                .CreateBuildResolver();
        }
    }
}
