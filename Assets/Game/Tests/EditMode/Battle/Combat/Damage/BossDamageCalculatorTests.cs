using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Damage;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Damage
{
    public sealed class BossDamageCalculatorTests
    {
        [Test]
        public void Calculate_MatchesRepresentativeDamage()
        {
            BossDamageResult result = BossDamageCalculator.Calculate(
                CreateContext(
                    baseAttack: 1550d,
                    attackCoefficient: 2.40d,
                    partyDamageReductionRateSum: 0.25d));

            Assert.That(result.FinalAttack, Is.EqualTo(1550d));
            Assert.That(result.PartyTakenDamageMultiplier, Is.EqualTo(0.75d));
            Assert.That(result.RawDamage, Is.EqualTo(2790d));
            Assert.That(result.FinalDamageBeforeShield, Is.EqualTo(2790L));
        }

        [Test]
        public void Calculate_CapsPartyDamageReductionAtSeventyPercent()
        {
            BossDamageResult result = BossDamageCalculator.Calculate(
                CreateContext(partyDamageReductionRateSum: 0.90d));

            Assert.That(result.EffectivePartyDamageReductionRate,
                Is.EqualTo(0.70d));
            Assert.That(result.PartyTakenDamageMultiplier,
                Is.EqualTo(0.30d).Within(0.000000001d));
        }

        [Test]
        public void Calculate_UsesMinimumPartyTakenDamageMultiplier()
        {
            BossDamageResult result = BossDamageCalculator.Calculate(
                CreateContext(partyDamageReductionRateSum: 5d));

            Assert.That(result.PartyTakenDamageMultiplier,
                Is.EqualTo(0.30d).Within(0.000000001d));
        }

        [Test]
        public void Calculate_MultipliesBossDealtAndPartyTakenCategories()
        {
            BossDamageResult result = BossDamageCalculator.Calculate(
                CreateContext(
                    baseAttack: 100d,
                    dealtDamageIncreaseRateSum: 0.20d,
                    partyTakenDamageIncreaseRateSum: 0.30d));

            Assert.That(result.BossDealtDamageMultiplier, Is.EqualTo(1.20d));
            Assert.That(result.PartyTakenDamageMultiplier, Is.EqualTo(1.30d));
            Assert.That(result.RawDamage, Is.EqualTo(156d).Within(0.000000001d));
            Assert.That(result.FinalDamageBeforeShield, Is.EqualTo(156L));
        }

        [Test]
        public void Calculate_FloorsOnlyAfterAllMultiplication()
        {
            BossDamageResult result = BossDamageCalculator.Calculate(
                CreateContext(
                    baseAttack: 1d,
                    attackIncreaseRateSum: 0.01d,
                    attackCoefficient: 1.99d));

            Assert.That(result.FinalAttack, Is.EqualTo(1.01d));
            Assert.That(result.RawDamage, Is.EqualTo(2.0099d).Within(0.000000001d));
            Assert.That(result.FinalDamageBeforeShield, Is.EqualTo(2L));
        }

        [Test]
        public void Context_RejectsNegativeInputsAndNegativeFinalMultipliers()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(baseAttack: -1d));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(attackCoefficient: -1d));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(attackIncreaseRateSum: -0.01d));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(attackReductionRateSum: 1.01d));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(dealtDamageReductionRateSum: 1.01d));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(baseAttack: double.PositiveInfinity));
        }

        [Test]
        public void Calculate_RejectsNullContext()
        {
            Assert.Throws<ArgumentNullException>(
                () => BossDamageCalculator.Calculate(null));
        }

        private static BossDamageContext CreateContext(
            double baseAttack = 1d,
            double attackIncreaseRateSum = 0d,
            double attackReductionRateSum = 0d,
            double attackCoefficient = 1d,
            double dealtDamageIncreaseRateSum = 0d,
            double dealtDamageReductionRateSum = 0d,
            double partyTakenDamageIncreaseRateSum = 0d,
            double partyDamageReductionRateSum = 0d)
        {
            return new BossDamageContext(
                baseAttack,
                attackIncreaseRateSum,
                attackReductionRateSum,
                attackCoefficient,
                dealtDamageIncreaseRateSum,
                dealtDamageReductionRateSum,
                partyTakenDamageIncreaseRateSum,
                partyDamageReductionRateSum);
        }
    }
}
