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

        public string DisplayNameKey => displayNameKey;
    }
}
