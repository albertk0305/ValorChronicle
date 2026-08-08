using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Modifiers
{
    public sealed class SupportModifierCollectorTests
    {
        [Test]
        public void Collect_SumsCharacterAndPartySupportEffects()
        {
            CreateStates(
                out CharacterBattleState source,
                out PartyBattleState party,
                out BossBattleState boss);
            source.Effects.ApplyEffect(Effect(
                1,
                EffectModifierType.HealingIncrease,
                0.10d));
            party.Effects.ApplyEffect(Effect(
                2,
                EffectModifierType.HealingIncrease,
                0.08d));
            source.Effects.ApplyEffect(Effect(
                3,
                EffectModifierType.ShieldAmountIncrease,
                0.12d));
            party.Effects.ApplyEffect(Effect(
                4,
                EffectModifierType.ShieldAmountIncrease,
                0.03d));
            source.Effects.ApplyEffect(Effect(
                5,
                EffectModifierType.AttackIncrease,
                0.90d));

            SupportModifierSnapshot result =
                SupportModifierCollector.Collect(source, party);

            Assert.That(result.HealingIncreaseRateSum,
                Is.EqualTo(0.18d).Within(0.000000001d));
            Assert.That(result.ShieldAmountIncreaseRateSum,
                Is.EqualTo(0.15d).Within(0.000000001d));

            AllyDamageModifierSnapshot damage =
                CombatModifierCollector.CollectAllyDamage(
                    source,
                    party,
                    boss,
                    ElementType.Fire);
            Assert.That(damage.AttackIncreaseRateSum, Is.EqualTo(0.90d));
            Assert.That(damage.DealtDamageIncreaseRateSum, Is.Zero);
        }

        [Test]
        public void Collect_IgnoresExpiredEffectsAndRejectsNull()
        {
            CreateStates(
                out CharacterBattleState source,
                out PartyBattleState party,
                out BossBattleState boss);
            source.Effects.ApplyEffect(Effect(
                1,
                EffectModifierType.HealingIncrease,
                0.10d,
                1));
            source.Effects.ProcessTurnEnd();

            SupportModifierSnapshot result =
                SupportModifierCollector.Collect(source, party);

            Assert.That(result.HealingIncreaseRateSum, Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                SupportModifierCollector.Collect(null, party));
            Assert.Throws<ArgumentNullException>(() =>
                SupportModifierCollector.Collect(source, null));
        }

        private static void CreateStates(
            out CharacterBattleState source,
            out PartyBattleState party,
            out BossBattleState boss)
        {
            source = new CharacterBattleState(
                "source",
                0,
                ElementType.Fire,
                1000,
                100d);
            party = new PartyBattleState(new[] { source });
            boss = new BossBattleState(
                "boss",
                ElementType.Water,
                5000,
                100d);
        }

        private static EffectInstance Effect(
            long runtimeId,
            EffectModifierType modifierType,
            double magnitude,
            int? remainingTurns = 2)
        {
            return new EffectInstance(
                runtimeId,
                $"effect_{runtimeId}",
                "source",
                EffectCategory.Buff,
                modifierType,
                magnitude,
                remainingTurns,
                runtimeId);
        }
    }
}
