using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "RelicDefinition",
        menuName = "Valor Chronicle/Definitions/Relic")]
    public sealed class RelicDefinition : GameDefinition
    {
        public const int AwakeningValueCount = 6;

        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private string descriptionKey;

        [SerializeField]
        private RelicEffectType effectType;

        [SerializeField]
        private double[] effectRates = Array.Empty<double>();

        public string DisplayNameKey => displayNameKey;
        public string DescriptionKey => descriptionKey;
        public RelicEffectType EffectType => effectType;
        public IReadOnlyList<double> EffectRates =>
            Array.AsReadOnly(effectRates ?? Array.Empty<double>());

        public double GetEffectRate(int awakening)
        {
            if (awakening < 0 || awakening >= AwakeningValueCount)
            {
                throw new ArgumentOutOfRangeException(nameof(awakening));
            }

            if (effectRates == null
                || effectRates.Length != AwakeningValueCount)
            {
                throw new InvalidOperationException(
                    $"Relic '{Id}' must define exactly "
                        + $"{AwakeningValueCount} awakening effect rates.");
            }

            return effectRates[awakening];
        }
    }
}
