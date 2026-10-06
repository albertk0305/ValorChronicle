using System;

namespace ValorChronicle.Characters.Presentation
{
    public enum CharacterLookupMode
    {
        Awakening = 0,
        Skills = 1
    }

    public enum CharacterLookupSkillType
    {
        Match3 = 0,
        Match4 = 1,
        Match5 = 2,
        Passive = 3,
        Active = 4
    }

    public sealed class CharacterLookupPresentationModel
    {
        public CharacterLookupPresentationModel(
            string characterId,
            CharacterLookupMode mode,
            int selectedAwakeningStage,
            CharacterLookupSkillType selectedSkillType,
            string description)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "A character ID is required.",
                    nameof(characterId));
            }

            if (selectedAwakeningStage < 1
                || selectedAwakeningStage > 6)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(selectedAwakeningStage));
            }

            if (!Enum.IsDefined(typeof(CharacterLookupMode), mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            if (!Enum.IsDefined(
                    typeof(CharacterLookupSkillType),
                    selectedSkillType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(selectedSkillType));
            }

            CharacterId = characterId;
            Mode = mode;
            SelectedAwakeningStage = selectedAwakeningStage;
            SelectedSkillType = selectedSkillType;
            Description = description ?? string.Empty;
        }

        public string CharacterId { get; }
        public CharacterLookupMode Mode { get; }
        public int SelectedAwakeningStage { get; }
        public CharacterLookupSkillType SelectedSkillType { get; }
        public string Description { get; }

        public CharacterLookupPresentationModel WithDescription(
            string description)
        {
            return new CharacterLookupPresentationModel(
                CharacterId,
                Mode,
                SelectedAwakeningStage,
                SelectedSkillType,
                description);
        }
    }
}
