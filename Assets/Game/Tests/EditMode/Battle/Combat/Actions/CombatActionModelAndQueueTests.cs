using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class CombatActionModelAndQueueTests
    {
        [Test]
        public void FollowUpPolicy_IsFixedByActionOrigin()
        {
            BossBattleState boss = Boss();

            var match = Damage(1, ActionOrigin.Match, boss);
            var active = Damage(2, ActionOrigin.Active, boss);
            var additional = Damage(
                3,
                ActionOrigin.Additional,
                boss,
                1,
                2);
            var chase = Damage(4, ActionOrigin.Chase, boss, 1, 3);
            var dot = Damage(5, ActionOrigin.DamageOverTime, boss);
            var system = Damage(6, ActionOrigin.System, boss);
            var activeResource = Action(7, ActionOrigin.Active, boss);

            Assert.That(match.CanTriggerFollowUp, Is.True);
            Assert.That(active.CanTriggerFollowUp, Is.True);
            Assert.That(additional.CanTriggerFollowUp, Is.False);
            Assert.That(chase.CanTriggerFollowUp, Is.False);
            Assert.That(dot.CanTriggerFollowUp, Is.False);
            Assert.That(system.CanTriggerFollowUp, Is.False);
            Assert.That(activeResource.CanTriggerFollowUp, Is.False);
            Assert.That(additional.RootActionId, Is.EqualTo(1L));
            Assert.That(additional.SourceActionId, Is.EqualTo(2L));
        }

        [Test]
        public void FollowUpOrigin_RequiresRootAndSourceTracking()
        {
            BossBattleState boss = Boss();

            Assert.Throws<ArgumentException>(() =>
                Damage(1, ActionOrigin.Additional, boss));
            Assert.Throws<ArgumentException>(() =>
                Damage(2, ActionOrigin.Chase, boss, 2, 1));
        }

        [Test]
        public void Queue_PreservesFifoWhenActionIsAddedAfterDequeue()
        {
            BossBattleState boss = Boss();
            CombatAction first = Action(1, ActionOrigin.System, boss);
            CombatAction second = Action(2, ActionOrigin.System, boss);
            CombatAction third = Action(3, ActionOrigin.System, boss);
            var queue = new CombatActionQueue(
                new[] { first, second });

            Assert.That(queue.TryDequeue(out CombatAction dequeued), Is.True);
            Assert.That(dequeued, Is.SameAs(first));
            queue.Enqueue(third);

            Assert.That(queue.TryDequeue(out dequeued), Is.True);
            Assert.That(dequeued, Is.SameAs(second));
            Assert.That(queue.TryDequeue(out dequeued), Is.True);
            Assert.That(dequeued, Is.SameAs(third));
            Assert.That(queue.TryDequeue(out dequeued), Is.False);
            Assert.That(dequeued, Is.Null);
        }

        [Test]
        public void Queue_RejectsNullWithoutPartialRangeInsertion()
        {
            BossBattleState boss = Boss();
            CombatAction existing = Action(1, ActionOrigin.System, boss);
            var queue = new CombatActionQueue(new[] { existing });

            Assert.Throws<ArgumentNullException>(() => queue.Enqueue(null));
            Assert.Throws<ArgumentException>(() => queue.EnqueueRange(
                new CombatAction[]
                {
                    Action(2, ActionOrigin.System, boss),
                    null,
                    Action(3, ActionOrigin.System, boss)
                }));

            Assert.That(queue.Count, Is.EqualTo(1));
            Assert.That(queue.TryDequeue(out CombatAction remaining), Is.True);
            Assert.That(remaining, Is.SameAs(existing));
        }

        [Test]
        public void Queue_ClearRemovesAllActions()
        {
            BossBattleState boss = Boss();
            var queue = new CombatActionQueue(new[]
            {
                Action(1, ActionOrigin.System, boss),
                Action(2, ActionOrigin.System, boss)
            });

            queue.Clear();

            Assert.That(queue.Count, Is.Zero);
            Assert.That(queue.TryDequeue(out _), Is.False);
        }

        private static AddResourceAction Action(
            long actionId,
            ActionOrigin origin,
            BossBattleState boss,
            long? rootActionId = null,
            long? sourceActionId = null)
        {
            return new AddResourceAction(
                actionId,
                origin,
                boss,
                "resource",
                1,
                rootActionId,
                sourceActionId);
        }

        private static DamageAction Damage(
            long actionId,
            ActionOrigin origin,
            BossBattleState boss,
            long? rootActionId = null,
            long? sourceActionId = null)
        {
            var character = new CharacterBattleState(
                $"hero_{actionId}",
                0,
                ElementType.Fire,
                1000,
                100d);
            var party = new PartyBattleState(new[] { character });
            AttackType attackType;
            switch (origin)
            {
                case ActionOrigin.Match:
                    attackType = AttackType.Match;
                    break;
                case ActionOrigin.Additional:
                    attackType = AttackType.Additional;
                    break;
                case ActionOrigin.Chase:
                    attackType = AttackType.Chase;
                    break;
                case ActionOrigin.DamageOverTime:
                    attackType = AttackType.DamageOverTime;
                    break;
                default:
                    attackType = AttackType.Active;
                    break;
            }

            return new DamageAction(
                actionId,
                origin,
                new DamageContextBuildRequest(
                    character,
                    party,
                    boss,
                    ElementType.Fire,
                    attackType,
                    AttackTag.None,
                    1d,
                    false,
                    0,
                    false),
                rootActionId,
                sourceActionId);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "boss",
                ElementType.Fire,
                1000,
                100d);
        }
    }
}
