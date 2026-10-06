using ValorChronicle.Characters.Marea;
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
            return new CharacterLookupDescriptionResolver(
                definitionDatabase,
                localizationService,
                new ICharacterLookupDescriptionProvider[]
                {
                    new MareaBluefangLookupDescriptionProvider()
                });
        }
    }
}
