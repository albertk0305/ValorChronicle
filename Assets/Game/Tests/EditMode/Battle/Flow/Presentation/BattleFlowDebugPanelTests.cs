using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Data.Definitions;

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
        private BossDefinition bossDefinition;

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

            bossDefinition =
                ScriptableObject.CreateInstance<BossDefinition>();
            createdObjects.Add(bossDefinition);
            SetField(bossDefinition, "turnLimit", 7);

            debugPanel = root.AddComponent<BattleFlowDebugPanel>();
            SetField(
                debugPanel,
                "battleFlowController",
                flowController);
            SetField(
                debugPanel,
                "fallbackBossDefinition",
                bossDefinition);
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
        public void InitializationUsesBossTurnLimitAndEmptyActiveListOnce()
        {
            InvokePrivate(debugPanel, "InitializeFlowOnce");
            InvokePrivate(debugPanel, "InitializeFlowOnce");

            Assert.That(debugPanel.HasInitializedFlow, Is.True);
            Assert.That(flowController.Setup.TurnLimit, Is.EqualTo(7));
            Assert.That(flowController.Setup.ActiveAbilityCooldowns, Is.Empty);
            Assert.That(flowController.Context, Is.Not.Null);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.NotStarted));
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
            InvokePrivate(debugPanel, "InitializeFlowOnce");
            InvokePrivate(flowController, "OnEnable");
            InvokePrivate(debugPanel, "RefreshState");

            Assert.That(debugPanel.StatusText, Is.EqualTo(
                "Turn: 1 / 7\nPhase: ActiveInput\nResult: None"));
            Assert.That(
                debugPanel.CompleteActivePhaseInteractable,
                Is.True);
            Assert.That(debugPanel.EndBattleButtonsInteractable, Is.True);
            Assert.That(
                GetField<Text>(debugPanel, "statusLabel").text,
                Is.EqualTo(debugPanel.StatusText));
            Assert.That(
                GetField<Button>(
                    debugPanel,
                    "completeActivePhaseButton").interactable,
                Is.True);

            debugPanel.CompleteActivePhase();

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PuzzleInput));
            Assert.That(
                debugPanel.CompleteActivePhaseInteractable,
                Is.False);
            Assert.That(debugPanel.EndBattleButtonsInteractable, Is.True);
            Assert.That(
                GetField<Button>(
                    debugPanel,
                    "completeActivePhaseButton").interactable,
                Is.False);
            InvokePrivate(flowController, "OnDisable");
        }

        [Test]
        public void ResultDisablesEveryButtonAndRefreshesText()
        {
            SetField(boardController, "initialBoardReadyPublished", true);
            InvokePrivate(debugPanel, "InitializeFlowOnce");
            InvokePrivate(flowController, "OnEnable");

            debugPanel.NotifyBossDefeated();

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(debugPanel.StatusText, Does.EndWith("Result: Victory"));
            Assert.That(
                debugPanel.CompleteActivePhaseInteractable,
                Is.False);
            Assert.That(debugPanel.EndBattleButtonsInteractable, Is.False);
            InvokePrivate(flowController, "OnDisable");
        }

        [Test]
        public void DisablingPanelDoesNotChangeBattleState()
        {
            SetField(boardController, "initialBoardReadyPublished", true);
            InvokePrivate(debugPanel, "InitializeFlowOnce");
            InvokePrivate(flowController, "OnEnable");
            BattlePhase phase = flowController.Context.Phase;
            BattleResultKind result = flowController.Context.Result;

            InvokePrivate(debugPanel, "OnDisable");

            Assert.That(flowController.Context.Phase, Is.EqualTo(phase));
            Assert.That(flowController.Context.Result, Is.EqualTo(result));
            InvokePrivate(flowController, "OnDisable");
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
