using NUnit.Framework;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Damage
{
    public sealed class BossDamageContextFactoryTests
    {
        [Test]
        public void FactoryToCalculatorUsesBossAndPartySnapshots()
        {
            var character = new CharacterBattleState(
                "character",
                0,
                ElementType.Grass,
                10000,
                100d);
            var party = new PartyBattleState(
                new[] { character });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                100000,
                1550d);
            boss.Effects.ApplyEffect(Effect(
                1,
                "boss_attack",
                EffectModifierType.BossAttackIncrease,
                0.10d));
            boss.Effects.ApplyEffect(Effect(
                2,
                "boss_dealt",
                EffectModifierType.BossDealtDamageIncrease,
                0.05d));
            party.Effects.ApplyEffect(Effect(
                3,
                "party_reduction",
                EffectModifierType.PartyDamageReduction,
                0.25d));
            var request = new BossDamageContextBuildRequest(
                boss,
                party,
                2.40d,
                AttackTag.Heavy | AttackTag.Elemental);

            BossDamageContext context =
                BossDamageContextFactory.Build(request);
            BossDamageResult result =
                BossDamageCalculator.Calculate(context);

            Assert.That(
                request.AttackTags,
                Is.EqualTo(AttackTag.Heavy | AttackTag.Elemental));
            Assert.That(
                context.AttackIncreaseRateSum,
                Is.EqualTo(0.10d).Within(0.000000001d));
            Assert.That(
                context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.05d).Within(0.000000001d));
            Assert.That(
                context.PartyDamageReductionRateSum,
                Is.EqualTo(0.25d).Within(0.000000001d));
            Assert.That(
                result.FinalAttack,
                Is.EqualTo(1705d).Within(0.000000001d));
            Assert.That(
                result.RawDamage,
                Is.EqualTo(3222.45d).Within(0.000000001d));
            Assert.That(
                result.FinalDamageBeforeShield,
                Is.EqualTo(3222L));
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
    }
}
