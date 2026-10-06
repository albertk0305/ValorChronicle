using System;
using System.Collections.Generic;
using System.Text;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterLookupDescriptionResolver
    {
        public const string UnavailableKey =
            "character.lookup.description_unavailable";

        private readonly DefinitionDatabase definitionDatabase;
        private readonly LocalizationService localizationService;
        private readonly Dictionary<string, ICharacterLookupDescriptionProvider>
            providersByCharacterId;

        public CharacterLookupDescriptionResolver(
            DefinitionDatabase definitionDatabase,
            LocalizationService localizationService,
            IReadOnlyList<ICharacterLookupDescriptionProvider> providers)
        {
            this.definitionDatabase = definitionDatabase
                ?? throw new ArgumentNullException(nameof(definitionDatabase));
            this.localizationService = localizationService
                ?? throw new ArgumentNullException(nameof(localizationService));
            if (!definitionDatabase.IsInitialized)
            {
                throw new InvalidOperationException(
                    "DefinitionDatabase must be initialized.");
            }

            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            providersByCharacterId = new Dictionary<
                string,
                ICharacterLookupDescriptionProvider>(
                    providers.Count,
                    StringComparer.Ordinal);
            for (int index = 0; index < providers.Count; index++)
            {
                ICharacterLookupDescriptionProvider provider = providers[index]
                    ?? throw new ArgumentException(
                        $"Description provider {index} is null.",
                        nameof(providers));
                if (string.IsNullOrWhiteSpace(provider.CharacterId))
                {
                    throw new ArgumentException(
                        $"Description provider {index} has no character ID.",
                        nameof(providers));
                }

                if (!providersByCharacterId.TryAdd(
                        provider.CharacterId,
                        provider))
                {
                    throw new ArgumentException(
                        $"A description provider for '{provider.CharacterId}' "
                            + "is already registered.",
                        nameof(providers));
                }
            }
        }

        public CharacterLookupDescriptionResult ResolveSkill(
            string characterId,
            int currentAwakening,
            CharacterLookupSkillType skillType)
        {
            if (currentAwakening < 0 || currentAwakening > 6)
            {
                return Failure(
                    $"Awakening {currentAwakening} is outside 0 through 6.");
            }

            if (!Enum.IsDefined(typeof(CharacterLookupSkillType), skillType))
            {
                return Failure($"Skill type '{skillType}' is not supported.");
            }

            return Resolve(
                characterId,
                (provider, definition) => provider.ResolveSkill(
                    definition,
                    currentAwakening,
                    skillType));
        }

        public CharacterLookupDescriptionResult ResolveAwakening(
            string characterId,
            int awakeningStage)
        {
            if (awakeningStage < 1 || awakeningStage > 6)
            {
                return Failure(
                    $"Awakening stage {awakeningStage} is outside 1 through 6.");
            }

            return Resolve(
                characterId,
                (provider, definition) => provider.ResolveAwakening(
                    definition,
                    awakeningStage));
        }

        private CharacterLookupDescriptionResult Resolve(
            string characterId,
            Func<
                ICharacterLookupDescriptionProvider,
                CharacterDefinition,
                CharacterLookupDescriptionDefinition> createDefinition)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return Failure("A character ID is required.");
            }

            if (!definitionDatabase.TryGetCharacter(
                    characterId,
                    out CharacterDefinition definition))
            {
                return Failure(
                    $"Character definition '{characterId}' was not found.");
            }

            if (!providersByCharacterId.TryGetValue(
                    characterId,
                    out ICharacterLookupDescriptionProvider provider))
            {
                return Failure(
                    $"Character lookup provider '{characterId}' was not found.");
            }

            try
            {
                CharacterLookupDescriptionDefinition description =
                    createDefinition(provider, definition);
                if (description == null)
                {
                    return Failure(
                        $"Character lookup provider '{characterId}' returned "
                            + "no description.");
                }

                if (!TryFormat(
                        description.Title,
                        out string title,
                        out string error))
                {
                    return Failure(error);
                }

                var text = new StringBuilder(title);
                for (int index = 0; index < description.Sections.Count; index++)
                {
                    if (!TryFormat(
                            description.Sections[index],
                            out string section,
                            out error))
                    {
                        return Failure(error);
                    }

                    text.Append("\n\n");
                    text.Append(section);
                }

                return CharacterLookupDescriptionResult.Success(
                    text.ToString());
            }
            catch (Exception exception)
            {
                return Failure(
                    $"Character lookup provider '{characterId}' failed. "
                        + exception.Message);
            }
        }

        private bool TryFormat(
            CharacterLookupLocalizedText request,
            out string text,
            out string error)
        {
            if (request == null)
            {
                text = string.Empty;
                error = "A localized description request is missing.";
                return false;
            }

            string missingKey = LocalizationService.MissingKey(request.Key);
            if (string.Equals(
                    localizationService.GetText(request.Key),
                    missingKey,
                    StringComparison.Ordinal))
            {
                text = missingKey;
                error = $"Localization key '{request.Key}' was not found.";
                return false;
            }

            var resolvedArguments = new Dictionary<string, object>(
                request.Arguments.Count,
                StringComparer.Ordinal);
            foreach (var argument in request.Arguments)
            {
                if (argument.Value is CharacterLookupLocalizedText localized)
                {
                    if (!TryFormat(localized, out string value, out error))
                    {
                        text = string.Empty;
                        return false;
                    }

                    resolvedArguments.Add(argument.Key, value);
                    continue;
                }

                resolvedArguments.Add(argument.Key, argument.Value);
            }

            if (!localizationService.TryFormat(
                    request.Key,
                    resolvedArguments,
                    out text,
                    out error))
            {
                return false;
            }

            return true;
        }

        private CharacterLookupDescriptionResult Failure(string error)
        {
            return CharacterLookupDescriptionResult.Failure(
                localizationService.GetText(UnavailableKey),
                error);
        }
    }
}
