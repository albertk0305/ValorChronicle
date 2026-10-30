using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.BaekHyeon;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonCombatConfigTests
    {
        [Test]
        public void DefaultConfigContainsDesignedValues()
        {
            BaekHyeonCombatConfig config = BaekHyeonTestConfig.Create();
            try
            {
                Assert.That(config.InitialMarkCount, Is.Zero);
                Assert.That(config.MarkMaxStackCount, Is.EqualTo(10));
                Assert.That(config.MarkRemainingTurns, Is.Null);
                Assert.That(config.MarkStackPolicy,
                    Is.EqualTo(EffectStackPolicy.StackCount));
                Assert.That(config.Match3MarkGainAmount, Is.EqualTo(1));
                Assert.That(config.Match4MarkGainAmount, Is.EqualTo(2));
                Assert.That(config.Match5MarkGainAmount, Is.EqualTo(3));
                Assert.That(config.MarkDamageIncreaseRate,
                    Is.EqualTo(0.03d));
                Assert.That(config.Awakening3MarkDamageIncreaseRate,
                    Is.EqualTo(0.035d));
                Assert.That(config.MarkDamageAttackTypes,
                    Is.EqualTo(AttackTypeMask.Additional
                        | AttackTypeMask.Chase));
                Assert.That(config.MinimumActualDamageToConsumeMarks,
                    Is.EqualTo(1));
                Assert.That(config.MarkConsumptionAttackTypes,
                    Is.EqualTo(AttackTypeMask.Additional
                        | AttackTypeMask.Chase));
                Assert.That(config.ConsumeAllMarksOnQualifyingDamage,
                    Is.True);
                Assert.That(config.ConsumeMarksAfterAttackCompletion,
                    Is.True);
                Assert.That(config.ApplyMatchMarksAfterDamage, Is.True);

                Assert.That(config.Match3BaseCoefficient,
                    Is.EqualTo(0.70d));
                Assert.That(config.Match4BaseCoefficient,
                    Is.EqualTo(1.25d));
                Assert.That(config.Match5BaseCoefficient,
                    Is.EqualTo(2.00d));
                Assert.That(config.Match3MarkCoefficient,
                    Is.EqualTo(0.03d));
                Assert.That(config.Match4MarkCoefficient,
                    Is.EqualTo(0.05d));
                Assert.That(config.Match5MarkCoefficient,
                    Is.EqualTo(0.08d));
                Assert.That(config.Awakening4Match3BaseCoefficient,
                    Is.EqualTo(0.75d));
                Assert.That(config.Awakening4Match4BaseCoefficient,
                    Is.EqualTo(1.35d));
                Assert.That(config.MatchAttackType,
                    Is.EqualTo(AttackType.Match));
                Assert.That(config.MatchUsesCombo, Is.True);

                Assert.That(config.ActiveBaseCoefficient,
                    Is.EqualTo(2.50d));
                Assert.That(config.ActiveMarkCoefficient,
                    Is.EqualTo(0.20d));
                Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
                Assert.That(config.Awakening5ActiveBaseCoefficient,
                    Is.EqualTo(2.80d));
                Assert.That(config.Awakening5ActiveMarkCoefficient,
                    Is.EqualTo(0.22d));
                Assert.That(config.ActiveAttackType,
                    Is.EqualTo(AttackType.Active));
                Assert.That(config.ActiveAvailableAtBattleStart, Is.True);
                Assert.That(config.ActiveUsesCombo, Is.False);

                Assert.That(config.ExposureDamageIncreaseRate,
                    Is.EqualTo(0.08d));
                Assert.That(config.ExposureDurationTurns, Is.EqualTo(2));
                Assert.That(config.ExposureMaxStackCount, Is.EqualTo(1));
                Assert.That(config.ExposureStackPolicy,
                    Is.EqualTo(EffectStackPolicy.RefreshDuration));
                Assert.That(config.ExposureAttackTypeMask,
                    Is.EqualTo(AttackTypeMask.None));
                Assert.That(config.ExposureMinimumMatchSize,
                    Is.EqualTo(5));
                Assert.That(config.ApplyExposureAfterMarkGain, Is.True);
                Assert.That(config.TryValidate(out _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("markDamageIncreaseRate", double.NaN)]
        [TestCase("awakening3MarkDamageIncreaseRate", -0.01d)]
        [TestCase("match3BaseCoefficient", double.PositiveInfinity)]
        [TestCase("match4MarkCoefficient", -0.01d)]
        [TestCase("activeBaseCoefficient", double.NaN)]
        [TestCase("awakening5ActiveMarkCoefficient", -0.01d)]
        [TestCase("exposureDamageIncreaseRate", double.NegativeInfinity)]
        public void InvalidDoubleIsRejected(string fieldName, double value)
        {
            BaekHyeonCombatConfig config = BaekHyeonTestConfig.Create();
            try
            {
                BaekHyeonTestConfig.SetField(config, fieldName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Is.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("initialMarkCount", 1)]
        [TestCase("markMaxStackCount", 0)]
        [TestCase("match3MarkGainAmount", 0)]
        [TestCase("match5MarkGainAmount", 11)]
        [TestCase("minimumActualDamageToConsumeMarks", 0)]
        [TestCase("activeCooldownTurns", -1)]
        [TestCase("exposureDurationTurns", 0)]
        [TestCase("exposureMaxStackCount", 2)]
        public void InvalidIntegerIsRejected(string fieldName, int value)
        {
            BaekHyeonCombatConfig config = BaekHyeonTestConfig.Create();
            try
            {
                BaekHyeonTestConfig.SetField(config, fieldName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Is.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void InvalidAttackTypeAndExposurePolicyAreRejected()
        {
            BaekHyeonCombatConfig config = BaekHyeonTestConfig.Create();
            try
            {
                BaekHyeonTestConfig.SetField(
                    config,
                    "markDamageAttackTypes",
                    AttackTypeMask.Additional);
                Assert.That(config.TryValidate(out _), Is.False);

                BaekHyeonTestConfig.SetField(
                    config,
                    "markDamageAttackTypes",
                    BaekHyeonCombatConfig.RequiredMarkDamageAttackTypes);
                BaekHyeonTestConfig.SetField(
                    config,
                    "exposureStackPolicy",
                    EffectStackPolicy.ReplaceWithNewest);
                Assert.That(config.TryValidate(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
