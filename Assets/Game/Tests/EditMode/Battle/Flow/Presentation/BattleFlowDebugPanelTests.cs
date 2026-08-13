using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Bosses.Kragmor;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleFlowDebugPanelTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private GameObject root;
        private BattleBoardController boardController;
        private BattleFlowController flowController;
        private BattleFlowDebugPanel debugPanel;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("BattleFlowDebugPanelTests");
            root.SetActive(false);
            createdObjects.Add(root);

            boardController = root.AddComponent<BattleBoardController>();
            boardController.enabled = false;
            flowController = root.AddComponent<BattleFlowController>();
            SetField(flowController, "boardController", boardController);

            debugPanel = root.AddComponent<BattleFlowDebugPanel>();
            SetField(
                debugPanel,
                "battleFlowController",
                flowController);
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
        public void PanelDoesNotInitializeBattleFlow()
        {
            InvokePrivate(debugPanel, "Awake");

            Assert.That(flowController.Context, Is.Null);
            Assert.That(flowController.CombatBridge, Is.Null);
        }

        [Test]
        public void RefreshShowsStateAndEnablesOnlyValidButtons()
        {
            InvokePrivate(debugPanel, "BuildPanelIfNeeded");
            GameObject panelRoot =
                GetField<GameObject>(debugPanel, "panelRoot");
            Assert.That(
                panelRoot.GetComponent<Image>().raycastTarget,
                Is.False);
            SetField(boardController, "initialBoardReadyPublished", true);
            InitializeFlow();
            InvokePrivate(flowController, "OnEnable");
            InvokePrivate(debugPanel, "RefreshState");

            Assert.That(debugPanel.StatusText, Is.EqualTo(
                "Turn: 1 / 7\nPhase: PlayerInput\nResult: None\n"
                    + "Boss Intent: Unavailable"));
            Assert.That(debugPanel.EndBattleButtonsInteractable, Is.True);
            Assert.That(
                GetField<Text>(debugPanel, "statusLabel").text,
                Is.EqualTo(debugPanel.StatusText));
            InvokePrivate(flowController, "OnDisable");
        }

        [Test]
        public void ResultDisablesEveryButtonAndRefreshesText()
        {
            SetField(boardController, "initialBoardReadyPublished", true);
            InitializeFlow();
            InvokePrivate(flowController, "OnEnable");

            debugPanel.NotifyBossDefeated();

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(debugPanel.StatusText,
                Does.Contain("Result: Victory"));
            Assert.That(debugPanel.EndBattleButtonsInteractable, Is.False);
            InvokePrivate(flowController, "OnDisable");
        }

        [Test]
        public void DisablingPanelDoesNotChangeBattleState()
        {
            SetField(boardController, "initialBoardReadyPublished", true);
            InitializeFlow();
            InvokePrivate(flowController, "OnEnable");
            BattlePhase phase = flowController.Context.Phase;
            BattleResultKind result = flowController.Context.Result;

            InvokePrivate(debugPanel, "OnDisable");

            Assert.That(flowController.Context.Phase, Is.EqualTo(phase));
            Assert.That(flowController.Context.Result, Is.EqualTo(result));
            InvokePrivate(flowController, "OnDisable");
        }

        [Test]
        public void StartRetriesSubscriptionAfterFlowInitialization()
        {
            InvokePrivate(debugPanel, "Awake");
            InvokePrivate(debugPanel, "OnEnable");
            Assert.That(
                GetField<BattleFlowCoordinator>(
                    debugPanel,
                    "subscribedCoordinator"),
                Is.Null);

            SetField(boardController, "initialBoardReadyPublished", true);
            InitializeFlow();
            InvokePrivate(flowController, "OnEnable");
            InvokePrivate(debugPanel, "Start");

            Assert.That(
                GetField<BattleFlowCoordinator>(
                    debugPanel,
                    "subscribedCoordinator"),
                Is.SameAs(flowController.Coordinator));
            Assert.That(debugPanel.StatusText,
                Does.Contain("Phase: PlayerInput"));
            Assert.That(debugPanel.BossIntentText,
                Is.EqualTo("Boss Intent: Unavailable"));
            InvokePrivate(flowController, "OnDisable");
        }

        [Test]
        public void FormatShowsReadonlyBossIntentDetails()
        {
            MethodInfo formatBossIntent = typeof(BattleFlowDebugPanel)
                .GetMethod(
                    "FormatBossIntent",
                    BindingFlags.Static | BindingFlags.NonPublic);

            string formatted = (string)formatBossIntent.Invoke(
                null,
                new object[]
                {
                    new KragmorBattleRuntimeState(
                        KragmorTestConfig.Create())
                        .GetIntentForecast(4)[3].Intent
                });

            Assert.That(formatted, Is.EqualTo(
                "Boss Intent: EarthCollapse | Direct: true | "
                    + "Heavy: true | Coefficient: 2.40"));
        }

        private void InitializeFlow()
        {
            flowController.Initialize(
                new BattleFlowSetup(7, new[] { 8 }));
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
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
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
            return (T)target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(
                target);
        }
    }
}
