using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.State
{
    public sealed class ShieldCollectionTests
    {
        [Test]
        public void Add_IncreasesTotalShieldAndExposesReadOnlyList()
        {
            var shields = new ShieldCollection();
            shields.Add(Shield(1, 500, 2, 1));
            shields.Add(Shield(2, 800, null, 2));

            Assert.That(shields.TotalShield, Is.EqualTo(1300L));
            Assert.That(shields.ActiveShields, Has.Count.EqualTo(2));
            var exposed = shields.ActiveShields as IList<ShieldInstance>;
            Assert.That(exposed, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() =>
                exposed.Add(Shield(3, 100, 1, 3)));
        }

        [Test]
        public void Absorb_PartiallyConsumesShield()
        {
            var shields = new ShieldCollection();
            ShieldInstance shield = Shield(1, 500, 1, 1);
            shields.Add(shield);

            ShieldAbsorptionResult result = shields.Absorb(200);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(200L));
            Assert.That(result.RemainingDamage, Is.Zero);
            Assert.That(shield.CurrentAmount, Is.EqualTo(300L));
            Assert.That(shields.TotalShield, Is.EqualTo(300L));
        }

        [Test]
        public void Absorb_ShieldCanFullyAbsorbIncomingDamage()
        {
            var shields = new ShieldCollection();
            shields.Add(Shield(1, 1000, 1, 1));

            ShieldAbsorptionResult result = shields.Absorb(900);

            Assert.That(result.IncomingDamage, Is.EqualTo(900L));
            Assert.That(result.AbsorbedDamage, Is.EqualTo(900L));
            Assert.That(result.RemainingDamage, Is.Zero);
            Assert.That(result.TotalShieldAfter, Is.EqualTo(100L));
        }

        [Test]
        public void Absorb_ExactlyDepletedShieldIsRemoved()
        {
            var shields = new ShieldCollection();
            shields.Add(Shield(7, 500, 1, 1));

            ShieldAbsorptionResult result = shields.Absorb(500);

            Assert.That(shields.ActiveShields, Is.Empty);
            Assert.That(shields.TotalShield, Is.Zero);
            Assert.That(result.DepletedShieldRuntimeIds,
                Is.EqualTo(new long[] { 7 }));
        }

        [Test]
        public void Absorb_UsesDurationThenCreationOrderThenIndefinite()
        {
            var shields = new ShieldCollection();
            ShieldInstance indefinite = Shield(3, 1000, null, 3);
            ShieldInstance twoTurns = Shield(2, 800, 2, 2);
            ShieldInstance oneTurn = Shield(1, 500, 1, 1);
            shields.Add(indefinite);
            shields.Add(twoTurns);
            shields.Add(oneTurn);

            ShieldAbsorptionResult result = shields.Absorb(900);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(900L));
            Assert.That(result.RemainingDamage, Is.Zero);
            Assert.That(result.TotalShieldBefore, Is.EqualTo(2300L));
            Assert.That(result.TotalShieldAfter, Is.EqualTo(1400L));
            Assert.That(oneTurn.IsDepleted, Is.True);
            Assert.That(twoTurns.CurrentAmount, Is.EqualTo(400L));
            Assert.That(indefinite.CurrentAmount, Is.EqualTo(1000L));
            Assert.That(result.DepletedShieldRuntimeIds,
                Is.EqualTo(new long[] { 1 }));
        }

        [Test]
        public void Absorb_SameDurationUsesAscendingCreationOrder()
        {
            var shields = new ShieldCollection();
            ShieldInstance later = Shield(2, 100, 2, 2);
            ShieldInstance earlier = Shield(1, 100, 2, 1);
            shields.Add(later);
            shields.Add(earlier);

            shields.Absorb(150);

            Assert.That(earlier.IsDepleted, Is.True);
            Assert.That(later.CurrentAmount, Is.EqualTo(50L));
        }

        [Test]
        public void Absorb_IndefiniteShieldsUseAscendingCreationOrderLast()
        {
            var shields = new ShieldCollection();
            ShieldInstance indefiniteEarlier = Shield(2, 100, null, 2);
            ShieldInstance finite = Shield(3, 100, 5, 3);
            ShieldInstance indefiniteLater = Shield(1, 100, null, 4);
            shields.Add(indefiniteLater);
            shields.Add(indefiniteEarlier);
            shields.Add(finite);

            shields.Absorb(250);

            Assert.That(finite.IsDepleted, Is.True);
            Assert.That(indefiniteEarlier.IsDepleted, Is.True);
            Assert.That(indefiniteLater.CurrentAmount, Is.EqualTo(50L));
        }

        [Test]
        public void ProcessTurnEnd_DecrementsFiniteAndRemovesExpiredOnly()
        {
            var shields = new ShieldCollection();
            ShieldInstance finite = Shield(1, 500, 2, 1);
            ShieldInstance indefinite = Shield(2, 600, null, 2);
            shields.Add(finite);
            shields.Add(indefinite);

            shields.ProcessTurnEnd();

            Assert.That(finite.RemainingTurns, Is.EqualTo(1));
            Assert.That(indefinite.RemainingTurns, Is.Null);
            Assert.That(shields.ActiveShields, Has.Count.EqualTo(2));

            shields.ProcessTurnEnd();

            Assert.That(finite.RemainingTurns, Is.Zero);
            Assert.That(finite.IsExpired, Is.True);
            Assert.That(shields.ActiveShields,
                Is.EqualTo(new[] { indefinite }));
            Assert.That(shields.TotalShield, Is.EqualTo(600L));
        }

        [Test]
        public void Constructor_RejectsInvalidShieldInputs()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Shield(0, 100, 1, 1));
            Assert.Throws<ArgumentException>(() =>
                new ShieldInstance(1, "", 100, 1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Shield(1, 0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShieldInstance(1, "source", 100, 0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Shield(1, 100, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Shield(1, 100, 1, 0));
        }

        [Test]
        public void Add_RejectsDuplicateRuntimeIdAndCreationOrder()
        {
            var shields = new ShieldCollection();
            shields.Add(Shield(1, 100, 1, 1));

            Assert.Throws<ArgumentException>(() =>
                shields.Add(Shield(1, 200, 2, 2)));
            Assert.Throws<ArgumentException>(() =>
                shields.Add(Shield(2, 200, 2, 1)));
        }

        [Test]
        public void Add_RejectsShieldAlreadyRegisteredElsewhere()
        {
            var first = new ShieldCollection();
            var second = new ShieldCollection();
            ShieldInstance shield = Shield(1, 100, 1, 1);
            first.Add(shield);

            Assert.Throws<InvalidOperationException>(() =>
                second.Add(shield));
            Assert.That(first.TotalShield, Is.EqualTo(100L));
            Assert.That(second.TotalShield, Is.Zero);
        }

        [Test]
        public void Absorb_RejectsNegativeDamage()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShieldCollection().Absorb(-1));
        }

        private static ShieldInstance Shield(
            long runtimeId,
            long amount,
            int? remainingTurns,
            long creationOrder)
        {
            return new ShieldInstance(
                runtimeId,
                "source",
                amount,
                1,
                remainingTurns,
                creationOrder);
        }
    }
}
