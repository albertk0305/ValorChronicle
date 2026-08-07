using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Application
{
    public sealed class BossHealthDamageApplierTests
    {
        [Test]
        public void Apply_ReducesBossHpByRequestedDamage()
        {
            BossBattleState boss = Boss(1000);

            BossDamageApplicationResult result =
                BossHealthDamageApplier.Apply(boss, Damage(300));

            Assert.That(result.RequestedDamage, Is.EqualTo(300L));
            Assert.That(result.AppliedDamage, Is.EqualTo(300L));
            Assert.That(result.OverkillDamage, Is.Zero);
            Assert.That(result.HpBefore, Is.EqualTo(1000L));
            Assert.That(result.HpAfter, Is.EqualTo(700L));
            Assert.That(boss.CurrentHp, Is.EqualTo(700L));
        }

        [Test]
        public void Apply_ExactDamageDefeatsBoss()
        {
            BossBattleState boss = Boss(500);

            BossDamageApplicationResult result =
                BossHealthDamageApplier.Apply(boss, Damage(500));

            Assert.That(result.AppliedDamage, Is.EqualTo(500L));
            Assert.That(result.OverkillDamage, Is.Zero);
            Assert.That(result.BecameDefeated, Is.True);
            Assert.That(result.IsDefeatedAfter, Is.True);
            Assert.That(boss.CurrentHp, Is.Zero);
        }

        [Test]
        public void Apply_SeparatesOverkillAndNeverMakesHpNegative()
        {
            BossBattleState boss = Boss(500);

            BossDamageApplicationResult result =
                BossHealthDamageApplier.Apply(boss, Damage(800));

            Assert.That(result.AppliedDamage, Is.EqualTo(500L));
            Assert.That(result.OverkillDamage, Is.EqualTo(300L));
            Assert.That(result.HpAfter, Is.Zero);
            Assert.That(boss.CurrentHp, Is.Zero);
        }

        [Test]
        public void Apply_AlreadyDefeatedBossIgnoresDamageWithoutOverkill()
        {
            BossBattleState boss = Boss(100);
            BossHealthDamageApplier.Apply(boss, Damage(100));

            BossDamageApplicationResult result =
                BossHealthDamageApplier.Apply(boss, Damage(50));

            Assert.That(result.RequestedDamage, Is.EqualTo(50L));
            Assert.That(result.AppliedDamage, Is.Zero);
            Assert.That(result.OverkillDamage, Is.Zero);
            Assert.That(result.WasDefeatedBefore, Is.True);
            Assert.That(result.IsDefeatedAfter, Is.True);
            Assert.That(result.BecameDefeated, Is.False);
        }

        [Test]
        public void Apply_RejectsNullInputs()
        {
            BossBattleState boss = Boss(100);
            DamageResult damage = Damage(10);

            Assert.Throws<ArgumentNullException>(() =>
                BossHealthDamageApplier.Apply(null, damage));
            Assert.Throws<ArgumentNullException>(() =>
                BossHealthDamageApplier.Apply(boss, null));
        }

        private static BossBattleState Boss(long maxHp)
        {
            return new BossBattleState(
                "boss",
                ElementType.Dark,
                maxHp,
                100d);
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
