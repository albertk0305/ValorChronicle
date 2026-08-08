using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Healing
{
    public sealed class HealingPipelineTests
    {
        [Test]
        public void Factory_CapturesExecutionTimeModifierSnapshot()
        {
            CharacterBattleState source = Character(3000);
            var party = new PartyBattleState(new[] { source });
            source.Effects.ApplyEffect(Effect(
                1,
                EffectModifierType.HealingIncrease,
                0.10d));
            var request = new HealingContextBuildRequest(
                source,
                party,
                0.20d,
                true,
                3);

            HealingContext first = HealingContextFactory.Build(request);
            party.Effects.ApplyEffect(Effect(
                2,
                EffectModifierType.HealingIncrease,
                0.08d));
            HealingContext second = HealingContextFactory.Build(request);

            Assert.That(first.HealingIncreaseRateSum, Is.EqualTo(0.10d));
            Assert.That(second.HealingIncreaseRateSum,
                Is.EqualTo(0.18d).Within(0.000000001d));
            Assert.That(first.HealingIncreaseRateSum, Is.EqualTo(0.10d));
        }

        [Test]
        public void FullPipeline_CollectsCalculatesAndAppliesHealing()
        {
            CharacterBattleState source = Character(5800);
            var party = new PartyBattleState(new[] { source });
            source.Effects.ApplyEffect(Effect(
                1,
                EffectModifierType.HealingIncrease,
                0.10d));
            party.Effects.ApplyEffect(Effect(
                2,
                EffectModifierType.HealingIncrease,
                0.08d));
            PartyDamageApplier.Apply(party, Damage(1000));

            HealingContext context = HealingContextFactory.Build(
                new HealingContextBuildRequest(
                    source,
                    party,
                    0.08d,
                    true,
                    3));
            HealingResult healing = HealingCalculator.Calculate(context);
            PartyHealingApplicationResult result =
                PartyHealingApplier.Apply(party, healing);

            Assert.That(context.HealingIncreaseRateSum,
                Is.EqualTo(0.18d).Within(0.000000001d));
            Assert.That(healing.RawHealing,
                Is.EqualTo(613.2224d).Within(0.000000001d));
            Assert.That(result.RequestedHealing, Is.EqualTo(613L));
            Assert.That(result.AppliedHealing, Is.EqualTo(613L));
            Assert.That(result.OverhealAmount, Is.Zero);
            Assert.That(result.HpBefore, Is.EqualTo(4800L));
            Assert.That(result.HpAfter, Is.EqualTo(5413L));
        }

        [TestCase(2000, 1500, 1500, 0, 9500, false)]
        [TestCase(500, 1500, 500, 1000, 10000, true)]
        [TestCase(0, 1500, 0, 1500, 10000, true)]
        public void Apply_ReportsNormalOverhealAndFullHpCases(
            long priorDamage,
            long requestedHealing,
            long expectedApplied,
            long expectedOverheal,
            long expectedHpAfter,
            bool expectedFullAfter)
        {
            CharacterBattleState source = Character(10000);
            var party = new PartyBattleState(new[] { source });
            if (priorDamage > 0)
            {
                PartyDamageApplier.Apply(party, Damage(priorDamage));
            }

            HealingResult healing = HealingCalculator.Calculate(
                new HealingContext(
                    10000,
                    requestedHealing / 10000d,
                    false,
                    0,
                    0d));
            PartyHealingApplicationResult result =
                PartyHealingApplier.Apply(party, healing);

            Assert.That(result.RequestedHealing,
                Is.EqualTo(requestedHealing));
            Assert.That(result.AppliedHealing, Is.EqualTo(expectedApplied));
            Assert.That(result.OverhealAmount,
                Is.EqualTo(expectedOverheal));
            Assert.That(result.HpAfter, Is.EqualTo(expectedHpAfter));
            Assert.That(result.IsFullAfter, Is.EqualTo(expectedFullAfter));
            Assert.That(result.WasFullBefore,
                Is.EqualTo(priorDamage == 0));
        }

        [Test]
        public void Apply_IncapacitatedPartyRejectsGeneralHealing()
        {
            CharacterBattleState source = Character(1000);
            var party = new PartyBattleState(new[] { source });
            PartyDamageApplier.Apply(party, Damage(1000));
            HealingResult healing = HealingCalculator.Calculate(
                new HealingContext(1000, 0.5d, false, 0, 0d));

            PartyHealingApplicationResult result =
                PartyHealingApplier.Apply(party, healing);

            Assert.That(result.RequestedHealing, Is.EqualTo(500L));
            Assert.That(result.AppliedHealing, Is.Zero);
            Assert.That(result.OverhealAmount, Is.EqualTo(500L));
            Assert.That(result.HpBefore, Is.Zero);
            Assert.That(result.HpAfter, Is.Zero);
            Assert.That(result.IsFullAfter, Is.False);
        }

        [Test]
        public void FactoryAndApplier_RejectNullInputs()
        {
            CharacterBattleState source = Character(1000);
            var party = new PartyBattleState(new[] { source });
            HealingResult healing = HealingCalculator.Calculate(
                new HealingContext(1000, 0.1d, false, 0, 0d));

            Assert.Throws<ArgumentNullException>(() =>
                HealingContextFactory.Build(null));
            Assert.Throws<ArgumentNullException>(() =>
                PartyHealingApplier.Apply(null, healing));
            Assert.Throws<ArgumentNullException>(() =>
                PartyHealingApplier.Apply(party, null));
        }

        private static CharacterBattleState Character(long maximumHp)
        {
            return new CharacterBattleState(
                "source",
                0,
                ElementType.Fire,
                maximumHp,
                100d);
        }

        private static EffectInstance Effect(
            long runtimeId,
            EffectModifierType modifierType,
            double magnitude)
        {
            return new EffectInstance(
                runtimeId,
                $"effect_{runtimeId}",
                "source",
                EffectCategory.Buff,
                modifierType,
                magnitude,
                2,
                runtimeId);
        }

        private static BossDamageResult Damage(long damage)
        {
            return BossDamageCalculator.Calculate(new BossDamageContext(
                damage,
                0d,
                0d,
                1d,
                0d,
                0d,
                0d,
                0d));
        }
    }
}
