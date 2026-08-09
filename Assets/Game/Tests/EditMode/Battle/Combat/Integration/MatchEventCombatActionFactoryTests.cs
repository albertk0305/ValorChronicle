using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class MatchEventCombatActionFactoryTests
    {
        [Test]
        public void FactoryFiltersElementAndPreservesSlotActionAndIdOrder()
        {
            PartyBattleState party = Party(
                Character("fire_4", 4, ElementType.Fire),
                Character("water_1", 1, ElementType.Water),
                Character("fire_2", 2, ElementType.Fire),
                Character("fire_0", 0, ElementType.Fire));
            BossBattleState boss = Boss();
            var actionIds = new CombatActionIdSequence();
            var calls = new List<MatchEventActionContext>();
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                calls.Add(context);
                return new CombatAction[]
                {
                    CombatIntegrationTestSupport.MatchDamage(context, 1d),
                    CombatIntegrationTestSupport.MatchDamage(context, 2d)
                };
            });
            var factory = new MatchEventCombatActionFactory(
                party,
                boss,
                provider,
                actionIds);
            MatchEventExecution execution = BeginSingleMatch(
                ElementType.Fire,
                4);

            IReadOnlyList<CombatAction> actions =
                factory.CreateRootActions(execution);

            Assert.That(
                calls.ConvertAll(call => call.Character.PartySlotIndex),
                Is.EqualTo(new[] { 0, 2, 4 }));
            Assert.That(actions, Has.Count.EqualTo(6));
            Assert.That(
                Array.ConvertAll(
                    new List<CombatAction>(actions).ToArray(),
                    action => action.ActionId),
                Is.EqualTo(new long[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(calls, Has.All.Matches<MatchEventActionContext>(
                call => call.FinalComboCount == 1
                    && call.MatchAttackTag == AttackTag.Match4));
            Assert.That(actions, Has.All.Matches<CombatAction>(
                action => action.RootActionId == action.ActionId
                    && !action.SourceActionId.HasValue));
        }

        [Test]
        public void FactoryAllowsZeroActionsAndNoMatchingCharacter()
        {
            int providerCallCount = 0;
            PartyBattleState party = Party(
                Character("water", 1, ElementType.Water),
                Character("fire", 3, ElementType.Fire));
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                providerCallCount++;
                return Array.Empty<CombatAction>();
            });
            var factory = new MatchEventCombatActionFactory(
                party,
                Boss(),
                provider,
                new CombatActionIdSequence());

            Assert.That(
                factory.CreateRootActions(
                    BeginSingleMatch(ElementType.Fire, 3)),
                Is.Empty);
            Assert.That(providerCallCount, Is.EqualTo(1));
            Assert.That(
                factory.CreateRootActions(
                    BeginSingleMatch(ElementType.Dark, 5)),
                Is.Empty);
            Assert.That(providerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedElementEventsRemainSeparateAndMapEveryTier()
        {
            PartyBattleState party = Party(
                Character("fire_2", 2, ElementType.Fire),
                Character("fire_0", 0, ElementType.Fire));
            var contexts = new List<MatchEventActionContext>();
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                contexts.Add(context);
                return Array.Empty<CombatAction>();
            });
            var factory = new MatchEventCombatActionFactory(
                party,
                Boss(),
                provider,
                new CombatActionIdSequence());
            BattleFlowCoordinator coordinator = StartBoardResolution(25);
            coordinator.NotifyBoardActionResolved(
                ThreeTierCascade(),
                true);

            for (int eventIndex = 0; eventIndex < 3; eventIndex++)
            {
                coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution execution);
                Assert.That(factory.CreateRootActions(execution), Is.Empty);
                coordinator.CompleteCurrentMatchEvent(execution.ExecutionId);
            }

            Assert.That(
                contexts.ConvertAll(context =>
                    $"E{context.MatchEvent.SequenceIndex}-"
                        + $"S{context.Character.PartySlotIndex}"),
                Is.EqualTo(new[]
                {
                    "E0-S0", "E0-S2",
                    "E1-S0", "E1-S2",
                    "E2-S0", "E2-S2"
                }));
            Assert.That(
                contexts.ConvertAll(context => context.MatchAttackTag),
                Is.EqualTo(new[]
                {
                    AttackTag.Match3, AttackTag.Match3,
                    AttackTag.Match4, AttackTag.Match4,
                    AttackTag.Match5Plus, AttackTag.Match5Plus
                }));
            Assert.That(
                contexts.ConvertAll(context => context.FinalComboCount),
                Has.All.EqualTo(3));
        }

        [Test]
        public void FourEventBatchSharesFinalComboAndResetsNextTurn()
        {
            BattleFlowCoordinator coordinator = StartBoardResolution(2);
            coordinator.NotifyBoardActionResolved(
                FourMatchCascade(),
                true);

            var observedCounts = new List<int>();
            while (coordinator.Context.Phase
                == BattlePhase.MatchEventResolving)
            {
                if (!coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution execution))
                {
                    break;
                }

                observedCounts.Add(execution.FinalComboCount);
                coordinator.CompleteCurrentMatchEvent(
                    execution.ExecutionId);
            }

            Assert.That(observedCounts, Is.EqualTo(new[] { 4, 4, 4, 4 }));
            Assert.That(coordinator.CompleteBossAction(), Is.True);
            Assert.That(coordinator.CompleteActiveInput(), Is.True);
            Assert.That(coordinator.NotifyBoardActionStarted(), Is.True);
            Assert.That(
                coordinator.NotifyBoardActionResolved(null, false),
                Is.True);
            Assert.That(coordinator.NotifyBoardActionStarted(), Is.True);
            Assert.That(
                coordinator.NotifyBoardActionResolved(
                    SingleMatchCascade(ElementType.Fire, 3),
                    true),
                Is.True);
            Assert.That(
                coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution nextTurnExecution),
                Is.True);
            Assert.That(nextTurnExecution.FinalComboCount, Is.EqualTo(1));
        }

        [Test]
        public void FactoryRejectsDamageWithWrongFinalComboMetadata()
        {
            CharacterBattleState character =
                Character("fire", 0, ElementType.Fire);
            PartyBattleState party = Party(character);
            BossBattleState boss = Boss();
            var provider = new DelegateMatchEventActionProvider(context =>
                new CombatAction[]
                {
                    new DamageAction(
                        context.ActionIds.Next(),
                        ActionOrigin.Match,
                        new DamageContextBuildRequest(
                            character,
                            party,
                            boss,
                            ElementType.Fire,
                            AttackType.Match,
                            AttackTag.Match3,
                            1d,
                            true,
                            99,
                            false))
                });
            var factory = new MatchEventCombatActionFactory(
                party,
                boss,
                provider,
                new CombatActionIdSequence());

            Assert.Throws<InvalidOperationException>(() =>
                factory.CreateRootActions(
                    BeginSingleMatch(ElementType.Fire, 3)));
        }

        private static MatchEventExecution BeginSingleMatch(
            ElementType element,
            int blockCount)
        {
            BattleFlowCoordinator coordinator = StartBoardResolution(25);
            coordinator.NotifyBoardActionResolved(
                SingleMatchCascade(element, blockCount),
                true);
            coordinator.TryBeginNextMatchEvent(out MatchEventExecution result);
            return result;
        }

        private static BattleFlowCoordinator StartBoardResolution(int turns)
        {
            var coordinator = new BattleFlowCoordinator(turns);
            coordinator.StartBattle();
            coordinator.CompleteActiveInput();
            coordinator.NotifyBoardActionStarted();
            return coordinator;
        }

        private static BoardCascadeResult SingleMatchCascade(
            ElementType element,
            int blockCount)
        {
            var positions = new BoardPosition[blockCount];
            for (int index = 0; index < blockCount; index++)
            {
                positions[index] = new BoardPosition(index, 0);
            }

            return BattleFlowTestSupport.CreateCascade(
                new[] { BattleFlowTestSupport.Match(element, positions) });
        }

        private static BoardCascadeResult FourMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(new[]
            {
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0),
                    new BoardPosition(2, 0)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 1),
                    new BoardPosition(1, 1),
                    new BoardPosition(2, 1)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 2),
                    new BoardPosition(1, 2),
                    new BoardPosition(2, 2)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 3),
                    new BoardPosition(1, 3),
                    new BoardPosition(2, 3))
            });
        }

        private static BoardCascadeResult ThreeTierCascade()
        {
            return BattleFlowTestSupport.CreateCascade(new[]
            {
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0),
                    new BoardPosition(2, 0)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 1),
                    new BoardPosition(1, 1),
                    new BoardPosition(2, 1),
                    new BoardPosition(3, 1)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 2),
                    new BoardPosition(1, 2),
                    new BoardPosition(2, 2),
                    new BoardPosition(3, 2),
                    new BoardPosition(4, 2))
            });
        }

        private static CharacterBattleState Character(
            string id,
            int slot,
            ElementType element)
        {
            return new CharacterBattleState(id, slot, element, 1000, 100d);
        }

        private static PartyBattleState Party(
            params CharacterBattleState[] characters)
        {
            return new PartyBattleState(characters);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "boss",
                ElementType.Fire,
                10000,
                100d);
        }
    }
}
