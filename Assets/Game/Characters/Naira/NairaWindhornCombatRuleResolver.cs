using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Naira
{
    public sealed class NairaWindhornResolvedCombatRules
    {
        internal NairaWindhornResolvedCombatRules(
            double match3ShieldMaxHpRate,
            double match4ShieldMaxHpRate,
            double match5ShieldMaxHpRate,
            double match4BaseDamageCoefficient,
            double match4ChargeDamageIncreaseRate,
            double match5BaseDamageCoefficient,
            double match5ChargeDamageIncreaseRate,
            double activeShieldMaxHpRate,
            double whiteStagGraceDamageReductionRate,
            double passiveTargetTakenDamageIncreaseRate,
            int shieldDurationTurns,
            int activeCooldownTurns,
            bool matchShieldUsesComboMultiplier,
            bool activeShieldUsesComboMultiplier,
            bool ancientGuardianEnabled,
            double ancientGuardianDamageReductionRate)
        {
            Match3ShieldMaxHpRate = match3ShieldMaxHpRate;
            Match4ShieldMaxHpRate = match4ShieldMaxHpRate;
            Match5ShieldMaxHpRate = match5ShieldMaxHpRate;
            Match4BaseDamageCoefficient = match4BaseDamageCoefficient;
            Match4ChargeDamageIncreaseRate =
                match4ChargeDamageIncreaseRate;
            Match5BaseDamageCoefficient = match5BaseDamageCoefficient;
            Match5ChargeDamageIncreaseRate =
                match5ChargeDamageIncreaseRate;
            ActiveShieldMaxHpRate = activeShieldMaxHpRate;
            WhiteStagGraceDamageReductionRate =
                whiteStagGraceDamageReductionRate;
            PassiveTargetTakenDamageIncreaseRate =
                passiveTargetTakenDamageIncreaseRate;
            ShieldDurationTurns = shieldDurationTurns;
            ActiveCooldownTurns = activeCooldownTurns;
            MatchShieldUsesComboMultiplier =
                matchShieldUsesComboMultiplier;
            ActiveShieldUsesComboMultiplier =
                activeShieldUsesComboMultiplier;
            AncientGuardianEnabled = ancientGuardianEnabled;
            AncientGuardianDamageReductionRate =
                ancientGuardianDamageReductionRate;
        }

        public double Match3ShieldMaxHpRate { get; }
        public double Match4ShieldMaxHpRate { get; }
        public double Match5ShieldMaxHpRate { get; }
        public double Match4BaseDamageCoefficient { get; }
        public double Match4ChargeDamageIncreaseRate { get; }
        public double Match5BaseDamageCoefficient { get; }
        public double Match5ChargeDamageIncreaseRate { get; }
        public double ActiveShieldMaxHpRate { get; }
        public double WhiteStagGraceDamageReductionRate { get; }
        public double PassiveTargetTakenDamageIncreaseRate { get; }
        public int ShieldDurationTurns { get; }
        public int ActiveCooldownTurns { get; }
        public bool MatchShieldUsesComboMultiplier { get; }
        public bool ActiveShieldUsesComboMultiplier { get; }
        public bool AncientGuardianEnabled { get; }
        public double AncientGuardianDamageReductionRate { get; }
    }

    public static class NairaWindhornCombatRuleResolver
    {
        private const int PassiveEnhancementAwakening = 3;
        private const int MatchShieldEnhancementAwakening = 4;
        private const int WhiteStagGraceEnhancementAwakening = 5;
        private const int AncientGuardianAwakening = 6;

        public static NairaWindhornResolvedCombatRules Resolve(
            NairaWindhornCombatConfig config,
            int awakening)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakening),
                    awakening,
                    $"Awakening must be between "
                        + $"{CharacterBuildResolver.MinimumAwakening} and "
                        + $"{CharacterBuildResolver.MaximumAwakening}.");
            }

            return new NairaWindhornResolvedCombatRules(
                awakening >= MatchShieldEnhancementAwakening
                    ? config.Awakening4Match3ShieldMaxHpRate
                    : config.Match3ShieldMaxHpRate,
                awakening >= MatchShieldEnhancementAwakening
                    ? config.Awakening4Match4ShieldMaxHpRate
                    : config.Match4ShieldMaxHpRate,
                config.Match5ShieldMaxHpRate,
                config.Match4BaseDamageCoefficient,
                config.Match4ChargeDamageIncreaseRate,
                config.Match5BaseDamageCoefficient,
                config.Match5ChargeDamageIncreaseRate,
                config.ActiveShieldMaxHpRate,
                awakening >= WhiteStagGraceEnhancementAwakening
                    ? config.Awakening5WhiteStagGraceDamageReductionRate
                    : config.WhiteStagGraceDamageReductionRate,
                awakening >= PassiveEnhancementAwakening
                    ? config.Awakening3PassiveTargetTakenDamageIncreaseRate
                    : config.PassiveTargetTakenDamageIncreaseRate,
                config.ShieldDurationTurns,
                config.ActiveCooldownTurns,
                config.MatchShieldUsesComboMultiplier,
                config.ActiveShieldUsesComboMultiplier,
                awakening >= AncientGuardianAwakening,
                config.AncientGuardianDamageReductionRate);
        }
    }
}

