using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiActiveBoardPlanProviderTests
    {
        private static readonly int[] PlayableBoard =
        {
            3, 0, 2, 1, 0,
            0, 2, 3, 1, 2,
            4, 4, 0, 2, 4,
            1, 2, 0, 0, 4,
            0, 1, 2, 1, 2,
            0, 4, 4, 0, 3
        };

        [TestCase(0, 4)]
        [TestCase(4, 4)]
        [TestCase(5, 5)]
        [TestCase(6, 5)]
        public void SuccessfulUseRequestsAwakeningConversionCount(
            int awakening,
            int expectedCount)
        {
            using (var battle = new TestBattle(awakening, succeeds: true))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);

                Assert.That(battle.Sink.RequestCount, Is.EqualTo(1));
                Assert.That(battle.Sink.Target, Is.EqualTo(ElementType.Water));
                Assert.That(battle.Sink.RequestedCount,
                    Is.EqualTo(expectedCount));
                Assert.That(battle.ActiveState.UsedThisTurn, Is.True);
                Assert.That(battle.ActiveState.RemainingCooldown,
                    Is.EqualTo(8));
                Assert.That(battle.ActionIds.LastIssuedId, Is.Zero);
            }
        }

        [TestCase(0)]
        [TestCase(5)]
        public void SuccessBeforeAwakeningSixDoesNotGrantEcho(int awakening)
        {
            using (var battle = new TestBattle(awakening, succeeds: true))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);

                Assert.That(battle.State.HasFrostEcho, Is.False);
            }
        }

        [Test]
        public void AwakeningSixSuccessGrantsOneEcho()
        {
            using (var battle = new TestBattle(6, succeeds: true))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);

                Assert.That(battle.State.FrostEchoCount, Is.EqualTo(1));
                Assert.That(battle.State.IsVisible, Is.True);
            }
        }

        [Test]
        public void FailedBoardConversionChangesNoRuntimeOrEchoState()
        {
            using (var battle = new TestBattle(6, succeeds: false))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.False);

                Assert.That(battle.Sink.RequestCount, Is.EqualTo(1));
                Assert.That(battle.ActiveState.UsedThisTurn, Is.False);
                Assert.That(battle.ActiveState.RemainingCooldown, Is.Zero);
                Assert.That(battle.State.HasFrostEcho, Is.False);
                Assert.That(battle.ActionIds.LastIssuedId, Is.Zero);
            }
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly ShimotsukiMiyukiCombatConfig config;

            public TestBattle(int awakening, bool succeeds)
            {
                config = ShimotsukiMiyukiTestConfig.Create();
                Character = new CharacterBattleState(
                    ShimotsukiMiyukiRules.CharacterId,
                    0,
                    ElementType.Water,
                    1000,
                    100d);
                State = new ShimotsukiMiyukiBattleState(
                    Character,
                    awakening >= 6);
                var provider = new ShimotsukiMiyukiActiveBoardPlanProvider(
                    config,
                    awakening,
                    State);
                var registry = new ActiveAbilityActionProviderRegistry();
                registry.RegisterBoard(
                    ShimotsukiMiyukiRules.CharacterId,
                    ShimotsukiMiyukiRules.ActiveAbilityId,
                    provider);
                Coordinator = new BattleFlowCoordinator(25, new[] { 8 });
                var party = new PartyBattleState(new[] { Character });
                var boss = new BossBattleState(
                    "miyuki_active_test_boss",
                    ElementType.Fire,
                    100000,
                    100d);
                ActionIds = new CombatActionIdSequence();
                Sink = new TestSink(succeeds);
                Executor = new ActiveAbilityCombatExecutor(
                    Coordinator,
                    party,
                    boss,
                    new CombatActionExecutor(
                        boss,
                        party,
                        new DamageContextFactory(
                            new SeededRandomSource(1))),
                    ActionIds,
                    new ActiveAbilityBinding[]
                    {
                        new ActiveAbilityBinding(
                            0,
                            0,
                            ShimotsukiMiyukiRules.CharacterId,
                            ShimotsukiMiyukiRules.ActiveAbilityId)
                    },
                    registry,
                    Sink);
                Coordinator.StartBattle();
            }

            public CharacterBattleState Character { get; }
            public ShimotsukiMiyukiBattleState State { get; }
            public BattleFlowCoordinator Coordinator { get; }
            public ActiveAbilityCombatExecutor Executor { get; }
            public CombatActionIdSequence ActionIds { get; }
            public TestSink Sink { get; }
            public ActiveAbilityRuntimeState ActiveState =>
                Coordinator.Context.ActiveAbilities[0];

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private sealed class TestSink : IBattleBoardElementConversionSink
        {
            private readonly bool succeeds;

            public TestSink(bool succeeds)
            {
                this.succeeds = succeeds;
            }

            public bool CanAcceptElementConversion => true;
            public int RequestCount { get; private set; }
            public ElementType Target { get; private set; }
            public int RequestedCount { get; private set; }

            public bool TryConvertElements(
                ElementType targetElement,
                int requestedCount,
                out BoardElementConversionResult result)
            {
                RequestCount++;
                Target = targetElement;
                RequestedCount = requestedCount;
                if (!succeeds)
                {
                    result = null;
                    return false;
                }

                result = new BoardElementConversionResolver(
                    new SequenceRandomSource(
                        requestedCount == 4
                            ? new[] { 0, 1, 2, 4 }
                            : new[] { 0, 1, 2, 3, 4 }),
                    new BoardMoveAnalyzer()).Resolve(
                        CreateBoard(),
                        targetElement,
                        requestedCount);
                return result.Succeeded;
            }

            private static BoardState CreateBoard()
            {
                var board = new BoardState();
                for (int index = 0;
                    index < BoardConstants.CellCount;
                    index++)
                {
                    BoardPosition position = BoardPosition.FromIndex(index);
                    board.Set(position, new BoardBlock(
                        index + 1,
                        BoardBlockType.Normal,
                        (ElementType)PlayableBoard[index]));
                }

                return board;
            }
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly Queue<int> values;

            public SequenceRandomSource(IEnumerable<int> values)
            {
                this.values = new Queue<int>(values);
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                int value = values.Dequeue();
                Assert.That(value, Is.InRange(minInclusive, maxExclusive - 1));
                return value;
            }

            public float NextFloat()
            {
                return 0f;
            }
        }
    }
}
