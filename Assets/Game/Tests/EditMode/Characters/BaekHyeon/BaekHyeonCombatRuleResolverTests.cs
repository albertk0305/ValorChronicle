using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.BaekHyeon;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonCombatRuleResolverTests
    {
        [TestCase(0, 0.70d, 1.25d, 0.03d, 2.50d, 0.20d, false)]
        [TestCase(1, 0.70d, 1.25d, 0.03d, 2.50d, 0.20d, false)]
        [TestCase(2, 0.70d, 1.25d, 0.03d, 2.50d, 0.20d, false)]
        [TestCase(3, 0.70d, 1.25d, 0.035d, 2.50d, 0.20d, false)]
        [TestCase(4, 0.75d, 1.35d, 0.035d, 2.50d, 0.20d, false)]
        [TestCase(5, 0.75d, 1.35d, 0.035d, 2.80d, 0.22d, false)]
        [TestCase(6, 0.75d, 1.35d, 0.035d, 2.80d, 0.22d, true)]
        public void ResolveAppliesEachAwakeningOnce(
            int awakening,
            double expectedMatch3,
            double expectedMatch4,
            double expectedMarkRate,
            double expectedActiveBase,
            double expectedActiveMark,
            bool expectedExposure)
        {
            WithRules(awakening, rules =>
            {
                Assert.That(rules.Match3BaseCoefficient,
                    Is.EqualTo(expectedMatch3));
                Assert.That(rules.Match4BaseCoefficient,
                    Is.EqualTo(expectedMatch4));
                Assert.That(rules.Match5BaseCoefficient,
                    Is.EqualTo(2.00d));
                Assert.That(rules.MarkDamageIncreaseRatePerStack,
                    Is.EqualTo(expectedMarkRate));
                Assert.That(rules.ActiveBaseCoefficient,
                    Is.EqualTo(expectedActiveBase));
                Assert.That(rules.ActiveMarkCoefficient,
                    Is.EqualTo(expectedActiveMark));
                Assert.That(rules.ExposureEnabled,
                    Is.EqualTo(expectedExposure));
                Assert.That(rules.ActiveCooldownTurns, Is.EqualTo(8));
            });
        }

        [TestCase(0, 3, 0, 0.70d)]
        [TestCase(0, 3, 5, 0.85d)]
        [TestCase(0, 3, 10, 1.00d)]
        [TestCase(0, 4, 0, 1.25d)]
        [TestCase(0, 4, 5, 1.50d)]
        [TestCase(0, 4, 10, 1.75d)]
        [TestCase(0, 5, 0, 2.00d)]
        [TestCase(0, 5, 5, 2.40d)]
        [TestCase(0, 5, 10, 2.80d)]
        [TestCase(6, 3, 0, 0.75d)]
        [TestCase(6, 3, 5, 0.90d)]
        [TestCase(6, 3, 10, 1.05d)]
        [TestCase(6, 4, 0, 1.35d)]
        [TestCase(6, 4, 5, 1.60d)]
        [TestCase(6, 4, 10, 1.85d)]
        [TestCase(6, 6, 0, 2.00d)]
        [TestCase(6, 6, 5, 2.40d)]
        [TestCase(6, 6, 10, 2.80d)]
        public void CalculateMatchCoefficientUsesExistingMarks(
            int awakening,
            int matchSize,
            int markCount,
            double expected)
        {
            WithRules(awakening, rules =>
                Assert.That(
                    rules.CalculateMatchCoefficient(matchSize, markCount),
                    Is.EqualTo(expected).Within(0.0000001d)));
        }

        [TestCase(0, 0, 2.50d)]
        [TestCase(0, 5, 3.50d)]
        [TestCase(0, 10, 4.50d)]
        [TestCase(5, 0, 2.80d)]
        [TestCase(5, 5, 3.90d)]
        [TestCase(5, 10, 5.00d)]
        public void CalculateActiveCoefficientUsesAwakeningFiveRules(
            int awakening,
            int markCount,
            double expected)
        {
            WithRules(awakening, rules =>
            {
                Assert.That(rules.CalculateActiveCoefficient(markCount),
                    Is.EqualTo(expected).Within(0.0000001d));
                Assert.That(rules.ActiveUsesCombo, Is.False);
                Assert.That(rules.ActiveAttackType,
                    Is.EqualTo(AttackType.Active));
                Assert.That(rules.ActiveAvailableAtBattleStart, Is.True);
                Assert.That(rules.ActiveCooldownTurns, Is.EqualTo(8));
            });
        }

        [TestCase(3, 1)]
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        [TestCase(6, 3)]
        [TestCase(12, 3)]
        public void GetMarkGainAmountBucketsFiveAndLargerMatches(
            int matchSize,
            int expected)
        {
            WithRules(0, rules =>
                Assert.That(rules.GetMarkGainAmount(matchSize),
                    Is.EqualTo(expected)));
        }

        [Test]
        public void MarkRulesExposeIndefiniteAdditionalAndChaseDebuff()
        {
            WithRules(6, rules =>
            {
                Assert.That(rules.InitialMarkCount, Is.Zero);
                Assert.That(rules.MarkMaxStackCount, Is.EqualTo(10));
                Assert.That(rules.MarkRemainingTurns, Is.Null);
                Assert.That(rules.MarkStackPolicy,
                    Is.EqualTo(EffectStackPolicy.StackCount));
                Assert.That(rules.MarkDamageAttackTypes,
                    Is.EqualTo(AttackTypeMask.Additional
                        | AttackTypeMask.Chase));
                Assert.That(rules.MarkConsumptionAttackTypes,
                    Is.EqualTo(AttackTypeMask.Additional
                        | AttackTypeMask.Chase));
                Assert.That(rules.MinimumActualDamageToConsumeMarks,
                    Is.EqualTo(1));
                Assert.That(rules.ConsumeAllMarksOnQualifyingDamage,
                    Is.True);
                Assert.That(rules.ConsumeMarksAfterAttackCompletion,
                    Is.True);
                Assert.That(rules.ApplyMatchMarksAfterDamage, Is.True);
                Assert.That(rules.MatchAttackType,
                    Is.EqualTo(AttackType.Match));
                Assert.That(rules.MatchUsesCombo, Is.True);
                Assert.That(rules.CalculateMarkDamageIncreaseRate(10),
                    Is.EqualTo(0.35d).Within(0.0000001d));
            });
        }

        [Test]
        public void ExposureIsInactiveBeforeSixAndConfiguredAtSix()
        {
            WithRules(5, rules =>
            {
                Assert.That(rules.ExposureEnabled, Is.False);
                Assert.That(rules.ExposureDamageIncreaseRate, Is.Zero);
                Assert.That(rules.ExposureDurationTurns, Is.Zero);
                Assert.That(rules.CanApplyExposure(5), Is.False);
            });
            WithRules(6, rules =>
            {
                Assert.That(rules.ExposureEnabled, Is.True);
                Assert.That(rules.ExposureDamageIncreaseRate,
                    Is.EqualTo(0.08d));
                Assert.That(rules.ExposureDurationTurns, Is.EqualTo(2));
                Assert.That(rules.ExposureMaxStackCount, Is.EqualTo(1));
                Assert.That(rules.ExposureStackPolicy,
                    Is.EqualTo(EffectStackPolicy.RefreshDuration));
                Assert.That(rules.ExposureAttackTypeMask,
                    Is.EqualTo(AttackTypeMask.None));
                Assert.That(rules.ApplyExposureAfterMarkGain, Is.True);
                Assert.That(rules.CanApplyExposure(4), Is.False);
                Assert.That(rules.CanApplyExposure(5), Is.True);
                Assert.That(rules.CanApplyExposure(7), Is.True);
            });
        }

        [TestCase(-1)]
        [TestCase(11)]
        public void CalculationsRejectMarkCountOutsideSupportedRange(
            int markCount)
        {
            WithRules(0, rules =>
            {
                Assert.That(
                    () => rules.CalculateMatchCoefficient(3, markCount),
                    Throws.TypeOf<System.ArgumentOutOfRangeException>()
                        .With.Property("ParamName").EqualTo("markCount"));
                Assert.That(
                    () => rules.CalculateActiveCoefficient(markCount),
                    Throws.TypeOf<System.ArgumentOutOfRangeException>()
                        .With.Property("ParamName").EqualTo("markCount"));
            });
        }

        [TestCase(0)]
        [TestCase(2)]
        public void MatchRulesRejectUnsupportedSmallMatchSize(int matchSize)
        {
            WithRules(0, rules =>
                Assert.That(
                    () => rules.GetMarkGainAmount(matchSize),
                    Throws.TypeOf<System.ArgumentOutOfRangeException>()
                        .With.Property("ParamName").EqualTo("matchSize")));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ResolveRejectsAwakeningOutsideSupportedRange(
            int awakening)
        {
            BaekHyeonCombatConfig config = BaekHyeonTestConfig.Create();
            try
            {
                Assert.That(
                    () => BaekHyeonCombatRuleResolver.Resolve(
                        config,
                        awakening),
                    Throws.TypeOf<System.ArgumentOutOfRangeException>()
                        .With.Property("ParamName").EqualTo("awakening"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ResolveRejectsMissingOrInvalidConfig()
        {
            Assert.That(
                () => BaekHyeonCombatRuleResolver.Resolve(null, 0),
                Throws.ArgumentNullException
                    .With.Property("ParamName").EqualTo("config"));

            BaekHyeonCombatConfig config = BaekHyeonTestConfig.Create();
            try
            {
                BaekHyeonTestConfig.SetField(
                    config,
                    "markMaxStackCount",
                    0);
                Assert.That(
                    () => BaekHyeonCombatRuleResolver.Resolve(config, 0),
                    Throws.ArgumentException
                        .With.Property("ParamName").EqualTo("config"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static void WithRules(
            int awakening,
            System.Action<BaekHyeonResolvedCombatRules> assertion)
        {
            BaekHyeonCombatConfig config = BaekHyeonTestConfig.Create();
            try
            {
                assertion(BaekHyeonCombatRuleResolver.Resolve(
                    config,
                    awakening));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
