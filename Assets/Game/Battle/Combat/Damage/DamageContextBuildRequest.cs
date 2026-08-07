using System;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Damage
{
    public sealed class DamageContextBuildRequest
    {
        public const double DefaultBaseCriticalChance = 0d;
        public const double DefaultBaseCriticalDamageMultiplier = 1.50d;

        public DamageContextBuildRequest(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState targetBoss,
            ElementType attackElement,
            AttackType attackType,
            AttackTag attackTags,
            double skillCoefficient,
            bool appliesCombo,
            int finalComboCount,
            bool canCritical,
            double baseCriticalChance = DefaultBaseCriticalChance,
            double baseCriticalDamageMultiplier =
                DefaultBaseCriticalDamageMultiplier)
        {
            Attacker = attacker
                ?? throw new ArgumentNullException(nameof(attacker));
            Party = party
                ?? throw new ArgumentNullException(nameof(party));
            TargetBoss = targetBoss
                ?? throw new ArgumentNullException(nameof(targetBoss));
            ValidateElement(attackElement);
            ValidateAttackType(attackType);
            ValidateAttackTags(attackTags);
            ValidateNonNegative(skillCoefficient, nameof(skillCoefficient));
            if (appliesCombo && finalComboCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount),
                    finalComboCount,
                    "Final combo count must be positive when combo applies.");
            }

            ValidateFinite(baseCriticalChance, nameof(baseCriticalChance));
            ValidateFinite(
                baseCriticalDamageMultiplier,
                nameof(baseCriticalDamageMultiplier));
            if (baseCriticalDamageMultiplier < 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseCriticalDamageMultiplier),
                    baseCriticalDamageMultiplier,
                    "Base critical damage multiplier must be at least one.");
            }

            AttackElement = attackElement;
            AttackType = attackType;
            AttackTags = attackTags;
            SkillCoefficient = skillCoefficient;
            AppliesCombo = appliesCombo;
            FinalComboCount = finalComboCount;
            CanCritical = canCritical;
            BaseCriticalChance = baseCriticalChance;
            BaseCriticalDamageMultiplier = baseCriticalDamageMultiplier;
        }

        public CharacterBattleState Attacker { get; }
        public PartyBattleState Party { get; }
        public BossBattleState TargetBoss { get; }
        public ElementType AttackElement { get; }
        public AttackType AttackType { get; }
        public AttackTag AttackTags { get; }
        public double SkillCoefficient { get; }
        public bool AppliesCombo { get; }
        public int FinalComboCount { get; }
        public bool CanCritical { get; }
        public double BaseCriticalChance { get; }
        public double BaseCriticalDamageMultiplier { get; }

        private static void ValidateElement(ElementType element)
        {
            if (!Enum.IsDefined(typeof(ElementType), element))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(element),
                    element,
                    "Attack element must be defined.");
            }
        }

        private static void ValidateAttackType(AttackType attackType)
        {
            if (!Enum.IsDefined(typeof(AttackType), attackType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attackType),
                    attackType,
                    "Attack type must be defined.");
            }
        }

        private static void ValidateAttackTags(AttackTag attackTags)
        {
            const AttackTag allAttackTags =
                AttackTag.Match3
                | AttackTag.Match4
                | AttackTag.Match5Plus
                | AttackTag.Heavy
                | AttackTag.MultiHit
                | AttackTag.Elemental
                | AttackTag.FixedDamage
                | AttackTag.MaxHpRatio;
            if ((attackTags & ~allAttackTags) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attackTags),
                    attackTags,
                    "Attack tags contain undefined flags.");
            }
        }

        private static void ValidateNonNegative(
            double value,
            string parameterName)
        {
            ValidateFinite(value, parameterName);
            if (value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value cannot be negative.");
            }
        }

        private static void ValidateFinite(
            double value,
            string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite.");
            }
        }
    }
}
