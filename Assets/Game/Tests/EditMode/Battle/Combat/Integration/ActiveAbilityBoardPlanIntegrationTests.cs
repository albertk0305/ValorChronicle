using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class ActiveAbilityBoardPlanIntegrationTests
    {
        private static readonly int[] DeadBoardElements =
        {
            3, 0, 2, 1, 0,
            0, 2, 3, 1, 2,
            4, 4, 0, 2, 4,
            1, 2, 0, 0, 4,
            0, 1, 2, 1, 2,
            0, 4, 4, 0, 3
        };

        [TestCase(4, new int[] { 0, 1, 2, 4 })]
        [TestCase(5, new int[] { 0, 1, 2, 3, 4 })]
        public void BoardOnlySuccessConvertsRequestedCountAndConsumesUse(
            int requestedCount,
            int[] randomValues)
        {
            BoardState original = CreateBoard(DeadBoardElements);
            var sink = new ResolverBoardSink(original, randomValues);
            RuntimeBattle battle = CreateBoardBattle(sink, requestedCount);
            int appliedNotifications = 0;
            int actionSteps = 0;
            battle.Bridge.CombatActionsApplied +=
                () => appliedNotifications++;
            battle.Bridge.ActiveCombatActionStepApplied +=
                _ => actionSteps++;

            Assert.That(battle.Bridge.TryUseActive(0), Is.True);

            Assert.That(sink.RequestCount, Is.EqualTo(1));
            Assert.That(sink.LastTargetElement, Is.EqualTo(ElementType.Water));
            Assert.That(sink.LastRequestedCount, Is.EqualTo(requestedCount));
            Assert.That(sink.Board, Is.Not.SameAs(original));
            Assert.That(sink.LastResult.ConvertedCount,
                Is.EqualTo(requestedCount));
            Assert.That(((RecordingBoardProvider)battle.BoardProvider)
                .SuccessCommitCount, Is.EqualTo(1));
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(battle.Coordinator.PendingMatchEventCount, Is.Zero);
            Assert.That(battle.Coordinator.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(battle.ActiveState.UsedThisTurn, Is.True);
            Assert.That(battle.ActiveState.RemainingCooldown, Is.EqualTo(3));
            Assert.That(battle.ActionIds.LastIssuedId, Is.Zero);
            Assert.That(battle.Bridge.LastActiveExecutionResult.ActionResults,
                Is.Empty);
            Assert.That(appliedNotifications, Is.Zero);
            Assert.That(actionSteps, Is.Zero);
        }

        [Test]
        public void BoardSuccessActionsRunAfterCommitThroughActivePipeline()
        {
            BoardState original = CreateBoard(DeadBoardElements);
            var sink = new ResolverBoardSink(
                original,
                new[] { 0, 1, 2, 4 });
            var order = new List<string>();
            var provider = new RecordingSuccessActionBoardProvider(
                order,
                useForeignPartyTarget: false);
            RuntimeBattle battle = CreateBoardBattle(sink, 4, provider);
            provider.SetActiveStateSource(() => battle.ActiveState);
            battle.Bridge.ActiveCombatActionStepApplied +=
                _ => order.Add("action_step");
            battle.Bridge.CombatActionsApplied +=
                () => order.Add("actions_applied");

            Assert.That(battle.Bridge.TryUseActive(0), Is.True);

            Assert.That(order, Is.EqualTo(new[]
            {
                "plan",
                "commit",
                "provide_actions",
                "action_step",
                "actions_applied"
            }));
            Assert.That(provider.CommitObservedCommittedCooldown, Is.True);
            Assert.That(provider.ObservedTurn, Is.EqualTo(1));
            Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
            Assert.That(battle.Party.Effects.FindByEffectId(
                "board_success_effect"), Has.Count.EqualTo(1));
            Assert.That(battle.ActionIds.LastIssuedId, Is.EqualTo(1));
            Assert.That(battle.Bridge.LastActiveExecutionResult.ActionResults,
                Has.Count.EqualTo(1));
            Assert.That(battle.Bridge.LastActiveExecutionResult
                .ActionResults[0], Is.TypeOf<ApplyEffectActionResult>());
            Assert.That(battle.Bridge.LastActiveExecutionResult
                .ActionResults[0].ExecutionOrder, Is.EqualTo(1));
            CombatAction recordedAction = battle.Bridge
                .LastActiveExecutionResult.ActionResults[0].Action;
            Assert.That(recordedAction.ActionId, Is.EqualTo(1));
            Assert.That(recordedAction.RootActionId, Is.EqualTo(1));
            Assert.That(recordedAction.SourceActionId, Is.Null);
            Assert.That(recordedAction.Origin, Is.EqualTo(ActionOrigin.Active));
        }

        [Test]
        public void NoCandidatesFailurePreservesBoardRuntimeAndCanRetry()
        {
            BoardState original = CreateUniformBoard(ElementType.Water);
            var sink = new ResolverBoardSink(original, Array.Empty<int>());
            var provider = new RecordingSuccessActionBoardProvider(
                new List<string>(),
                useForeignPartyTarget: false);
            RuntimeBattle battle = CreateBoardBattle(sink, 4, provider);

            Assert.That(battle.Bridge.TryUseActive(0), Is.False);
            AssertFailureStateUnchanged(battle, sink, original);
            Assert.That(provider.SuccessCommitCount, Is.Zero);
            Assert.That(provider.SuccessActionCallCount, Is.Zero);
            Assert.That(sink.LastResult.Failure,
                Is.EqualTo(BoardElementConversionFailure.NoCandidates));

            sink.SetBoard(CreateBoard(DeadBoardElements), 0, 1, 2, 4);
            Assert.That(battle.Bridge.TryUseActive(0), Is.True);
            Assert.That(battle.ActiveState.UsedThisTurn, Is.True);
        }

        [Test]
        public void NoPlayableCombinationFailurePreservesBoardAndRuntime()
        {
            BoardState original = CreateUniformBoard(ElementType.Water);
            SetElement(original, 0, 0, ElementType.Fire);
            var sink = new ResolverBoardSink(
                original,
                new[] { 0 },
                maxRandomSelectionAttempts: 1);
            var provider = new RecordingSuccessActionBoardProvider(
                new List<string>(),
                useForeignPartyTarget: false);
            RuntimeBattle battle = CreateBoardBattle(sink, 4, provider);

            Assert.That(battle.Bridge.TryUseActive(0), Is.False);

            AssertFailureStateUnchanged(battle, sink, original);
            Assert.That(sink.LastResult.Failure,
                Is.EqualTo(
                    BoardElementConversionFailure.NoPlayableCombination));
            Assert.That(provider.SuccessCommitCount, Is.Zero);
            Assert.That(provider.SuccessActionCallCount, Is.Zero);
        }

        [Test]
        public void BoardSuccessActionFailureKeepsCommittedBoardAndRuntime()
        {
            BoardState original = CreateBoard(DeadBoardElements);
            var sink = new ResolverBoardSink(
                original,
                new[] { 0, 1, 2, 4 });
            var provider = new RecordingSuccessActionBoardProvider(
                new List<string>(),
                useForeignPartyTarget: true);
            RuntimeBattle battle = CreateBoardBattle(sink, 4, provider);
            provider.SetActiveStateSource(() => battle.ActiveState);
            int actionSteps = 0;
            battle.Bridge.ActiveCombatActionStepApplied +=
                _ => actionSteps++;

            Assert.That(
                () => battle.Bridge.TryUseActive(0),
                Throws.TypeOf<InvalidOperationException>());

            Assert.That(sink.Board, Is.Not.SameAs(original));
            Assert.That(sink.LastResult.Succeeded, Is.True);
            Assert.That(battle.ActiveState.UsedThisTurn, Is.True);
            Assert.That(battle.ActiveState.RemainingCooldown, Is.EqualTo(3));
            Assert.That(provider.SuccessCommitCount, Is.EqualTo(1));
            Assert.That(provider.SuccessActionCallCount, Is.EqualTo(1));
            Assert.That(battle.ActionIds.LastIssuedId, Is.EqualTo(1));
            Assert.That(battle.Party.Effects.Count, Is.Zero);
            Assert.That(battle.Bridge.LastActiveExecutionResult, Is.Null);
            Assert.That(actionSteps, Is.Zero);
            Assert.That(battle.Bridge.CancelActiveCombat(), Is.True);
        }

        [Test]
        public void UnavailableSinkRejectsBeforePlanOrRuntimeMutation()
        {
            BoardState original = CreateBoard(DeadBoardElements);
            var sink = new ResolverBoardSink(original, new[] { 0, 1, 2, 4 })
            {
                CanAcceptElementConversion = false
            };
            var provider = new RecordingBoardProvider(4);
            RuntimeBattle battle = CreateBoardBattle(sink, 4, provider);

            Assert.That(battle.Bridge.CanUseActive(0), Is.False);
            Assert.That(battle.Bridge.TryUseActive(0), Is.False);
            Assert.That(provider.CallCount, Is.Zero);
            Assert.That(provider.SuccessCommitCount, Is.Zero);
            AssertFailureStateUnchanged(battle, sink, original);
        }

        [Test]
        public void SinkExceptionRollsBackRuntimeAndLeavesNoActiveSession()
        {
            BoardState original = CreateBoard(DeadBoardElements);
            var sink = new ResolverBoardSink(original, new[] { 0, 1, 2, 4 })
            {
                RequestException = new InvalidOperationException("view failed")
            };
            RuntimeBattle battle = CreateBoardBattle(sink, 4);

            Assert.That(
                () => battle.Bridge.TryUseActive(0),
                Throws.TypeOf<InvalidOperationException>());

            AssertFailureStateUnchanged(battle, sink, original);
            Assert.That(((RecordingBoardProvider)battle.BoardProvider)
                .SuccessCommitCount, Is.Zero);
            Assert.That(battle.Bridge.TryCompleteActiveCombat(), Is.False);
        }

        [Test]
        public void SinkFalseWithNullResultRollsBackAndCanRetry()
        {
            BoardState original = CreateBoard(DeadBoardElements);
            var sink = new ResolverBoardSink(original, new[] { 0, 1, 2, 4 })
            {
                ReturnNullFailure = true
            };
            RuntimeBattle battle = CreateBoardBattle(sink, 4);

            Assert.That(battle.Bridge.TryUseActive(0), Is.False);
            AssertFailureStateUnchanged(battle, sink, original);
            Assert.That(((RecordingBoardProvider)battle.BoardProvider)
                .SuccessCommitCount, Is.Zero);

            sink.ReturnNullFailure = false;
            Assert.That(battle.Bridge.TryUseActive(0), Is.True);
            Assert.That(((RecordingBoardProvider)battle.BoardProvider)
                .SuccessCommitCount, Is.EqualTo(1));
        }

        [Test]
        public void UnsafeBoardAfterViewRollbackFailureRejectsBeforeProvider()
        {
            BoardState original = CreateBoard(DeadBoardElements);
            var sink = new ResolverBoardSink(original, new[] { 0, 1, 2, 4 })
            {
                ReturnNullFailure = true
            };
            var provider = new RecordingBoardProvider(4);
            RuntimeBattle battle = CreateBoardBattle(sink, 4, provider);

            Assert.That(battle.Bridge.TryUseActive(0), Is.False);
            sink.CanAcceptElementConversion = false;

            Assert.That(battle.Bridge.CanUseActive(0), Is.False);
            Assert.That(battle.Bridge.TryUseActive(0), Is.False);
            Assert.That(provider.CallCount, Is.EqualTo(1));
            Assert.That(provider.SuccessCommitCount, Is.Zero);
            Assert.That(sink.RequestCount, Is.EqualTo(1));
            AssertFailureStateUnchanged(battle, sink, original);
        }

        [Test]
        public void SuccessfulUseRejectsSecondRequestInSameTurn()
        {
            var sink = new ResolverBoardSink(
                CreateBoard(DeadBoardElements),
                new[] { 0, 1, 2, 4 });
            RuntimeBattle battle = CreateBoardBattle(sink, 4);

            Assert.That(battle.Bridge.TryUseActive(0), Is.True);
            Assert.That(battle.Bridge.TryUseActive(0), Is.False);
            Assert.That(sink.RequestCount, Is.EqualTo(1));
            Assert.That(((RecordingBoardProvider)battle.BoardProvider)
                .SuccessCommitCount, Is.EqualTo(1));
        }

        [Test]
        public void ActiveSessionInProgressRejectsDuplicateBeginRequest()
        {
            var sink = new ResolverBoardSink(
                CreateBoard(DeadBoardElements),
                new[] { 0, 1, 2, 4 });
            RuntimeBattle battle = CreateBoardBattle(sink, 4);

            Assert.That(battle.Bridge.TryBeginActiveCombat(0), Is.True);
            Assert.That(battle.Bridge.TryBeginActiveCombat(0), Is.False);
            Assert.That(sink.RequestCount, Is.EqualTo(1));
            Assert.That(((RecordingBoardProvider)battle.BoardProvider)
                .SuccessCommitCount, Is.EqualTo(1));
            Assert.That(battle.Bridge.TryCompleteActiveCombat(), Is.True);
        }

        [Test]
        public void RegistryRejectsCombatAndBoardProvidersForSameActive()
        {
            var registry = new ActiveAbilityActionProviderRegistry();
            registry.RegisterBoard(
                "hero",
                "active",
                new RecordingBoardProvider(4));

            Assert.That(
                () => registry.Register(
                    "hero",
                    "active",
                    new DamageActiveProvider()),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void CombatActionOnlyProviderStillUsesExistingExecutionPath()
        {
            RuntimeBattle battle = CreateCombatBattle();
            long hpBefore = battle.Boss.CurrentHp;

            Assert.That(battle.Bridge.TryUseActive(0), Is.True);

            Assert.That(battle.Boss.CurrentHp, Is.LessThan(hpBefore));
            Assert.That(battle.ActionIds.LastIssuedId, Is.EqualTo(1));
            Assert.That(battle.ActiveState.UsedThisTurn, Is.True);
            Assert.That(battle.ActiveState.RemainingCooldown, Is.EqualTo(3));
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
        }

        [Test]
        public void NullBoardPlanFailsBeforeBoardOrRuntimeMutation()
        {
            BoardState original = CreateBoard(DeadBoardElements);
            var sink = new ResolverBoardSink(original, new[] { 0, 1, 2, 4 });
            RuntimeBattle battle = CreateBoardBattle(
                sink,
                4,
                new NullBoardProvider());

            Assert.That(
                () => battle.Bridge.TryUseActive(0),
                Throws.TypeOf<InvalidOperationException>());
            AssertFailureStateUnchanged(battle, sink, original);
        }

        [Test]
        public void SuccessCommitExceptionCannotRetroactivelyFailBoardActive()
        {
            var sink = new ResolverBoardSink(
                CreateBoard(DeadBoardElements),
                new[] { 0, 1, 2, 4 });
            RuntimeBattle battle = CreateBoardBattle(
                sink,
                4,
                new ThrowingSuccessCommitProvider());
            LogAssert.Expect(
                LogType.Exception,
                new Regex("commit failed"));

            Assert.That(battle.Bridge.TryUseActive(0), Is.True);

            Assert.That(sink.LastResult.Succeeded, Is.True);
            Assert.That(battle.ActiveState.UsedThisTurn, Is.True);
            Assert.That(battle.ActiveState.RemainingCooldown, Is.EqualTo(3));
        }

        private static RuntimeBattle CreateBoardBattle(
            ResolverBoardSink sink,
            int requestedCount,
            IActiveAbilityBoardPlanProvider provider = null)
        {
            var registry = new ActiveAbilityActionProviderRegistry();
            IActiveAbilityBoardPlanProvider selectedProvider =
                provider ?? new RecordingBoardProvider(requestedCount);
            registry.RegisterBoard(
                "hero",
                "board_active",
                selectedProvider);
            return CreateBattle(registry, sink, selectedProvider);
        }

        private static RuntimeBattle CreateCombatBattle()
        {
            var registry = new ActiveAbilityActionProviderRegistry();
            registry.Register(
                "hero",
                "board_active",
                new DamageActiveProvider());
            return CreateBattle(registry, boardSink: null, boardProvider: null);
        }

        private static RuntimeBattle CreateBattle(
            ActiveAbilityActionProviderRegistry registry,
            IBattleBoardElementConversionSink boardSink,
            IActiveAbilityBoardPlanProvider boardProvider)
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 3 });
            var character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Water,
                1000,
                100d);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                10000,
                100d);
            var actionIds = new CombatActionIdSequence();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            var bindings = new[]
            {
                new ActiveAbilityBinding(0, 0, "hero", "board_active")
            };
            var bridge = new BattleFlowCombatBridge(
                coordinator,
                party,
                boss,
                executor,
                new DelegateMatchEventActionProvider(
                    _ => Array.Empty<CombatAction>()),
                new DelegateBossCombatActionProvider(
                    _ => Array.Empty<CombatAction>()),
                actionIds,
                bindings,
                registry,
                boardMutationSink: null,
                difficultyId: null,
                resultBalance: null,
                boardElementConversionSink: boardSink);
            coordinator.StartBattle();
            return new RuntimeBattle(
                coordinator,
                bridge,
                actionIds,
                party,
                boss,
                boardProvider);
        }

        private static void AssertFailureStateUnchanged(
            RuntimeBattle battle,
            ResolverBoardSink sink,
            BoardState original)
        {
            Assert.That(sink.Board, Is.SameAs(original));
            Assert.That(battle.ActiveState.UsedThisTurn, Is.False);
            Assert.That(battle.ActiveState.RemainingCooldown, Is.Zero);
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(battle.Coordinator.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(battle.Coordinator.PendingMatchEventCount, Is.Zero);
            Assert.That(battle.ActionIds.LastIssuedId, Is.Zero);
            Assert.That(battle.Party.CurrentHp, Is.EqualTo(battle.Party.MaxHp));
            Assert.That(battle.Party.Effects.Count, Is.Zero);
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(battle.Boss.MaxHp));
            Assert.That(battle.Bridge.LastActiveExecutionResult, Is.Null);
        }

        private static BoardState CreateBoard(int[] elements)
        {
            var board = new BoardState();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                board.Set(
                    position,
                    new BoardBlock(
                        index + 1,
                        BoardBlockType.Normal,
                        (ElementType)elements[index]));
            }

            return board;
        }

        private static BoardState CreateUniformBoard(ElementType element)
        {
            var elements = new int[BoardConstants.CellCount];
            for (int index = 0; index < elements.Length; index++)
            {
                elements[index] = (int)element;
            }

            return CreateBoard(elements);
        }

        private static void SetElement(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            var position = new BoardPosition(x, y);
            BoardBlock before = board.Get(position);
            board.Set(
                position,
                new BoardBlock(
                    before.RuntimeId,
                    before.BlockType,
                    element));
        }

        private sealed class RecordingBoardProvider
            : IActiveAbilityBoardPlanProvider,
              IActiveAbilityBoardSuccessCommitter
        {
            private readonly int requestedCount;

            public RecordingBoardProvider(int requestedCount)
            {
                this.requestedCount = requestedCount;
            }

            public int CallCount { get; private set; }
            public int SuccessCommitCount { get; private set; }

            public ActiveAbilityBoardElementConversionPlan CreateBoardPlan(
                ActiveAbilityBoardPlanContext context)
            {
                CallCount++;
                Assert.That(context.Binding.ActiveAbilityId,
                    Is.EqualTo("board_active"));
                Assert.That(context.Character, Is.Not.Null);
                Assert.That(context.Party, Is.Not.Null);
                Assert.That(context.Boss, Is.Not.Null);
                return new ActiveAbilityBoardElementConversionPlan(
                    ElementType.Water,
                    requestedCount);
            }

            public void CommitBoardSuccess(
                ActiveAbilityBoardSuccessContext context)
            {
                Assert.That(context, Is.Not.Null);
                Assert.That(context.Result.Succeeded, Is.True);
                Assert.That(context.Plan.RequestedCount,
                    Is.EqualTo(requestedCount));
                SuccessCommitCount++;
            }
        }

        private sealed class NullBoardProvider
            : IActiveAbilityBoardPlanProvider
        {
            public ActiveAbilityBoardElementConversionPlan CreateBoardPlan(
                ActiveAbilityBoardPlanContext context)
            {
                return null;
            }
        }

        private sealed class ThrowingSuccessCommitProvider
            : IActiveAbilityBoardPlanProvider,
              IActiveAbilityBoardSuccessCommitter
        {
            public ActiveAbilityBoardElementConversionPlan CreateBoardPlan(
                ActiveAbilityBoardPlanContext context)
            {
                return new ActiveAbilityBoardElementConversionPlan(
                    ElementType.Water,
                    4);
            }

            public void CommitBoardSuccess(
                ActiveAbilityBoardSuccessContext context)
            {
                throw new InvalidOperationException("commit failed");
            }
        }

        private sealed class RecordingSuccessActionBoardProvider
            : IActiveAbilityBoardPlanProvider,
              IActiveAbilityBoardSuccessCommitter,
              IActiveAbilityBoardSuccessActionProvider
        {
            private readonly List<string> order;
            private readonly bool useForeignPartyTarget;
            private Func<ActiveAbilityRuntimeState> activeStateSource;

            public RecordingSuccessActionBoardProvider(
                List<string> order,
                bool useForeignPartyTarget)
            {
                this.order = order
                    ?? throw new ArgumentNullException(nameof(order));
                this.useForeignPartyTarget = useForeignPartyTarget;
            }

            public int SuccessCommitCount { get; private set; }
            public int SuccessActionCallCount { get; private set; }
            public bool CommitObservedCommittedCooldown { get; private set; }
            public int ObservedTurn { get; private set; }

            public void SetActiveStateSource(
                Func<ActiveAbilityRuntimeState> source)
            {
                activeStateSource = source
                    ?? throw new ArgumentNullException(nameof(source));
            }

            public ActiveAbilityBoardElementConversionPlan CreateBoardPlan(
                ActiveAbilityBoardPlanContext context)
            {
                order.Add("plan");
                return new ActiveAbilityBoardElementConversionPlan(
                    ElementType.Water,
                    requestedCount: 4);
            }

            public void CommitBoardSuccess(
                ActiveAbilityBoardSuccessContext context)
            {
                order.Add("commit");
                SuccessCommitCount++;
                if (activeStateSource != null)
                {
                    ActiveAbilityRuntimeState state = activeStateSource();
                    CommitObservedCommittedCooldown = state.UsedThisTurn
                        && state.RemainingCooldown == 3;
                }
            }

            public IReadOnlyList<CombatAction> CreateBoardSuccessActions(
                ActiveAbilityBoardSuccessActionContext context)
            {
                order.Add("provide_actions");
                SuccessActionCallCount++;
                ObservedTurn = context.CurrentTurn;
                PartyBattleState target = context.Party;
                if (useForeignPartyTarget)
                {
                    target = new PartyBattleState(new[]
                    {
                        new CharacterBattleState(
                            "foreign",
                            1,
                            ElementType.Fire,
                            100,
                            10d)
                    });
                }

                return new CombatAction[]
                {
                    new ApplyEffectAction(
                        context.ActionIds.Next(),
                        ActionOrigin.Active,
                        target,
                        new EffectInstance(
                            runtimeId: 100,
                            effectId: "board_success_effect",
                            sourceId: context.Character.CharacterId,
                            EffectCategory.Buff,
                            EffectModifierType.AttackIncrease,
                            magnitude: 0.10d,
                            remainingTurns: 1,
                            creationOrder: 100))
                };
            }
        }

        private sealed class DamageActiveProvider
            : IActiveAbilityActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                ActiveAbilityActionContext context)
            {
                return new CombatAction[]
                {
                    new DamageAction(
                        context.ActionIds.Next(),
                        ActionOrigin.Active,
                        new DamageContextBuildRequest(
                            context.Character,
                            context.Party,
                            context.Boss,
                            context.Character.Element,
                            AttackType.Active,
                            AttackTag.None,
                            1d,
                            false,
                            0,
                            false))
                };
            }
        }

        private sealed class ResolverBoardSink
            : IBattleBoardElementConversionSink
        {
            private int[] randomValues;
            private readonly int maxRandomSelectionAttempts;

            public ResolverBoardSink(
                BoardState board,
                int[] randomValues,
                int maxRandomSelectionAttempts = 100)
            {
                Board = board;
                this.randomValues = randomValues;
                this.maxRandomSelectionAttempts = maxRandomSelectionAttempts;
            }

            public bool CanAcceptElementConversion { get; set; } = true;
            public Exception RequestException { get; set; }
            public bool ReturnNullFailure { get; set; }
            public int RequestCount { get; private set; }
            public ElementType LastTargetElement { get; private set; }
            public int LastRequestedCount { get; private set; }
            public BoardState Board { get; private set; }
            public BoardElementConversionResult LastResult { get; private set; }

            public bool TryConvertElements(
                ElementType targetElement,
                int requestedCount,
                out BoardElementConversionResult result)
            {
                RequestCount++;
                LastTargetElement = targetElement;
                LastRequestedCount = requestedCount;
                if (RequestException != null)
                {
                    throw RequestException;
                }

                if (ReturnNullFailure)
                {
                    result = null;
                    return false;
                }

                result = new BoardElementConversionResolver(
                    new SequenceRandomSource(randomValues),
                    new BoardMoveAnalyzer(),
                    maxRandomSelectionAttempts).Resolve(
                        Board,
                        targetElement,
                        requestedCount);
                LastResult = result;
                if (!result.Succeeded)
                {
                    return false;
                }

                Board = result.Board;
                return true;
            }

            public void SetBoard(BoardState board, params int[] values)
            {
                Board = board;
                randomValues = values;
                LastResult = null;
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
                if (values.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No recorded random value remains.");
                }

                int value = values.Dequeue();
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        "Recorded random value is outside the requested range.");
                }

                return value;
            }

            public float NextFloat()
            {
                throw new NotSupportedException();
            }
        }

        private sealed class RuntimeBattle
        {
            public RuntimeBattle(
                BattleFlowCoordinator coordinator,
                BattleFlowCombatBridge bridge,
                CombatActionIdSequence actionIds,
                PartyBattleState party,
                BossBattleState boss,
                IActiveAbilityBoardPlanProvider boardProvider)
            {
                Coordinator = coordinator;
                Bridge = bridge;
                ActionIds = actionIds;
                Party = party;
                Boss = boss;
                BoardProvider = boardProvider;
            }

            public BattleFlowCoordinator Coordinator { get; }
            public BattleFlowCombatBridge Bridge { get; }
            public CombatActionIdSequence ActionIds { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public IActiveAbilityBoardPlanProvider BoardProvider { get; }
            public ActiveAbilityRuntimeState ActiveState =>
                Coordinator.Context.ActiveAbilities[0];
        }
    }
}
