using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.State
{
    public sealed class BossBattleStateTests
    {
        [Test]
        public void Constructor_CreatesFullHpRuntimeState()
        {
            var boss = new BossBattleState(
                "boss_test",
                ElementType.Dark,
                50000,
                1550d);

            Assert.That(boss.BossId, Is.EqualTo("boss_test"));
            Assert.That(boss.Element, Is.EqualTo(ElementType.Dark));
            Assert.That(boss.MaxHp, Is.EqualTo(50000L));
            Assert.That(boss.CurrentHp, Is.EqualTo(50000L));
            Assert.That(boss.Attack, Is.EqualTo(1550d));
            Assert.That(boss.IsDefeated, Is.False);
        }

        [Test]
        public void IsDefeated_IsTrueWhenHpReachesZero()
        {
            var boss = CreateBoss(maxHp: 100);

            BossHealthDamageApplier.Apply(boss, Damage(100));

            Assert.That(boss.CurrentHp, Is.Zero);
            Assert.That(boss.IsDefeated, Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RejectsMissingBossId(string bossId)
        {
            Assert.Throws<ArgumentException>(() => CreateBoss(bossId));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RejectsNonPositiveMaximumHp(long maxHp)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateBoss(maxHp: maxHp));
        }

        [TestCase(-0.01d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Constructor_RejectsInvalidAttack(double attack)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateBoss(attack: attack));
        }

        private static BossBattleState CreateBoss(
            string bossId = "boss",
            long maxHp = 100,
            double attack = 10d)
        {
            return new BossBattleState(
                bossId,
                ElementType.Dark,
                maxHp,
                attack);
        }

        private static DamageResult Damage(long damage)
        {
            return DamageCalculator.Calculate(new DamageContext(
                damage,
                0d,
                1d,
                false,
                0,
                false,
                false,
                1d,
                0d,
                0d,
                0d,
                ElementType.Fire,
                ElementType.Fire,
                0d,
                0d));
        }
    }
}
