using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "RelicDefinition",
        menuName = "Valor Chronicle/Definitions/Relic")]
    public sealed class RelicDefinition : GameDefinition
    {
        [SerializeField]
        private string displayNameKey;

        public string DisplayNameKey => displayNameKey;
    }
}
