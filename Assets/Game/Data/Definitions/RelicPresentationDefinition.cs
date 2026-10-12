using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "RelicPresentationDefinition",
        menuName = "Valor Chronicle/Definitions/Relic Presentation")]
    public sealed class RelicPresentationDefinition : ScriptableObject
    {
        [SerializeField]
        private string relicId;

        [SerializeField]
        private Sprite icon;

        public string RelicId => relicId;
        public Sprite Icon => icon;
    }
}
