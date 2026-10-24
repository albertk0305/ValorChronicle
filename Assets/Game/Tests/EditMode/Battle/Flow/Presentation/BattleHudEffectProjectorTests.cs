using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Characters.Fiora;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleHudEffectProjectorTests
    {
        private BattleHudEffectProjector projector;

        [SetUp]
        public void SetUp()
        {
            projector = BattleHudEffectProjector.CreateDefault();
        }

        [Test]
        public void OnePassiveInstanceUsesOneCappedMagnitudeProjection()
        {
            EffectInstance blessing = Blessing(1, 1, 0.03d, 2);

            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(new[] { blessing });

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].EffectId,
                Is.EqualTo(FioraAmberwingRules.PassiveBlessingEffectId));
            Assert.That(result[0].BadgeText, Is.EqualTo("3"));
        }

        [Test]
        public void ThreePassiveInstancesUseOneSummedProjection()
        {
            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(new[]
                {
                    Blessing(1, 1, 0.03d, 1),
                    Blessing(2, 2, 0.05d, 2),
                    Blessing(3, 3, 0.04d, 2)
                });

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].BadgeText, Is.EqualTo("12"));
        }

        [Test]
        public void PassiveProjectionCapsDisplayWithoutChangingInstances()
        {
            EffectInstance first = Blessing(1, 1, 0.12d, 1);
            EffectInstance second = Blessing(2, 2, 0.12d, 2);

            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(new[] { first, second });

            Assert.That(result[0].BadgeText, Is.EqualTo("20"));
            Assert.That(first.Magnitude, Is.EqualTo(0.12d));
            Assert.That(first.RemainingTurns, Is.EqualTo(1));
            Assert.That(second.Magnitude, Is.EqualTo(0.12d));
            Assert.That(second.RemainingTurns, Is.EqualTo(2));
        }

        [Test]
        public void TurnEndRecalculatesFromRemainingOriginalInstances()
        {
            var effects = new EffectCollection();
            effects.ApplyEffect(Blessing(1, 1, 0.035d, 1));
            effects.ApplyEffect(Blessing(2, 2, 0.0249d, 2));

            Assert.That(
                projector.Project(effects.GetActiveEffects())[0].BadgeText,
                Is.EqualTo("6"));

            effects.ProcessTurnEnd();

            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(effects.GetActiveEffects());
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].BadgeText, Is.EqualTo("2"));

            effects.ProcessTurnEnd();
            Assert.That(projector.Project(effects.GetActiveEffects()),
                Is.Empty);
        }

        [Test]
        public void OnlyExplicitPassiveEffectIsGrouped()
        {
            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(new[]
                {
                    Blessing(1, 1, 0.03d, 2),
                    MatchBuff(
                        2,
                        2,
                        FioraAmberwingRules.Match4AttackEffectId,
                        0.05d,
                        2),
                    Blessing(3, 3, 0.04d, 2),
                    MatchBuff(
                        4,
                        4,
                        FioraAmberwingRules.Match5AttackEffectId,
                        0.09d,
                        3)
                });

            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result[0].BadgeText, Is.EqualTo("7"));
            Assert.That(result[1].EffectId,
                Is.EqualTo(FioraAmberwingRules.Match4AttackEffectId));
            Assert.That(result[1].BadgeText, Is.EqualTo("2"));
            Assert.That(result[2].EffectId,
                Is.EqualTo(FioraAmberwingRules.Match5AttackEffectId));
            Assert.That(result[2].BadgeText, Is.EqualTo("3"));
        }

        [TestCase(
            FioraAmberwingRules.Match4AttackEffectId,
            0.05d,
            2)]
        [TestCase(
            FioraAmberwingRules.Match5AttackEffectId,
            0.09d,
            3)]
        public void RefreshDurationRemainsOneProjectionWithNewTurns(
            string effectId,
            double magnitude,
            int refreshedTurns)
        {
            var effects = new EffectCollection();
            effects.ApplyEffect(MatchBuff(
                1,
                1,
                effectId,
                magnitude,
                1));
            effects.ApplyEffect(MatchBuff(
                2,
                2,
                effectId,
                magnitude,
                refreshedTurns));

            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(effects.GetActiveEffects());
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].BadgeText,
                Is.EqualTo(refreshedTurns.ToString()));
        }

        [TestCase(0.0249d, "2")]
        [TestCase(0.0250d, "3")]
        [TestCase(0.0349d, "3")]
        [TestCase(0.0350d, "4")]
        [TestCase(0.0570d, "6")]
        [TestCase(0.1245d, "12")]
        [TestCase(0.1980d, "20")]
        [TestCase(0.2000d, "20")]
        public void PassiveBadgeRoundsAppliedPercentageHalfUp(
            double magnitude,
            string expectedBadge)
        {
            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(new[] { Blessing(1, 1, magnitude, 2) });

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].BadgeText, Is.EqualTo(expectedBadge));
            Assert.That(result[0].BadgeVisible, Is.True);
        }

        [Test]
        public void PassiveBadgeCapsBeforeRounding()
        {
            EffectInstance blessing = Blessing(1, 1, 0.24d, 2);

            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(new[] { blessing });

            Assert.That(result[0].BadgeText, Is.EqualTo("20"));
            Assert.That(blessing.Magnitude, Is.EqualTo(0.24d));
            Assert.That(blessing.EffectiveMagnitude, Is.EqualTo(0.24d));
        }

        [Test]
        public void PassiveBadgeSumsInstancesBeforeRounding()
        {
            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(new[]
                {
                    Blessing(1, 1, 0.014d, 2),
                    Blessing(2, 2, 0.014d, 2)
                });

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].BadgeText, Is.EqualTo("3"));
        }

        [Test]
        public void PassiveBadgeRoundingDoesNotChangeCombatModifierPrecision()
        {
            var attacker = new CharacterBattleState(
                "hero",
                0,
                ElementType.Water,
                1000,
                100d);
            var party = new PartyBattleState(new[] { attacker });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                5000,
                500d);
            EffectInstance blessing = Blessing(1, 1, 0.0362d, 2);
            party.Effects.ApplyEffect(blessing);

            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(party.Effects.GetActiveEffects());
            double attackIncrease = CombatModifierCollector
                .CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water)
                .AttackIncreaseRateSum;

            Assert.That(result[0].BadgeText, Is.EqualTo("4"));
            Assert.That(attackIncrease,
                Is.EqualTo(0.0362d).Within(0.000000001d));
            Assert.That(blessing.Magnitude, Is.EqualTo(0.0362d));
            Assert.That(blessing.EffectiveMagnitude, Is.EqualTo(0.0362d));
        }

        [Test]
        public void ActiveSubHalfPercentBlessingKeepsVisibleZeroBadge()
        {
            IReadOnlyList<BattleHudEffectProjection> result =
                projector.Project(new[] { Blessing(1, 1, 0.004d, 2) });

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].BadgeText, Is.EqualTo("0"));
            Assert.That(result[0].BadgeVisible, Is.True);
        }

        private static EffectInstance Blessing(
            long runtimeId,
            long creationOrder,
            double magnitude,
            int turns)
        {
            return new EffectInstance(
                runtimeId,
                FioraAmberwingRules.PassiveBlessingEffectId,
                FioraAmberwingRules.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackIncrease,
                magnitude,
                turns,
                creationOrder,
                EffectStackPolicy.SeparateInstance,
                modifierGroupId:
                    FioraAmberwingRules.PassiveBlessingModifierGroupId,
                modifierGroupCap: 0.20d);
        }

        private static EffectInstance MatchBuff(
            long runtimeId,
            long creationOrder,
            string effectId,
            double magnitude,
            int turns)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                FioraAmberwingRules.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackIncrease,
                magnitude,
                turns,
                creationOrder,
                EffectStackPolicy.RefreshDuration);
        }
    }
}
