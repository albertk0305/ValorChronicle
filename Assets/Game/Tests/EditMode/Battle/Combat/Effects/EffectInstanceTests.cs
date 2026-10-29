using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Effects
{
    public sealed class EffectInstanceTests
    {
        [Test]
        public void Constructor_CreatesValidatedRuntimeEffect()
        {
            var effect = new EffectInstance(
                10,
                "water_damage_up",
                "hero_water",
                EffectCategory.Buff,
                EffectModifierType.ElementDamageIncrease,
                0.25d,
                2,
                20,
                elementFilter: ElementType.Water);

            Assert.That(effect.RuntimeId, Is.EqualTo(10L));
            Assert.That(effect.EffectId, Is.EqualTo("water_damage_up"));
            Assert.That(effect.SourceId, Is.EqualTo("hero_water"));
            Assert.That(effect.Category, Is.EqualTo(EffectCategory.Buff));
            Assert.That(effect.StackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
            Assert.That(effect.Magnitude, Is.EqualTo(0.25d));
            Assert.That(effect.RemainingTurns, Is.EqualTo(2));
            Assert.That(effect.StackCount, Is.EqualTo(1));
            Assert.That(effect.MaxStackCount, Is.EqualTo(1));
            Assert.That(effect.CreationOrder, Is.EqualTo(20L));
            Assert.That(effect.ElementFilter, Is.EqualTo(ElementType.Water));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RejectsMissingEffectId(string effectId)
        {
            Assert.Throws<ArgumentException>(() =>
                Effect(effectId: effectId));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RejectsMissingSourceId(string sourceId)
        {
            Assert.Throws<ArgumentException>(() =>
                Effect(sourceId: sourceId));
        }

        [TestCase(-0.01d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Constructor_RejectsInvalidMagnitude(double magnitude)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Effect(magnitude: magnitude));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RejectsInvalidFiniteDuration(int duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Effect(remainingTurns: duration));
        }

        [Test]
        public void Constructor_ValidatesStackCountConfiguration()
        {
            EffectInstance valid = Effect(
                stackPolicy: EffectStackPolicy.StackCount,
                maxStackCount: 3);

            Assert.That(valid.StackCount, Is.EqualTo(1));
            Assert.That(valid.MaxStackCount, Is.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Effect(
                    stackPolicy: EffectStackPolicy.StackCount,
                    stackCount: 2,
                    maxStackCount: 3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Effect(
                    stackPolicy: EffectStackPolicy.StackCount,
                    maxStackCount: 0));
            Assert.Throws<ArgumentException>(() =>
                Effect(maxStackCount: 2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RejectsInvalidCreationOrder(long creationOrder)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Effect(creationOrder: creationOrder));
        }

        [Test]
        public void Constructor_RequiresElementFilterOnlyForElementDamage()
        {
            Assert.Throws<ArgumentException>(() =>
                Effect(
                    modifierType:
                        EffectModifierType.ElementDamageIncrease));
            Assert.Throws<ArgumentException>(() =>
                Effect(elementFilter: ElementType.Water));
        }

        [Test]
        public void Constructor_ValidatesConfirmedAttackTypeFilters()
        {
            Assert.Throws<ArgumentException>(() =>
                Effect(
                    modifierType:
                        EffectModifierType.AttackTypeDamageIncrease));
            Assert.Throws<ArgumentException>(() =>
                Effect(attackTypeMask: AttackTypeMask.Match));

            EffectInstance effect = Effect(
                modifierType:
                    EffectModifierType.AttackTypeDamageIncrease,
                attackTypeMask:
                    AttackTypeMask.Additional | AttackTypeMask.Chase,
                requiredAttackTags: AttackTag.Heavy);

            Assert.That(
                effect.AttackTypeMask,
                Is.EqualTo(
                    AttackTypeMask.Additional | AttackTypeMask.Chase));
            Assert.That(
                effect.RequiredAttackTags,
                Is.EqualTo(AttackTag.Heavy));
        }

        [Test]
        public void Constructor_ValidatesOptionalModifierGroupCap()
        {
            EffectInstance grouped = Effect(
                modifierGroupId: "phoenix_blessing",
                modifierGroupCap: 0.20d);

            Assert.That(grouped.ModifierGroupId,
                Is.EqualTo("phoenix_blessing"));
            Assert.That(grouped.ModifierGroupCap, Is.EqualTo(0.20d));
            EffectInstance groupedTakenDamage = Effect(
                modifierType:
                    EffectModifierType.TargetTakenDamageIncrease,
                modifierGroupId: "ally_vulnerability",
                modifierGroupCap: 0.25d);
            Assert.That(groupedTakenDamage.ModifierGroupId,
                Is.EqualTo("ally_vulnerability"));
            Assert.That(groupedTakenDamage.ModifierGroupCap,
                Is.EqualTo(0.25d));
            Assert.Throws<ArgumentException>(() =>
                Effect(modifierGroupId: "phoenix_blessing"));
            Assert.Throws<ArgumentException>(() =>
                Effect(modifierGroupCap: 0.20d));
            Assert.Throws<ArgumentException>(() =>
                Effect(
                    modifierType: EffectModifierType.HealingIncrease,
                    modifierGroupId: "healing",
                    modifierGroupCap: 0.20d));
        }

        private static EffectInstance Effect(
            long runtimeId = 1,
            string effectId = "effect",
            string sourceId = "source",
            EffectModifierType modifierType =
                EffectModifierType.AttackIncrease,
            double magnitude = 0.1d,
            int? remainingTurns = 2,
            long creationOrder = 1,
            EffectStackPolicy stackPolicy =
                EffectStackPolicy.RefreshDuration,
            int stackCount = 1,
            int maxStackCount = 1,
            ElementType? elementFilter = null,
            AttackTypeMask attackTypeMask = AttackTypeMask.None,
            AttackTag requiredAttackTags = AttackTag.None,
            string modifierGroupId = null,
            double? modifierGroupCap = null)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                sourceId,
                EffectCategory.Buff,
                modifierType,
                magnitude,
                remainingTurns,
                creationOrder,
                stackPolicy,
                stackCount,
                maxStackCount,
                elementFilter,
                attackTypeMask,
                requiredAttackTags,
                modifierGroupId,
                modifierGroupCap);
        }
    }
}
