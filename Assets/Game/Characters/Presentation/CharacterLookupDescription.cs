using System;
using System.Collections.Generic;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterLookupLocalizedText
    {
        public CharacterLookupLocalizedText(
            string key,
            IReadOnlyDictionary<string, object> arguments = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "A localization key is required.",
                    nameof(key));
            }

            Key = key;
            Arguments = arguments
                ?? new Dictionary<string, object>();
        }

        public string Key { get; }
        public IReadOnlyDictionary<string, object> Arguments { get; }
    }

    public sealed class CharacterLookupDescriptionDefinition
    {
        public CharacterLookupDescriptionDefinition(
            CharacterLookupLocalizedText title,
            IReadOnlyList<CharacterLookupLocalizedText> sections)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Sections = sections
                ?? throw new ArgumentNullException(nameof(sections));
            if (sections.Count == 0)
            {
                throw new ArgumentException(
                    "At least one description section is required.",
                    nameof(sections));
            }
        }

        public CharacterLookupLocalizedText Title { get; }
        public IReadOnlyList<CharacterLookupLocalizedText> Sections { get; }
    }

    public sealed class CharacterLookupDescriptionResult
    {
        private CharacterLookupDescriptionResult(
            bool isSuccess,
            string text,
            string error)
        {
            IsSuccess = isSuccess;
            Text = text ?? string.Empty;
            Error = error;
        }

        public bool IsSuccess { get; }
        public string Text { get; }
        public string Error { get; }

        public static CharacterLookupDescriptionResult Success(string text)
        {
            return new CharacterLookupDescriptionResult(true, text, null);
        }

        public static CharacterLookupDescriptionResult Failure(
            string fallbackText,
            string error)
        {
            return new CharacterLookupDescriptionResult(
                false,
                fallbackText,
                error ?? "Character lookup description resolution failed.");
        }
    }

    public interface ICharacterLookupDescriptionProvider
    {
        string CharacterId { get; }

        CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType);

        CharacterLookupDescriptionDefinition ResolveAwakening(
            CharacterDefinition definition,
            int awakeningStage);
    }
}
