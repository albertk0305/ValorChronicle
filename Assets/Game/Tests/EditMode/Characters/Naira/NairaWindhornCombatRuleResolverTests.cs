using System;
using NUnit.Framework;
using ValorChronicle.Characters.Naira;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornCombatRuleResolverTests
    {
        [TestCase(0, 0.07d, 0.12d, 0.05d, 0.35d, false)]
        [TestCase(1, 0.07d, 0.12d, 0.05d, 0.35d, false)]
        [TestCase(2, 0.07d, 0.12d, 0.05d, 0.35d, false)]
        [TestCase(3, 0.07d, 0.12d, 0.06d, 0.35d, false)]
        [TestCase(4, 0.08d, 0.13d, 0.06d, 0.35d, false)]
        [TestCase(5, 0.08d, 0.13d, 0.06d, 0.40d, false)]
        [TestCase(6, 0.08d, 0.13d, 0.06d, 0.40d, true)]
        public void ResolveAppliesOnlyReachedAwakeningRules(
            int awakening,
            double expectedMatch3ShieldRate,
            double expectedMatch4ShieldRate,
            double expectedPassiveRate,
            double expectedWhiteStagRate,
            bool expectedAncientGuardianEnabled)
        {
            NairaWindhornResolvedCombatRules rules =
                NairaWindhornCombatRuleResolver.Resolve(
                    NairaWindhornTestConfig.Canonical,
                    awakening);

            Assert.That(rules.Match3ShieldMaxHpRate,
                Is.EqualTo(expectedMatch3ShieldRate));
            Assert.That(rules.Match4ShieldMaxHpRate,
                Is.EqualTo(expectedMatch4ShieldRate));
            Assert.That(rules.Match5ShieldMaxHpRate, Is.EqualTo(0.20d));
            Assert.That(rules.Match4BaseDamageCoefficient,
                Is.EqualTo(1.20d));
            Assert.That(rules.Match4ChargeDamageIncreaseRate,
                Is.EqualTo(0.50d));
            Assert.That(rules.Match5BaseDamageCoefficient,
                Is.EqualTo(2.20d));
            Assert.That(rules.Match5ChargeDamageIncreaseRate,
                Is.EqualTo(1.00d));
            Assert.That(rules.ActiveShieldMaxHpRate, Is.EqualTo(0.35d));
            Assert.That(rules.PassiveTargetTakenDamageIncreaseRate,
                Is.EqualTo(expectedPassiveRate));
            Assert.That(rules.WhiteStagGraceDamageReductionRate,
                Is.EqualTo(expectedWhiteStagRate));
            Assert.That(rules.ShieldDurationTurns, Is.EqualTo(3));
            Assert.That(rules.ActiveCooldownTurns, Is.EqualTo(12));
            Assert.That(rules.MatchShieldUsesComboMultiplier, Is.True);
            Assert.That(rules.ActiveShieldUsesComboMultiplier, Is.False);
            Assert.That(rules.AncientGuardianEnabled,
                Is.EqualTo(expectedAncientGuardianEnabled));
            Assert.That(rules.AncientGuardianDamageReductionRate,
                Is.EqualTo(0.15d));
        }

        [Test]
        public void ResolveDoesNotMutateConfig()
        {
            NairaWindhornCombatConfig config =
                NairaWindhornTestConfig.Canonical;

            NairaWindhornCombatRuleResolver.Resolve(config, 6);
            NairaWindhornResolvedCombatRules second =
                NairaWindhornCombatRuleResolver.Resolve(config, 0);

            Assert.That(config.Match3ShieldMaxHpRate, Is.EqualTo(0.07d));
            Assert.That(config.Match4ShieldMaxHpRate, Is.EqualTo(0.12d));
            Assert.That(config.WhiteStagGraceDamageReductionRate,
                Is.EqualTo(0.35d));
            Assert.That(second.Match3ShieldMaxHpRate, Is.EqualTo(0.07d));
            Assert.That(second.AncientGuardianEnabled, Is.False);
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ResolveRejectsAwakeningOutsideZeroToSix(int awakening)
        {
            Assert.That(
                () => NairaWindhornCombatRuleResolver.Resolve(
                    NairaWindhornTestConfig.Canonical,
                    awakening),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("awakening"));
        }

        [Test]
        public void ResolveRejectsNullConfig()
        {
            Assert.That(
                () => NairaWindhornCombatRuleResolver.Resolve(null, 0),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property("ParamName").EqualTo("config"));
        }
    }
}

