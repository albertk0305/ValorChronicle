using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatBootstrapTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private GameObject root;
        private BattleFlowController flowController;
        private BattleSceneCombatBootstrap combatBootstrap;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("BattleSceneCombatBootstrapTests");
            root.SetActive(false);
            createdObjects.Add(root);

            BattleBoardController boardController =
                root.AddComponent<BattleBoardController>();
            boardController.enabled = false;
            flowController = root.AddComponent<BattleFlowController>();
            SetField(flowController, "boardController", boardController);
            SetField(flowController, "requireCombatBridge", true);

            BossDefinition bossDefinition =
                ScriptableObject.CreateInstance<BossDefinition>();
            createdObjects.Add(bossDefinition);
            SetField(bossDefinition, "id", "kragmor");
            SetField(bossDefinition, "element", ElementType.Fire);
            SetField(bossDefinition, "turnLimit", 7);
            SetField(
                bossDefinition,
                "difficultyStats",
                new[]
                {
                    new BossDifficultyStats(
                        "difficulty_normal",
                        66000,
                        850d)
                });

            CharacterDefinition mareaDefinition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            createdObjects.Add(mareaDefinition);
            SetField(
                mareaDefinition,
                "id",
                "character_marea_bluefang");
            SetField(mareaDefinition, "element", ElementType.Water);
            SetField(mareaDefinition, "level1Hp", 900);
            SetField(mareaDefinition, "level1Attack", 180);
            SetField(mareaDefinition, "level100Hp", 3400);
            SetField(mareaDefinition, "level100Attack", 1050);

            combatBootstrap =
                root.AddComponent<BattleSceneCombatBootstrap>();
            SetField(
                combatBootstrap,
                "battleFlowController",
                flowController);
            SetField(
                combatBootstrap,
                "fallbackBossDefinition",
                bossDefinition);
            SetField(
                combatBootstrap,
                "fallbackMareaDefinition",
                mareaDefinition);
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
        public void BootstrapComposesRequiredCombatRuntimeOnceWithoutDebugPanel()
        {
            Assert.That(root.GetComponent<BattleFlowDebugPanel>(), Is.Null);

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");
            BattleSceneCombatComposition composition =
                combatBootstrap.CombatComposition;
            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedFlow, Is.True);
            Assert.That(combatBootstrap.HasInitializedCombat, Is.True);
            Assert.That(combatBootstrap.CombatComposition,
                Is.SameAs(composition));
            Assert.That(flowController.Setup.TurnLimit, Is.EqualTo(7));
            Assert.That(flowController.Setup.ActiveAbilityCooldowns,
                Is.EqualTo(new[] { 8 }));
            Assert.That(flowController.Context, Is.Not.Null);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.NotStarted));
            Assert.That(flowController.CombatBridge,
                Is.SameAs(composition.Bridge));
            Assert.That(composition.Marea.CharacterId,
                Is.EqualTo("character_marea_bluefang"));
            Assert.That(composition.Marea.MaxHp, Is.EqualTo(900));
            Assert.That(composition.Marea.Attack, Is.EqualTo(180d));
            Assert.That(composition.Boss.MaxHp, Is.EqualTo(66000));
            Assert.That(composition.Boss.CurrentHp, Is.EqualTo(66000));
            Assert.That(composition.Boss.Attack, Is.EqualTo(850d));
            Assert.That(composition.BossActionProvider, Is.Not.Null);
            Assert.That(composition.BossIntentSource,
                Is.SameAs(composition.BossActionProvider));
            Assert.That(combatBootstrap.BossIntentSource,
                Is.SameAs(composition.BossIntentSource));
            Assert.That(
                combatBootstrap.BossIntentSource.NextIntent.ActionKind,
                Is.EqualTo(KragmorActionKind.ColossusIronFist));
            Assert.That(composition.BossActionProvider.RuntimeState,
                Is.SameAs(composition.KragmorRuntimeState));
            Assert.That(composition.KragmorRuntimeState.PatternIndex, Is.Zero);
            Assert.That(composition.KragmorRuntimeState.NextActionKind,
                Is.EqualTo(KragmorActionKind.ColossusIronFist));
            Assert.That(composition.KragmorRuntimeState.CurrentDefenseState,
                Is.EqualTo(KragmorDefenseState.VolcanicCarapace));
            Assert.That(
                KragmorDefenseEffectFactory.CountActiveDefenseEffects(
                    composition.Boss),
                Is.EqualTo(1));
            Assert.That(composition.Boss.Effects.FindByEffectId(
                    KragmorRules.VolcanicCarapaceEffectId),
                Has.Count.EqualTo(1));
            Assert.That(composition.WaterElement.MaxAmount, Is.EqualTo(5));
            Assert.That(composition.WaterElement.CurrentAmount, Is.Zero);
        }

        [Test]
        public void UnknownDifficultyDoesNotInitializeOrUseFallbackStats()
        {
            SetField(
                combatBootstrap,
                "developmentDifficultyId",
                "difficulty_unknown");
            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "Boss difficulty is not available.*"
                        + "DifficultyId=difficulty_unknown"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedFlow, Is.False);
            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(combatBootstrap.CombatComposition, Is.Null);
            Assert.That(flowController.Context, Is.Null);
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
    }
}
