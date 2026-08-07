using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Damage
{
    public sealed class DamageContextFactoryTests
    {
        [Test]
        public void BuildCapturesEffectsAtEachAttackExecution()
        {
            CreateStates(
                1000d,
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(ElementEffect(
                1,
                "personal_water",
                0.25d));
            boss.Effects.ApplyEffect(Effect(
                2,
                "taken_damage",
                EffectModifierType.TargetTakenDamageIncrease,
                0.05d));
            var factory = new DamageContextFactory(
                new SequenceRandomSource());
            var request = Request(
                attacker,
                party,
                boss,
                AttackType.Match,
                false);

            DamageContext first = factory.Build(request);
            party.Effects.ApplyEffect(ElementEffect(
                3,
                "party_water",
                0.08d));
            DamageContext second = factory.Build(request);

            Assert.That(
                first.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.25d).Within(0.000000001d));
            Assert.That(
                first.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.05d).Within(0.000000001d));
            Assert.That(
                second.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.33d).Within(0.000000001d));
            Assert.That(
                first.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.25d).Within(0.000000001d));
        }

        [Test]
        public void BuildClampsChanceAndAddsCriticalDamageModifier()
        {
            CreateStates(
                1000d,
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(Effect(
                1,
                "critical_chance",
                EffectModifierType.CriticalChanceIncrease,
                1.10d));
            attacker.Effects.ApplyEffect(Effect(
                2,
                "critical_damage",
                EffectModifierType.CriticalDamageIncrease,
                0.20d));
            var random = new SequenceRandomSource(0.99f);
            var factory = new DamageContextFactory(random);

            DamageContext context = factory.Build(Request(
                attacker,
                party,
                boss,
                AttackType.Active,
                true));

            Assert.That(context.IsCritical, Is.True);
            Assert.That(
                context.CriticalDamageMultiplier,
                Is.EqualTo(1.70d).Within(0.000000001d));
            Assert.That(random.NextFloatCallCount, Is.Zero);
        }

        [Test]
        public void OneBuildRollsOnceAndSeparateFollowUpsRollSeparately()
        {
            CreateStates(
                1000d,
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(Effect(
                1,
                "critical_chance",
                EffectModifierType.CriticalChanceIncrease,
                0.10d));
            var random = new SequenceRandomSource(0.05f, 0.15f, 0.01f);
            var factory = new DamageContextFactory(random);

            DamageContext additional = factory.Build(Request(
                attacker,
                party,
                boss,
                AttackType.Additional,
                true));
            bool sameStoredResult = additional.IsCritical;
            DamageContext chase = factory.Build(Request(
                attacker,
                party,
                boss,
                AttackType.Chase,
                true));

            Assert.That(additional.IsCritical, Is.True);
            Assert.That(sameStoredResult, Is.True);
            Assert.That(chase.IsCritical, Is.False);
            Assert.That(random.NextFloatCallCount, Is.EqualTo(2));
        }

        [Test]
        public void FactoryToCalculatorUsesEveryConfirmedCategory()
        {
            CreateStates(
                1000d,
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(Effect(
                1,
                "personal_attack",
                EffectModifierType.AttackIncrease,
                0.20d));
            attacker.Effects.ApplyEffect(ElementEffect(
                2,
                "personal_water",
                0.25d));
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                3,
                "follow_up_damage",
                0.12d));
            party.Effects.ApplyEffect(Effect(
                4,
                "party_attack",
                EffectModifierType.AttackIncrease,
                0.10d));
            party.Effects.ApplyEffect(ElementEffect(
                5,
                "party_water",
                0.08d));
            boss.Effects.ApplyEffect(Effect(
                6,
                "taken_damage",
                EffectModifierType.TargetTakenDamageIncrease,
                0.05d));
            var factory = new DamageContextFactory(
                new SequenceRandomSource());

            DamageContext context = factory.Build(
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    AttackType.Additional,
                    AttackTag.Elemental,
                    2d,
                    false,
                    0,
                    false));
            DamageResult result = DamageCalculator.Calculate(context);

            Assert.That(
                context.AttackIncreaseRateSum,
                Is.EqualTo(0.30d).Within(0.000000001d));
            Assert.That(
                context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.33d).Within(0.000000001d));
            Assert.That(
                context.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.12d).Within(0.000000001d));
            Assert.That(
                context.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.05d).Within(0.000000001d));
            Assert.That(
                result.RawDamage,
                Is.EqualTo(4066.608d).Within(0.000000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(4066L));
        }

        private static DamageContextBuildRequest Request(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            AttackType attackType,
            bool canCritical)
        {
            return new DamageContextBuildRequest(
                attacker,
                party,
                boss,
                ElementType.Water,
                attackType,
                AttackTag.None,
                1d,
                false,
                0,
                canCritical);
        }

        private static EffectInstance Effect(
            long id,
            string effectId,
            EffectModifierType modifierType,
            double magnitude)
        {
            return new EffectInstance(
                id,
                effectId,
                "source",
                EffectCategory.Buff,
                modifierType,
                magnitude,
                2,
                id);
        }

        private static EffectInstance ElementEffect(
            long id,
            string effectId,
            double magnitude)
        {
            return new EffectInstance(
                id,
                effectId,
                "source",
                EffectCategory.Buff,
                EffectModifierType.ElementDamageIncrease,
                magnitude,
                2,
                id,
                elementFilter: ElementType.Water);
        }

        private static EffectInstance AttackTypeEffect(
            long id,
            string effectId,
            double magnitude)
        {
            return new EffectInstance(
                id,
                effectId,
                "source",
                EffectCategory.Buff,
                EffectModifierType.AttackTypeDamageIncrease,
                magnitude,
                2,
                id,
                attackTypeMask:
                    AttackTypeMask.Additional | AttackTypeMask.Chase);
        }

        private static void CreateStates(
            double attack,
            out CharacterBattleState attacker,
            out PartyBattleState party,
            out BossBattleState boss)
        {
            attacker = new CharacterBattleState(
                "attacker",
                0,
                ElementType.Water,
                100,
                attack);
            party = new PartyBattleState(new[] { attacker });
            boss = new BossBattleState(
                "boss",
                ElementType.Water,
                100000,
                100d);
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly Queue<float> values;

            public SequenceRandomSource(params float[] values)
            {
                this.values = new Queue<float>(values);
            }

            public int NextFloatCallCount { get; private set; }

            public int Next(int minInclusive, int maxExclusive)
            {
                throw new NotSupportedException();
            }

            public float NextFloat()
            {
                NextFloatCallCount++;
                return values.Dequeue();
            }
        }
    }
}
