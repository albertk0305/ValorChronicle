using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Application
{
    public sealed class PartyDamageApplierTests
    {
        [Test]
        public void Apply_WithoutShieldDamagesSharedHp()
        {
            PartyBattleState party = Party(1000);

            PartyDamageApplicationResult result =
                PartyDamageApplier.Apply(party, Damage(300));

            Assert.That(result.RequestedDamage, Is.EqualTo(300L));
            Assert.That(result.ShieldAbsorbedDamage, Is.Zero);
            Assert.That(result.HpDamage, Is.EqualTo(300L));
            Assert.That(result.HpAfter, Is.EqualTo(700L));
            Assert.That(party.CurrentHp, Is.EqualTo(700L));
        }

        [Test]
        public void Apply_ShieldAbsorbsBeforeRemainingHpDamage()
        {
            PartyBattleState party = Party(1000);
            party.Shields.Add(Shield(1, 100, 1));

            PartyDamageApplicationResult result =
                PartyDamageApplier.Apply(party, Damage(300));

            Assert.That(result.ShieldAbsorbedDamage, Is.EqualTo(100L));
            Assert.That(result.HpDamage, Is.EqualTo(200L));
            Assert.That(result.HpAfter, Is.EqualTo(800L));
            Assert.That(result.TotalShieldAfter, Is.Zero);
        }

        [Test]
        public void Apply_ShieldCanAbsorbAllDamage()
        {
            PartyBattleState party = Party(1000);
            party.Shields.Add(Shield(1, 500, 1));

            PartyDamageApplicationResult result =
                PartyDamageApplier.Apply(party, Damage(300));

            Assert.That(result.ShieldAbsorbedDamage, Is.EqualTo(300L));
            Assert.That(result.HpDamage, Is.Zero);
            Assert.That(result.HpAfter, Is.EqualTo(1000L));
            Assert.That(result.TotalShieldAfter, Is.EqualTo(200L));
        }

        [Test]
        public void Apply_MultipleShieldsMatchesRepresentativeDamage()
        {
            PartyBattleState party = Party(10000);
            party.Shields.Add(Shield(1, 500, 1));
            party.Shields.Add(Shield(2, 800, 2));

            PartyDamageApplicationResult result =
                PartyDamageApplier.Apply(party, Damage(2790));

            Assert.That(result.RequestedDamage, Is.EqualTo(2790L));
            Assert.That(result.ShieldAbsorbedDamage, Is.EqualTo(1300L));
            Assert.That(result.HpDamage, Is.EqualTo(1490L));
            Assert.That(result.OverkillDamage, Is.Zero);
            Assert.That(result.HpAfter, Is.EqualTo(8510L));
            Assert.That(result.TotalShieldBefore, Is.EqualTo(1300L));
            Assert.That(result.TotalShieldAfter, Is.Zero);
        }

        [Test]
        public void Apply_ExactHpDamageIncapacitatesParty()
        {
            PartyBattleState party = Party(500);

            PartyDamageApplicationResult result =
                PartyDamageApplier.Apply(party, Damage(500));

            Assert.That(result.HpDamage, Is.EqualTo(500L));
            Assert.That(result.OverkillDamage, Is.Zero);
            Assert.That(result.BecameIncapacitated, Is.True);
            Assert.That(result.IsIncapacitatedAfter, Is.True);
            Assert.That(party.CurrentHp, Is.Zero);
        }

        [Test]
        public void Apply_SeparatesHpOverkillAndNeverMakesHpNegative()
        {
            PartyBattleState party = Party(500);
            party.Shields.Add(Shield(1, 100, 1));

            PartyDamageApplicationResult result =
                PartyDamageApplier.Apply(party, Damage(800));

            Assert.That(result.ShieldAbsorbedDamage, Is.EqualTo(100L));
            Assert.That(result.HpDamage, Is.EqualTo(500L));
            Assert.That(result.OverkillDamage, Is.EqualTo(200L));
            Assert.That(result.HpAfter, Is.Zero);
            Assert.That(party.CurrentHp, Is.Zero);
        }

        [Test]
        public void Apply_AlreadyIncapacitatedPartyIgnoresDamageAndShield()
        {
            PartyBattleState party = Party(100);
            PartyDamageApplier.Apply(party, Damage(100));
            party.Shields.Add(Shield(1, 500, 1));

            PartyDamageApplicationResult result =
                PartyDamageApplier.Apply(party, Damage(300));

            Assert.That(result.RequestedDamage, Is.EqualTo(300L));
            Assert.That(result.ShieldAbsorbedDamage, Is.Zero);
            Assert.That(result.HpDamage, Is.Zero);
            Assert.That(result.OverkillDamage, Is.Zero);
            Assert.That(result.WasIncapacitatedBefore, Is.True);
            Assert.That(result.BecameIncapacitated, Is.False);
            Assert.That(party.Shields.TotalShield, Is.EqualTo(500L));
        }

        [Test]
        public void Apply_RejectsNullInputs()
        {
            PartyBattleState party = Party(100);
            BossDamageResult damage = Damage(10);

            Assert.Throws<ArgumentNullException>(() =>
                PartyDamageApplier.Apply(null, damage));
            Assert.Throws<ArgumentNullException>(() =>
                PartyDamageApplier.Apply(party, null));
        }

        private static PartyBattleState Party(long maximumHp)
        {
            return new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero",
                    0,
                    ElementType.Fire,
                    maximumHp,
                    100d)
            });
        }

        private static ShieldInstance Shield(
            long runtimeId,
            long amount,
            long creationOrder)
        {
            return new ShieldInstance(
                runtimeId,
                "source",
                amount,
                1,
                1,
                creationOrder);
        }

        private static BossDamageResult Damage(long damage)
        {
            return BossDamageCalculator.Calculate(new BossDamageContext(
                damage,
                0d,
                0d,
                1d,
                0d,
                0d,
                0d,
                0d));
        }
    }
}
