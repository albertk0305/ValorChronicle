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
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

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

            hudController = root.AddComponent<BattleHudController>();
            SetField(
                hudController,
                "battleFlowController",
                flowController);
            SetField(hudController, "turnText", CreateText("TurnText"));
            SetField(hudController, "comboText", CreateText("ComboText"));
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
            SetField(
                hudController,
                "coreExposureBossSprite",
                exposedBossSprite);
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
            matchEventSlots = CreateMatchEventSlots(10);
            SetField(hudController, "matchEventSlots", matchEventSlots);
            bossIntentSlots = CreateBossIntentSlots(10);
            SetField(hudController, "bossIntentSlots", bossIntentSlots);
            SetField(
                hudController,
                "colossusIronFistIcon",
                fistIntentSprite);
            SetField(
                hudController,
                "rockshardEruptionIcon",
                eruptionIntentSprite);
            SetField(
                hudController,
                "coreCompressionIntentIcon",
                compressionIntentSprite);
            SetField(
                hudController,
                "earthCollapseIcon",
                collapseIntentSprite);
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
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);

            InitializeCombat();
            InvokePrivate(hudController, "Start");

            Assert.That(hudController.IsRuntimeConnected, Is.True);
            Assert.That(hudController.Coordinator,
                Is.SameAs(flowController.Coordinator));
            Assert.That(hudController.CombatBridge, Is.SameAs(bridge));
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(emptySlot.ActiveButton.interactable, Is.False);
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
        public void MissingElementSpriteAndBattleResultHideQueueSafely()
        {
            SetField(elementSpriteSet, "dark", null);
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            EnterMatchResolving(3);

            Assert.DoesNotThrow(() => hudController.RefreshInitialSnapshot());
            Assert.That(matchEventSlots[2].Root.activeSelf, Is.False);
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.EqualTo(3));

            Assert.That(flowController.Coordinator.NotifyBossDefeated(),
                Is.True);

            AssertAllMatchSlotsHidden();
            Assert.That(hudController.ComboText.text, Is.Empty);
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.Zero);
        }

        [Test]
        public void ActiveButtonsUseControllerPathAndSkipEmptyPartySlot()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

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

        [Test]
        public void ActiveUseShowsCooldownAndTurnProgressRefreshesIt()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat();
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            Color baseColor = occupiedSlot.CharacterImage.color;

            occupiedSlot.ActiveButton.onClick.Invoke();

            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(occupiedSlot.CooldownText.gameObject.activeSelf, Is.True);
            Assert.That(occupiedSlot.CooldownText.text,
                Is.EqualTo(MareaBluefangRules.ActiveCooldownTurns.ToString()));
            Assert.That(occupiedSlot.CharacterImage.color, Is.EqualTo(baseColor));

            CompleteActiveAndBoardWithoutMatches();
            Assert.That(bridge.ResolveBossAction(1), Is.True);

            Assert.That(occupiedSlot.CooldownText.text,
                Is.EqualTo((MareaBluefangRules.ActiveCooldownTurns - 1)
                    .ToString()));
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
        }

        [Test]
        public void CooldownZeroHidesTextAndRestoresInteractable()
        {
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(turnLimit: 12);
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");
            occupiedSlot.ActiveButton.onClick.Invoke();

            for (int index = 0;
                index < MareaBluefangRules.ActiveCooldownTurns;
                index++)
            {
                CompleteActiveAndBoardWithoutMatches();
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

            Assert.That(flowController.Coordinator.CompleteActiveInput(),
                Is.True);
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(flowController.Coordinator.NotifyBoardActionStarted(),
                Is.True);
            Assert.That(occupiedSlot.ActiveButton.interactable, Is.False);
            Assert.That(
                flowController.Coordinator.NotifyBoardActionResolved(
                    null,
                    true),
                Is.True);
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
                Is.SameAs(waterSprite));
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
        public void MareaMatchRefreshesWaterElementFromCombatAppliedEvent()
        {
            var mareaProvider = new MareaBluefangMatchActionProvider();
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(
                matchActions: context =>
                    mareaProvider.CreateRootActions(context));
            Assert.That(flowController.Coordinator.StartBattle(), Is.True);
            InvokePrivate(hudController, "Start");

            MatchEventExecution execution = BeginSingleWaterMatch();
            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            ResourceState water = bridge.Boss.Resources.Get(
                WaterElementResource.Id);
            Assert.That(water.CurrentAmount, Is.EqualTo(1));
            Assert.That(bossStatusSlots[0].Root.activeSelf, Is.True);
            Assert.That(bossStatusSlots[0].IconImage.sprite,
                Is.SameAs(waterSprite));
            Assert.That(bossStatusSlots[0].ValueText.text, Is.EqualTo("1"));
        }

        [Test]
        public void MareaFiveMatchConsumptionRemovesWaterElementStatus()
        {
            var mareaProvider = new MareaBluefangMatchActionProvider();
            InvokePrivate(hudController, "OnEnable");
            InitializeCombat(
                bossMaxHp: 100000,
                matchActions: context =>
                    mareaProvider.CreateRootActions(context));
            ResourceState water = bridge.Boss.Resources.Get(
                WaterElementResource.Id);
            water.Add(WaterElementResource.MaxAmount);
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

            CompleteActiveAndBoardWithoutMatches();
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

            occupiedSlot.ActiveButton.onClick.Invoke();

            BattleStatusIconView status = occupiedSlot.GetStatusSlot(0);
            Assert.That(status.Root.activeSelf, Is.True);
            Assert.That(status.IconImage.sprite, Is.SameAs(waterSprite));
            Assert.That(status.ValueText.text,
                Is.EqualTo(MareaBluefangRules.ActiveDurationTurns.ToString()));

            for (int index = 0;
                index < MareaBluefangRules.ActiveDurationTurns;
                index++)
            {
                CompleteActiveAndBoardWithoutMatches();
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

            CompleteActiveAndBoardWithoutMatches();
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
                Is.EqualTo(BattlePhase.ActiveInput));
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

            CompleteActiveAndBoardWithoutMatches();
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

            CompleteActiveAndBoardWithoutMatches();
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
            Assert.That(flowController.Coordinator.CompleteActiveInput(),
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
            Assert.That(flowController.Coordinator.NotifyBoardActionStarted(),
                Is.True);

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
            CompleteActiveAndBoardWithoutMatches();

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
                cooldowns[index] = MareaBluefangRules.ActiveCooldownTurns;
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
                    WaterElementResource.Register(boss.Resources);
                    var actionIds = new CombatActionIdSequence();
                    var providers =
                        new ActiveAbilityActionProviderRegistry();
                    providers.Register(
                        MareaBluefangRules.CharacterId,
                        MareaBluefangRules.ActiveAbilityId,
                        new MareaBluefangActiveActionProvider());
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

        private BattleSceneCombatComposition CreateProductionComposition()
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
                850d);
            SetField(
                bossDefinition,
                "difficultyStats",
                new[] { difficulty });

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

            return new BattleSceneCombatComposition(
                mareaDefinition,
                1,
                bossDefinition,
                difficulty,
                new SeededRandomSource(1));
        }

        private MatchEventExecution BeginSingleWaterMatch(
            int blockCount = 3)
        {
            var positions = new BoardPosition[blockCount];
            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = new BoardPosition(index, 0);
            }

            Assert.That(flowController.Coordinator.CompleteActiveInput(),
                Is.True);
            Assert.That(flowController.Coordinator.NotifyBoardActionStarted(),
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

            Assert.That(flowController.Coordinator.CompleteActiveInput(),
                Is.True);
            Assert.That(flowController.Coordinator.NotifyBoardActionStarted(),
                Is.True);
            Assert.That(
                flowController.Coordinator.NotifyBoardActionResolved(
                    BattleFlowTestSupport.CreateCascade(steps),
                    true),
                Is.True);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));
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

        private void CompleteActiveAndBoardWithoutMatches()
        {
            Assert.That(flowController.Coordinator.CompleteActiveInput(),
                Is.True);
            Assert.That(flowController.Coordinator.NotifyBoardActionStarted(),
                Is.True);
            Assert.That(
                flowController.Coordinator.NotifyBoardActionResolved(
                    null,
                    true),
                Is.True);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
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
            string methodName)
        {
            return target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(
                target,
                null);
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(
                    target.GetType().FullName,
                    fieldName);
            }

            field.SetValue(target, value);
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
