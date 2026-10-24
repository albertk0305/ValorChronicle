using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Healing
{
    public sealed class HealingContextBuildRequest
    {
        public HealingContextBuildRequest(
            CharacterBattleState source,
            PartyBattleState party,
            double healingCoefficient,
            bool appliesCombo,
            int finalComboCount,
            double actionLocalHealingIncreaseRate = 0d)
        {
            Source = source
                ?? throw new ArgumentNullException(nameof(source));
            Party = party
                ?? throw new ArgumentNullException(nameof(party));
            if (double.IsNaN(healingCoefficient)
                || double.IsInfinity(healingCoefficient)
                || healingCoefficient < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(healingCoefficient));
            }

            if (appliesCombo && finalComboCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount),
                    finalComboCount,
                    "Final combo count must be positive when combo applies.");
            }

            if (double.IsNaN(actionLocalHealingIncreaseRate)
                || double.IsInfinity(actionLocalHealingIncreaseRate)
                || actionLocalHealingIncreaseRate < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionLocalHealingIncreaseRate),
                    actionLocalHealingIncreaseRate,
                    "Action-local healing increase must be finite and non-negative.");
            }

            HealingCoefficient = healingCoefficient;
            AppliesCombo = appliesCombo;
            FinalComboCount = finalComboCount;
            ActionLocalHealingIncreaseRate =
                actionLocalHealingIncreaseRate;
        }

        public CharacterBattleState Source { get; }
        public PartyBattleState Party { get; }
        public double HealingCoefficient { get; }
        public bool AppliesCombo { get; }
        public int FinalComboCount { get; }
        public double ActionLocalHealingIncreaseRate { get; }
    }
}
