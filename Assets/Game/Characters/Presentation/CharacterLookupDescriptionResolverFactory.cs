using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Localization;

namespace ValorChronicle.Characters.Presentation
{
    public static class CharacterLookupDescriptionResolverFactory
    {
        public static CharacterLookupDescriptionResolver CreateDefault(
            DefinitionDatabase definitionDatabase,
            LocalizationService localizationService)
        {
            return CharacterRuntimeRegistrationCatalog
                .CreateDefault()
                .CreateLookupDescriptionResolver(
                    definitionDatabase,
                    localizationService);
        }
    }
}
