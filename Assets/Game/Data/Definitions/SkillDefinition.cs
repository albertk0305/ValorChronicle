using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "SkillDefinition",
        menuName = "Valor Chronicle/Definitions/Skill")]
    public sealed class SkillDefinition : GameDefinition
    {
        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private string descriptionKey;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private SkillKind skillKind;

        public string DisplayNameKey => displayNameKey;
        public string DescriptionKey => descriptionKey;
        public Sprite Icon => icon;
        public SkillKind SkillKind => skillKind;
    }
}
