using System;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Core.Random;

namespace ValorChronicle.Battle.Combat.Damage
{
    public sealed class DamageContextFactory
    {
        private readonly IRandomSource randomSource;

        public DamageContextFactory(IRandomSource randomSource)
        {
            this.randomSource = randomSource
                ?? throw new ArgumentNullException(nameof(randomSource));
        }

        public DamageContext Build(DamageContextBuildRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            AllyDamageModifierSnapshot modifiers =
                CombatModifierCollector.CollectAllyDamage(
                    request.Attacker,
                    request.Party,
                    request.TargetBoss,
                    request.AttackElement,
                    request.AttackType,
                    request.AttackTags);

            double finalCriticalChance = AddFinite(
                request.BaseCriticalChance,
                modifiers.CriticalChanceIncreaseRateSum,
                "Final critical chance");
            finalCriticalChance = Math.Max(
                0d,
                Math.Min(1d, finalCriticalChance));
            bool isCritical = CriticalResolver.Resolve(
                request.CanCritical,
                finalCriticalChance,
                randomSource);
            double criticalDamageMultiplier = AddFinite(
                request.BaseCriticalDamageMultiplier,
                modifiers.CriticalDamageIncreaseRateSum,
                "Final critical damage multiplier");

            return new DamageContext(
                request.Attacker.Attack,
                modifiers.AttackIncreaseRateSum
                    - modifiers.AttackReductionRateSum,
                request.SkillCoefficient,
                request.AppliesCombo,
                request.FinalComboCount,
                request.CanCritical,
                isCritical,
                criticalDamageMultiplier,
                modifiers.ElementDamageIncreaseRateSum,
                modifiers.AttackTypeDamageIncreaseRateSum,
                modifiers.DealtDamageIncreaseRateSum
                    - modifiers.DealtDamageReductionRateSum,
                request.AttackElement,
                request.TargetBoss.Element,
                modifiers.TargetTakenDamageIncreaseRateSum,
                modifiers.TargetTakenDamageReductionRateSum);
        }

        private static double AddFinite(
            double left,
            double right,
            string valueName)
        {
            double result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new OverflowException(
                    $"{valueName} must remain finite.");
            }

            return result;
        }
    }
}
