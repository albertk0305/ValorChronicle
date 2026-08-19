using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleFlowControllerTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private GameObject root;
        private GameObject notUsersTurnPanel;
        private BattleBoardController boardController;
        private BattleFlowController flowController;

        [UnitySetUp]
        public IEnumerator EnterRuntimeMode()
        {
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ExitRuntimeMode()
        {
            yield return new ExitPlayMode();
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("BattleFlowControllerTests");
            root.SetActive(false);
            createdObjects.Add(root);
            boardController = root.AddComponent<BattleBoardController>();
            boardController.enabled = false;
            flowController = root.AddComponent<BattleFlowController>();
            notUsersTurnPanel = new GameObject("NotUsersTurnPanel");
            notUsersTurnPanel.transform.SetParent(root.transform, false);
            SetField(flowController, "boardController", boardController);
            SetField(
                flowController,
                "notUsersTurnPanel",
                notUsersTurnPanel);
            SetField(flowController, "preAttackDelaySeconds", 0f);
            SetField(flowController, "emptyQueueItemDelaySeconds", 0f);
            SetField(flowController, "preBossActionDelaySeconds", 0f);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void SetupCopiesCooldownsAndRejectsInvalidValues()
        {
            var cooldowns = new[] { 3, 8 };
            var setup = new BattleFlowSetup(25, cooldowns);
            cooldowns[0] = 99;

            Assert.That(setup.TurnLimit, Is.EqualTo(25));
            Assert.That(setup.ActiveAbilityCooldowns,
                Is.EqualTo(new[] { 3, 8 }));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BattleFlowSetup(0));
            Assert.Throws<ArgumentNullException>(
                () => new BattleFlowSetup(25, null));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BattleFlowSetup(25, new[] { -1 }));
        }

        [Test]
        public void RequiredCombatConfigurationRejectsBridgeLessInitialization()
        {
            SetField(flowController, "requireCombatBridge", true);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() =>
                    flowController.Initialize(new BattleFlowSetup(25)));

            Assert.That(exception.Message,
                Does.Contain("requires a combat bridge"));
            Assert.That(flowController.Context, Is.Null);
            Assert.That(flowController.CombatBridge, Is.Null);
        }

        [Test]
        public void InitializationWaitsForBoardAndCatchesMissedReadyOnce()
        {
            Assert.That(boardController.HasInitialBoardReady, Is.False);
            InitializeFlow(new BattleFlowSetup(25));
            ActivateFixture();

            Assert.That(GetField<bool>(flowController, "connectionEnabled"),
                Is.True);
            Assert.That(GetField<bool>(flowController, "boardEventsSubscribed"),
                Is.True);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(boardController.CanAcceptBoardInput, Is.False);
            Assert.That(notUsersTurnPanel.activeSelf, Is.False);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.NotStarted));

            PublishInitialBoardReady();

            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
            Assert.That(notUsersTurnPanel.activeSelf, Is.False);

            PublishInitialBoardReady();
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(1));
        }

        [Test]
        public void LateInitializationStartsFromReadyStateAndDoesNotRestart()
        {
            SetField(boardController, "initialBoardReadyPublished", true);
            InitializeFlow(new BattleFlowSetup(25));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.NotStarted));
            ActivateFixture();

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.Throws<InvalidOperationException>(() =>
                flowController.Initialize(new BattleFlowSetup(25)));

            DeactivateFixture();
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            InvokePrivate(flowController, "HandleInitialBoardReady");
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(1));
        }

        [Test]
        public void PlayerInputAllowsActiveAndBoardInputTogether()
        {
            InitializeStarted(new[] { 3 });

            Assert.That(flowController.Context.ActiveAbilities, Has.Count.EqualTo(1));
            Assert.That(flowController.CanUseActive(0), Is.True);
            Assert.That(flowController.TryUseActive(0), Is.True);
            Assert.That(flowController.CanUseActive(0), Is.False);
            Assert.That(flowController.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(3));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(flowController.CanUseActive(0), Is.False);
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
            Assert.That(boardController.CanAcceptBoardInput, Is.False);
        }

        [UnityTest]
        public IEnumerator ActiveDamageWaitsForProjectileAndLocksInput()
        {
            InitializeStarted(new[] { 3, 4 });
            var characters = new[]
            {
                new CharacterBattleState(
                    "active_hero", 0, ElementType.Water, 1000, 100d),
                new CharacterBattleState(
                    "ready_ally", 1, ElementType.Fire, 1000, 100d)
            };
            var party = new PartyBattleState(characters);
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 0d);
            BattleFlowCombatBridge bridge = AttachActiveCombatBridge(
                party,
                boss,
                new DelegateActiveActionProvider(context =>
                    new CombatAction[]
                    {
                        CreateActiveDamage(context, ElementType.Dark)
                    }),
                new DelegateActiveActionProvider(context =>
                    new CombatAction[]
                    {
                        new AddResourceAction(
                            context.ActionIds.Next(),
                            ActionOrigin.Active,
                            context.Boss,
                            "ally_resource",
                            1)
                    }));
            var presenter = new ControllableActiveProjectilePresenter();
            SetField(
                flowController,
                "activeProjectilePresenterOverride",
                presenter);
            int stepCount = 0;
            int batchCount = 0;
            bridge.ActiveCombatActionStepApplied += step => stepCount++;
            bridge.CombatActionsApplied += () => batchCount++;

            Assert.That(flowController.TryUseActive(0), Is.True);

            Assert.That(presenter.Requests, Has.Count.EqualTo(1));
            Assert.That(presenter.Requests[0].PartySlotIndex, Is.Zero);
            Assert.That(presenter.Requests[0].CharacterId,
                Is.EqualTo("active_hero"));
            Assert.That(presenter.Requests[0].AttackElement,
                Is.EqualTo(ElementType.Dark));
            Assert.That(boss.CurrentHp, Is.EqualTo(1000));
            Assert.That(stepCount, Is.Zero);
            Assert.That(batchCount, Is.Zero);
            Assert.That(flowController.IsResolvingActivePresentation,
                Is.True);
            Assert.That(flowController.CanUseActive(1), Is.False);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(notUsersTurnPanel.activeSelf, Is.True);

            presenter.Arrive();
            yield return null;

            Assert.That(boss.CurrentHp, Is.LessThan(1000));
            Assert.That(stepCount, Is.EqualTo(1));
            Assert.That(batchCount, Is.EqualTo(1));
            Assert.That(flowController.IsResolvingActivePresentation,
                Is.False);
            Assert.That(flowController.CanUseActive(1), Is.True);
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
            Assert.That(notUsersTurnPanel.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ActiveMultiHitUsesOneProjectilePerDamageAction()
        {
            InitializeStarted(new[] { 3 });
            var character = new CharacterBattleState(
                "active_hero", 2, ElementType.Fire, 1000, 100d);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 0d);
            BattleFlowCombatBridge bridge = AttachActiveCombatBridge(
                party,
                boss,
                new DelegateActiveActionProvider(context =>
                    new CombatAction[]
                    {
                        CreateActiveDamage(context, ElementType.Fire),
                        CreateActiveDamage(context, ElementType.Water)
                    }));
            var presenter = new ControllableActiveProjectilePresenter();
            SetField(
                flowController,
                "activeProjectilePresenterOverride",
                presenter);
            var applied = new List<ActiveCombatActionExecutionStepResult>();
            bridge.ActiveCombatActionStepApplied += applied.Add;

            Assert.That(flowController.TryUseActive(0), Is.True);
            Assert.That(presenter.Requests, Has.Count.EqualTo(1));
            Assert.That(boss.CurrentHp, Is.EqualTo(1000));

            presenter.Arrive();
            yield return null;
            long hpAfterFirst = boss.CurrentHp;
            Assert.That(hpAfterFirst, Is.LessThan(1000));
            Assert.That(presenter.Requests, Has.Count.EqualTo(2));
            Assert.That(presenter.Requests[1].AttackElement,
                Is.EqualTo(ElementType.Water));

            presenter.Arrive();
            yield return null;
            Assert.That(boss.CurrentHp, Is.LessThan(hpAfterFirst));
            Assert.That(applied, Has.Count.EqualTo(2));
            Assert.That(bridge.LastActiveExecutionResult.ActionResults,
                Has.Count.EqualTo(2));
        }

        [Test]
        public void ActiveHealAndNonDamageActionsApplyWithoutProjectile()
        {
            InitializeStarted(new[] { 3 });
            var character = new CharacterBattleState(
                "active_healer", 0, ElementType.Light, 1000, 100d);
            var party = new PartyBattleState(new[] { character });
            SetField(party, "<CurrentHp>k__BackingField", 500L);
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 0d);
            boss.Resources.Register("active_resource", 10);
            BattleFlowCombatBridge bridge = AttachActiveCombatBridge(
                party,
                boss,
                new DelegateActiveActionProvider(context =>
                    new CombatAction[]
                    {
                        new HealAction(
                            context.ActionIds.Next(),
                            ActionOrigin.Active,
                            new HealingContextBuildRequest(
                                context.Character,
                                context.Party,
                                1d,
                                false,
                                0)),
                        new AddResourceAction(
                            context.ActionIds.Next(),
                            ActionOrigin.Active,
                            context.Boss,
                            "active_resource",
                            1)
                    }));
            var presenter = new ControllableActiveProjectilePresenter();
            SetField(
                flowController,
                "activeProjectilePresenterOverride",
                presenter);
            var steps = new List<ActiveCombatActionExecutionStepResult>();
            bridge.ActiveCombatActionStepApplied += steps.Add;

            Assert.That(flowController.TryUseActive(0), Is.True);

            Assert.That(presenter.Requests, Is.Empty);
            Assert.That(party.CurrentHp, Is.GreaterThan(500));
            Assert.That(steps, Has.Count.EqualTo(2));
            Assert.That(steps[0].ActionStep.Result,
                Is.TypeOf<HealActionResult>());
            Assert.That(boss.Resources.GetAmount("active_resource"),
                Is.EqualTo(1));
            Assert.That(flowController.IsResolvingActivePresentation,
                Is.False);
        }

        [UnityTest]
        public IEnumerator AbortDuringActiveProjectilePreventsStaleDamage()
        {
            InitializeStarted(new[] { 3 });
            var character = new CharacterBattleState(
                "active_hero", 0, ElementType.Fire, 1000, 100d);
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 0d);
            AttachActiveCombatBridge(
                new PartyBattleState(new[] { character }),
                boss,
                new DelegateActiveActionProvider(context =>
                    new CombatAction[]
                    {
                        CreateActiveDamage(context, ElementType.Fire)
                    }));
            var presenter = new ControllableActiveProjectilePresenter();
            SetField(
                flowController,
                "activeProjectilePresenterOverride",
                presenter);

            Assert.That(flowController.TryUseActive(0), Is.True);
            Assert.That(flowController.AbortBattle(), Is.True);
            Assert.That(presenter.CancelCount, Is.EqualTo(1));

            presenter.Arrive();
            yield return null;

            Assert.That(boss.CurrentHp, Is.EqualTo(1000));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(flowController.IsResolvingActivePresentation,
                Is.False);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
        }

        [Test]
        public void MissingActivePresenterAppliesImmediatelyWithoutDeadlock()
        {
            InitializeStarted(new[] { 3 });
            var character = new CharacterBattleState(
                "active_hero", 0, ElementType.Fire, 1000, 100d);
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 0d);
            AttachActiveCombatBridge(
                new PartyBattleState(new[] { character }),
                boss,
                new DelegateActiveActionProvider(context =>
                    new CombatAction[]
                    {
                        CreateActiveDamage(context, ElementType.Fire)
                    }));
            LogAssert.Expect(
                LogType.Warning,
                new Regex("Active damage projectile presenter is unavailable"));

            Assert.That(flowController.TryUseActive(0), Is.True);

            Assert.That(boss.CurrentHp, Is.LessThan(1000));
            Assert.That(flowController.IsResolvingActivePresentation,
                Is.False);
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
            Assert.That(notUsersTurnPanel.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator LethalActiveStopsRemainingActionsAndDelaysVictoryUi()
        {
            InitializeStarted(new[] { 3 });
            var character = new CharacterBattleState(
                "active_hero", 0, ElementType.Fire, 1000, 100d);
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 100, 0d);
            BattleFlowCombatBridge bridge = AttachActiveCombatBridge(
                new PartyBattleState(new[] { character }),
                boss,
                new DelegateActiveActionProvider(context =>
                    new CombatAction[]
                    {
                        CreateActiveDamage(context, ElementType.Fire),
                        CreateActiveDamage(context, ElementType.Water)
                    }));
            var presenter = new ControllableActiveProjectilePresenter();
            SetField(
                flowController,
                "activeProjectilePresenterOverride",
                presenter);
            BattleHudController hud = CreateResultHud(0.04f);

            Assert.That(flowController.TryUseActive(0), Is.True);
            presenter.Arrive();
            yield return null;

            Assert.That(boss.CurrentHp, Is.Zero);
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(presenter.Requests, Has.Count.EqualTo(1));
            Assert.That(bridge.LastActiveExecutionResult.StoppedEarly,
                Is.True);
            Assert.That(hud.ResultOverlay.activeSelf, Is.False);
            Assert.That(flowController.IsResolvingActivePresentation,
                Is.False);
            AssertResultInputLocked();

            yield return WaitUntil(
                () => hud.ResultOverlay.activeSelf,
                "Active victory overlay did not appear after its delay.",
                maximumFrames: 120);
        }

        [Test]
        public void EmptyActiveListStillEntersPlayerInput()
        {
            InitializeStarted(Array.Empty<int>());

            Assert.That(flowController.Context.ActiveAbilities, Is.Empty);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
        }

        [Test]
        public void NoMatchBoardActionStaysInPlayerInputAndValidatesActionId()
        {
            InitializeStarted(Array.Empty<int>());
            BoardSwapActionResult result = CreateNoMatchResult();

            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(flowController.MatchQueuePresentation.Count, Is.Zero);
            Assert.That(flowController.MatchQueuePresentation.ActiveActionId,
                Is.Zero);

            LogAssert.Expect(
                LogType.Warning,
                new Regex("Ignored duplicate or stale board action"));
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(2, result));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));

            LogAssert.Expect(
                LogType.Warning,
                new Regex("Ignored unexpected board completion"));
            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    2,
                    result,
                    BoardActionCompletionStatus.Completed));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));

            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    result,
                    BoardActionCompletionStatus.Completed));
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
        }

        [Test]
        public void NoMatchKeepsPlayerInputAndPreservesAppliedActiveState()
        {
            InitializeStarted(new[] { 4, 5 });
            flowController.TryUseActive(0);
            ActiveAbilityRuntimeState active =
                flowController.Context.ActiveAbilities[0];
            BoardSwapActionResult result = CreateNoMatchResult();

            SendCompletedAction(1, result);

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(active.RemainingCooldown, Is.EqualTo(4));
            Assert.That(active.UsedThisTurn, Is.True);
            Assert.That(flowController.CanUseActive(1), Is.True);
            Assert.That(flowController.TryUseActive(1), Is.True);
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.Zero);
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
        }

        [Test]
        public void NoMatchPresentationTemporarilyLocksActiveInput()
        {
            InitializeStarted(new[] { 4 });
            ActiveAbilityRuntimeState active =
                flowController.Context.ActiveAbilities[0];
            BoardSwapActionResult result = CreateNoMatchResult();
            Assert.That(flowController.CanUseActive(0), Is.True);

            SetField(boardController, "isActionInProgress", true);
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(flowController.CanUseActive(0), Is.False);
            Assert.That(flowController.TryUseActive(0), Is.False);
            Assert.That(active.RemainingCooldown, Is.Zero);
            Assert.That(active.UsedThisTurn, Is.False);
            Assert.That(boardController.CanAcceptBoardInput, Is.False);
            Assert.That(notUsersTurnPanel.activeSelf, Is.True);

            SetField(boardController, "isActionInProgress", false);
            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    result,
                    BoardActionCompletionStatus.Completed));

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(flowController.CanUseActive(0), Is.True);
            Assert.That(flowController.TryUseActive(0), Is.True);
            Assert.That(active.RemainingCooldown, Is.EqualTo(4));
            Assert.That(active.UsedThisTurn, Is.True);
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
            Assert.That(notUsersTurnPanel.activeSelf, Is.False);
        }

        [Test]
        public void ResolvedBoardActionLeavesPlayerInputWhenExecutionStarts()
        {
            InitializeStarted(new[] { 3 });
            Assert.That(flowController.TryUseActive(0), Is.True);
            BoardSwapActionResult result =
                CreateResolvedResult(CreateSingleMatchCascade());

            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BoardResolving));
            Assert.That(flowController.CanUseActive(0), Is.False);
            Assert.That(flowController.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(3));
            Assert.That(flowController.Context.ActiveAbilities[0]
                .UsedThisTurn, Is.True);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(notUsersTurnPanel.activeSelf, Is.True);
        }

        [Test]
        public void InitialPanelSuppressionDoesNotHideLaterCombatPhases()
        {
            InitializeStarted(Array.Empty<int>());

            Assert.That(notUsersTurnPanel.activeSelf, Is.False);
            Assert.That(flowController.Coordinator.TryBeginBoardResolution(),
                Is.True);
            Assert.That(notUsersTurnPanel.activeSelf, Is.True);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);

            Assert.That(
                flowController.Coordinator.NotifyBoardActionResolved(
                    CreateSingleMatchCascade(),
                    consumesTurn: true),
                Is.True);
            Assert.That(flowController.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);
            Assert.That(
                flowController.Coordinator.CompleteCurrentMatchEvent(
                    execution.ExecutionId),
                Is.True);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(notUsersTurnPanel.activeSelf, Is.True);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);

            Assert.That(flowController.Coordinator.CompleteBossAction(),
                Is.True);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(notUsersTurnPanel.activeSelf, Is.False);
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
        }

        [Test]
        public void CascadePresentationRevealsCanonicalEntriesBeforeResolve()
        {
            InitializeStarted(Array.Empty<int>());
            BoardCascadeResult cascade = BattleFlowTestSupport.CreateCascade(
                new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Water,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0)),
                    BattleFlowTestSupport.Match(
                        ElementType.Fire,
                        new BoardPosition(0, 1),
                        new BoardPosition(1, 1),
                        new BoardPosition(2, 1),
                        new BoardPosition(3, 1))
                });
            BoardSwapActionResult result = CreateResolvedResult(cascade);

            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BoardResolving));
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.Zero);
            RevealStep(1, cascade, 0);

            IReadOnlyList<BattleMatchQueuePresentationEntry> entries =
                flowController.MatchQueuePresentation.GetSnapshot();
            Assert.That(entries, Has.Count.EqualTo(2));
            AssertPresentationEntry(entries[0], 0, 0, ElementType.Water, 3);
            AssertPresentationEntry(entries[1], 1, 0, ElementType.Fire, 4);
            var mutable = entries as IList<
                BattleMatchQueuePresentationEntry>;
            Assert.That(mutable, Is.Not.Null);
            Assert.Throws<NotSupportedException>(
                () => mutable.Add(entries[0]));
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.EqualTo(2));
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.Zero);
        }

        [Test]
        public void MultiStepRevealAppendsAndRejectsStaleOrDuplicateSignals()
        {
            InitializeStarted(Array.Empty<int>());
            BoardCascadeResult cascade = BattleFlowTestSupport.CreateCascade(
                new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Water,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0)),
                    BattleFlowTestSupport.Match(
                        ElementType.Water,
                        new BoardPosition(0, 1),
                        new BoardPosition(1, 1),
                        new BoardPosition(2, 1),
                        new BoardPosition(3, 1))
                },
                new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Dark,
                        new BoardPosition(0, 2),
                        new BoardPosition(1, 2),
                        new BoardPosition(2, 2))
                });
            BoardSwapActionResult result = CreateResolvedResult(cascade);
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));

            RevealStep(1, cascade, 0);
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.EqualTo(2));
            LogAssert.Expect(
                LogType.Warning,
                new Regex("Ignored stale, duplicate, or out-of-order"));
            RevealStep(1, cascade, 0);
            LogAssert.Expect(
                LogType.Warning,
                new Regex("Ignored stale, duplicate, or out-of-order"));
            RevealStep(99, cascade, 1);
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.EqualTo(2));

            RevealStep(1, cascade, 1);
            IReadOnlyList<BattleMatchQueuePresentationEntry> entries =
                flowController.MatchQueuePresentation.GetSnapshot();
            Assert.That(entries, Has.Count.EqualTo(3));
            AssertPresentationEntry(entries[0], 0, 0, ElementType.Water, 3);
            AssertPresentationEntry(entries[1], 1, 0, ElementType.Water, 4);
            AssertPresentationEntry(entries[2], 2, 1, ElementType.Dark, 3);
        }

        [Test]
        public void MatchExecutionPopsBySequenceEvenForRepeatedElement()
        {
            InitializeStarted(Array.Empty<int>());
            BoardCascadeResult cascade = BattleFlowTestSupport.CreateCascade(
                new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Water,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0)),
                    BattleFlowTestSupport.Match(
                        ElementType.Water,
                        new BoardPosition(0, 1),
                        new BoardPosition(1, 1),
                        new BoardPosition(2, 1),
                        new BoardPosition(3, 1))
                });
            BoardSwapActionResult result = CreateResolvedResult(cascade);
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));
            RevealStep(1, cascade, 0);
            Assert.That(flowController.Coordinator.NotifyBoardActionResolved(
                cascade,
                true), Is.True);

            Assert.That(flowController.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);

            Assert.That(execution.MatchEvent.SequenceIndex, Is.Zero);
            IReadOnlyList<BattleMatchQueuePresentationEntry> remaining =
                flowController.MatchQueuePresentation.GetSnapshot();
            Assert.That(remaining, Has.Count.EqualTo(1));
            AssertPresentationEntry(
                remaining[0],
                1,
                0,
                ElementType.Water,
                4);
        }

        [Test]
        public void MissingOrInconsistentRevealRebuildsFromAuthoritativeQueue()
        {
            InitializeStarted(Array.Empty<int>());
            BoardCascadeResult canonical = CreateTwoMatchCascade();
            BoardSwapActionResult result = CreateResolvedResult(canonical);
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));
            LogAssert.Expect(
                LogType.Warning,
                new Regex("Rebuilt presentation MatchEvent queue"));

            Assert.That(flowController.Coordinator.NotifyBoardActionResolved(
                canonical,
                true), Is.True);

            AssertPresentationMatchesAuthoritative();

            flowController.Coordinator.AbortBattle();
            InitializeReplacementControllerForReconcile();
            BoardCascadeResult wrong = BattleFlowTestSupport.CreateCascade(
                new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Dark,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0),
                        new BoardPosition(3, 0))
                });
            result = CreateResolvedResult(canonical);
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));
            RevealStep(1, wrong, 0);
            LogAssert.Expect(
                LogType.Warning,
                new Regex("Rebuilt presentation MatchEvent queue"));

            Assert.That(flowController.Coordinator.NotifyBoardActionResolved(
                canonical,
                true), Is.True);

            AssertPresentationMatchesAuthoritative();
        }

        [Test]
        public void MatchExecutionMismatchRecoversWithoutBlockingGameplay()
        {
            InitializeStarted(Array.Empty<int>());
            BoardCascadeResult cascade = CreateTwoMatchCascade();
            BoardSwapActionResult result = CreateResolvedResult(cascade);
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));
            RevealStep(1, cascade, 0);
            Assert.That(flowController.Coordinator.NotifyBoardActionResolved(
                cascade,
                true), Is.True);
            InvokePrivate(
                flowController.MatchQueuePresentation,
                "BeginAction",
                99L);
            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "Presentation MatchEvent did not match the "
                        + "authoritative execution"));
            LogAssert.Expect(
                LogType.Warning,
                new Regex("Rebuilt presentation MatchEvent queue"));

            Assert.That(flowController.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);

            Assert.That(execution.MatchEvent.SequenceIndex, Is.Zero);
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.EqualTo(1));
            AssertPresentationMatchesAuthoritative();
        }

        [Test]
        public void EnableDoesNotDuplicateCascadePresentationSubscription()
        {
            InitializeStarted(Array.Empty<int>());
            Assert.That(CountTargetSubscribers(
                boardController,
                "CascadeStepPresented",
                flowController), Is.EqualTo(1));

            InvokePrivate(flowController, "OnEnable");

            Assert.That(CountTargetSubscribers(
                boardController,
                "CascadeStepPresented",
                flowController), Is.EqualTo(1));
        }

        [Test]
        public void TerminalAndDisableClearPresentationAndSubscriptions()
        {
            InitializeStarted(Array.Empty<int>());
            BoardCascadeResult cascade = CreateSingleMatchCascade();
            BoardSwapActionResult result = CreateResolvedResult(cascade);
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));
            RevealStep(1, cascade, 0);
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.EqualTo(1));
            Assert.That(CountTargetSubscribers(
                boardController,
                "CascadeStepPresented",
                flowController), Is.EqualTo(1));

            Assert.That(flowController.AbortBattle(), Is.True);

            Assert.That(flowController.MatchQueuePresentation.Count, Is.Zero);
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            DeactivateFixture();
            Assert.That(CountTargetSubscribers(
                boardController,
                "CascadeStepPresented",
                flowController), Is.Zero);
        }

        [UnityTest]
        public IEnumerator ResolvedActionExecutesMatchesInOrderThenBossAndNextTurn()
        {
            InitializeStarted(Array.Empty<int>());
            BoardCascadeResult cascade = CreateTwoMatchCascade();
            BoardSwapActionResult result = CreateResolvedResult(cascade);
            var executed = new List<MatchEvent>();
            int bossActionCount = 0;
            flowController.Coordinator.MatchEventExecuting += executed.Add;
            flowController.Coordinator.BossActionStarted +=
                () => bossActionCount++;

            SendCompletedAction(1, result);

            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(executed, Has.Count.EqualTo(1));
            Assert.That(flowController.Coordinator.HasMatchEventInFlight,
                Is.True);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));
            Assert.That(bossActionCount, Is.Zero);

            yield return WaitUntil(
                () => executed.Count == 2
                    && flowController.Context.Phase
                        == BattlePhase.BossActing,
                "The second MatchEvent and BossActing did not begin.");
            Assert.That(executed, Has.Count.EqualTo(2));
            Assert.That(executed[0].SequenceIndex, Is.Zero);
            Assert.That(executed[1].SequenceIndex, Is.EqualTo(1));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(bossActionCount, Is.EqualTo(1));

            yield return WaitUntil(
                () => flowController.Context.CurrentTurn == 2
                    && flowController.Context.Phase
                        == BattlePhase.PlayerInput,
                "The placeholder Boss action did not complete the turn.");

            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(2));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator PreAttackDelayKeepsQueueBeforeFirstDequeue()
        {
            InitializeStarted(Array.Empty<int>());
            SetField(flowController, "preAttackDelaySeconds", 0.05f);
            var executed = new List<MatchEvent>();
            flowController.Coordinator.MatchEventExecuting += executed.Add;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateTwoMatchCascade()));

            Assert.That(executed, Is.Empty);
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.EqualTo(2));
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.EqualTo(2));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));

            yield return WaitUntil(
                () => executed.Count > 0,
                "Pre-attack delay did not release the first MatchEvent.",
                maximumFrames: 120);

            Assert.That(executed[0].SequenceIndex, Is.Zero);
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.LessThan(2));
        }

        [UnityTest]
        public IEnumerator PreAttackDelayIsNotRepeatedBetweenMatchEvents()
        {
            InitializeStarted(Array.Empty<int>());
            var executed = new List<MatchEvent>();
            flowController.Coordinator.MatchEventExecuting += matchEvent =>
            {
                executed.Add(matchEvent);
                if (executed.Count == 1)
                {
                    SetField(
                        flowController,
                        "preAttackDelaySeconds",
                        10f);
                }
            };

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateTwoMatchCascade()));

            Assert.That(executed, Has.Count.EqualTo(1));
            yield return WaitUntil(
                () => executed.Count == 2,
                "Pre-attack delay was repeated between MatchEvents.",
                maximumFrames: 8);
            Assert.That(executed[1].SequenceIndex, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ConsecutiveEmptyItemsPopAcrossSeparateDelays()
        {
            InitializeStarted(Array.Empty<int>());
            SetField(flowController, "emptyQueueItemDelaySeconds", 0.04f);
            var character = new CharacterBattleState(
                "fire",
                0,
                ElementType.Fire,
                1000,
                100d);
            AttachCombatBridge(
                new PartyBattleState(new[] { character }),
                new BossBattleState(
                    "boss",
                    ElementType.Fire,
                    1000,
                    0d),
                new ControllerMatchActionProvider());
            var executed = new List<MatchEvent>();
            flowController.Coordinator.MatchEventExecuting += executed.Add;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateTwoWaterMatchCascade()));

            Assert.That(executed, Has.Count.EqualTo(1));
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.EqualTo(1));
            Assert.That(flowController.Coordinator.HasMatchEventInFlight,
                Is.True);

            yield return WaitUntil(
                () => executed.Count == 2,
                "The second empty queue item was not released.",
                maximumFrames: 120);

            Assert.That(executed[1].SequenceIndex, Is.EqualTo(1));
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.Zero);
            Assert.That(flowController.Coordinator.HasMatchEventInFlight,
                Is.True);
        }

        [UnityTest]
        public IEnumerator PreBossDelayStartsOnlyAfterLastMatchCombat()
        {
            InitializeStarted(new[] { 3 });
            SetField(flowController, "preBossActionDelaySeconds", 0.04f);
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "fire", 0, ElementType.Fire, 1000, 100d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 0d);
            var bossProvider = new ControllerBossDamageActionProvider(1);
            AttachCombatBridge(
                party,
                boss,
                new ControllerMatchActionProvider(),
                bossProvider);
            int bossStartedCount = 0;
            int matchExecutionCount = 0;
            flowController.Coordinator.MatchEventExecuting +=
                matchEvent => matchExecutionCount++;
            flowController.Coordinator.BossActionStarted +=
                () => bossStartedCount++;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateTwoMatchCascade()));

            Assert.That(matchExecutionCount, Is.EqualTo(2));
            Assert.That(boss.CurrentHp, Is.EqualTo(900));
            Assert.That(party.CurrentHp, Is.EqualTo(1000));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));
            Assert.That(bossStartedCount, Is.Zero);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(boardController.CanAcceptBoardInput, Is.False);
            Assert.That(flowController.CanUseActive(0), Is.False);
            Assert.That(notUsersTurnPanel.activeSelf, Is.True);

            yield return WaitUntil(
                () => bossStartedCount == 1 && party.CurrentHp < 1000,
                "The delayed Boss action did not progress.",
                maximumFrames: 120);

            Assert.That(bossStartedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EmptyMatchItemsStillUseOnePreBossDelay()
        {
            InitializeStarted(Array.Empty<int>());
            SetField(flowController, "preBossActionDelaySeconds", 0.04f);
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "fire", 0, ElementType.Fire, 1000, 100d)
            });
            var bossProvider = new ControllerBossActionProvider();
            AttachCombatBridge(
                party,
                new BossBattleState(
                    "boss", ElementType.Fire, 1000, 0d),
                new ControllerMatchActionProvider(),
                bossProvider);
            int executionCount = 0;
            int bossStartedCount = 0;
            flowController.Coordinator.MatchEventExecuting +=
                matchEvent => executionCount++;
            flowController.Coordinator.BossActionStarted +=
                () => bossStartedCount++;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateTwoWaterMatchCascade()));

            Assert.That(executionCount, Is.EqualTo(2));
            Assert.That(bossStartedCount, Is.Zero);
            Assert.That(bossProvider.CallCount, Is.Zero);

            yield return WaitUntil(
                () => bossProvider.CallCount == 1,
                "Empty MatchEvents did not reach the delayed Boss turn.",
                maximumFrames: 120);

            Assert.That(bossStartedCount, Is.EqualTo(1));
            Assert.That(bossProvider.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void MatchingCharacterDoesNotUseEmptyQueueItemDelay()
        {
            InitializeStarted(Array.Empty<int>());
            SetField(flowController, "emptyQueueItemDelaySeconds", 10f);
            var character = new CharacterBattleState(
                "fire",
                0,
                ElementType.Fire,
                1000,
                100d);
            var provider = new ControllerMatchActionProvider();
            BattleFlowCombatBridge bridge = AttachCombatBridge(
                new PartyBattleState(new[] { character }),
                new BossBattleState(
                    "boss",
                    ElementType.Fire,
                    1000,
                    0d),
                provider);
            int stepCount = 0;
            int batchCount = 0;
            bridge.CombatActionStepApplied += step => stepCount++;
            bridge.CombatActionsApplied += () => batchCount++;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            Assert.That(provider.CallCount, Is.EqualTo(1));
            Assert.That(stepCount, Is.EqualTo(1));
            Assert.That(batchCount, Is.EqualTo(1));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(flowController.Coordinator.HasMatchEventInFlight,
                Is.False);
        }

        [UnityTest]
        public IEnumerator DamageWaitsForProjectileArrivalBeforeApplying()
        {
            InitializeStarted(Array.Empty<int>());
            var character = new CharacterBattleState(
                "fire",
                0,
                ElementType.Fire,
                1000,
                100d);
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                1000,
                0d);
            BattleFlowCombatBridge bridge = AttachCombatBridge(
                new PartyBattleState(new[] { character }),
                boss,
                new ControllerMatchActionProvider());
            var presenter = new ControllableProjectilePresenter();
            SetField(
                flowController,
                "projectilePresenterOverride",
                presenter);
            int stepCount = 0;
            bridge.CombatActionStepApplied += step => stepCount++;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            Assert.That(presenter.Requests, Has.Count.EqualTo(1));
            Assert.That(presenter.Requests[0].PartySlotIndex, Is.Zero);
            Assert.That(presenter.Requests[0].CharacterId, Is.EqualTo("fire"));
            Assert.That(presenter.Requests[0].AttackElement,
                Is.EqualTo(ElementType.Fire));
            Assert.That(boss.CurrentHp, Is.EqualTo(1000));
            Assert.That(stepCount, Is.Zero);

            presenter.Arrive();
            yield return null;

            Assert.That(boss.CurrentHp, Is.EqualTo(900));
            Assert.That(stepCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EachDamageActionRequestsOneProjectileInOrder()
        {
            InitializeStarted(Array.Empty<int>());
            var character = new CharacterBattleState(
                "fire",
                2,
                ElementType.Fire,
                1000,
                100d);
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                1000,
                0d);
            AttachCombatBridge(
                new PartyBattleState(new[] { character }),
                boss,
                new ControllerMultiDamageActionProvider());
            var presenter = new ControllableProjectilePresenter();
            SetField(
                flowController,
                "projectilePresenterOverride",
                presenter);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            Assert.That(presenter.Requests, Has.Count.EqualTo(1));
            Assert.That(boss.CurrentHp, Is.EqualTo(1000));
            presenter.Arrive();
            yield return null;

            Assert.That(presenter.Requests, Has.Count.EqualTo(2));
            Assert.That(presenter.Requests[0].PartySlotIndex, Is.EqualTo(2));
            Assert.That(presenter.Requests[1].PartySlotIndex, Is.EqualTo(2));
            Assert.That(boss.CurrentHp, Is.EqualTo(900));
            presenter.Arrive();
            yield return null;

            Assert.That(boss.CurrentHp, Is.EqualTo(800));
        }

        [UnityTest]
        public IEnumerator MatchingCharactersRequestProjectilesLeftToRight()
        {
            InitializeStarted(Array.Empty<int>());
            var characters = new[]
            {
                new CharacterBattleState(
                    "slot_4", 4, ElementType.Fire, 1000, 100d),
                new CharacterBattleState(
                    "slot_0", 0, ElementType.Fire, 1000, 100d),
                new CharacterBattleState(
                    "slot_2", 2, ElementType.Fire, 1000, 100d)
            };
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                10000,
                0d);
            AttachCombatBridge(
                new PartyBattleState(characters),
                boss,
                new ControllerMatchActionProvider());
            var presenter = new ControllableProjectilePresenter();
            SetField(
                flowController,
                "projectilePresenterOverride",
                presenter);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));
            Assert.That(presenter.Requests[0].PartySlotIndex, Is.Zero);

            presenter.Arrive();
            yield return null;
            Assert.That(presenter.Requests[1].PartySlotIndex, Is.EqualTo(2));

            presenter.Arrive();
            yield return null;
            Assert.That(presenter.Requests[2].PartySlotIndex, Is.EqualTo(4));

            presenter.Arrive();
            yield return null;
            Assert.That(
                presenter.Requests.ConvertAll(item => item.CharacterId),
                Is.EqualTo(new[] { "slot_0", "slot_2", "slot_4" }));
        }

        [UnityTest]
        public IEnumerator TerminalDamageDoesNotRequestRemainingProjectile()
        {
            InitializeStarted(Array.Empty<int>());
            var character = new CharacterBattleState(
                "fire",
                0,
                ElementType.Fire,
                1000,
                100d);
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                100,
                0d);
            AttachCombatBridge(
                new PartyBattleState(new[] { character }),
                boss,
                new ControllerMultiDamageActionProvider());
            var presenter = new ControllableProjectilePresenter();
            SetField(
                flowController,
                "projectilePresenterOverride",
                presenter);
            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            Assert.That(presenter.Requests, Has.Count.EqualTo(1));
            presenter.Arrive();
            yield return null;

            Assert.That(boss.IsDefeated, Is.True);
            Assert.That(presenter.Requests, Has.Count.EqualTo(1));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
        }

        [UnityTest]
        public IEnumerator DerivedDamageUsesTheSameProjectileGate()
        {
            InitializeStarted(Array.Empty<int>());
            var character = new CharacterBattleState(
                "fire",
                0,
                ElementType.Fire,
                1000,
                100d);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                1000,
                0d);
            var actionIds = new CombatActionIdSequence();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(
                    new ICombatTriggerRule[]
                    {
                        new DerivedDamageTriggerRule(actionIds)
                    }));
            var bridge = new BattleFlowCombatBridge(
                flowController.Coordinator,
                party,
                boss,
                executor,
                new ControllerMatchActionProvider(),
                new ControllerBossActionProvider(),
                actionIds);
            flowController.AttachCombatBridge(bridge);
            var presenter = new ControllableProjectilePresenter();
            SetField(
                flowController,
                "projectilePresenterOverride",
                presenter);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));
            presenter.Arrive();
            yield return null;

            Assert.That(presenter.Requests, Has.Count.EqualTo(2));
            Assert.That(boss.CurrentHp, Is.EqualTo(900));
            presenter.Arrive();
            yield return null;

            Assert.That(boss.CurrentHp, Is.EqualTo(800));
            Assert.That(bridge.LastMatchExecutionResult, Is.Not.Null);
            Assert.That(
                bridge.LastMatchExecutionResult.ActionResults,
                Has.Count.EqualTo(2));
            Assert.That(
                bridge.LastMatchExecutionResult.ActionResults[0]
                    .Action.ActionId,
                Is.EqualTo(1));
            Assert.That(
                bridge.LastMatchExecutionResult.ActionResults[1]
                    .Action.ActionId,
                Is.EqualTo(2));
            Assert.That(actionIds.LastIssuedId, Is.EqualTo(2));
        }

        [Test]
        public void NonDamageActionAppliesWithoutProjectile()
        {
            InitializeStarted(Array.Empty<int>());
            var character = new CharacterBattleState(
                "fire",
                0,
                ElementType.Fire,
                1000,
                100d);
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                1000,
                0d);
            boss.Resources.Register("test_resource", 10);
            AttachCombatBridge(
                new PartyBattleState(new[] { character }),
                boss,
                new ControllerResourceActionProvider());
            var presenter = new ControllableProjectilePresenter();
            SetField(
                flowController,
                "projectilePresenterOverride",
                presenter);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            Assert.That(presenter.Requests, Is.Empty);
            Assert.That(boss.Resources.GetAmount("test_resource"),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BossDamageWaitsForProjectileArrivalBeforeApplying()
        {
            InitializeStarted(Array.Empty<int>());
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero", 0, ElementType.Water, 1000, 0d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 100d);
            BattleFlowCombatBridge bridge = AttachCombatBridge(
                party,
                boss,
                new ControllerResourceActionProvider(),
                new ControllerBossDamageActionProvider(1));
            var presenter = new ControllableBossProjectilePresenter();
            SetField(
                flowController,
                "bossProjectilePresenterOverride",
                presenter);
            int stepCount = 0;
            int batchCount = 0;
            bridge.BossCombatActionStepApplied += step => stepCount++;
            bridge.CombatActionsApplied += () => batchCount++;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));
            yield return null;

            Assert.That(presenter.Requests, Has.Count.EqualTo(1));
            Assert.That(presenter.Requests[0].CurrentTurn, Is.EqualTo(1));
            Assert.That(presenter.Requests[0].ActionId, Is.EqualTo(1));
            Assert.That(party.CurrentHp, Is.EqualTo(1000));
            Assert.That(stepCount, Is.Zero);
            Assert.That(batchCount, Is.Zero);

            presenter.Arrive();
            yield return null;

            Assert.That(party.CurrentHp, Is.LessThan(1000));
            Assert.That(stepCount, Is.EqualTo(1));
            Assert.That(batchCount, Is.EqualTo(1));
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator EachBossDamageRequestsOneProjectileInOrder()
        {
            InitializeStarted(Array.Empty<int>());
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero", 0, ElementType.Water, 1000, 0d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 100d);
            AttachCombatBridge(
                party,
                boss,
                new ControllerResourceActionProvider(),
                new ControllerBossDamageActionProvider(2));
            var presenter = new ControllableBossProjectilePresenter();
            SetField(
                flowController,
                "bossProjectilePresenterOverride",
                presenter);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));
            yield return null;
            Assert.That(presenter.Requests, Has.Count.EqualTo(1));
            Assert.That(party.CurrentHp, Is.EqualTo(1000));

            presenter.Arrive();
            yield return null;
            Assert.That(presenter.Requests, Has.Count.EqualTo(2));
            long hpAfterFirst = party.CurrentHp;
            Assert.That(hpAfterFirst, Is.LessThan(1000));

            presenter.Arrive();
            yield return null;
            Assert.That(party.CurrentHp, Is.LessThan(hpAfterFirst));
            Assert.That(presenter.Requests.ConvertAll(item => item.ActionId),
                Is.EqualTo(new long[] { 1, 2 }));
        }

        [UnityTest]
        public IEnumerator TerminalBossDamageDoesNotRequestNextProjectile()
        {
            InitializeStarted(Array.Empty<int>());
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero", 0, ElementType.Water, 1, 0d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 100d);
            AttachCombatBridge(
                party,
                boss,
                new ControllerResourceActionProvider(),
                new ControllerBossDamageActionProvider(2));
            var presenter = new ControllableBossProjectilePresenter();
            SetField(
                flowController,
                "bossProjectilePresenterOverride",
                presenter);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));
            yield return null;
            Assert.That(presenter.Requests, Has.Count.EqualTo(1));

            presenter.Arrive();
            yield return null;

            Assert.That(party.IsIncapacitated, Is.True);
            Assert.That(presenter.Requests, Has.Count.EqualTo(1));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
        }

        [UnityTest]
        public IEnumerator DerivedBossDamageUsesTheSameProjectileGate()
        {
            InitializeStarted(Array.Empty<int>());
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero", 0, ElementType.Water, 1000, 0d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 100d);
            var actionIds = new CombatActionIdSequence();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(new ICombatTriggerRule[]
                {
                    new DerivedBossDamageTriggerRule(actionIds)
                }));
            var bridge = new BattleFlowCombatBridge(
                flowController.Coordinator,
                party,
                boss,
                executor,
                new ControllerResourceActionProvider(),
                new ControllerBossDamageActionProvider(1),
                actionIds);
            flowController.AttachCombatBridge(bridge);
            var presenter = new ControllableBossProjectilePresenter();
            SetField(
                flowController,
                "bossProjectilePresenterOverride",
                presenter);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));
            yield return null;
            Assert.That(presenter.Requests, Has.Count.EqualTo(1));

            presenter.Arrive();
            yield return null;
            Assert.That(presenter.Requests, Has.Count.EqualTo(2));
            long hpAfterRoot = party.CurrentHp;

            presenter.Arrive();
            yield return null;
            Assert.That(party.CurrentHp, Is.LessThan(hpAfterRoot));
            Assert.That(presenter.Requests.ConvertAll(item => item.ActionId),
                Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(actionIds.LastIssuedId, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator BossDamageFullShieldAbsorbWaitsForArrival()
        {
            InitializeStarted(Array.Empty<int>());
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero", 0, ElementType.Water, 1000, 0d)
            });
            party.Shields.Add(new ShieldInstance(
                1, "hero", 1000, 1, 5, 1));
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 100d);
            AttachCombatBridge(
                party,
                boss,
                new ControllerResourceActionProvider(),
                new ControllerBossDamageActionProvider(1));
            var presenter = new ControllableBossProjectilePresenter();
            SetField(
                flowController,
                "bossProjectilePresenterOverride",
                presenter);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));
            yield return null;
            Assert.That(party.CurrentHp, Is.EqualTo(1000));
            Assert.That(party.Shields.TotalShield, Is.EqualTo(1000));

            presenter.Arrive();
            yield return null;
            Assert.That(party.CurrentHp, Is.EqualTo(1000));
            Assert.That(party.Shields.TotalShield, Is.LessThan(1000));
        }

        [UnityTest]
        public IEnumerator AbortDuringBossProjectilePreventsDamage()
        {
            InitializeStarted(Array.Empty<int>());
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero", 0, ElementType.Water, 1000, 0d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 100d);
            AttachCombatBridge(
                party,
                boss,
                new ControllerResourceActionProvider(),
                new ControllerBossDamageActionProvider(1));
            var presenter = new ControllableBossProjectilePresenter();
            SetField(
                flowController,
                "bossProjectilePresenterOverride",
                presenter);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));
            yield return null;
            Assert.That(presenter.HasActiveRequest, Is.True);

            Assert.That(flowController.AbortBattle(), Is.True);
            presenter.Arrive();
            yield return null;

            Assert.That(presenter.CancelCount, Is.EqualTo(1));
            Assert.That(party.CurrentHp, Is.EqualTo(1000));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
        }

        [UnityTest]
        public IEnumerator MissingBossPresenterAppliesDamageImmediately()
        {
            InitializeStarted(Array.Empty<int>());
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero", 0, ElementType.Water, 1000, 0d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 100d);
            AttachCombatBridge(
                party,
                boss,
                new ControllerResourceActionProvider(),
                new ControllerBossDamageActionProvider(1));
            LogAssert.Expect(
                LogType.Warning,
                "[BattleFlow] Boss damage projectile presenter is "
                    + "unavailable; applying immediately.");

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));
            yield return null;

            Assert.That(party.CurrentHp, Is.LessThan(1000));
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator BossNonDamageActionSkipsProjectile()
        {
            InitializeStarted(Array.Empty<int>());
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero", 0, ElementType.Water, 1000, 0d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 100d);
            boss.Resources.Register("test_resource", 5);
            AttachCombatBridge(
                party,
                boss,
                new ControllerResourceActionProvider(),
                new ControllerBossResourceActionProvider());
            var presenter = new ControllableBossProjectilePresenter();
            SetField(
                flowController,
                "bossProjectilePresenterOverride",
                presenter);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));
            yield return null;

            Assert.That(presenter.Requests, Is.Empty);
            Assert.That(boss.Resources.GetAmount("test_resource"),
                Is.EqualTo(1));
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator AbortDuringProjectileCancelsWithoutStaleApply()
        {
            InitializeStarted(Array.Empty<int>());
            var character = new CharacterBattleState(
                "fire",
                0,
                ElementType.Fire,
                1000,
                100d);
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                1000,
                0d);
            AttachCombatBridge(
                new PartyBattleState(new[] { character }),
                boss,
                new ControllerMatchActionProvider());
            var presenter = new ControllableProjectilePresenter();
            SetField(
                flowController,
                "projectilePresenterOverride",
                presenter);
            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            Assert.That(presenter.HasActiveRequest, Is.True);
            Assert.That(flowController.AbortBattle(), Is.True);
            presenter.Arrive();
            yield return null;

            Assert.That(presenter.CancelCount, Is.EqualTo(1));
            Assert.That(boss.CurrentHp, Is.EqualTo(1000));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
        }

        [Test]
        public void StopFlowProgressionCancelsSharedPresenterOnce()
        {
            InitializeStarted(Array.Empty<int>());
            var presenter = new CountingSharedProjectilePresenter();
            SetField(
                flowController,
                "projectilePresenterOverride",
                presenter);
            SetField(
                flowController,
                "bossProjectilePresenterOverride",
                presenter);

            InvokePrivate(flowController, "StopFlowProgression");

            Assert.That(presenter.CancelCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AbortDuringPreAttackDelayCancelsContinuation()
        {
            InitializeStarted(Array.Empty<int>());
            SetField(flowController, "preAttackDelaySeconds", 10f);
            int executionCount = 0;
            flowController.Coordinator.MatchEventExecuting +=
                matchEvent => executionCount++;
            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            Assert.That(flowController.AbortBattle(), Is.True);
            yield return null;

            Assert.That(executionCount, Is.Zero);
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.Zero);
        }

        [UnityTest]
        public IEnumerator AbortDuringEmptyItemDelayDropsStagedSession()
        {
            InitializeStarted(Array.Empty<int>());
            SetField(flowController, "emptyQueueItemDelaySeconds", 10f);
            var character = new CharacterBattleState(
                "fire",
                0,
                ElementType.Fire,
                1000,
                100d);
            BattleFlowCombatBridge bridge = AttachCombatBridge(
                new PartyBattleState(new[] { character }),
                new BossBattleState(
                    "boss",
                    ElementType.Fire,
                    1000,
                    0d),
                new ControllerMatchActionProvider());
            int executionCount = 0;
            flowController.Coordinator.MatchEventExecuting +=
                matchEvent => executionCount++;
            SendCompletedAction(
                1,
                CreateResolvedResult(CreateTwoWaterMatchCascade()));

            Assert.That(executionCount, Is.EqualTo(1));
            Assert.That(GetField<object>(
                bridge,
                "currentMatchCombatSession"), Is.Not.Null);
            Assert.That(flowController.AbortBattle(), Is.True);
            yield return null;

            Assert.That(executionCount, Is.EqualTo(1));
            Assert.That(GetField<object>(
                bridge,
                "currentMatchCombatSession"), Is.Null);
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
        }

        [UnityTest]
        public IEnumerator AbortDuringPreBossDelayCancelsBossContinuation()
        {
            InitializeStarted(Array.Empty<int>());
            SetField(flowController, "preBossActionDelaySeconds", 10f);
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "fire", 0, ElementType.Fire, 1000, 100d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 1000, 0d);
            var bossProvider = new ControllerBossActionProvider();
            BattleFlowCombatBridge bridge = AttachCombatBridge(
                party,
                boss,
                new ControllerMatchActionProvider(),
                bossProvider);
            int bossStartedCount = 0;
            flowController.Coordinator.BossActionStarted +=
                () => bossStartedCount++;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            Assert.That(boss.CurrentHp, Is.EqualTo(900));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));
            Assert.That(flowController.AbortBattle(), Is.True);
            yield return null;

            Assert.That(bossStartedCount, Is.Zero);
            Assert.That(bossProvider.CallCount, Is.Zero);
            Assert.That(GetField<object>(
                bridge,
                "currentMatchCombatSession"), Is.Null);
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
        }

        [Test]
        public void TerminalMatchSkipsPreBossDelayAndBossAction()
        {
            InitializeStarted(Array.Empty<int>());
            SetField(flowController, "preBossActionDelaySeconds", 10f);
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "fire", 0, ElementType.Fire, 1000, 100d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 100, 0d);
            var bossProvider = new ControllerBossActionProvider();
            AttachCombatBridge(
                party,
                boss,
                new ControllerMatchActionProvider(),
                bossProvider);
            int bossStartedCount = 0;
            flowController.Coordinator.BossActionStarted +=
                () => bossStartedCount++;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            Assert.That(boss.IsDefeated, Is.True);
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.Result));
            Assert.That(bossStartedCount, Is.Zero);
            Assert.That(bossProvider.CallCount, Is.Zero);
            Assert.That(notUsersTurnPanel.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator LethalVictoryRefreshesHpThenDelaysResultOverlay()
        {
            InitializeStarted(new[] { 3 });
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "fire", 0, ElementType.Fire, 1000, 100d)
            });
            var boss = new BossBattleState(
                "boss", ElementType.Fire, 100, 0d);
            AttachCombatBridge(
                party,
                boss,
                new ControllerMatchActionProvider());
            BattleHudController hud = CreateResultHud(0.04f);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(boss.CurrentHp, Is.Zero);
            Assert.That(hud.BossHpSlider.value, Is.Zero);
            Assert.That(hud.ResultOverlay.activeSelf, Is.False);
            AssertResultInputLocked();
            Coroutine pending = GetField<Coroutine>(
                hud,
                "resultOverlayCoroutine");
            Assert.That(pending, Is.Not.Null);

            hud.RefreshInitialSnapshot();
            InvokePrivate(
                hud,
                "HandleResultReached",
                BattleResultKind.Victory);
            Assert.That(GetField<Coroutine>(
                hud,
                "resultOverlayCoroutine"), Is.SameAs(pending));

            yield return WaitUntil(
                () => hud.ResultOverlay.activeSelf,
                "Victory overlay did not appear after its delay.",
                maximumFrames: 120);
        }

        [UnityTest]
        public IEnumerator DefeatUsesTheSharedResultOverlayDelay()
        {
            InitializeStarted(new[] { 3 });
            AttachResultPresentationBridge();
            BattleHudController hud = CreateResultHud(0.04f);

            Assert.That(
                flowController.Coordinator.NotifyPartyIncapacitated(),
                Is.True);

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(hud.ResultOverlay.activeSelf, Is.False);
            AssertResultInputLocked();

            yield return WaitUntil(
                () => hud.ResultOverlay.activeSelf,
                "Defeat overlay did not appear after its delay.",
                maximumFrames: 120);
        }

        [UnityTest]
        public IEnumerator TurnLimitUsesTheSharedResultOverlayDelay()
        {
            InitializeStarted(new[] { 3 }, turnLimit: 1);
            AttachResultPresentationBridge();
            BattleHudController hud = CreateResultHud(0.04f);

            Assert.That(flowController.Coordinator.TryBeginBoardResolution(),
                Is.True);
            Assert.That(
                flowController.Coordinator.NotifyBoardActionResolved(
                    CreateSingleMatchCascade(),
                    consumesTurn: true),
                Is.True);
            Assert.That(
                flowController.Coordinator.ExecuteRemainingMatchEvents(),
                Is.EqualTo(1));
            Assert.That(flowController.Coordinator.CompleteBossAction(),
                Is.True);

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
            Assert.That(hud.ResultOverlay.activeSelf, Is.False);
            AssertResultInputLocked();

            yield return WaitUntil(
                () => hud.ResultOverlay.activeSelf,
                "Turn-limit overlay did not appear after its delay.",
                maximumFrames: 120);
        }

        [Test]
        public void AbortedResultBypassesOverlayDelayAndCoroutine()
        {
            InitializeStarted(new[] { 3 });
            AttachResultPresentationBridge();
            BattleHudController hud = CreateResultHud(10f);

            Assert.That(flowController.AbortBattle(), Is.True);

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(hud.ResultOverlay.activeSelf, Is.True);
            Assert.That(hud.ResultText.text, Is.EqualTo("Battle Aborted"));
            Assert.That(GetField<Coroutine>(
                hud,
                "resultOverlayCoroutine"), Is.Null);
            AssertResultInputLocked();
        }

        [UnityTest]
        public IEnumerator DisableCancelsPendingResultOverlayActivation()
        {
            InitializeStarted(new[] { 3 });
            AttachResultPresentationBridge();
            BattleHudController hud = CreateResultHud(0.04f);

            Assert.That(flowController.Coordinator.NotifyBossDefeated(),
                Is.True);
            Assert.That(hud.ResultOverlay.activeSelf, Is.False);
            Assert.That(GetField<Coroutine>(
                hud,
                "resultOverlayCoroutine"), Is.Not.Null);

            hud.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.08f);

            Assert.That(hud.ResultOverlay.activeSelf, Is.False);
            Assert.That(GetField<Coroutine>(
                hud,
                "resultOverlayCoroutine"), Is.Null);
        }

        [UnityTest]
        public IEnumerator AttachedBridgeExecutesMatchCombatBeforeBossCompletes()
        {
            InitializeStarted(Array.Empty<int>());
            var character = new CharacterBattleState(
                "fire",
                0,
                ElementType.Fire,
                1000,
                100d);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                1000,
                0d);
            var actionIds = new CombatActionIdSequence();
            var matchProvider =
                new ControllerMatchActionProvider();
            var bossProvider = new ControllerBossActionProvider();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            var bridge = new BattleFlowCombatBridge(
                flowController.Coordinator,
                party,
                boss,
                executor,
                matchProvider,
                bossProvider,
                actionIds);
            flowController.AttachCombatBridge(bridge);

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateSingleMatchCascade()));

            yield return WaitUntil(
                () => flowController.Context.CurrentTurn == 2
                    && flowController.Context.Phase
                        == BattlePhase.PlayerInput,
                "The combat bridge did not complete the integrated turn.");

            Assert.That(matchProvider.CallCount, Is.EqualTo(1));
            Assert.That(bossProvider.CallCount, Is.EqualTo(1));
            Assert.That(boss.CurrentHp, Is.EqualTo(900));
            Assert.That(actionIds.LastIssuedId, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TwentyFiveTurnsFinishAfterFinalMatchAndBoss()
        {
            InitializeStarted(Array.Empty<int>());
            int matchCount = 0;
            int finalTurnMatchCount = 0;
            int bossCount = 0;
            flowController.Coordinator.MatchEventExecuting += matchEvent =>
            {
                matchCount++;
                if (flowController.Context.CurrentTurn == 25)
                {
                    finalTurnMatchCount++;
                }
            };
            flowController.Coordinator.BossActionStarted += () => bossCount++;

            long actionId = 1;
            for (int turn = 1; turn <= 25; turn++)
            {
                SendCompletedAction(
                    actionId++,
                    CreateResolvedResult(CreateSingleMatchCascade()));

                yield return WaitUntil(
                    () => flowController.Context.Result
                            != BattleResultKind.None
                        || flowController.Context.Phase
                            == BattlePhase.PlayerInput,
                    $"Turn {turn} did not finish within the frame limit.");

                if (turn < 25)
                {
                    Assert.That(flowController.Context.CurrentTurn,
                        Is.EqualTo(turn + 1));
                    Assert.That(flowController.Context.Result,
                        Is.EqualTo(BattleResultKind.None));
                }
            }

            Assert.That(matchCount, Is.EqualTo(25));
            Assert.That(finalTurnMatchCount, Is.EqualTo(1));
            Assert.That(bossCount, Is.EqualTo(25));
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(25));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.Result));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
        }

        [UnityTest]
        public IEnumerator BossDefeatDuringFirstMatchStopsRemainingMatchesAndBoss()
        {
            InitializeStarted(Array.Empty<int>());
            int matchCount = 0;
            int bossCount = 0;
            int resultCount = 0;
            long interruptedExecutionId = 0;
            flowController.Coordinator.MatchEventExecuting += matchEvent =>
            {
                matchCount++;
                interruptedExecutionId = flowController.Coordinator
                    .CurrentMatchEventExecution.ExecutionId;
                flowController.NotifyBossDefeated();
            };
            flowController.Coordinator.BossActionStarted += () => bossCount++;
            flowController.Coordinator.ResultReached += result => resultCount++;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateTwoMatchCascade()));

            yield return WaitUntil(
                () => flowController.Context.Result
                    == BattleResultKind.Victory,
                "Boss defeat did not end the battle.");

            Assert.That(matchCount, Is.EqualTo(1));
            Assert.That(bossCount, Is.Zero);
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.Zero);
            Assert.That(flowController.Coordinator.HasMatchEventInFlight,
                Is.False);
            Assert.That(
                flowController.Coordinator.CompleteCurrentMatchEvent(
                    interruptedExecutionId),
                Is.False);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);

            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    CreateNoMatchResult(),
                    BoardActionCompletionStatus.Interrupted));
            Assert.That(resultCount, Is.EqualTo(1));
        }

        [Test]
        public void PartyDefeatAndAbortAreTerminalAndNotifyOnce()
        {
            InitializeStarted(Array.Empty<int>());
            int resultCount = 0;
            flowController.Coordinator.ResultReached += result => resultCount++;

            Assert.That(flowController.NotifyPartyDefeated(), Is.True);
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(flowController.AbortBattle(), Is.False);
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(notUsersTurnPanel.activeSelf, Is.True);
        }

        [Test]
        public void FailedBoardCompletionAbortsAndPreservesFailureLog()
        {
            InitializeStarted(Array.Empty<int>());
            BoardSwapActionResult result = CreateNoMatchResult();
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));
            var failure = new InvalidOperationException(
                "Injected connected presentation failure.");
            LogAssert.Expect(
                LogType.Exception,
                new Regex("Injected connected presentation failure\\."));
            LogAssert.Expect(
                LogType.Error,
                "[BattleFlow] Board presentation failed. ActionId=1.");

            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    result,
                    BoardActionCompletionStatus.Failed,
                    failure));

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.Zero);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
        }

        [Test]
        public void InterruptedBoardCompletionAbortsDuringBattle()
        {
            InitializeStarted(Array.Empty<int>());
            BoardSwapActionResult result = CreateNoMatchResult();
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));

            LogAssert.Expect(
                LogType.Warning,
                new Regex("Board presentation was interrupted"));
            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    result,
                    BoardActionCompletionStatus.Interrupted));

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
        }

        [Test]
        public void InterruptedAndOnDisableAbortExactlyOnceAndUnsubscribe()
        {
            InitializeStarted(Array.Empty<int>());
            BoardSwapActionResult result = CreateNoMatchResult();
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));
            int resultCount = 0;
            flowController.Coordinator.ResultReached += value => resultCount++;

            DeactivateFixture();
            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    result,
                    BoardActionCompletionStatus.Interrupted));

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(GetField(flowController, "boardEventsSubscribed"),
                Is.False);
        }

        private void InitializeStarted(
            IReadOnlyList<int> cooldowns,
            int turnLimit = 25)
        {
            SetField(boardController, "initialBoardReadyPublished", true);
            Assert.That(boardController.HasInitialBoardReady, Is.True);
            InitializeFlow(new BattleFlowSetup(turnLimit, cooldowns));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.NotStarted));
            ActivateFixture();
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
        }

        private BattleFlowCombatBridge AttachResultPresentationBridge()
        {
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero", 0, ElementType.Water, 1000, 0d)
            });
            return AttachCombatBridge(
                party,
                new BossBattleState(
                    "boss", ElementType.Fire, 1000, 0d),
                new ControllerResourceActionProvider());
        }

        private BattleHudController CreateResultHud(float delaySeconds)
        {
            var hudObject = new GameObject(
                "ResultHud",
                typeof(RectTransform));
            hudObject.SetActive(false);
            hudObject.transform.SetParent(root.transform, false);
            BattleHudController hud =
                hudObject.AddComponent<BattleHudController>();
            var overlay = new GameObject(
                "BattleResultOverlay",
                typeof(RectTransform));
            overlay.transform.SetParent(hudObject.transform, false);
            var bossSliderObject = new GameObject(
                "BossHpSlider",
                typeof(RectTransform),
                typeof(Slider));
            bossSliderObject.transform.SetParent(
                hudObject.transform,
                false);
            var partySliderObject = new GameObject(
                "PartyHpSlider",
                typeof(RectTransform),
                typeof(Slider));
            partySliderObject.transform.SetParent(
                hudObject.transform,
                false);

            SetField(hud, "battleFlowController", flowController);
            SetField(hud, "resultOverlay", overlay);
            SetField(hud, "resultOverlayDelaySeconds", delaySeconds);
            SetField(
                hud,
                "bossHpSlider",
                bossSliderObject.GetComponent<Slider>());
            SetField(
                hud,
                "partyHpSlider",
                partySliderObject.GetComponent<Slider>());
            hudObject.SetActive(true);
            InvokePrivate(hud, "Start");
            Assert.That(hud.IsRuntimeConnected, Is.True);
            Assert.That(hud.ResultOverlay.activeSelf, Is.False);
            return hud;
        }

        private void AssertResultInputLocked()
        {
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(flowController.CanUseActive(0), Is.False);
            Assert.That(notUsersTurnPanel.activeSelf, Is.True);
        }

        private void InitializeFlow(BattleFlowSetup setup)
        {
            flowController.Initialize(setup);
            Assert.That(flowController.Context, Is.Not.Null);
        }

        private void ActivateFixture()
        {
            root.SetActive(true);
            Assert.That(flowController.isActiveAndEnabled, Is.True);
            if (!GetField<bool>(flowController, "connectionEnabled"))
            {
                InvokePrivate(flowController, "OnEnable");
            }

            Assert.That(GetField<bool>(flowController, "connectionEnabled"),
                Is.True);
            Assert.That(
                GetField<BattleBoardController>(
                    flowController,
                    "boardController"),
                Is.SameAs(boardController));
            Assert.That(flowController.Context, Is.Not.Null);
        }

        private void DeactivateFixture()
        {
            root.SetActive(false);
            Assert.That(flowController.isActiveAndEnabled, Is.False);
            if (GetField<bool>(flowController, "connectionEnabled"))
            {
                InvokePrivate(flowController, "OnDisable");
            }

            Assert.That(GetField<bool>(flowController, "connectionEnabled"),
                Is.False);
        }

        private void PublishInitialBoardReady()
        {
            SetField(boardController, "initialBoardReadyPublished", true);
            GetField<Action>(boardController, "InitialBoardReady")?.Invoke();
        }

        private static IEnumerator WaitUntil(
            Func<bool> condition,
            string failureMessage,
            int maximumFrames = 16)
        {
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.That(condition(), Is.True, failureMessage);
        }

        private void SendCompletedAction(
            long actionId,
            BoardSwapActionResult result)
        {
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(actionId, result));
            if (result.ConsumesTurn && result.Cascade != null)
            {
                for (int index = 0;
                    index < result.Cascade.Steps.Count;
                    index++)
                {
                    RevealStep(actionId, result.Cascade, index);
                }
            }

            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    actionId,
                    result,
                    BoardActionCompletionStatus.Completed));
        }

        private void RevealStep(
            long actionId,
            BoardCascadeResult cascade,
            int stepIndex)
        {
            InvokePrivate(
                flowController,
                "HandleCascadeStepPresented",
                CreateInternal<BoardCascadeStepPresentation>(
                    actionId,
                    stepIndex,
                    cascade.Steps[stepIndex]));
        }

        private static void AssertPresentationEntry(
            BattleMatchQueuePresentationEntry entry,
            int sequenceIndex,
            int cascadeStepIndex,
            ElementType element,
            int removedBlockCount)
        {
            Assert.That(entry.SequenceIndex, Is.EqualTo(sequenceIndex));
            Assert.That(entry.CascadeStepIndex,
                Is.EqualTo(cascadeStepIndex));
            Assert.That(entry.Element, Is.EqualTo(element));
            Assert.That(entry.RemovedBlockCount,
                Is.EqualTo(removedBlockCount));
        }

        private void AssertPresentationMatchesAuthoritative()
        {
            IReadOnlyList<MatchEvent> authoritative =
                flowController.Coordinator.GetPendingMatchEvents();
            IReadOnlyList<BattleMatchQueuePresentationEntry> presentation =
                flowController.MatchQueuePresentation.GetSnapshot();
            Assert.That(presentation.Count,
                Is.EqualTo(authoritative.Count));
            for (int index = 0; index < authoritative.Count; index++)
            {
                AssertPresentationEntry(
                    presentation[index],
                    authoritative[index].SequenceIndex,
                    authoritative[index].CascadeStepIndex,
                    authoritative[index].Element,
                    authoritative[index].RemovedBlockCount);
            }
        }

        private void InitializeReplacementControllerForReconcile()
        {
            root.SetActive(false);
            var replacement = new GameObject("ReplacementFlowController");
            replacement.SetActive(false);
            createdObjects.Add(replacement);
            boardController = replacement.AddComponent<
                BattleBoardController>();
            boardController.enabled = false;
            flowController = replacement.AddComponent<BattleFlowController>();
            SetField(flowController, "boardController", boardController);
            root = replacement;
            InitializeStarted(Array.Empty<int>());
        }

        private static int CountTargetSubscribers(
            object source,
            string eventName,
            object target)
        {
            FieldInfo field = source.GetType().GetField(
                eventName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            var callback = field?.GetValue(source) as Delegate;
            if (callback == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Delegate subscriber in callback.GetInvocationList())
            {
                if (ReferenceEquals(subscriber.Target, target))
                {
                    count++;
                }
            }

            return count;
        }

        private static BoardCascadeResult CreateSingleMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(
                new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Fire,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0))
                });
        }

        private static BoardCascadeResult CreateTwoMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(
                new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Fire,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0)),
                    BattleFlowTestSupport.Match(
                        ElementType.Water,
                        new BoardPosition(3, 1),
                        new BoardPosition(4, 1),
                        new BoardPosition(5, 1))
                });
        }

        private static BoardCascadeResult CreateTwoWaterMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(
                new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Water,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0)),
                    BattleFlowTestSupport.Match(
                        ElementType.Water,
                        new BoardPosition(3, 1),
                        new BoardPosition(4, 1),
                        new BoardPosition(5, 1))
                });
        }

        private BattleFlowCombatBridge AttachCombatBridge(
            PartyBattleState party,
            BossBattleState boss,
            IMatchEventActionProvider matchProvider,
            IBossCombatActionProvider bossProvider = null)
        {
            var bridge = new BattleFlowCombatBridge(
                flowController.Coordinator,
                party,
                boss,
                new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(new SeededRandomSource(1))),
                matchProvider,
                bossProvider ?? new ControllerBossActionProvider(),
                new CombatActionIdSequence());
            flowController.AttachCombatBridge(bridge);
            return bridge;
        }

        private BattleFlowCombatBridge AttachActiveCombatBridge(
            PartyBattleState party,
            BossBattleState boss,
            params IActiveAbilityActionProvider[] providers)
        {
            Assert.That(providers, Has.Length.EqualTo(
                flowController.Context.ActiveAbilities.Count));
            Assert.That(party.Characters.Count,
                Is.GreaterThanOrEqualTo(providers.Length));
            var bindings = new ActiveAbilityBinding[providers.Length];
            var registry = new ActiveAbilityActionProviderRegistry();
            for (int index = 0; index < providers.Length; index++)
            {
                CharacterBattleState character = party.Characters[index];
                bindings[index] = new ActiveAbilityBinding(
                    index,
                    character.PartySlotIndex,
                    character.CharacterId,
                    "active");
                registry.Register(
                    character.CharacterId,
                    "active",
                    providers[index]);
            }

            var actionIds = new CombatActionIdSequence();
            var bridge = new BattleFlowCombatBridge(
                flowController.Coordinator,
                party,
                boss,
                new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(new SeededRandomSource(1))),
                new ControllerMatchActionProvider(),
                new ControllerBossActionProvider(),
                actionIds,
                bindings,
                registry);
            flowController.AttachCombatBridge(bridge);
            return bridge;
        }

        private static DamageAction CreateActiveDamage(
            ActiveAbilityActionContext context,
            ElementType attackElement)
        {
            return new DamageAction(
                context.ActionIds.Next(),
                ActionOrigin.Active,
                new DamageContextBuildRequest(
                    context.Character,
                    context.Party,
                    context.Boss,
                    attackElement,
                    AttackType.Active,
                    AttackTag.None,
                    1d,
                    false,
                    0,
                    false));
        }

        private static BoardSwapActionResult CreateNoMatchResult()
        {
            return CreateInternal<BoardSwapActionResult>(
                TestSwap(),
                BoardSwapActionStatus.NoMatch,
                new BoardState(),
                null,
                null,
                new BoardState());
        }

        private static BoardSwapActionResult CreateResolvedResult(
            BoardCascadeResult cascade)
        {
            BoardState finalBoard = cascade.Board;
            BoardShuffleResult shuffle = CreateInternal<BoardShuffleResult>(
                finalBoard,
                BoardShuffleKind.None,
                new List<BoardShuffleEntry>(),
                0);
            return CreateInternal<BoardSwapActionResult>(
                TestSwap(),
                BoardSwapActionStatus.Resolved,
                new BoardState(),
                cascade,
                shuffle,
                finalBoard);
        }

        private static BoardSwap TestSwap()
        {
            return new BoardSwap(
                new BoardPosition(0, 0),
                new BoardPosition(1, 0));
        }

        private static BoardActionExecution CreateExecution(
            long actionId,
            BoardSwapActionResult result)
        {
            return CreateInternal<BoardActionExecution>(actionId, result);
        }

        private static BoardActionCompletion CreateCompletion(
            long actionId,
            BoardSwapActionResult result,
            BoardActionCompletionStatus status,
            Exception failure = null)
        {
            return CreateInternal<BoardActionCompletion>(
                actionId,
                result,
                status,
                failure);
        }

        private static T CreateInternal<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic,
                null,
                arguments,
                null);
        }

        private static object InvokePrivate(
            object target,
            string methodName,
            params object[] arguments)
        {
            return target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(
                    target,
                    arguments);
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                    target,
                    value);
        }

        private static object GetField(object target, string fieldName)
        {
            return target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(
                    target);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            return (T)GetField(target, fieldName);
        }

        private sealed class ControllerMatchActionProvider
            : IMatchEventActionProvider
        {
            public int CallCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                MatchEventActionContext context)
            {
                CallCount++;
                return new CombatAction[]
                {
                    new DamageAction(
                        context.ActionIds.Next(),
                        ActionOrigin.Match,
                        new DamageContextBuildRequest(
                            context.Character,
                            context.Party,
                            context.Boss,
                            context.Character.Element,
                            ValorChronicle.Battle.Combat.Attacks.AttackType.Match,
                            context.MatchAttackTag,
                            1d,
                            true,
                            context.FinalComboCount,
                            false))
                };
            }
        }

        private sealed class DelegateActiveActionProvider
            : IActiveAbilityActionProvider
        {
            private readonly Func<
                ActiveAbilityActionContext,
                IReadOnlyList<CombatAction>> createActions;

            public DelegateActiveActionProvider(
                Func<
                    ActiveAbilityActionContext,
                    IReadOnlyList<CombatAction>> createActions)
            {
                this.createActions = createActions
                    ?? throw new ArgumentNullException(nameof(createActions));
            }

            public IReadOnlyList<CombatAction> CreateRootActions(
                ActiveAbilityActionContext context)
            {
                return createActions(context);
            }
        }

        private sealed class ControllerBossActionProvider
            : IBossCombatActionProvider
        {
            public int CallCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                BossCombatActionContext context)
            {
                CallCount++;
                return Array.Empty<CombatAction>();
            }
        }

        private sealed class ControllerBossDamageActionProvider
            : IBossCombatActionProvider
        {
            private readonly int actionCount;

            public ControllerBossDamageActionProvider(int actionCount)
            {
                if (actionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(actionCount));
                }

                this.actionCount = actionCount;
            }

            public IReadOnlyList<CombatAction> CreateRootActions(
                BossCombatActionContext context)
            {
                var actions = new CombatAction[actionCount];
                for (int index = 0; index < actions.Length; index++)
                {
                    actions[index] = new BossDamageAction(
                        context.ActionIds.Next(),
                        new BossDamageContextBuildRequest(
                            context.Boss,
                            context.Party,
                            1d,
                            ValorChronicle.Battle.Combat.Attacks.AttackTag
                                .None));
                }

                return actions;
            }
        }

        private sealed class ControllerBossResourceActionProvider
            : IBossCombatActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                BossCombatActionContext context)
            {
                return new CombatAction[]
                {
                    new AddResourceAction(
                        context.ActionIds.Next(),
                        ActionOrigin.System,
                        context.Boss,
                        "test_resource",
                        1)
                };
            }
        }

        private sealed class ControllerMultiDamageActionProvider
            : IMatchEventActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                MatchEventActionContext context)
            {
                return new CombatAction[]
                {
                    CreateDamage(context),
                    CreateDamage(context)
                };
            }

            private static DamageAction CreateDamage(
                MatchEventActionContext context)
            {
                return new DamageAction(
                    context.ActionIds.Next(),
                    ActionOrigin.Match,
                    new DamageContextBuildRequest(
                        context.Character,
                        context.Party,
                        context.Boss,
                        context.Character.Element,
                        ValorChronicle.Battle.Combat.Attacks.AttackType.Match,
                        context.MatchAttackTag,
                        1d,
                        true,
                        context.FinalComboCount,
                        false));
            }
        }

        private sealed class ControllerResourceActionProvider
            : IMatchEventActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                MatchEventActionContext context)
            {
                return new CombatAction[]
                {
                    new AddResourceAction(
                        context.ActionIds.Next(),
                        ActionOrigin.Match,
                        context.Boss,
                        "test_resource",
                        1)
                };
            }
        }

        private sealed class ControllableProjectilePresenter
            : IMatchDamageProjectilePresenter
        {
            private Action<MatchDamageProjectileCompletion> completion;

            public List<MatchDamageProjectileRequest> Requests { get; } =
                new List<MatchDamageProjectileRequest>();
            public bool HasActiveRequest => completion != null;
            public int CancelCount { get; private set; }

            public bool TryPresent(
                MatchDamageProjectileRequest request,
                Action<MatchDamageProjectileCompletion> callback)
            {
                Assert.That(completion, Is.Null);
                Requests.Add(request);
                completion = callback;
                return true;
            }

            public void CancelActive()
            {
                if (completion == null)
                {
                    return;
                }

                CancelCount++;
                Action<MatchDamageProjectileCompletion> callback = completion;
                completion = null;
                callback(MatchDamageProjectileCompletion.Cancelled);
            }

            public void Arrive()
            {
                if (completion == null)
                {
                    return;
                }

                Action<MatchDamageProjectileCompletion> callback = completion;
                completion = null;
                callback(MatchDamageProjectileCompletion.Arrived);
            }
        }

        private sealed class ControllableActiveProjectilePresenter
            : IActiveDamageProjectilePresenter
        {
            private Action<ActiveDamageProjectileCompletion> completion;

            public List<ActiveDamageProjectileRequest> Requests { get; } =
                new List<ActiveDamageProjectileRequest>();
            public int CancelCount { get; private set; }

            public bool TryPresent(
                ActiveDamageProjectileRequest request,
                Action<ActiveDamageProjectileCompletion> callback)
            {
                Assert.That(completion, Is.Null);
                Requests.Add(request);
                completion = callback;
                return true;
            }

            public void CancelActive()
            {
                if (completion == null)
                {
                    return;
                }

                CancelCount++;
                Action<ActiveDamageProjectileCompletion> callback =
                    completion;
                completion = null;
                callback(ActiveDamageProjectileCompletion.Cancelled);
            }

            public void Arrive()
            {
                if (completion == null)
                {
                    return;
                }

                Action<ActiveDamageProjectileCompletion> callback =
                    completion;
                completion = null;
                callback(ActiveDamageProjectileCompletion.Arrived);
            }
        }

        private sealed class ControllableBossProjectilePresenter
            : IBossDamageProjectilePresenter
        {
            private Action<BossDamageProjectileCompletion> completion;

            public List<BossDamageProjectileRequest> Requests { get; } =
                new List<BossDamageProjectileRequest>();
            public bool HasActiveRequest => completion != null;
            public int CancelCount { get; private set; }

            public bool TryPresent(
                BossDamageProjectileRequest request,
                Action<BossDamageProjectileCompletion> callback)
            {
                Assert.That(completion, Is.Null);
                Requests.Add(request);
                completion = callback;
                return true;
            }

            public void CancelActive()
            {
                if (completion == null)
                {
                    return;
                }

                CancelCount++;
                Action<BossDamageProjectileCompletion> callback = completion;
                completion = null;
                callback(BossDamageProjectileCompletion.Cancelled);
            }

            public void Arrive()
            {
                if (completion == null)
                {
                    return;
                }

                Action<BossDamageProjectileCompletion> callback = completion;
                completion = null;
                callback(BossDamageProjectileCompletion.Arrived);
            }
        }

        private sealed class CountingSharedProjectilePresenter
            : IMatchDamageProjectilePresenter, IBossDamageProjectilePresenter
        {
            public int CancelCount { get; private set; }

            public bool TryPresent(
                MatchDamageProjectileRequest request,
                Action<MatchDamageProjectileCompletion> callback)
            {
                return false;
            }

            public bool TryPresent(
                BossDamageProjectileRequest request,
                Action<BossDamageProjectileCompletion> callback)
            {
                return false;
            }

            public void CancelActive()
            {
                CancelCount++;
            }
        }

        private sealed class DerivedDamageTriggerRule : ICombatTriggerRule
        {
            private readonly CombatActionIdSequence actionIds;

            public DerivedDamageTriggerRule(
                CombatActionIdSequence actionIds)
            {
                this.actionIds = actionIds
                    ?? throw new ArgumentNullException(nameof(actionIds));
            }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (!(context.CompletedAction is DamageAction source)
                    || source.Origin != ActionOrigin.Match)
                {
                    return Array.Empty<CombatAction>();
                }

                DamageContextBuildRequest request = source.ContextRequest;
                return new CombatAction[]
                {
                    new DamageAction(
                        actionIds.Next(),
                        ActionOrigin.Additional,
                        new DamageContextBuildRequest(
                            request.Attacker,
                            request.Party,
                            request.TargetBoss,
                            request.AttackElement,
                            ValorChronicle.Battle.Combat.Attacks.AttackType
                                .Additional,
                            request.AttackTags,
                            request.SkillCoefficient,
                            false,
                            0,
                            false),
                        context.RootActionId,
                        context.ActionId)
                };
            }
        }

        private sealed class DerivedBossDamageTriggerRule
            : ICombatTriggerRule
        {
            private readonly CombatActionIdSequence actionIds;

            public DerivedBossDamageTriggerRule(
                CombatActionIdSequence actionIds)
            {
                this.actionIds = actionIds
                    ?? throw new ArgumentNullException(nameof(actionIds));
            }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (!(context.CompletedAction is BossDamageAction source)
                    || source.SourceActionId.HasValue)
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    new BossDamageAction(
                        actionIds.Next(),
                        source.ContextRequest,
                        context.RootActionId,
                        context.ActionId)
                };
            }
        }
    }
}
