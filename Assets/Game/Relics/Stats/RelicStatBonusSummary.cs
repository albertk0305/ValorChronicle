using System;
using System.Collections.Generic;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Relics.Stats
{
    public sealed class RelicStatBonusSummary
    {
        public RelicStatBonusSummary(
            double maxHpIncreaseRateSum,
            double attackIncreaseRateSum,
            IReadOnlyList<RelicStatBonusSource> sources = null)
        {
            ValidateRate(
                maxHpIncreaseRateSum,
                nameof(maxHpIncreaseRateSum));
            ValidateRate(
                attackIncreaseRateSum,
                nameof(attackIncreaseRateSum));
            MaxHpIncreaseRateSum = maxHpIncreaseRateSum;
            AttackIncreaseRateSum = attackIncreaseRateSum;
            Sources = sources ?? Array.Empty<RelicStatBonusSource>();
        }

        public static RelicStatBonusSummary None { get; } =
            new RelicStatBonusSummary(0d, 0d);

        public double MaxHpIncreaseRateSum { get; }
        public double AttackIncreaseRateSum { get; }
        public IReadOnlyList<RelicStatBonusSource> Sources { get; }

        private static void ValidateRate(double rate, string parameterName)
        {
            if (double.IsNaN(rate)
                || double.IsInfinity(rate)
                || rate < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    rate,
                    "A relic stat increase rate must be finite and "
                        + "non-negative.");
            }
        }
    }

    public sealed class RelicStatBonusSource
    {
        public RelicStatBonusSource(
            string definitionId,
            string displayNameKey,
            string descriptionKey,
            RelicEffectType effectType,
            int awakening,
            double rate)
        {
            DefinitionId = definitionId;
            DisplayNameKey = displayNameKey;
            DescriptionKey = descriptionKey;
            EffectType = effectType;
            Awakening = awakening;
            Rate = rate;
        }

        public string DefinitionId { get; }
        public string DisplayNameKey { get; }
        public string DescriptionKey { get; }
        public RelicEffectType EffectType { get; }
        public int Awakening { get; }
        public double Rate { get; }
    }
}
