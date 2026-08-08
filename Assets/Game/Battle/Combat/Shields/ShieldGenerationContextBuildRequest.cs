using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Shields
{
    public sealed class ShieldGenerationContextBuildRequest
    {
        public ShieldGenerationContextBuildRequest(
            CharacterBattleState source,
            PartyBattleState party,
            double shieldCoefficient,
            bool appliesCombo,
            int finalComboCount)
        {
            Source = source
                ?? throw new ArgumentNullException(nameof(source));
            Party = party
                ?? throw new ArgumentNullException(nameof(party));
            if (double.IsNaN(shieldCoefficient)
                || double.IsInfinity(shieldCoefficient)
                || shieldCoefficient < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(shieldCoefficient));
            }

            if (appliesCombo && finalComboCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount),
                    finalComboCount,
                    "Final combo count must be positive when combo applies.");
            }

            ShieldCoefficient = shieldCoefficient;
            AppliesCombo = appliesCombo;
            FinalComboCount = finalComboCount;
        }

        public CharacterBattleState Source { get; }
        public PartyBattleState Party { get; }
        public double ShieldCoefficient { get; }
        public bool AppliesCombo { get; }
        public int FinalComboCount { get; }
    }
}
