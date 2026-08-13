using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "EffectDefinition",
        menuName = "Valor Chronicle/Definitions/Effect")]
    public sealed class EffectDefinition : GameDefinition
    {
        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private string descriptionKey;

        [SerializeField]
        private Sprite icon;

        public string DisplayNameKey => displayNameKey;
        public string DescriptionKey => descriptionKey;
        public Sprite Icon => icon;
    }
}
