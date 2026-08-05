using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Flow
{
    public sealed class BattleContextTests
    {
        [Test]
        public void Constructor_CreatesNotStartedStateAndZeroCooldowns()
        {
            var context = new BattleContext(25, new[] { 8, 3 });

            Assert.That(context.TurnLimit, Is.EqualTo(25));
            Assert.That(context.CurrentTurn, Is.Zero);
            Assert.That(context.Phase, Is.EqualTo(BattlePhase.NotStarted));
            Assert.That(context.Result, Is.EqualTo(BattleResultKind.None));
            Assert.That(context.ActiveAbilities, Has.Count.EqualTo(2));
            Assert.That(
                context.ActiveAbilities[0].RemainingCooldown,
                Is.Zero);
            Assert.That(context.ActiveAbilities[0].UsedThisTurn, Is.False);
        }

        [Test]
        public void Constructor_CopiesCooldownInputAndExposesReadOnlyList()
        {
            var cooldowns = new[] { 4 };
            var context = new BattleContext(10, cooldowns);
            cooldowns[0] = 99;

            Assert.That(
                context.ActiveAbilities[0].CooldownTurns,
                Is.EqualTo(4));
            var list = context.ActiveAbilities
                as IList<ActiveAbilityRuntimeState>;
            Assert.That(list, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() => list.Add(null));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RejectsNonPositiveTurnLimit(int turnLimit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BattleContext(turnLimit));
        }

        [Test]
        public void Constructor_RejectsNullOrNegativeCooldownInput()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BattleContext(25, null));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BattleContext(25, new[] { -1 }));
        }
    }
}
