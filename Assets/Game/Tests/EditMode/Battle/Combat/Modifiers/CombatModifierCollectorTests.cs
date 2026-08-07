using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Modifiers
{
    public sealed class CombatModifierCollectorTests
    {
        [Test]
        public void CollectAllyDamage_MatchesRepresentativeSources()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(ElementEffect(
                1,
                "attacker_water",
                0.25d,
                ElementType.Water));
            attacker.Effects.ApplyEffect(Effect(
                2,
                "attacker_dealt",
                EffectModifierType.DealtDamageIncrease,
                0.15d));
            party.Effects.ApplyEffect(ElementEffect(
                3,
                "party_water",
                0.08d,
                ElementType.Water));
            party.Effects.ApplyEffect(Effect(
                4,
                "party_attack",
                EffectModifierType.AttackIncrease,
                0.10d));
            boss.Effects.ApplyEffect(Effect(
                5,
                "boss_taken_up",
                EffectModifierType.TargetTakenDamageIncrease,
                0.05d));
            boss.Effects.ApplyEffect(Effect(
                6,
                "boss_taken_down",
                EffectModifierType.TargetTakenDamageReduction,
                0.10d));

            AllyDamageModifierSnapshot result =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water);

            Assert.That(result.AttackIncreaseRateSum,
                Is.EqualTo(0.10d));
            Assert.That(result.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.33d).Within(0.000000001d));
            Assert.That(result.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.15d));
            Assert.That(result.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.05d));
            Assert.That(result.TargetTakenDamageReductionRateSum,
                Is.EqualTo(0.10d));
            Assert.That(result.AttackTypeDamageIncreaseRateSum, Is.Zero);
        }

        [Test]
        public void CollectAllyDamage_SumsDistinctEffectsInSameCategory()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(Effect(
                1,
                "attack_one",
                EffectModifierType.AttackIncrease,
                0.20d));
            party.Effects.ApplyEffect(Effect(
                2,
                "attack_two",
                EffectModifierType.AttackIncrease,
                0.15d));
            attacker.Effects.ApplyEffect(Effect(
                3,
                "dealt_one",
                EffectModifierType.DealtDamageIncrease,
                0.10d));
            party.Effects.ApplyEffect(Effect(
                4,
                "dealt_two",
                EffectModifierType.DealtDamageIncrease,
                0.05d));

            AllyDamageModifierSnapshot result =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water);

            Assert.That(result.AttackIncreaseRateSum,
                Is.EqualTo(0.35d).Within(0.000000001d));
            Assert.That(result.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.15d).Within(0.000000001d));
        }

        [Test]
        public void CollectAllyDamage_UsesOnlyMatchingElementEffects()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(ElementEffect(
                1,
                "water",
                0.25d,
                ElementType.Water));
            attacker.Effects.ApplyEffect(ElementEffect(
                2,
                "fire",
                0.30d,
                ElementType.Fire));

            AllyDamageModifierSnapshot water =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water);
            AllyDamageModifierSnapshot fire =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Fire);

            Assert.That(water.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.25d));
            Assert.That(fire.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.30d));
        }

        [Test]
        public void CollectAllyDamage_CollectsReductionAndCriticalCategories()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(Effect(
                1,
                "attack_down",
                EffectModifierType.AttackReduction,
                0.12d));
            party.Effects.ApplyEffect(Effect(
                2,
                "dealt_down",
                EffectModifierType.DealtDamageReduction,
                0.07d));
            attacker.Effects.ApplyEffect(Effect(
                3,
                "critical_chance",
                EffectModifierType.CriticalChanceIncrease,
                0.20d));
            party.Effects.ApplyEffect(Effect(
                4,
                "critical_damage",
                EffectModifierType.CriticalDamageIncrease,
                0.30d));

            AllyDamageModifierSnapshot result =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water);

            Assert.That(result.AttackReductionRateSum,
                Is.EqualTo(0.12d));
            Assert.That(result.DealtDamageReductionRateSum,
                Is.EqualTo(0.07d));
            Assert.That(result.CriticalChanceIncreaseRateSum,
                Is.EqualTo(0.20d));
            Assert.That(result.CriticalDamageIncreaseRateSum,
                Is.EqualTo(0.30d));
        }

        [Test]
        public void CollectAllyDamage_StackCountUsesEffectiveMagnitude()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(StackEffect(1, 1));
            attacker.Effects.ApplyEffect(StackEffect(2, 2));
            attacker.Effects.ApplyEffect(StackEffect(3, 3));

            AllyDamageModifierSnapshot result =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water);

            Assert.That(result.AttackIncreaseRateSum,
                Is.EqualTo(0.30d).Within(0.000000001d));
        }

        [Test]
        public void CollectAllyDamage_ExpiredEffectIsNotCollected()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(Effect(
                1,
                "one_turn",
                EffectModifierType.AttackIncrease,
                0.50d,
                turns: 1));
            Assert.That(
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water).AttackIncreaseRateSum,
                Is.EqualTo(0.50d));

            attacker.Effects.ProcessTurnEnd();

            Assert.That(
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water).AttackIncreaseRateSum,
                Is.Zero);
        }

        [Test]
        public void CollectBossAttack_SeparatesBossAndPartySources()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            boss.Effects.ApplyEffect(Effect(
                1,
                "boss_attack_up",
                EffectModifierType.BossAttackIncrease,
                0.20d));
            boss.Effects.ApplyEffect(Effect(
                2,
                "boss_attack_down",
                EffectModifierType.BossAttackReduction,
                0.10d));
            boss.Effects.ApplyEffect(Effect(
                3,
                "boss_dealt_up",
                EffectModifierType.BossDealtDamageIncrease,
                0.15d));
            boss.Effects.ApplyEffect(Effect(
                4,
                "boss_dealt_down",
                EffectModifierType.BossDealtDamageReduction,
                0.05d));
            party.Effects.ApplyEffect(Effect(
                5,
                "party_taken_up",
                EffectModifierType.PartyTakenDamageIncrease,
                0.12d));
            party.Effects.ApplyEffect(Effect(
                6,
                "party_reduction",
                EffectModifierType.PartyDamageReduction,
                0.25d));
            boss.Effects.ApplyEffect(Effect(
                7,
                "wrong_boss_source",
                EffectModifierType.PartyDamageReduction,
                0.90d));
            party.Effects.ApplyEffect(Effect(
                8,
                "wrong_party_source",
                EffectModifierType.BossAttackIncrease,
                0.80d));

            BossAttackModifierSnapshot result =
                CombatModifierCollector.CollectBossAttack(boss, party);

            Assert.That(result.AttackIncreaseRateSum,
                Is.EqualTo(0.20d));
            Assert.That(result.AttackReductionRateSum,
                Is.EqualTo(0.10d));
            Assert.That(result.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.15d));
            Assert.That(result.DealtDamageReductionRateSum,
                Is.EqualTo(0.05d));
            Assert.That(result.PartyTakenDamageIncreaseRateSum,
                Is.EqualTo(0.12d));
            Assert.That(result.PartyDamageReductionRateSum,
                Is.EqualTo(0.25d));
        }

        [Test]
        public void Collectors_RejectNullAndUndefinedElementInputs()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);

            Assert.Throws<ArgumentNullException>(() =>
                CombatModifierCollector.CollectAllyDamage(
                    null,
                    party,
                    boss,
                    ElementType.Water));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    (ElementType)999));
            Assert.Throws<ArgumentNullException>(() =>
                CombatModifierCollector.CollectBossAttack(null, party));
        }

        private static void CreateStates(
            out CharacterBattleState attacker,
            out PartyBattleState party,
            out BossBattleState boss)
        {
            attacker = new CharacterBattleState(
                "hero",
                0,
                ElementType.Water,
                1000,
                100d);
            party = new PartyBattleState(new[] { attacker });
            boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                5000,
                500d);
        }

        private static EffectInstance ElementEffect(
            long id,
            string effectId,
            double magnitude,
            ElementType element)
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
                elementFilter: element);
        }

        private static EffectInstance Effect(
            long id,
            string effectId,
            EffectModifierType type,
            double magnitude,
            int? turns = 2)
        {
            return new EffectInstance(
                id,
                effectId,
                "source",
                EffectCategory.Buff,
                type,
                magnitude,
                turns,
                id);
        }

        private static EffectInstance StackEffect(long id, long order)
        {
            return new EffectInstance(
                id,
                "stack_attack",
                "source",
                EffectCategory.Buff,
                EffectModifierType.AttackIncrease,
                0.10d,
                2,
                order,
                EffectStackPolicy.StackCount,
                1,
                3);
        }
    }
}
