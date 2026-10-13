using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "ExchangeCatalogDefinition",
        menuName = "Valor Chronicle/Definitions/Exchange Catalog")]
    public sealed class ExchangeCatalogDefinition : ScriptableObject
    {
        [SerializeField, Min(1)]
        private long characterExchangeCost = 60L;

        [SerializeField, Min(1)]
        private long relicExchangeCost = 25L;

        [SerializeField]
        private string[] characterIds = Array.Empty<string>();

        [SerializeField]
        private string[] relicIds = Array.Empty<string>();

        public long CharacterExchangeCost => characterExchangeCost;
        public long RelicExchangeCost => relicExchangeCost;
        public IReadOnlyList<string> CharacterIds => Array.AsReadOnly(
            characterIds ?? Array.Empty<string>());
        public IReadOnlyList<string> RelicIds => Array.AsReadOnly(
            relicIds ?? Array.Empty<string>());
    }
}
