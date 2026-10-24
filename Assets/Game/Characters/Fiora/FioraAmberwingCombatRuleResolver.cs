using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Fiora
{
    public sealed class FioraAmberwingResolvedCombatRules
    {
        internal FioraAmberwingResolvedCombatRules(
            double match3HealingCoefficient, double match4HealingCoefficient,
            double match5HealingCoefficient, double match4AttackIncreaseRate,
            double match5AttackIncreaseRate, int match4BuffDurationTurns,
            int match5BuffDurationTurns, double passiveConversionRate,
            double passivePerInstanceCap, double passiveGroupCap,
            int passiveDurationTurns, double activeInstantHealingCoefficient,
            double activeFollowUpHealingCoefficient, int activeFollowUpHealGrantCount,
            int activeFollowUpHealMaxCount, int activeCooldownTurns,
            double actionLocalHealingIncreaseRate,
            bool hasAwakening6FullOverheal,
            double awakening6FullOverhealConversionRate)
        {
            Match3HealingCoefficient = match3HealingCoefficient;
            Match4HealingCoefficient = match4HealingCoefficient;
            Match5HealingCoefficient = match5HealingCoefficient;
            Match4AttackIncreaseRate = match4AttackIncreaseRate;
            Match5AttackIncreaseRate = match5AttackIncreaseRate;
            Match4BuffDurationTurns = match4BuffDurationTurns;
            Match5BuffDurationTurns = match5BuffDurationTurns;
            PassiveConversionRate = passiveConversionRate;
            PassivePerInstanceCap = passivePerInstanceCap;
            PassiveGroupCap = passiveGroupCap;
            PassiveDurationTurns = passiveDurationTurns;
            ActiveInstantHealingCoefficient = activeInstantHealingCoefficient;
            ActiveFollowUpHealingCoefficient = activeFollowUpHealingCoefficient;
            ActiveFollowUpHealGrantCount = activeFollowUpHealGrantCount;
            ActiveFollowUpHealMaxCount = activeFollowUpHealMaxCount;
            ActiveCooldownTurns = activeCooldownTurns;
            ActionLocalHealingIncreaseRate = actionLocalHealingIncreaseRate;
            HasAwakening6FullOverheal = hasAwakening6FullOverheal;
            Awakening6FullOverhealConversionRate = awakening6FullOverhealConversionRate;
        }

        public double Match3HealingCoefficient { get; }
        public double Match4HealingCoefficient { get; }
        public double Match5HealingCoefficient { get; }
        public double Match4AttackIncreaseRate { get; }
        public double Match5AttackIncreaseRate { get; }
        public int Match4BuffDurationTurns { get; }
        public int Match5BuffDurationTurns { get; }
        public double PassiveConversionRate { get; }
        public double PassivePerInstanceCap { get; }
        public double PassiveGroupCap { get; }
        public int PassiveDurationTurns { get; }
        public double ActiveInstantHealingCoefficient { get; }
        public double ActiveFollowUpHealingCoefficient { get; }
        public int ActiveFollowUpHealGrantCount { get; }
        public int ActiveFollowUpHealMaxCount { get; }
        public int ActiveCooldownTurns { get; }
        public double ActionLocalHealingIncreaseRate { get; }
        public bool HasAwakening6FullOverheal { get; }
        public double Awakening6FullOverhealConversionRate { get; }
    }

    public static class FioraAmberwingCombatRuleResolver
    {
        public static FioraAmberwingResolvedCombatRules Resolve(FioraAmberwingCombatConfig config, int awakening)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (!config.TryValidate(out string errorMessage)) throw new ArgumentException(errorMessage, nameof(config));
            if (awakening < CharacterBuildResolver.MinimumAwakening || awakening > CharacterBuildResolver.MaximumAwakening)
                throw new ArgumentOutOfRangeException(nameof(awakening));

            return new FioraAmberwingResolvedCombatRules(
                awakening >= 4 ? config.Awakening4Match3HealingCoefficient : config.Match3HealingCoefficient,
                awakening >= 4 ? config.Awakening4Match4HealingCoefficient : config.Match4HealingCoefficient,
                config.Match5HealingCoefficient,
                config.Match4AttackIncreaseRate,
                config.Match5AttackIncreaseRate,
                config.Match4BuffDurationTurns,
                config.Match5BuffDurationTurns,
                awakening >= 3 ? config.Awakening3PassiveConversionRate : config.PassiveConversionRate,
                config.PassivePerInstanceCap,
                config.PassiveGroupCap,
                config.PassiveDurationTurns,
                awakening >= 5 ? config.Awakening5ActiveInstantHealingCoefficient : config.ActiveInstantHealingCoefficient,
                awakening >= 5 ? config.Awakening5ActiveFollowUpHealingCoefficient : config.ActiveFollowUpHealingCoefficient,
                config.ActiveFollowUpHealGrantCount,
                config.ActiveFollowUpHealMaxCount,
                config.ActiveCooldownTurns,
                awakening >= 2 ? config.Awakening2HealingIncreaseRate : 0d,
                awakening >= 6,
                config.Awakening6FullOverhealConversionRate);
        }
    }
}
