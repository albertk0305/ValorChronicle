using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleHudControllerTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private GameObject root;
        private BattleBoardController boardController;
        private BattleFlowController flowController;
        private BattleHudController hudController;
        private BattleCharacterSlotView occupiedSlot;
        private BattleCharacterSlotView emptySlot;
        private BattleCharacterSlotView[] characterSlots;
        private BattleFlowCombatBridge bridge;
        private BoardElementSpriteSet elementSpriteSet;
        private Sprite fireSprite;
        private Sprite waterSprite;
        private Sprite darkSprite;
        private Sprite genericStatusSprite;
        private Sprite defaultBossSprite;
        private Sprite exposedBossSprite;
        private Sprite genericIntentSprite;
        private Sprite fistIntentSprite;
        private Sprite eruptionIntentSprite;
        private Sprite compressionIntentSprite;
        private Sprite collapseIntentSprite;
        private BattleBossIntentSlotView[] bossIntentSlots;
        private BattleMatchEventSlotView[] matchEventSlots;
        private BattleStatusIconView[] bossStatusSlots;
        private BattleStatusIconView[] partyStatusSlots;
        private BattleSceneCombatBootstrap combatBootstrap;
        private DefinitionDatabase presentationDatabase;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("BattleHudControllerTests");
            root.SetActive(false);
            createdObjects.Add(root);

            boardController = root.AddComponent<BattleBoardController>();
            boardController.enabled = false;
            flowController = root.AddComponent<BattleFlowController>();
            SetField(flowController, "boardController", boardController);
            InvokePrivate(flowController, "OnEnable");

            hudController = root.AddComponent<BattleHudController>();
            SetField(
                hudController,
                "battleFlowController",
                flowController);
            SetField(hudController, "turnText", CreateText("TurnText"));
            SetField(hudController, "scoreText", CreateText("ScoreText"));
            SetField(hudController, "comboText", CreateText("ComboText"));
            var resultOverlay = new GameObject(
                "BattleResultOverlay",
                typeof(RectTransform));
            resultOverlay.transform.SetParent(root.transform, false);
            TMP_Text resultText = CreateText("ResultText");
            resultText.transform.SetParent(resultOverlay.transform, false);
            SetField(hudController, "resultOverlay", resultOverlay);
            SetField(hudController, "resultText", resultText);
            SetField(hudController, "resultOverlayDelaySeconds", 0f);
            SetField(hudController, "bossHpText", CreateText("BossHpText"));
            SetField(hudController, "partyHpText", CreateText("PartyHpText"));
            SetField(
                hudController,
                "bossHpSlider",
                CreateSlider("BossHpSlider"));
            SetField(
                hudController,
                "partyHpSlider",
                CreateSlider("PartyHpSlider"));
            SetField(
                hudController,
                "bossShieldImage",
                CreateImage("BossShield"));
            SetField(
                hudController,
                "partyShieldImage",
                CreateImage("PartyShield"));
            defaultBossSprite = CreateSprite("DefaultBossSprite");
            exposedBossSprite = CreateSprite("ExposedBossSprite");
            Image bossImage = CreateImage("BossImage");
            bossImage.sprite = defaultBossSprite;
            SetField(hudController, "bossImage", bossImage);
            combatBootstrap =
                root.AddComponent<BattleSceneCombatBootstrap>();
            SetField(
                hudController,
                "battleSceneCombatBootstrap",
                combatBootstrap);
            elementSpriteSet = ScriptableObject.CreateInstance<
                BoardElementSpriteSet>();
            createdObjects.Add(elementSpriteSet);
            fireSprite = CreateSprite("FireSprite");
            waterSprite = CreateSprite("WaterSprite");
            darkSprite = CreateSprite("DarkSprite");
            genericStatusSprite = CreateSprite("GenericStatusSprite");
            genericIntentSprite = CreateSprite("GenericIntentSprite");
            fistIntentSprite = CreateSprite("FistIntentSprite");
            eruptionIntentSprite = CreateSprite("EruptionIntentSprite");
            compressionIntentSprite = CreateSprite(
                "CompressionIntentSprite");
            collapseIntentSprite = CreateSprite("CollapseIntentSprite");
            SetField(elementSpriteSet, "fire", fireSprite);
            SetField(elementSpriteSet, "water", waterSprite);
            SetField(elementSpriteSet, "dark", darkSprite);
            SetField(hudController, "elementSpriteSet", elementSpriteSet);
            SetField(hudController, "resourceFallbackIcon", waterSprite);
            presentationDatabase = CreatePresentationDatabase(
                includeIcons: true,
                includeBossPresentation: true);
            SetProperty(
                combatBootstrap,
                "DefinitionDatabase",
                presentationDatabase);
            matchEventSlots = CreateMatchEventSlots(10);
            SetField(hudController, "matchEventSlots", matchEventSlots);
            bossIntentSlots = CreateBossIntentSlots(10);
            SetField(hudController, "bossIntentSlots", bossIntentSlots);
            bossStatusSlots = CreateStatusSlots("BossStatus", 16);
            partyStatusSlots = CreateStatusSlots("PartyStatus", 21);
            SetField(hudController, "bossStatusSlots", bossStatusSlots);
            SetField(hudController, "partyStatusSlots", partyStatusSlots);
            characterSlots = new BattleCharacterSlotView[5];
            for (int index = 0; index < characterSlots.Length; index++)
            {
                characterSlots[index] = CreateCharacterSlot(
                    $"CharacterSlot{index}");
            }

            occupiedSlot = characterSlots[0];
            emptySlot = characterSlots[1];
            SetField(
                hudController,
                "characterSlots",
                characterSlots);
        }

        [TearDown]
        public void TearDown()
        {
            if (flowController != null
                && GetField<bool>(flowController, "connectionEnabled"))
            {
                InvokePrivate(flowController, "OnDisable");
            }

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
        public void OnEnableBeforeBootstrapWaitsAndStartConnectsRuntime()
        {
            Assert.DoesNotThrow(() => InvokePrivate(
                hudController,
                "OnEnable"));
            Assert.That(hudController.IsRuntimeConnected, Is.False);
            Assert.That(hudController.ResultOverlay.activeSelf, Is.False);
            Assert.That(hudController.ResultText.text, Is.Empty);
            Assert.That(hudController.ScoreText.text, Is.Empty);
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);

            InitializeCombat();
            InvokePrivate(hudController, "Start");

            Assert.That(hudController.IsRuntimeConnected, Is.True);
            Assert.That(hudController.Coordinator,
                Is.SameAs(flowController.Coordinator));
            Assert.That(hudController.CombatBridge, Is.SameAs(bridge));
            Assert.That(hudController.ResultOverlay.activeSelf, Is.False);
            Assert.That(hudController.ResultText.text, Is.Empty);
            Assert.That(hudController.ScoreText.text, Is.EqualTo("Score --"));
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(emptySlot.ActiveButton.interactable, Is.False);
        }

        [TestCase(BattleResultKind.Victory)]
        [TestCase(BattleResultKind.Defeat)]
        [TestCase(BattleResultKind.TurnLimitReached)]
        [TestCase(BattleResultKind.Aborted)]
        public void ResultReachedDoesNotPresentUnpersistedResult(
            BattleResultKind result)
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(turnLimit: 1);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            Assert.That(hudController.ResultOverlay.activeSelf, Is.False);

            ReachResult(result);

            Assert.That(flowController.Context.Result, Is.EqualTo(result));
            Assert.That(hudController.ResultOverlay.activeSelf, Is.False);
            Assert.That(hudController.ResultText.text, Is.Empty);
            Assert.That(hudController.ScoreText.text, Is.EqualTo("Score --"));
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);

            InvokePrivate(hudController, "OnDisable");
            InvokePrivate(hudController, "OnEnable");
            Assert.That(hudController.ResultOverlay.activeSelf, Is.False);
            Assert.That(hudController.ResultText.text, Is.Empty);
            AssertSingleHudSubscription(flowController.Coordinator);
        }

        [TestCase(BattleResultKind.Victory)]
        [TestCase(BattleResultKind.Defeat)]
        [TestCase(BattleResultKind.TurnLimitReached)]
        public void NormalResultPreservesLastRenderedHudWhileInputsStayLocked(
            BattleResultKind result)
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(turnLimit: 1);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            if (result == BattleResultKind.TurnLimitReached)
            {
                CompletePlayerInputAndEnterBossActing();
            }

            bossIntentSlots[0].Render(genericIntentSprite, 2);
            matchEventSlots[0].Render(fireSprite, 3);
            hudController.ComboText.text = "Combo 7";
            string bossHpBefore = hudController.BossHpText.text;
            string partyHpBefore = hudController.PartyHpText.text;

            switch (result)
            {
                case BattleResultKind.Victory:
                    Assert.That(
                        flowController.Coordinator.NotifyBossDefeated(),
                        Is.True);
                    break;
                case BattleResultKind.Defeat:
                    Assert.That(
                        flowController.Coordinator.NotifyPartyIncapacitated(),
                        Is.True);
                    break;
                case BattleResultKind.TurnLimitReached:
                    Assert.That(
                        flowController.Coordinator.CompleteBossAction(),
                        Is.True);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }

            Assert.That(flowController.Context.Result, Is.EqualTo(result));
            Assert.That(bossIntentSlots[0].Root.activeSelf, Is.True);
            Assert.That(bossIntentSlots[0].RemainingTurnText.text,
                Is.EqualTo("2"));
            Assert.That(matchEventSlots[0].Root.activeSelf, Is.True);
            Assert.That(matchEventSlots[0].BlockCountText.text,
                Is.EqualTo("3"));
            Assert.That(hudController.ComboText.text, Is.EqualTo("Combo 7"));
            Assert.That(hudController.BossHpText.text,
                Is.EqualTo(bossHpBefore));
            Assert.That(hudController.PartyHpText.text,
                Is.EqualTo(partyHpBefore));
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(hudController.ResultOverlay.activeSelf, Is.False);

            InvokePrivate(hudController, "OnDisable");
            InvokePrivate(hudController, "OnEnable");
            Assert.That(bossIntentSlots[0].Root.activeSelf, Is.True);
            Assert.That(matchEventSlots[0].Root.activeSelf, Is.True);
            Assert.That(hudController.ComboText.text, Is.EqualTo("Combo 7"));
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(hudController.ResultOverlay.activeSelf, Is.False);
        }

        [Test]
        public void AbortedKeepsExistingTerminalHudRefreshBehavior()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            bossIntentSlots[0].Render(genericIntentSprite, 2);
            matchEventSlots[0].Render(fireSprite, 3);

            Assert.That(flowController.Coordinator.AbortBattle(), Is.True);

            Assert.That(bossIntentSlots[0].Root.activeSelf, Is.False);
            AssertAllMatchSlotsHidden();
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(hudController.ResultOverlay.activeSelf, Is.False);
        }

        [Test]
        public void VictoryPreservesIntentAndPendingQueuePresentation()
        {
            InvokePrivate(hudController, "OnEnable");
            BattleSceneCombatComposition composition =
                CreateProductionComposition();
            SetProperty(combatBootstrap, "CombatComposition", composition);
            flowController.Initialize(
                composition.FlowSetup,
                coordinator => composition.CreateBridge(coordinator));
            bridge = composition.Bridge;
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            Assert.That(bossIntentSlots[0].Root.activeSelf, Is.True);
            EnterMatchResolving(3);
            Assert.That(matchEventSlots[0].Root.activeSelf, Is.True);

            IReadOnlyList<MatchEvent> pendingBefore =
                flowController.Coordinator.GetPendingMatchEvents();
            Assert.That(flowController.Coordinator.NotifyBossDefeated(),
                Is.True);

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(bossIntentSlots[0].Root.activeSelf, Is.True);
            Assert.That(matchEventSlots[0].Root.activeSelf, Is.True);
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.Zero);
            Assert.That(flowController.MatchQueuePresentation.Count, Is.Zero);
            Assert.That(pendingBefore, Has.Count.EqualTo(3));
            Assert.That(hudController.ResultOverlay.activeSelf, Is.False);
        }

        [Test]
        public void DisableEnableDoesNotDuplicateEventsOrButtonListener()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            InvokePrivate(hudController, "Start");
            InvokePrivate(hudController, "OnEnable");
            InvokePrivate(hudController, "Start");

            AssertSingleHudSubscription(flowController.Coordinator);
            Assert.That(
                CountTargetSubscribers(
                    bridge,
                    "CombatActionsApplied",
                    hudController),
                Is.EqualTo(1));
            Assert.That(
                CountTargetSubscribers(
                    bridge,
                    "CombatActionStepApplied",
                    hudController),
                Is.EqualTo(1));
            Assert.That(occupiedSlot.HasActiveListener, Is.True);
            Assert.That(emptySlot.HasActiveListener, Is.True);

            InvokePrivate(hudController, "OnDisable");

            AssertNoHudSubscription(flowController.Coordinator);
            Assert.That(
                CountTargetSubscribers(
                    bridge,
                    "CombatActionsApplied",
                    hudController),
                Is.Zero);
            Assert.That(
                CountTargetSubscribers(
                    bridge,
                    "CombatActionStepApplied",
                    hudController),
                Is.Zero);
            Assert.That(occupiedSlot.HasActiveListener, Is.False);
            Assert.That(emptySlot.HasActiveListener, Is.False);

            InvokePrivate(hudController, "OnEnable");

            AssertSingleHudSubscription(flowController.Coordinator);
            Assert.That(
                CountTargetSubscribers(
                    bridge,
                    "CombatActionsApplied",
                    hudController),
                Is.EqualTo(1));
            Assert.That(
                CountTargetSubscribers(
                    bridge,
                    "CombatActionStepApplied",
                    hudController),
                Is.EqualTo(1));
            Assert.That(occupiedSlot.HasActiveListener, Is.True);
        }

        [Test]
        public void DestroyReleasesPreviousRuntimeDelegates()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            InvokePrivate(hudController, "Start");
            BattleFlowCoordinator coordinator =
                flowController.Coordinator;

            InvokePrivate(hudController, "OnDestroy");

            AssertNoHudSubscription(coordinator);
            Assert.That(
                CountTargetSubscribers(
                    bridge,
                    "CombatActionsApplied",
                    hudController),
                Is.Zero);
            Assert.That(
                CountTargetSubscribers(
                    bridge,
                    "CombatActionStepApplied",
                    hudController),
                Is.Zero);
            Assert.That(occupiedSlot.HasActiveListener, Is.False);
        }

        [TestCase(1)]
        [TestCase(10)]
        public void PendingMatchEventsRenderAvailableSlotsInFifoOrder(
            int eventCount)
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            Assert.That(hudController.ComboText.text, Is.EqualTo("Combo 0"));
            AssertAllMatchSlotsHidden();

            EnterMatchResolving(eventCount);
            IReadOnlyList<MatchEvent> beforeRefresh =
                flowController.Coordinator.GetPendingMatchEvents();

            hudController.RefreshInitialSnapshot();

            IReadOnlyList<MatchEvent> afterRefresh =
                flowController.Coordinator.GetPendingMatchEvents();
            Assert.That(afterRefresh.Count, Is.EqualTo(eventCount));
            for (int index = 0; index < eventCount; index++)
            {
                Assert.That(afterRefresh[index], Is.SameAs(beforeRefresh[index]));
                AssertMatchSlot(index, afterRefresh[index]);
            }

            for (int index = eventCount;
                index < matchEventSlots.Length;
                index++)
            {
                Assert.That(matchEventSlots[index].Root.activeSelf, Is.False);
            }

            Assert.That(hudController.ComboText.text, Is.EqualTo("Combo 0"));
            Assert.That(
                flowController.Coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution execution),
                Is.True);
            Assert.That(execution.FinalComboCount, Is.EqualTo(eventCount));
            Assert.That(hudController.ComboText.text,
                Is.EqualTo($"Combo {eventCount}"));
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.EqualTo(eventCount - 1));
        }

        [Test]
        public void BoardClearRendersPresentationQueueBeforeAuthoritativeQueue()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
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
            BoardSwapActionResult result =
                CreateResolvedBoardActionResult(cascade);
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateInternal<BoardActionExecution>(1L, result));

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BoardResolving));
            Assert.That(flowController.Coordinator.GetPendingMatchEvents(),
                Is.Empty);
            InvokePrivate(
                flowController,
                "HandleCascadeStepPresented",
                CreateInternal<BoardCascadeStepPresentation>(
                    1L,
                    0,
                    cascade.Steps[0]));

            Assert.That(flowController.Coordinator.GetPendingMatchEvents(),
                Is.Empty);
            Assert.That(matchEventSlots[0].Root.activeSelf, Is.True);
            Assert.That(matchEventSlots[0].ElementImage.sprite,
                Is.SameAs(waterSprite));
            Assert.That(matchEventSlots[0].BlockCountText.text,
                Is.EqualTo("3"));
            Assert.That(matchEventSlots[1].Root.activeSelf, Is.True);
            Assert.That(matchEventSlots[1].ElementImage.sprite,
                Is.SameAs(fireSprite));
            Assert.That(matchEventSlots[1].BlockCountText.text,
                Is.EqualTo("4"));
            Assert.That(matchEventSlots[2].Root.activeSelf, Is.False);
        }

        [Test]
        public void ThirteenPendingEventsRenderTenAndSlideAfterEachDequeue()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            EnterMatchResolving(13);
            IReadOnlyList<MatchEvent> initial =
                flowController.Coordinator.GetPendingMatchEvents();

            Assert.That(initial, Has.Count.EqualTo(13));
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.EqualTo(13));
            for (int index = 0; index < matchEventSlots.Length; index++)
            {
                AssertMatchSlot(index, initial[index]);
            }

            Assert.That(matchEventSlots[0].ElementImage.sprite,
                Is.SameAs(waterSprite));
            Assert.That(matchEventSlots[0].BlockCountText.text,
                Is.EqualTo("3"));
            Assert.That(matchEventSlots[1].ElementImage.sprite,
                Is.SameAs(waterSprite));
            Assert.That(matchEventSlots[1].BlockCountText.text,
                Is.EqualTo("4"));
            Assert.That(matchEventSlots[2].ElementImage.sprite,
                Is.SameAs(darkSprite));
            Assert.That(matchEventSlots[2].BlockCountText.text,
                Is.EqualTo("6"));

            Assert.That(
                flowController.Coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution firstExecution),
                Is.True);
            Assert.That(firstExecution.MatchEvent, Is.SameAs(initial[0]));
            Assert.That(hudController.ComboText.text, Is.EqualTo("Combo 13"));
            IReadOnlyList<MatchEvent> afterFirst =
                flowController.Coordinator.GetPendingMatchEvents();
            Assert.That(afterFirst, Has.Count.EqualTo(12));
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.EqualTo(12));
            for (int index = 0; index < matchEventSlots.Length; index++)
            {
                Assert.That(afterFirst[index], Is.SameAs(initial[index + 1]));
                AssertMatchSlot(index, afterFirst[index]);
            }

            InvokePrivate(hudController, "OnDisable");
            InvokePrivate(hudController, "OnEnable");
            Assert.That(hudController.ComboText.text, Is.EqualTo("Combo 13"));
            for (int index = 0; index < matchEventSlots.Length; index++)
            {
                AssertMatchSlot(index, afterFirst[index]);
            }

            AssertSingleHudSubscription(flowController.Coordinator);

            Assert.That(
                flowController.Coordinator.CompleteCurrentMatchEvent(
                    firstExecution.ExecutionId),
                Is.True);
            Assert.That(
                flowController.Coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution secondExecution),
                Is.True);
            Assert.That(secondExecution.MatchEvent, Is.SameAs(initial[1]));
            IReadOnlyList<MatchEvent> afterSecond =
                flowController.Coordinator.GetPendingMatchEvents();
            Assert.That(afterSecond, Has.Count.EqualTo(11));
            Assert.That(flowController.MatchQueuePresentation.Count,
                Is.EqualTo(11));
            for (int index = 0; index < matchEventSlots.Length; index++)
            {
                Assert.That(afterSecond[index], Is.SameAs(initial[index + 2]));
                AssertMatchSlot(index, afterSecond[index]);
            }

            Assert.That(hudController.ComboText.text, Is.EqualTo("Combo 13"));
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.EqualTo(11));
        }

        [Test]
        public void ComboPersistsThroughResolutionAndResetsAtNextTurnStart()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            EnterMatchResolving(12);

            while (flowController.Context.Phase
                == BattlePhase.MatchEventResolving)
            {
                if (flowController.Coordinator.CurrentMatchEventExecution
                    != null)
                {
                    Assert.That(
                        flowController.Coordinator.CompleteCurrentMatchEvent(
                            flowController.Coordinator
                                .CurrentMatchEventExecution.ExecutionId),
                        Is.True);
                    continue;
                }

                flowController.Coordinator.TryBeginNextMatchEvent(out _);
                Assert.That(hudController.ComboText.text,
                    Is.EqualTo("Combo 12"));
            }

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(hudController.ComboText.text, Is.EqualTo("Combo 12"));
            AssertAllMatchSlotsHidden();

            Assert.That(flowController.Coordinator.CompleteBossAction(),
                Is.True);

            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(2));
            Assert.That(hudController.ComboText.text, Is.EqualTo("Combo 0"));
            AssertAllMatchSlotsHidden();
        }

        [Test]
        public void MissingElementSpriteHidesOnlyAffectedSlotSafely()
        {
            SetField(elementSpriteSet, "dark", null);
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            EnterMatchResolving(3);

            Assert.DoesNotThrow(() => hudController.RefreshInitialSnapshot());
            Assert.That(matchEventSlots[0].Root.activeSelf, Is.True);
            Assert.That(matchEventSlots[0].ElementImage.sprite,
                Is.SameAs(waterSprite));
            Assert.That(matchEventSlots[0].BlockCountText.text,
                Is.EqualTo("3"));
            Assert.That(matchEventSlots[1].Root.activeSelf, Is.True);
            Assert.That(matchEventSlots[1].ElementImage.sprite,
                Is.SameAs(waterSprite));
            Assert.That(matchEventSlots[1].BlockCountText.text,
                Is.EqualTo("4"));
            Assert.That(matchEventSlots[2].Root.activeSelf, Is.False);
            Assert.That(matchEventSlots[2].BlockCountText.text, Is.Empty);
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.EqualTo(3));
        }

        [Test]
        public void ActiveButtonsUseControllerPathAndSkipEmptyPartySlot()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            root.SetActive(true);

            Assert.DoesNotThrow(() => emptySlot.ActiveButton.onClick.Invoke());
            Assert.That(
                flowController.Context.ActiveAbilities[0].UsedThisTurn,
                Is.False);
            Assert.That(bridge.Party.Characters[0].Effects.Count, Is.Zero);

            occupiedSlot.ActiveButton.onClick.Invoke();

            Assert.That(
                flowController.Context.ActiveAbilities[0].UsedThisTurn,
                Is.True);
            Assert.That(bridge.Party.Characters[0].Effects.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void InitialCharacterSnapshotRendersElementAndEmptySlots()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            Assert.That(occupiedSlot.CharacterImage.enabled, Is.True);
            Assert.That(occupiedSlot.TypeImage.gameObject.activeSelf, Is.True);
            Assert.That(occupiedSlot.TypeImage.sprite, Is.SameAs(waterSprite));
            Assert.That(occupiedSlot.CooldownText.gameObject.activeSelf,
                Is.False);
            Assert.That(occupiedSlot.CooldownOverlay.gameObject.activeSelf,
                Is.False);
            Assert.That(occupiedSlot.ActiveButton.transition,
                Is.EqualTo(Selectable.Transition.None));
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.True);
            for (int index = 1; index < characterSlots.Length; index++)
            {
                Assert.That(characterSlots[index].Root.activeSelf, Is.True);
                Assert.That(characterSlots[index].CharacterImage.enabled,
                    Is.False);
                Assert.That(characterSlots[index].TypeImage.gameObject.activeSelf,
                    Is.False);
                Assert.That(characterSlots[index].CooldownText.gameObject.activeSelf,
                    Is.False);
                Assert.That(characterSlots[index].CooldownOverlay.gameObject
                    .activeSelf, Is.False);
                Assert.That(characterSlots[index].ActiveButton.interactable,
                    Is.False);
            }
        }

        [Test]
        public void SlotUsesBindingActiveIndexInsteadOfPartySlotIndex()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(partySlotIndex: 2, activeAbilityIndex: 1);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            root.SetActive(true);

            Assert.That(characterSlots[0].ActiveButton.interactable, Is.False);
            Assert.That(characterSlots[2].ActiveButton.interactable, Is.True);

            characterSlots[2].ActiveButton.onClick.Invoke();

            Assert.That(flowController.Context.ActiveAbilities[0].UsedThisTurn,
                Is.False);
            Assert.That(flowController.Context.ActiveAbilities[1].UsedThisTurn,
                Is.True);
            Assert.That(bridge.Party.Characters[0].Effects.Count,
                Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(4)]
        public void ProjectileAnchorsUseExactOccupiedSlotAndBossImage(
            int partySlotIndex)
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(partySlotIndex: partySlotIndex);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            root.SetActive(true);
            Assert.That(
                characterSlots[partySlotIndex].CharacterImage.gameObject
                    .activeInHierarchy,
                Is.True);
            Assert.That(hudController.BossImage.gameObject.activeInHierarchy,
                Is.True);

            bool resolved = hudController.TryResolvePlayerProjectileAnchors(
                partySlotIndex,
                MareaBluefangRules.CharacterId,
                out RectTransform source,
                out RectTransform target);

            Assert.That(resolved, Is.True);
            Assert.That(source,
                Is.SameAs(characterSlots[partySlotIndex]
                    .CharacterImage.rectTransform));
            Assert.That(target,
                Is.SameAs(hudController.BossImage.rectTransform));
            Assert.That(hudController.TryResolvePlayerProjectileAnchors(
                partySlotIndex,
                "wrong_character",
                out _,
                out _), Is.False);

            Assert.That(hudController.TryResolveBossProjectileAnchors(
                out RectTransform bossSource,
                out IReadOnlyList<RectTransform> partyTargets), Is.True);
            Assert.That(bossSource,
                Is.SameAs(hudController.BossImage.rectTransform));
            Assert.That(partyTargets.Count, Is.EqualTo(1));
            Assert.That(partyTargets[0], Is.SameAs(source));
        }

        [TestCase(new[] { 0 })]
        [TestCase(new[] { 0, 4 })]
        [TestCase(new[] { 0, 1, 2, 3, 4 })]
        [TestCase(new[] { 0, 2, 4 })]
        public void BossProjectileTargetsUseActualOccupiedPortraits(
            int[] partySlotIndices)
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombatWithPartySlots(partySlotIndices);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            root.SetActive(true);
            for (int index = 0; index < characterSlots.Length; index++)
            {
                characterSlots[index].CharacterImage.rectTransform
                    .anchoredPosition = new Vector2(
                        -430f + (index * 173f),
                        25f + (index * 11f));
            }

            Assert.That(hudController.TryResolveBossProjectileAnchors(
                out RectTransform source,
                out IReadOnlyList<RectTransform> targets), Is.True);

            Assert.That(source,
                Is.SameAs(hudController.BossImage.rectTransform));
            Assert.That(targets.Count,
                Is.EqualTo(partySlotIndices.Length));
            for (int index = 0; index < partySlotIndices.Length; index++)
            {
                Assert.That(targets[index], Is.SameAs(
                    characterSlots[partySlotIndices[index]]
                        .CharacterImage.rectTransform));
            }

            AssertBossProjectilePositionsMatchCenters(source, targets);
        }

        [Test]
        public void BossProjectileUsesPartyHpFallbackWithoutVisiblePortrait()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombatWithPartySlots(new[] { 0 });
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            root.SetActive(true);
            Assert.That(bridge.Party.Characters.Count, Is.EqualTo(1));
            characterSlots[0].CharacterImage.enabled = false;
            Assert.That(characterSlots[0].CharacterImage.enabled, Is.False);

            Assert.That(hudController.TryResolveBossProjectileAnchors(
                out RectTransform source,
                out IReadOnlyList<RectTransform> targets), Is.True);

            Assert.That(source,
                Is.SameAs(hudController.BossImage.rectTransform));
            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets[0], Is.SameAs(
                hudController.PartyHpSlider.transform as RectTransform));
            AssertBossProjectilePositionsMatchCenters(source, targets);
        }

        [Test]
        public void ActiveUseShowsCooldownAndTurnProgressRefreshesIt()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            root.SetActive(true);
            Color baseColor = occupiedSlot.CharacterImage.color;

            occupiedSlot.ActiveButton.onClick.Invoke();

            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(occupiedSlot.CooldownText.gameObject.activeSelf, Is.True);
            Assert.That(occupiedSlot.CooldownText.text,
                Is.EqualTo("8"));
            Assert.That(occupiedSlot.CooldownOverlay.gameObject.activeSelf,
                Is.True);
            Assert.That(occupiedSlot.CooldownOverlay.raycastTarget, Is.False);
            Assert.That(occupiedSlot.CharacterImage.color, Is.EqualTo(baseColor));

            CompletePlayerInputAndEnterBossActing();
            Assert.That(bridge.ResolveBossAction(1), Is.True);

            Assert.That(occupiedSlot.CooldownText.text,
                Is.EqualTo("7"));
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
        }

        [Test]
        public void CooldownZeroHidesTextAndRestoresInteractable()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(turnLimit: 12);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            root.SetActive(true);
            occupiedSlot.ActiveButton.onClick.Invoke();

            for (int index = 0;
                index < 8;
                index++)
            {
                CompletePlayerInputAndEnterBossActing();
                Assert.That(
                    bridge.ResolveBossAction(
                        flowController.Context.CurrentTurn),
                    Is.True);
            }

            Assert.That(flowController.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.Zero);
            Assert.That(occupiedSlot.CooldownText.gameObject.activeSelf,
                Is.False);
            Assert.That(occupiedSlot.CooldownText.text, Is.Empty);
            Assert.That(occupiedSlot.CooldownOverlay.gameObject.activeSelf,
                Is.False);
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.True);
        }

        [Test]
        public void ActiveButtonTracksPhaseAndBattleResult()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.True);
            Assert.That(occupiedSlot.CooldownOverlay.gameObject.activeSelf,
                Is.False);
            Color portraitColor = occupiedSlot.CharacterImage.color;

            Assert.That(flowController.Coordinator.TryBeginBoardResolution(),
                Is.True);
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(occupiedSlot.CooldownOverlay.gameObject.activeSelf,
                Is.False);
            Assert.That(occupiedSlot.CharacterImage.color,
                Is.EqualTo(portraitColor));
            Assert.That(
                flowController.Coordinator.NotifyBoardActionResolved(
                    CreateSingleFireCascade(),
                    true),
                Is.True);
            flowController.Coordinator.ExecuteRemainingMatchEvents();
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(flowController.Coordinator.AbortBattle(), Is.True);
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);

            bool usedBefore = flowController.Context.ActiveAbilities[0]
                .UsedThisTurn;
            occupiedSlot.ActiveButton.onClick.Invoke();
            Assert.That(flowController.Context.ActiveAbilities[0].UsedThisTurn,
                Is.EqualTo(usedBefore));
        }

        [Test]
        public void MissingBindingDisablesOccupiedSlot()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(includeActiveBinding: false);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            Assert.That(occupiedSlot.CharacterImage.enabled, Is.True);
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(occupiedSlot.CooldownText.gameObject.activeSelf,
                Is.False);
        }

        [Test]
        public void MultipleBindingsForOneSlotAreNotChosenArbitrarily()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(ambiguousActiveBindings: true);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            Assert.That(
                bridge.TryGetSingleActiveBinding(
                    0,
                    MareaBluefangRules.CharacterId,
                    out ActiveAbilityBinding binding),
                Is.False);
            Assert.That(binding, Is.Null);
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            occupiedSlot.ActiveButton.onClick.Invoke();
            Assert.That(flowController.Context.ActiveAbilities[0].UsedThisTurn,
                Is.False);
            Assert.That(flowController.Context.ActiveAbilities[1].UsedThisTurn,
                Is.False);
        }

        [Test]
        public void EffectProjectionUsesCreationOrderAndBadgeRules()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            bridge.Boss.Effects.ApplyEffect(CreateEffect(
                101,
                "effect_indefinite",
                remainingTurns: null,
                EffectStackPolicy.Unique));
            bridge.Boss.Effects.ApplyEffect(CreateEffect(
                102,
                "effect_finite",
                remainingTurns: 3,
                EffectStackPolicy.RefreshDuration));
            bridge.Boss.Effects.ApplyEffect(CreateEffect(
                103,
                "effect_stack",
                remainingTurns: null,
                EffectStackPolicy.StackCount,
                maxStackCount: 5));
            bridge.Boss.Effects.ApplyEffect(CreateEffect(
                104,
                "effect_stack",
                remainingTurns: null,
                EffectStackPolicy.StackCount,
                maxStackCount: 5));
            InvokePrivate(hudController, "Start");

            Assert.That(bossStatusSlots[0].Root.activeSelf, Is.True);
            Assert.That(bossStatusSlots[0].ValueText.gameObject.activeSelf,
                Is.False);
            Assert.That(bossStatusSlots[0].IconImage.sprite,
                Is.SameAs(genericStatusSprite));
            Assert.That(bossStatusSlots[1].ValueText.text, Is.EqualTo("3"));
            Assert.That(bossStatusSlots[2].ValueText.text, Is.EqualTo("2"));
            Assert.That(bossStatusSlots[3].Root.activeSelf, Is.False);
        }

        [Test]
        public void WaterElementUsesActualResourceAmountWithoutMutatingIt()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            ResourceState water = bridge.Boss.Resources.Get(
                WaterElementResource.Id);
            InvokePrivate(hudController, "Start");
            Assert.That(bossStatusSlots[0].Root.activeSelf, Is.False);

            water.Add(3);
            hudController.RefreshInitialSnapshot();

            Assert.That(water.CurrentAmount, Is.EqualTo(3));
            Assert.That(bossStatusSlots[0].Root.activeSelf, Is.True);
            Assert.That(bossStatusSlots[0].IconImage.sprite,
                Is.SameAs(fireSprite));
            Assert.That(bossStatusSlots[0].ValueText.text, Is.EqualTo("3"));

            water.ConsumeAll();
            hudController.RefreshInitialSnapshot();
            Assert.That(water.CurrentAmount, Is.Zero);
            Assert.That(bossStatusSlots[0].Root.activeSelf, Is.False);

            InvokePrivate(hudController, "OnDisable");
            water.Add(2);
            InvokePrivate(hudController, "OnEnable");
            Assert.That(bossStatusSlots[0].ValueText.text, Is.EqualTo("2"));
            AssertSingleHudSubscription(flowController.Coordinator);
        }

        [Test]
        public void NullMetadataIconsUseGenericPresentationFallbacks()
        {
            DefinitionDatabase nullIconDatabase =
                CreatePresentationDatabase(
                    includeIcons: false,
                    includeBossPresentation: true);
            SetProperty(
                combatBootstrap,
                "DefinitionDatabase",
                nullIconDatabase);
            InvokePrivate(hudController, "OnEnable");
            BattleSceneCombatComposition composition =
                CreateProductionComposition();
            SetProperty(
                combatBootstrap,
                "CombatComposition",
                composition);
            flowController.Initialize(
                composition.FlowSetup,
                coordinator => composition.CreateBridge(coordinator));
            bridge = composition.Bridge;
            composition.Boss.Resources.Get(
                WaterElementResource.Id).Add(2);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            Assert.That(bossIntentSlots[0].ActionImage.sprite,
                Is.SameAs(genericIntentSprite));
            Assert.That(bossStatusSlots[0].IconImage.sprite,
                Is.SameAs(genericStatusSprite));
            Assert.That(bossStatusSlots[1].IconImage.sprite,
                Is.SameAs(waterSprite));
            Assert.That(bossStatusSlots[1].ValueText.text, Is.EqualTo("2"));
        }

        [Test]
        public void MissingPresentationMetadataUsesExistingHudFallbacks()
        {
            DefinitionDatabase emptyDatabase =
                ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(emptyDatabase);
            emptyDatabase.Initialize();
            SetProperty(
                combatBootstrap,
                "DefinitionDatabase",
                emptyDatabase);
            InvokePrivate(hudController, "OnEnable");
            BattleSceneCombatComposition composition =
                CreateProductionComposition();
            SetProperty(
                combatBootstrap,
                "CombatComposition",
                composition);
            flowController.Initialize(
                composition.FlowSetup,
                coordinator => composition.CreateBridge(coordinator));
            bridge = composition.Bridge;
            composition.Boss.Resources.Get(
                WaterElementResource.Id).Add(3);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            Assert.That(bossIntentSlots[0].ActionImage.sprite,
                Is.SameAs(genericIntentSprite));
            Assert.That(bossStatusSlots[0].IconImage.sprite,
                Is.SameAs(genericStatusSprite));
            Assert.That(bossStatusSlots[1].IconImage.sprite,
                Is.SameAs(waterSprite));
            Assert.That(hudController.BossImage.sprite,
                Is.SameAs(defaultBossSprite));
        }

        [Test]
        public void MareaMatchStepRefreshesWaterBeforeBatchCompletion()
        {
            var mareaProvider = new MareaBluefangMatchActionProvider(
                MareaBluefangTestConfig.Create());
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(
                matchActions: context =>
                    mareaProvider.CreateRootActions(context));
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            MatchEventExecution execution = BeginSingleWaterMatch();
            int batchCount = 0;
            bridge.CombatActionsApplied += () => batchCount++;
            Assert.That(bridge.TryBeginMatchEventCombat(
                execution,
                out bool hasMatchingCharacter), Is.True);
            Assert.That(hasMatchingCharacter, Is.True);
            Assert.That(bridge.TryGetNextMatchCombatAction(
                out CharacterBattleState damageCharacter,
                out CombatAction damageAction), Is.True);
            Assert.That(damageCharacter.CharacterId,
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(damageAction, Is.TypeOf<DamageAction>());
            Assert.That(bridge.TryApplyNextMatchCombatAction(out _), Is.True);

            ResourceState water = bridge.Boss.Resources.Get(
                WaterElementResource.Id);
            Assert.That(water.CurrentAmount, Is.Zero);
            Assert.That(batchCount, Is.Zero);

            Assert.That(bridge.TryGetNextMatchCombatAction(
                out CharacterBattleState resourceCharacter,
                out CombatAction resourceAction), Is.True);
            Assert.That(resourceCharacter, Is.SameAs(damageCharacter));
            Assert.That(resourceAction, Is.TypeOf<AddResourceAction>());
            Assert.That(bridge.TryApplyNextMatchCombatAction(out _), Is.True);

            Assert.That(water.CurrentAmount, Is.EqualTo(1));
            Assert.That(batchCount, Is.Zero);
            Assert.That(bossStatusSlots[0].Root.activeSelf, Is.True);
            Assert.That(bossStatusSlots[0].IconImage.sprite,
                Is.SameAs(fireSprite));
            Assert.That(bossStatusSlots[0].ValueText.text, Is.EqualTo("1"));
            Assert.That(bridge.TryCompleteMatchEventCombat(execution),
                Is.True);
            Assert.That(batchCount, Is.EqualTo(1));
        }

        [Test]
        public void BossDamageStepRefreshesPartyBeforeBatchCompletion()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(
                partyMaxHp: 900,
                bossAttack: 100d,
                bossActions: context => new CombatAction[]
                {
                    new BossDamageAction(
                        context.ActionIds.Next(),
                        new BossDamageContextBuildRequest(
                            context.Boss,
                            context.Party,
                            1d,
                            ValorChronicle.Battle.Combat.Attacks.AttackTag
                                .None))
                });
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            bridge.Party.Shields.Add(new ShieldInstance(
                901, "hero", 50, 1, 5, 1));
            hudController.RefreshInitialSnapshot();
            Assert.That(hudController.PartyShieldImage.gameObject.activeSelf,
                Is.True);
            CompletePlayerInputAndEnterBossActing();
            int batchCount = 0;
            bridge.CombatActionsApplied += () => batchCount++;
            string initialText = hudController.PartyHpText.text;

            Assert.That(bridge.TryBeginBossActionCombat(1), Is.True);
            Assert.That(bridge.TryApplyNextBossCombatAction(out _), Is.True);

            Assert.That(batchCount, Is.Zero);
            Assert.That(bridge.Party.CurrentHp, Is.LessThan(900));
            Assert.That(bridge.Party.Shields.TotalShield, Is.Zero);
            Assert.That(hudController.PartyShieldImage.gameObject.activeSelf,
                Is.False);
            Assert.That(hudController.PartyHpText.text,
                Is.Not.EqualTo(initialText));
            Assert.That(hudController.PartyHpText.text,
                Does.StartWith(bridge.Party.CurrentHp.ToString()));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(bridge.LastBossExecutionResult, Is.Null);
        }

        [Test]
        public void MareaFiveMatchConsumptionRemovesWaterElementStatus()
        {
            var mareaProvider = new MareaBluefangMatchActionProvider(
                MareaBluefangTestConfig.Create());
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(
                bossMaxHp: 100000,
                matchActions: context =>
                    mareaProvider.CreateRootActions(context));
            ResourceState water = bridge.Boss.Resources.Get(
                WaterElementResource.Id);
            water.Add(5);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            Assert.That(bossStatusSlots[0].ValueText.text, Is.EqualTo("5"));

            MatchEventExecution execution = BeginSingleWaterMatch(5);
            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(water.CurrentAmount, Is.Zero);
            Assert.That(bossStatusSlots[0].Root.activeSelf, Is.False);
        }

        [Test]
        public void FiniteBossAndPartyEffectsExpireAtPhaseBoundary()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            bridge.Boss.Effects.ApplyEffect(CreateEffect(
                150,
                "effect_boss_one_turn",
                remainingTurns: 1,
                EffectStackPolicy.RefreshDuration));
            bridge.Party.Effects.ApplyEffect(CreateEffect(
                151,
                "effect_party_one_turn",
                remainingTurns: 1,
                EffectStackPolicy.RefreshDuration));
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            Assert.That(bossStatusSlots[0].Root.activeSelf, Is.True);
            Assert.That(partyStatusSlots[0].Root.activeSelf, Is.True);

            CompletePlayerInputAndEnterBossActing();
            Assert.That(bridge.ResolveBossAction(1), Is.True);

            Assert.That(bridge.Boss.Effects.Count, Is.Zero);
            Assert.That(bridge.Party.Effects.Count, Is.Zero);
            Assert.That(bossStatusSlots[0].Root.activeSelf, Is.False);
            Assert.That(partyStatusSlots[0].Root.activeSelf, Is.False);
        }

        [Test]
        public void SuccessfulMareaActiveRendersAndExpiresCharacterEffect()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            root.SetActive(true);

            occupiedSlot.ActiveButton.onClick.Invoke();

            BattleStatusIconView status = occupiedSlot.GetStatusSlot(0);
            Assert.That(status.Root.activeSelf, Is.True);
            Assert.That(status.IconImage.sprite, Is.SameAs(darkSprite));
            Assert.That(status.ValueText.text,
                Is.EqualTo("3"));

            for (int index = 0;
                index < 3;
                index++)
            {
                CompletePlayerInputAndEnterBossActing();
                Assert.That(bridge.ResolveBossAction(
                    flowController.Context.CurrentTurn), Is.True);
            }

            Assert.That(bridge.Party.Characters[0].Effects.Count, Is.Zero);
            Assert.That(status.Root.activeSelf, Is.False);
            Assert.That(emptySlot.GetStatusSlot(0).Root.activeSelf, Is.False);
        }

        [Test]
        public void StatusCapacityNeverRemovesRuntimeEffects()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            for (int index = 0; index < 22; index++)
            {
                bridge.Party.Effects.ApplyEffect(CreateEffect(
                    200 + index,
                    $"effect_party_{index}",
                    remainingTurns: index + 1,
                    EffectStackPolicy.SeparateInstance));
            }

            for (int index = 0; index < 5; index++)
            {
                bridge.Party.Characters[0].Effects.ApplyEffect(CreateEffect(
                    300 + index,
                    $"effect_character_{index}",
                    remainingTurns: index + 1,
                    EffectStackPolicy.SeparateInstance));
            }

            InvokePrivate(hudController, "Start");

            Assert.That(bridge.Party.Effects.Count, Is.EqualTo(22));
            Assert.That(bridge.Party.Characters[0].Effects.Count,
                Is.EqualTo(5));
            Assert.That(partyStatusSlots[20].Root.activeSelf, Is.True);
            Assert.That(partyStatusSlots[20].ValueText.text,
                Is.EqualTo("21"));
            Assert.That(occupiedSlot.GetStatusSlot(3).Root.activeSelf,
                Is.True);
            Assert.That(occupiedSlot.GetStatusSlot(3).ValueText.text,
                Is.EqualTo("4"));
        }

        [Test]
        public void KragmorDefenseStateSelectsBossImageWithSafeFallback()
        {
            InvokePrivate(hudController, "OnEnable");
            BattleSceneCombatComposition composition =
                CreateProductionComposition();
            SetProperty(
                combatBootstrap,
                "CombatComposition",
                composition);
            flowController.Initialize(
                composition.FlowSetup,
                coordinator => composition.CreateBridge(coordinator));
            bridge = composition.Bridge;
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            Assert.That(composition.KragmorRuntimeState.CurrentDefenseState,
                Is.EqualTo(KragmorDefenseState.VolcanicCarapace));
            Assert.That(hudController.BossImage.sprite,
                Is.SameAs(defaultBossSprite));
            Assert.That(bossStatusSlots[0].Root.activeSelf, Is.True);
            Assert.That(bossStatusSlots[0].ValueText.gameObject.activeSelf,
                Is.False);

            composition.KragmorRuntimeState.CommitAction(
                KragmorDefenseState.VolcanicCarapace);
            composition.KragmorRuntimeState.CommitAction(
                KragmorDefenseState.VolcanicCarapace);
            composition.KragmorRuntimeState.CommitAction(
                KragmorDefenseState.CoreCompression);
            KragmorDefenseState stateBeforeRefresh =
                composition.KragmorRuntimeState.CurrentDefenseState;
            hudController.RefreshInitialSnapshot();
            Assert.That(composition.KragmorRuntimeState.CurrentDefenseState,
                Is.EqualTo(stateBeforeRefresh));
            Assert.That(hudController.BossImage.sprite,
                Is.SameAs(defaultBossSprite));

            composition.KragmorRuntimeState.CommitAction(
                KragmorDefenseState.CoreExposure);
            hudController.RefreshInitialSnapshot();
            Assert.That(hudController.BossImage.sprite,
                Is.SameAs(exposedBossSprite));

            composition.KragmorRuntimeState.CommitAction(
                KragmorDefenseState.VolcanicCarapace);
            hudController.RefreshInitialSnapshot();
            Assert.That(hudController.BossImage.sprite,
                Is.SameAs(defaultBossSprite));
        }

        [Test]
        public void BossImageChangesOnlyAfterDefenseTransitionCommit()
        {
            InvokePrivate(hudController, "OnEnable");
            BattleSceneCombatComposition composition =
                CreateProductionComposition(bossAttack: 1d);
            SetProperty(
                combatBootstrap,
                "CombatComposition",
                composition);
            flowController.Initialize(
                composition.FlowSetup,
                coordinator => composition.CreateBridge(coordinator));
            bridge = composition.Bridge;
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            IReadOnlyList<CombatAction> fist =
                composition.BossActionProvider.CreateActions(
                    composition.Boss,
                    composition.Party,
                    composition.ActionIds);
            Assert.That(fist, Is.Not.Empty);
            composition.Executor.Execute(new CombatActionQueue(fist));
            Assert.That(
                composition.BossActionProvider.TryCommitCompletedAction(),
                Is.True);
            IReadOnlyList<CombatAction> eruption =
                composition.BossActionProvider.CreateActions(
                    composition.Boss,
                    composition.Party,
                    composition.ActionIds);
            Assert.That(eruption, Is.Not.Empty);
            composition.Executor.Execute(new CombatActionQueue(eruption));
            Assert.That(
                composition.BossActionProvider.TryCommitCompletedAction(),
                Is.True);

            IReadOnlyList<CombatAction> compression =
                composition.BossActionProvider.CreateActions(
                    composition.Boss,
                    composition.Party,
                    composition.ActionIds);
            composition.Executor.Execute(
                new CombatActionQueue(compression));
            Assert.That(
                composition.BossActionProvider.TryCommitCompletedAction(),
                Is.True);
            Assert.That(composition.KragmorRuntimeState.CurrentVisualStateId,
                Is.EqualTo(KragmorRules.CoreCompressionVisualStateId));
            hudController.RefreshInitialSnapshot();
            Assert.That(hudController.BossImage.sprite,
                Is.SameAs(defaultBossSprite));

            composition.BossIntentSource.GetIntentForecast(10);
            hudController.RefreshInitialSnapshot();
            Assert.That(hudController.BossImage.sprite,
                Is.SameAs(defaultBossSprite));

            IReadOnlyList<CombatAction> collapse =
                composition.BossActionProvider.CreateActions(
                    composition.Boss,
                    composition.Party,
                    composition.ActionIds);
            composition.Executor.Execute(new CombatActionQueue(collapse));
            Assert.That(composition.KragmorRuntimeState.CurrentVisualStateId,
                Is.EqualTo(KragmorRules.CoreCompressionVisualStateId));
            hudController.RefreshInitialSnapshot();
            Assert.That(hudController.BossImage.sprite,
                Is.SameAs(defaultBossSprite));

            Assert.That(
                KragmorDefenseEffectFactory.GetRequiredActiveEffect(
                    composition.Boss,
                    KragmorDefenseState.CoreExposure),
                Is.Not.Null);
            Assert.That(
                composition.BossActionProvider.TryCommitCompletedAction(),
                Is.True);
            Assert.That(composition.KragmorRuntimeState.CurrentVisualStateId,
                Is.EqualTo(KragmorRules.CoreExposureVisualStateId));
            hudController.RefreshInitialSnapshot();
            Assert.That(hudController.BossImage.sprite,
                Is.SameAs(exposedBossSprite));
        }

        [Test]
        public void BossIntentForecastRendersTenAndSlidesAfterCommit()
        {
            InvokePrivate(hudController, "OnEnable");
            BattleSceneCombatComposition composition =
                CreateProductionComposition();
            SetProperty(
                combatBootstrap,
                "CombatComposition",
                composition);
            flowController.Initialize(
                composition.FlowSetup,
                coordinator => composition.CreateBridge(coordinator));
            bridge = composition.Bridge;
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            int patternBeforeRefresh =
                composition.KragmorRuntimeState.PatternIndex;

            Sprite[] expectedSprites =
            {
                fistIntentSprite,
                eruptionIntentSprite,
                compressionIntentSprite,
                collapseIntentSprite,
                fistIntentSprite,
                eruptionIntentSprite,
                compressionIntentSprite,
                collapseIntentSprite,
                fistIntentSprite,
                eruptionIntentSprite
            };
            for (int index = 0; index < bossIntentSlots.Length; index++)
            {
                Assert.That(bossIntentSlots[index].Root.activeSelf, Is.True);
                Assert.That(bossIntentSlots[index].ActionImage.sprite,
                    Is.SameAs(expectedSprites[index]));
                Assert.That(bossIntentSlots[index].RemainingTurnText.text,
                    Is.EqualTo((index + 1).ToString()));
            }

            hudController.RefreshInitialSnapshot();
            Assert.That(composition.KragmorRuntimeState.PatternIndex,
                Is.EqualTo(patternBeforeRefresh));

            CompletePlayerInputAndEnterBossActing();
            Assert.That(bossIntentSlots[0].ActionImage.sprite,
                Is.SameAs(fistIntentSprite));
            Assert.That(bridge.ResolveBossAction(1), Is.True);

            Assert.That(composition.KragmorRuntimeState.PatternIndex,
                Is.EqualTo(1));
            Assert.That(bossIntentSlots[0].ActionImage.sprite,
                Is.SameAs(eruptionIntentSprite));
            Assert.That(bossIntentSlots[1].ActionImage.sprite,
                Is.SameAs(compressionIntentSprite));
            Assert.That(bossIntentSlots[0].RemainingTurnText.text,
                Is.EqualTo("1"));

            InvokePrivate(hudController, "OnDisable");
            InvokePrivate(hudController, "OnEnable");
            Assert.That(bossIntentSlots[0].ActionImage.sprite,
                Is.SameAs(eruptionIntentSprite));
            AssertSingleHudSubscription(flowController.Coordinator);

            Assert.That(flowController.Coordinator.AbortBattle(), Is.True);
            for (int index = 0; index < bossIntentSlots.Length; index++)
            {
                Assert.That(bossIntentSlots[index].Root.activeSelf, Is.False);
            }
        }

        [Test]
        public void MissingIntentSourceKeepsAllForecastSlotsHidden()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            for (int index = 0; index < bossIntentSlots.Length; index++)
            {
                Assert.That(bossIntentSlots[index].Root.activeSelf, Is.False);
                Assert.That(bossIntentSlots[index].RemainingTurnText.text,
                    Is.Empty);
            }
        }

        [Test]
        public void DisabledHudDoesNotBlockBoardReadyBattleStart()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            InvokePrivate(hudController, "Start");
            InvokePrivate(hudController, "OnDisable");
            SetField(
                boardController,
                "initialBoardReadyPublished",
                true);

            InvokePrivate(flowController, "OnEnable");

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.None));
            InvokePrivate(flowController, "OnDisable");
        }

        [Test]
        public void InitialSnapshotUsesRuntimeTurnAndNormalizedHp()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(turnLimit: 25);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            int turnBeforeRefresh = flowController.Context.CurrentTurn;

            InvokePrivate(hudController, "Start");

            Assert.That(hudController.TurnText.text, Is.EqualTo(
                "Turn\n1 / 25"));
            Assert.That(flowController.Context.CurrentTurn,
                Is.EqualTo(turnBeforeRefresh));
            AssertHp(
                hudController.BossHpText,
                hudController.BossHpSlider,
                "1000 / 1000",
                1f);
            AssertHp(
                hudController.PartyHpText,
                hudController.PartyHpSlider,
                "900 / 900",
                1f);
            Assert.That(hudController.PartyShieldImage.gameObject.activeSelf,
                Is.False);
            Assert.That(hudController.BossShieldImage.gameObject.activeSelf,
                Is.False);
        }

        [Test]
        public void PhaseChangeRefreshesNextTurnAndExpiredShield()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(turnLimit: 25);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            bridge.Party.Shields.Add(new ShieldInstance(
                1,
                "test",
                225,
                1,
                1,
                1));

            CompletePlayerInputAndEnterBossActing();
            Assert.That(bridge.ResolveBossAction(1), Is.True);

            Assert.That(hudController.TurnText.text, Is.EqualTo(
                "Turn\n2 / 25"));
            Assert.That(bridge.Party.Shields.TotalShield, Is.Zero);
            Assert.That(hudController.PartyShieldImage.gameObject.activeSelf,
                Is.False);
            Assert.That(hudController.PartyHpText.text,
                Is.EqualTo("900 / 900"));
        }

        [Test]
        public void MatchDamageRefreshesBossHpAndClampsDefeatToZero()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(
                bossMaxHp: 100,
                characterAttack: 100d,
                matchActions: context => new CombatAction[]
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
                });
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            MatchEventExecution execution = BeginSingleWaterMatch();
            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(bridge.Boss.CurrentHp, Is.Zero);
            AssertHp(
                hudController.BossHpText,
                hudController.BossHpSlider,
                "0 / 100",
                0f);
            Assert.That(hudController.BossShieldImage.gameObject.activeSelf,
                Is.False);
        }

        [Test]
        public void BossDamageAndMatchHealRefreshPartyHp()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(
                bossAttack: 400d,
                matchActions: context => new CombatAction[]
                {
                    new HealAction(
                        context.ActionIds.Next(),
                        ActionOrigin.Match,
                        new HealingContextBuildRequest(
                            context.Character,
                            context.Party,
                            0.50d,
                            false,
                            0))
                },
                bossActions: context => new CombatAction[]
                {
                    new BossDamageAction(
                        context.ActionIds.Next(),
                        new BossDamageContextBuildRequest(
                            context.Boss,
                            context.Party,
                            1d,
                            ValorChronicle.Battle.Combat.Attacks.AttackTag.None))
                });
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            CompletePlayerInputAndEnterBossActing();
            Assert.That(bridge.ResolveBossAction(1), Is.True);
            long damagedHp = bridge.Party.CurrentHp;
            Assert.That(damagedHp, Is.LessThan(bridge.Party.MaxHp));
            Assert.That(hudController.PartyHpText.text, Is.EqualTo(
                $"{damagedHp} / {bridge.Party.MaxHp}"));

            MatchEventExecution execution = BeginSingleWaterMatch();
            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(bridge.Party.CurrentHp, Is.GreaterThan(damagedHp));
            Assert.That(hudController.PartyHpText.text, Is.EqualTo(
                $"{bridge.Party.CurrentHp} / {bridge.Party.MaxHp}"));
            Assert.That(hudController.PartyHpSlider.value,
                Is.EqualTo(
                    (float)((double)bridge.Party.CurrentHp
                        / bridge.Party.MaxHp)).Within(0.000001f));
        }

        [Test]
        public void PartyShieldShowsExactTextAndClampedLeftAnchoredWidth()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            bridge.Party.Shields.Add(new ShieldInstance(
                1,
                "test",
                225,
                1,
                3,
                1));
            Assert.That(flowController.Coordinator.TryBeginBoardResolution(),
                Is.True);

            Assert.That(hudController.PartyShieldImage.gameObject.activeSelf,
                Is.True);
            Assert.That(hudController.PartyShieldImage.type,
                Is.EqualTo(Image.Type.Simple));
            Assert.That(
                hudController.PartyShieldImage.rectTransform.sizeDelta.x,
                Is.EqualTo(225f).Within(0.000001f));
            Assert.That(GetLeftEdge(
                    hudController.PartyShieldImage.rectTransform),
                Is.EqualTo(-450f).Within(0.000001f));
            Assert.That(hudController.PartyHpText.text,
                Is.EqualTo("900 (+225) / 900"));

            bridge.Party.Shields.Add(new ShieldInstance(
                2,
                "test",
                1000,
                1,
                3,
                2));
            hudController.RefreshInitialSnapshot();

            Assert.That(
                hudController.PartyShieldImage.rectTransform.sizeDelta.x,
                Is.EqualTo(900f).Within(0.000001f));
            Assert.That(hudController.PartyHpText.text,
                Is.EqualTo("900 (+1225) / 900"));
        }

        [Test]
        public void ConsumedShieldHidesOverlayAndReenableReadsLatestState()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(
                bossAttack: 500d,
                bossActions: context => new CombatAction[]
                {
                    new BossDamageAction(
                        context.ActionIds.Next(),
                        new BossDamageContextBuildRequest(
                            context.Boss,
                            context.Party,
                            1d,
                            ValorChronicle.Battle.Combat.Attacks.AttackTag.None))
                });
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            bridge.Party.Shields.Add(new ShieldInstance(
                1,
                "test",
                500,
                1,
                3,
                1));
            CompletePlayerInputAndEnterBossActing();

            Assert.That(bridge.ResolveBossAction(1), Is.True);
            Assert.That(bridge.Party.Shields.TotalShield, Is.Zero);
            Assert.That(hudController.PartyShieldImage.gameObject.activeSelf,
                Is.False);
            Assert.That(hudController.PartyHpText.text,
                Is.EqualTo("900 / 900"));

            InvokePrivate(hudController, "OnDisable");
            bridge.Party.Shields.Add(new ShieldInstance(
                2,
                "test",
                90,
                2,
                3,
                2));
            InvokePrivate(hudController, "OnEnable");

            Assert.That(hudController.PartyShieldImage.gameObject.activeSelf,
                Is.True);
            Assert.That(
                hudController.PartyShieldImage.rectTransform.sizeDelta.x,
                Is.EqualTo(90f).Within(0.000001f));
            Assert.That(hudController.PartyHpText.text,
                Is.EqualTo("900 (+90) / 900"));
            AssertSingleHudSubscription(flowController.Coordinator);
        }

        private void InitializeCombat(
            int turnLimit = 7,
            long partyMaxHp = 900,
            long bossMaxHp = 1000,
            double characterAttack = 180d,
            double bossAttack = 0d,
            int partySlotIndex = 0,
            int activeAbilityIndex = 0,
            bool includeActiveBinding = true,
            bool ambiguousActiveBindings = false,
            Func<MatchEventActionContext, IReadOnlyList<CombatAction>>
                matchActions = null,
            Func<BossCombatActionContext, IReadOnlyList<CombatAction>>
                bossActions = null)
        {
            int activeAbilityCount = ambiguousActiveBindings
                ? Math.Max(2, activeAbilityIndex + 1)
                : activeAbilityIndex + 1;
            var cooldowns = new int[activeAbilityCount];
            for (int index = 0; index < cooldowns.Length; index++)
            {
                cooldowns[index] = 8;
            }

            flowController.Initialize(
                new BattleFlowSetup(
                    turnLimit,
                    cooldowns),
                coordinator =>
                {
                    var marea = new CharacterBattleState(
                        MareaBluefangRules.CharacterId,
                        partySlotIndex,
                        ElementType.Water,
                        partyMaxHp,
                        characterAttack);
                    var party = new PartyBattleState(new[] { marea });
                    var boss = new BossBattleState(
                        "boss",
                        ElementType.Fire,
                        bossMaxHp,
                        bossAttack);
                    WaterElementResource.Register(boss.Resources, 5);
                    var actionIds = new CombatActionIdSequence();
                    var providers =
                        new ActiveAbilityActionProviderRegistry();
                    providers.Register(
                        MareaBluefangRules.CharacterId,
                        MareaBluefangRules.ActiveAbilityId,
                        new MareaBluefangActiveActionProvider(
                            MareaBluefangTestConfig.Create()));
                    IReadOnlyList<ActiveAbilityBinding> bindings;
                    if (!includeActiveBinding)
                    {
                        bindings = Array.Empty<ActiveAbilityBinding>();
                    }
                    else if (ambiguousActiveBindings)
                    {
                        bindings = new[]
                        {
                            new ActiveAbilityBinding(
                                0,
                                partySlotIndex,
                                MareaBluefangRules.CharacterId,
                                MareaBluefangRules.ActiveAbilityId),
                            new ActiveAbilityBinding(
                                1,
                                partySlotIndex,
                                MareaBluefangRules.CharacterId,
                                MareaBluefangRules.ActiveAbilityId)
                        };
                    }
                    else
                    {
                        bindings = new[]
                        {
                            new ActiveAbilityBinding(
                                activeAbilityIndex,
                                partySlotIndex,
                                MareaBluefangRules.CharacterId,
                                MareaBluefangRules.ActiveAbilityId)
                        };
                    }

                    bridge = new BattleFlowCombatBridge(
                        coordinator,
                        party,
                        boss,
                        new CombatActionExecutor(
                            boss,
                            party,
                            new DamageContextFactory(
                                new SeededRandomSource(1))),
                        matchActions == null
                            ? new EmptyMatchProvider()
                            : new DelegateMatchProvider(matchActions),
                        bossActions == null
                            ? new EmptyBossProvider()
                            : new DelegateBossProvider(bossActions),
                        actionIds,
                        bindings,
                        providers);
                    return bridge;
                });
        }

        private void InitializeCombatWithPartySlots(
            IReadOnlyList<int> partySlotIndices)
        {
            flowController.Initialize(
                new BattleFlowSetup(7, Array.Empty<int>()),
                coordinator =>
                {
                    var characters = new CharacterBattleState[
                        partySlotIndices.Count];
                    for (int index = 0;
                        index < partySlotIndices.Count;
                        index++)
                    {
                        int partySlotIndex = partySlotIndices[index];
                        characters[index] = new CharacterBattleState(
                            $"character_{partySlotIndex}",
                            partySlotIndex,
                            ElementType.Water,
                            900,
                            100d);
                    }

                    var party = new PartyBattleState(characters);
                    var boss = new BossBattleState(
                        "boss",
                        ElementType.Fire,
                        1000,
                        100d);
                    bridge = new BattleFlowCombatBridge(
                        coordinator,
                        party,
                        boss,
                        new CombatActionExecutor(
                            boss,
                            party,
                            new DamageContextFactory(
                                new SeededRandomSource(1))),
                        new EmptyMatchProvider(),
                        new EmptyBossProvider(),
                        new CombatActionIdSequence());
                    return bridge;
                });
        }

        private BattleSceneCombatComposition CreateProductionComposition(
            double bossAttack = 850d)
        {
            BossDefinition bossDefinition =
                ScriptableObject.CreateInstance<BossDefinition>();
            createdObjects.Add(bossDefinition);
            SetField(bossDefinition, "id", KragmorRules.BossId);
            SetField(bossDefinition, "element", ElementType.Fire);
            SetField(bossDefinition, "turnLimit", 25);
            var difficulty = new BossDifficultyStats(
                "difficulty_normal",
                66000,
                bossAttack);
            SetField(
                bossDefinition,
                "difficultyStats",
                new[] { difficulty });
            KragmorCombatConfig kragmorConfig =
                KragmorTestConfig.Create();
            createdObjects.Add(kragmorConfig);
            KragmorTestConfig.Assign(
                bossDefinition,
                kragmorConfig);

            CharacterDefinition mareaDefinition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            createdObjects.Add(mareaDefinition);
            SetField(
                mareaDefinition,
                "id",
                MareaBluefangRules.CharacterId);
            SetField(mareaDefinition, "element", ElementType.Water);
            SetField(mareaDefinition, "level1Hp", 900);
            SetField(mareaDefinition, "level1Attack", 180);
            SetField(mareaDefinition, "level100Hp", 3400);
            SetField(mareaDefinition, "level100Attack", 1050);
            MareaBluefangCombatConfig config =
                MareaBluefangTestConfig.Create();
            createdObjects.Add(config);
            MareaBluefangTestConfig.Assign(mareaDefinition, config);

            return new BattleSceneCombatComposition(
                new[]
                {
                    new BattlePartyMemberInput(
                        mareaDefinition.Id,
                        partySlotIndex: 0,
                        level: 1,
                        awakening: 0,
                        characterDefinition: mareaDefinition)
                },
                new CharacterCombatProviderRegistrarCatalog(
                    new ICharacterCombatProviderRegistrar[]
                    {
                        new MareaBluefangCombatProviderRegistrar()
                    }),
                bossDefinition,
                kragmorConfig,
                difficulty,
                new SeededRandomSource(1),
                BattleResultBalanceDefaults.Create());
        }

        private MatchEventExecution BeginSingleWaterMatch(
            int blockCount = 3)
        {
            var positions = new BoardPosition[blockCount];
            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = new BoardPosition(index, 0);
            }

            Assert.That(flowController.Coordinator.TryBeginBoardResolution(),
                Is.True);
            Assert.That(
                flowController.Coordinator.NotifyBoardActionResolved(
                    BattleFlowTestSupport.CreateCascade(
                        new[]
                        {
                            BattleFlowTestSupport.Match(
                                ElementType.Water,
                                positions)
                        }),
                    true),
                Is.True);
            Assert.That(
                flowController.Coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution execution),
                Is.True);
            return execution;
        }

        private void EnterMatchResolving(int eventCount)
        {
            Assert.That(eventCount, Is.Positive);
            var steps = new BattleFlowTestSupport.MatchFixture[eventCount][];
            for (int index = 0; index < steps.Length; index++)
            {
                ElementType element;
                if (index < 2)
                {
                    element = ElementType.Water;
                }
                else if (index == 2)
                {
                    element = ElementType.Dark;
                }
                else
                {
                    element = index % 2 == 0
                        ? ElementType.Fire
                        : ElementType.Water;
                }

                int blockCount = index == 2
                    ? 6
                    : 3 + (index % 3);
                var positions = new BoardPosition[blockCount];
                for (int positionIndex = 0;
                    positionIndex < positions.Length;
                    positionIndex++)
                {
                    positions[positionIndex] = new BoardPosition(
                        positionIndex,
                        index % 5);
                }

                steps[index] = new[]
                {
                    BattleFlowTestSupport.Match(element, positions)
                };
            }

            Assert.That(flowController.Coordinator.TryBeginBoardResolution(),
                Is.True);
            Assert.That(
                flowController.Coordinator.NotifyBoardActionResolved(
                    BattleFlowTestSupport.CreateCascade(steps),
                    true),
                Is.True);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));
        }

        private static BoardSwapActionResult CreateResolvedBoardActionResult(
            BoardCascadeResult cascade)
        {
            BoardState finalBoard = cascade.Board;
            BoardShuffleResult shuffle = CreateInternal<BoardShuffleResult>(
                finalBoard,
                BoardShuffleKind.None,
                new List<BoardShuffleEntry>(),
                0);
            return CreateInternal<BoardSwapActionResult>(
                new BoardSwap(
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0)),
                BoardSwapActionStatus.Resolved,
                new BoardState(),
                cascade,
                shuffle,
                finalBoard);
        }

        private void ReachResult(BattleResultKind result)
        {
            switch (result)
            {
                case BattleResultKind.Victory:
                    Assert.That(
                        flowController.Coordinator.NotifyBossDefeated(),
                        Is.True);
                    break;
                case BattleResultKind.Defeat:
                    Assert.That(
                        flowController.Coordinator
                            .NotifyPartyIncapacitated(),
                        Is.True);
                    break;
                case BattleResultKind.TurnLimitReached:
                    CompletePlayerInputAndEnterBossActing();
                    Assert.That(
                        flowController.Coordinator.CompleteBossAction(),
                        Is.True);
                    break;
                case BattleResultKind.Aborted:
                    Assert.That(flowController.Coordinator.AbortBattle(),
                        Is.True);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }

        private void AssertMatchSlot(int index, MatchEvent matchEvent)
        {
            BattleMatchEventSlotView slot = matchEventSlots[index];
            Assert.That(slot.Root.activeSelf, Is.True);
            Assert.That(slot.ElementImage.sprite,
                Is.SameAs(elementSpriteSet.GetSprite(matchEvent.Element)));
            Assert.That(slot.BlockCountText.text,
                Is.EqualTo(matchEvent.RemovedBlockCount.ToString()));
        }

        private void AssertAllMatchSlotsHidden()
        {
            for (int index = 0; index < matchEventSlots.Length; index++)
            {
                Assert.That(matchEventSlots[index].Root.activeSelf, Is.False);
                Assert.That(matchEventSlots[index].BlockCountText.text,
                    Is.Empty);
            }
        }

        private void CompletePlayerInputAndEnterBossActing()
        {
            Assert.That(flowController.Coordinator.TryBeginBoardResolution(),
                Is.True);
            Assert.That(
                flowController.Coordinator.NotifyBoardActionResolved(
                    CreateSingleFireCascade(),
                    true),
                Is.True);
            flowController.Coordinator.ExecuteRemainingMatchEvents();
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
        }

        private static BoardCascadeResult CreateSingleFireCascade()
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

        private static void AssertHp(
            TMP_Text text,
            Slider slider,
            string expectedText,
            float expectedRatio)
        {
            Assert.That(text.text, Is.EqualTo(expectedText));
            Assert.That(slider.minValue, Is.Zero);
            Assert.That(slider.maxValue, Is.EqualTo(1f));
            Assert.That(slider.wholeNumbers, Is.False);
            Assert.That(slider.value,
                Is.EqualTo(expectedRatio).Within(0.000001f));
        }

        private TMP_Text CreateText(string name)
        {
            var textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root.transform, false);
            return textObject.GetComponent<TextMeshProUGUI>();
        }

        private Slider CreateSlider(string name)
        {
            var sliderObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Slider));
            sliderObject.transform.SetParent(root.transform, false);
            return sliderObject.GetComponent<Slider>();
        }

        private Image CreateImage(string name)
        {
            var imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            imageObject.transform.SetParent(root.transform, false);
            RectTransform rectTransform =
                imageObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(900f, 60f);
            return imageObject.GetComponent<Image>();
        }

        private void AssertBossProjectilePositionsMatchCenters(
            RectTransform source,
            IReadOnlyList<RectTransform> targets)
        {
            var canvasObject = new GameObject(
                "ProjectileCanvas",
                typeof(RectTransform),
                typeof(Canvas));
            createdObjects.Add(canvasObject);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root.transform.SetParent(canvasObject.transform, false);

            var effectObject = new GameObject(
                "BattleEffectLayer",
                typeof(RectTransform),
                typeof(AttackProjectilePool),
                typeof(BattleCombatPresentationController));
            createdObjects.Add(effectObject);
            effectObject.transform.SetParent(canvasObject.transform, false);
            RectTransform effectLayer =
                effectObject.GetComponent<RectTransform>();
            var presentation = effectObject.GetComponent<
                BattleCombatPresentationController>();
            SetField(presentation, "hudController", hudController);
            SetField(presentation, "effectLayer", effectLayer);
            SetField(
                presentation,
                "projectilePool",
                effectObject.GetComponent<AttackProjectilePool>());
            SetField(presentation, "projectileSprite", fireSprite);
            Canvas.ForceUpdateCanvases();

            MethodInfo resolver = typeof(BattleCombatPresentationController)
                .GetMethod(
                    "TryResolveBossPositions",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            var arguments = new object[] { Vector2.zero, Vector2.zero };
            bool resolved = (bool)resolver.Invoke(presentation, arguments);
            Vector2 actualStart = (Vector2)arguments[0];
            Vector2 actualEnd = (Vector2)arguments[1];
            Vector2 expectedStart = ToEffectLayerCenter(
                source,
                effectLayer);
            Vector2 expectedEnd = Vector2.zero;
            for (int index = 0; index < targets.Count; index++)
            {
                expectedEnd += ToEffectLayerCenter(
                    targets[index],
                    effectLayer);
            }

            expectedEnd /= targets.Count;
            Assert.That(resolved, Is.True);
            Assert.That(actualStart.x,
                Is.EqualTo(expectedStart.x).Within(0.001f));
            Assert.That(actualStart.y,
                Is.EqualTo(expectedStart.y).Within(0.001f));
            Assert.That(actualEnd.x,
                Is.EqualTo(expectedEnd.x).Within(0.001f));
            Assert.That(actualEnd.y,
                Is.EqualTo(expectedEnd.y).Within(0.001f));
        }

        private static Vector2 ToEffectLayerCenter(
            RectTransform target,
            RectTransform effectLayer)
        {
            Vector3 worldCenter = target.TransformPoint(target.rect.center);
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
                null,
                worldCenter);
            Assert.That(
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    effectLayer,
                    screenPosition,
                    null,
                    out Vector2 localPosition),
                Is.True);
            return localPosition;
        }

        private static float GetLeftEdge(RectTransform rectTransform)
        {
            return rectTransform.anchoredPosition.x
                - (rectTransform.sizeDelta.x * rectTransform.pivot.x);
        }

        private BattleCharacterSlotView CreateCharacterSlot(string name)
        {
            var slotObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            slotObject.transform.SetParent(root.transform, false);

            var characterObject = new GameObject(
                "CharacterImage",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            characterObject.transform.SetParent(slotObject.transform, false);
            Image characterImage = characterObject.GetComponent<Image>();
            Button button = characterObject.GetComponent<Button>();
            button.targetGraphic = characterImage;

            var cooldownOverlayObject = new GameObject(
                "CooldownOverlay",
                typeof(RectTransform),
                typeof(Image));
            cooldownOverlayObject.transform.SetParent(
                characterObject.transform,
                false);
            Image cooldownOverlay =
                cooldownOverlayObject.GetComponent<Image>();
            cooldownOverlay.raycastTarget = false;

            var typeObject = new GameObject(
                "TypeImage",
                typeof(RectTransform),
                typeof(Image));
            typeObject.transform.SetParent(slotObject.transform, false);
            Image typeImage = typeObject.GetComponent<Image>();

            var cooldownObject = new GameObject(
                "Cooldown",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            cooldownObject.transform.SetParent(slotObject.transform, false);

            var slot = new BattleCharacterSlotView();
            SetField(slot, "root", slotObject);
            SetField(slot, "activeButton", button);
            SetField(slot, "characterImage", characterImage);
            SetField(slot, "typeImage", typeImage);
            SetField(slot, "cooldownOverlay", cooldownOverlay);
            SetField(
                slot,
                "cooldownText",
                cooldownObject.GetComponent<TextMeshProUGUI>());
            SetField(
                slot,
                "statusSlots",
                CreateStatusSlots($"{name}Status", 4));
            return slot;
        }

        private BattleStatusIconView[] CreateStatusSlots(
            string namePrefix,
            int count)
        {
            var slots = new BattleStatusIconView[count];
            for (int index = 0; index < count; index++)
            {
                var statusRoot = new GameObject(
                    $"{namePrefix}{index}",
                    typeof(RectTransform));
                statusRoot.transform.SetParent(root.transform, false);
                var iconObject = new GameObject(
                    "Icon",
                    typeof(RectTransform),
                    typeof(Image));
                iconObject.transform.SetParent(statusRoot.transform, false);
                Image icon = iconObject.GetComponent<Image>();
                icon.sprite = genericStatusSprite;
                var valueObject = new GameObject(
                    "Value",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                valueObject.transform.SetParent(statusRoot.transform, false);

                var slot = new BattleStatusIconView();
                SetField(slot, "root", statusRoot);
                SetField(slot, "iconImage", icon);
                SetField(
                    slot,
                    "valueText",
                    valueObject.GetComponent<TextMeshProUGUI>());
                slots[index] = slot;
            }

            return slots;
        }

        private BattleMatchEventSlotView[] CreateMatchEventSlots(int count)
        {
            var slots = new BattleMatchEventSlotView[count];
            for (int index = 0; index < count; index++)
            {
                var slotRoot = new GameObject(
                    $"MatchEvent{index}",
                    typeof(RectTransform));
                slotRoot.transform.SetParent(root.transform, false);
                var imageObject = new GameObject(
                    "ElementImage",
                    typeof(RectTransform),
                    typeof(Image));
                imageObject.transform.SetParent(slotRoot.transform, false);
                var countObject = new GameObject(
                    "BlockCount",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                countObject.transform.SetParent(slotRoot.transform, false);

                var slot = new BattleMatchEventSlotView();
                SetField(slot, "root", slotRoot);
                SetField(
                    slot,
                    "elementImage",
                    imageObject.GetComponent<Image>());
                SetField(
                    slot,
                    "blockCountText",
                    countObject.GetComponent<TextMeshProUGUI>());
                slots[index] = slot;
            }

            return slots;
        }

        private BattleBossIntentSlotView[] CreateBossIntentSlots(int count)
        {
            var slots = new BattleBossIntentSlotView[count];
            for (int index = 0; index < count; index++)
            {
                var intentRoot = new GameObject(
                    $"BossIntent{index}",
                    typeof(RectTransform));
                intentRoot.transform.SetParent(root.transform, false);
                var imageObject = new GameObject(
                    "ActionImage",
                    typeof(RectTransform),
                    typeof(Image));
                imageObject.transform.SetParent(intentRoot.transform, false);
                Image image = imageObject.GetComponent<Image>();
                image.sprite = genericIntentSprite;
                var turnObject = new GameObject(
                    "RemainingTurn",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                turnObject.transform.SetParent(intentRoot.transform, false);

                var slot = new BattleBossIntentSlotView();
                SetField(slot, "root", intentRoot);
                SetField(slot, "actionImage", image);
                SetField(
                    slot,
                    "remainingTurnText",
                    turnObject.GetComponent<TextMeshProUGUI>());
                slots[index] = slot;
            }

            return slots;
        }

        private DefinitionDatabase CreatePresentationDatabase(
            bool includeIcons,
            bool includeBossPresentation)
        {
            SkillDefinition fist = CreateSkillDefinition(
                KragmorRules.ColossusIronFistSkillId,
                includeIcons ? fistIntentSprite : null);
            SkillDefinition eruption = CreateSkillDefinition(
                KragmorRules.RockshardEruptionSkillId,
                includeIcons ? eruptionIntentSprite : null);
            SkillDefinition compression = CreateSkillDefinition(
                KragmorRules.CoreCompressionSkillId,
                includeIcons ? compressionIntentSprite : null);
            SkillDefinition collapse = CreateSkillDefinition(
                KragmorRules.EarthCollapseSkillId,
                includeIcons ? collapseIntentSprite : null);

            EffectDefinition mareaEffect = CreateEffectDefinition(
                MareaBluefangRules.ActiveEffectId,
                includeIcons ? darkSprite : null);
            EffectDefinition carapaceEffect = CreateEffectDefinition(
                KragmorRules.VolcanicCarapaceEffectId,
                null);
            EffectDefinition compressionEffect = CreateEffectDefinition(
                KragmorRules.CoreCompressionEffectId,
                null);
            EffectDefinition exposureEffect = CreateEffectDefinition(
                KragmorRules.CoreExposureEffectId,
                null);
            ResourceDefinition waterResource = CreateResourceDefinition(
                WaterElementResource.Id,
                includeIcons ? fireSprite : null);

            BossPresentationDefinition[] presentations =
                includeBossPresentation
                    ? new[] { CreateBossPresentationDefinition() }
                    : Array.Empty<BossPresentationDefinition>();
            DefinitionDatabase database =
                ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            SetField(
                database,
                "skills",
                new[] { fist, eruption, compression, collapse });
            SetField(
                database,
                "effects",
                new[]
                {
                    mareaEffect,
                    carapaceEffect,
                    compressionEffect,
                    exposureEffect
                });
            SetField(database, "resources", new[] { waterResource });
            SetField(database, "bossPresentations", presentations);
            database.Initialize();
            return database;
        }

        private SkillDefinition CreateSkillDefinition(
            string id,
            Sprite icon)
        {
            SkillDefinition definition =
                ScriptableObject.CreateInstance<SkillDefinition>();
            createdObjects.Add(definition);
            SetField(definition, "id", id);
            SetField(definition, "icon", icon);
            SetField(definition, "skillKind", SkillKind.BossAction);
            return definition;
        }

        private EffectDefinition CreateEffectDefinition(
            string id,
            Sprite icon)
        {
            EffectDefinition definition =
                ScriptableObject.CreateInstance<EffectDefinition>();
            createdObjects.Add(definition);
            SetField(definition, "id", id);
            SetField(definition, "icon", icon);
            return definition;
        }

        private ResourceDefinition CreateResourceDefinition(
            string id,
            Sprite icon)
        {
            ResourceDefinition definition =
                ScriptableObject.CreateInstance<ResourceDefinition>();
            createdObjects.Add(definition);
            SetField(definition, "id", id);
            SetField(definition, "icon", icon);
            return definition;
        }

        private BossPresentationDefinition
            CreateBossPresentationDefinition()
        {
            var exposure = new BossVisualStateEntry();
            SetField(
                exposure,
                "visualStateId",
                KragmorRules.CoreExposureVisualStateId);
            SetField(exposure, "sprite", exposedBossSprite);
            BossPresentationDefinition definition =
                ScriptableObject.CreateInstance<
                    BossPresentationDefinition>();
            createdObjects.Add(definition);
            SetField(definition, "bossId", KragmorRules.BossId);
            SetField(definition, "defaultSprite", defaultBossSprite);
            SetField(
                definition,
                "stateVisuals",
                new[] { exposure });
            return definition;
        }

        private Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(2, 2)
            {
                name = $"{name}Texture"
            };
            createdObjects.Add(texture);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            sprite.name = name;
            createdObjects.Add(sprite);
            return sprite;
        }

        private static EffectInstance CreateEffect(
            long runtimeId,
            string effectId,
            int? remainingTurns,
            EffectStackPolicy stackPolicy,
            int maxStackCount = 1)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.DealtDamageIncrease,
                0.1d,
                remainingTurns,
                runtimeId,
                stackPolicy,
                maxStackCount: maxStackCount);
        }

        private void AssertSingleHudSubscription(
            BattleFlowCoordinator coordinator)
        {
            Assert.That(
                CountTargetSubscribers(
                    coordinator,
                    "PhaseChanged",
                    hudController),
                Is.EqualTo(1));
            Assert.That(
                CountTargetSubscribers(
                    coordinator,
                    "MatchEventExecuting",
                    hudController),
                Is.EqualTo(1));
            Assert.That(
                CountTargetSubscribers(
                    coordinator,
                    "BossActionStarted",
                    hudController),
                Is.EqualTo(1));
            Assert.That(
                CountTargetSubscribers(
                    coordinator,
                    "ResultReached",
                    hudController),
                Is.EqualTo(1));
            Assert.That(
                CountTargetSubscribers(
                    flowController,
                    "PresentationMatchQueueChanged",
                    hudController),
                Is.EqualTo(1));
            Assert.That(
                CountTargetSubscribers(
                    flowController,
                    "ActivePresentationStateChanged",
                    hudController),
                Is.EqualTo(1));
            Assert.That(
                CountTargetSubscribers(
                    flowController.CombatBridge,
                    "CombatActionStepApplied",
                    hudController),
                Is.EqualTo(1));
            Assert.That(
                CountTargetSubscribers(
                    flowController.CombatBridge,
                    "BossCombatActionStepApplied",
                    hudController),
                Is.EqualTo(1));
            Assert.That(
                CountTargetSubscribers(
                    flowController.CombatBridge,
                    "ActiveCombatActionStepApplied",
                    hudController),
                Is.EqualTo(1));
        }

        private void AssertNoHudSubscription(
            BattleFlowCoordinator coordinator)
        {
            Assert.That(
                CountTargetSubscribers(
                    coordinator,
                    "PhaseChanged",
                    hudController),
                Is.Zero);
            Assert.That(
                CountTargetSubscribers(
                    coordinator,
                    "MatchEventExecuting",
                    hudController),
                Is.Zero);
            Assert.That(
                CountTargetSubscribers(
                    coordinator,
                    "BossActionStarted",
                    hudController),
                Is.Zero);
            Assert.That(
                CountTargetSubscribers(
                    coordinator,
                    "ResultReached",
                    hudController),
                Is.Zero);
            Assert.That(
                CountTargetSubscribers(
                    flowController,
                    "PresentationMatchQueueChanged",
                    hudController),
                Is.Zero);
            Assert.That(
                CountTargetSubscribers(
                    flowController,
                    "ActivePresentationStateChanged",
                    hudController),
                Is.Zero);
            Assert.That(
                CountTargetSubscribers(
                    flowController.CombatBridge,
                    "CombatActionStepApplied",
                    hudController),
                Is.Zero);
            Assert.That(
                CountTargetSubscribers(
                    flowController.CombatBridge,
                    "BossCombatActionStepApplied",
                    hudController),
                Is.Zero);
            Assert.That(
                CountTargetSubscribers(
                    flowController.CombatBridge,
                    "ActiveCombatActionStepApplied",
                    hudController),
                Is.Zero);
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
            Delegate[] callbacks = callback.GetInvocationList();
            for (int index = 0; index < callbacks.Length; index++)
            {
                if (ReferenceEquals(callbacks[index].Target, target))
                {
                    count++;
                }
            }

            return count;
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

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(
                target.GetType().FullName,
                fieldName);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return (T)field.GetValue(target);
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(
                target.GetType().FullName,
                fieldName);
        }

        private static void SetProperty(
            object target,
            string propertyName,
            object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic);
            if (property == null)
            {
                throw new MissingMemberException(
                    target.GetType().FullName,
                    propertyName);
            }

            property.SetValue(target, value);
        }

        private sealed class EmptyMatchProvider
            : IMatchEventActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                MatchEventActionContext context)
            {
                return Array.Empty<CombatAction>();
            }
        }

        private sealed class EmptyBossProvider
            : IBossCombatActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                BossCombatActionContext context)
            {
                return Array.Empty<CombatAction>();
            }
        }

        private sealed class DelegateMatchProvider
            : IMatchEventActionProvider
        {
            private readonly Func<
                MatchEventActionContext,
                IReadOnlyList<CombatAction>> createActions;

            public DelegateMatchProvider(
                Func<MatchEventActionContext, IReadOnlyList<CombatAction>>
                    createActions)
            {
                this.createActions = createActions;
            }

            public IReadOnlyList<CombatAction> CreateRootActions(
                MatchEventActionContext context)
            {
                return createActions(context);
            }
        }

        private sealed class DelegateBossProvider
            : IBossCombatActionProvider
        {
            private readonly Func<
                BossCombatActionContext,
                IReadOnlyList<CombatAction>> createActions;

            public DelegateBossProvider(
                Func<BossCombatActionContext, IReadOnlyList<CombatAction>>
                    createActions)
            {
                this.createActions = createActions;
            }

            public IReadOnlyList<CombatAction> CreateRootActions(
                BossCombatActionContext context)
            {
                return createActions(context);
            }
        }
    }
}
