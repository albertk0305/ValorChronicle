using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Fiora
{
    [CreateAssetMenu(fileName = "FioraAmberwingCombatConfig", menuName = "Valor Chronicle/Combat Configs/Fiora Amberwing")]
    public sealed class FioraAmberwingCombatConfig : CombatConfigDefinition
    {
        [SerializeField] private double match3HealingCoefficient = 0.08d;
        [SerializeField] private double match4HealingCoefficient = 0.14d;
        [SerializeField] private double match5HealingCoefficient = 0.22d;
        [SerializeField] private double match4AttackIncreaseRate = 0.05d;
        [SerializeField] private double match5AttackIncreaseRate = 0.09d;
        [SerializeField] private int match4BuffDurationTurns = 2;
        [SerializeField] private int match5BuffDurationTurns = 3;
        [SerializeField] private double passiveConversionRate = 0.50d;
        [SerializeField] private double passivePerInstanceCap = 0.05d;
        [SerializeField] private double passiveGroupCap = 0.20d;
        [SerializeField] private int passiveDurationTurns = 2;
        [SerializeField] private double activeInstantHealingCoefficient = 0.24d;
        [SerializeField] private double activeFollowUpHealingCoefficient = 0.06d;
        [SerializeField] private int activeFollowUpHealGrantCount = 2;
        [SerializeField] private int activeFollowUpHealMaxCount = 4;
        [SerializeField] private int activeCooldownTurns = 8;
        [SerializeField] private double awakening2HealingIncreaseRate = 0.05d;
        [SerializeField] private double awakening3PassiveConversionRate = 0.55d;
        [SerializeField] private double awakening4Match3HealingCoefficient = 0.09d;
        [SerializeField] private double awakening4Match4HealingCoefficient = 0.15d;
        [SerializeField] private double awakening5ActiveInstantHealingCoefficient = 0.26d;
        [SerializeField] private double awakening5ActiveFollowUpHealingCoefficient = 0.07d;
        [SerializeField] private double awakening6FullOverhealConversionRate = 0.70d;

        public double Match3HealingCoefficient => match3HealingCoefficient;
        public double Match4HealingCoefficient => match4HealingCoefficient;
        public double Match5HealingCoefficient => match5HealingCoefficient;
        public double Match4AttackIncreaseRate => match4AttackIncreaseRate;
        public double Match5AttackIncreaseRate => match5AttackIncreaseRate;
        public int Match4BuffDurationTurns => match4BuffDurationTurns;
        public int Match5BuffDurationTurns => match5BuffDurationTurns;
        public double PassiveConversionRate => passiveConversionRate;
        public double PassivePerInstanceCap => passivePerInstanceCap;
        public double PassiveGroupCap => passiveGroupCap;
        public int PassiveDurationTurns => passiveDurationTurns;
        public double ActiveInstantHealingCoefficient => activeInstantHealingCoefficient;
        public double ActiveFollowUpHealingCoefficient => activeFollowUpHealingCoefficient;
        public int ActiveFollowUpHealGrantCount => activeFollowUpHealGrantCount;
        public int ActiveFollowUpHealMaxCount => activeFollowUpHealMaxCount;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public double Awakening2HealingIncreaseRate => awakening2HealingIncreaseRate;
        public double Awakening3PassiveConversionRate => awakening3PassiveConversionRate;
        public double Awakening4Match3HealingCoefficient => awakening4Match3HealingCoefficient;
        public double Awakening4Match4HealingCoefficient => awakening4Match4HealingCoefficient;
        public double Awakening5ActiveInstantHealingCoefficient => awakening5ActiveInstantHealingCoefficient;
        public double Awakening5ActiveFollowUpHealingCoefficient => awakening5ActiveFollowUpHealingCoefficient;
        public double Awakening6FullOverhealConversionRate => awakening6FullOverhealConversionRate;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteNonNegative(match3HealingCoefficient)
                || !IsFiniteNonNegative(match4HealingCoefficient)
                || !IsFiniteNonNegative(match5HealingCoefficient)
                || !IsFiniteNonNegative(match4AttackIncreaseRate)
                || !IsFiniteNonNegative(match5AttackIncreaseRate)
                || !IsFiniteNonNegative(passiveConversionRate)
                || !IsFiniteNonNegative(passivePerInstanceCap)
                || !IsFiniteNonNegative(passiveGroupCap)
                || !IsFiniteNonNegative(activeInstantHealingCoefficient)
                || !IsFiniteNonNegative(activeFollowUpHealingCoefficient)
                || !IsFiniteNonNegative(awakening2HealingIncreaseRate)
                || !IsFiniteNonNegative(awakening3PassiveConversionRate)
                || !IsFiniteNonNegative(awakening4Match3HealingCoefficient)
                || !IsFiniteNonNegative(awakening4Match4HealingCoefficient)
                || !IsFiniteNonNegative(awakening5ActiveInstantHealingCoefficient)
                || !IsFiniteNonNegative(awakening5ActiveFollowUpHealingCoefficient)
                || !IsFiniteNonNegative(awakening6FullOverhealConversionRate))
            {
                errorMessage = "All Fiora rates and coefficients must be finite and non-negative.";
                return false;
            }

            if (match4BuffDurationTurns <= 0 || match5BuffDurationTurns <= 0 || passiveDurationTurns <= 0)
            {
                errorMessage = "All Fiora effect durations must be positive.";
                return false;
            }

            if (activeFollowUpHealGrantCount <= 0 || activeFollowUpHealMaxCount <= 0
                || activeFollowUpHealGrantCount > activeFollowUpHealMaxCount)
            {
                errorMessage = "Follow-up heal counts must be positive and the grant cannot exceed the maximum.";
                return false;
            }

            if (activeCooldownTurns < 0)
            {
                errorMessage = "Active cooldown cannot be negative.";
                return false;
            }

            if (passivePerInstanceCap > passiveGroupCap)
            {
                errorMessage = "The passive per-instance cap cannot exceed its group cap.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
        }
    }
}
