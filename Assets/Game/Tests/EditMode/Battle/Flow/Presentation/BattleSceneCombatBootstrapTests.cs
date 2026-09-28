using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatBootstrapTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private GameObject root;
        private BattleFlowController flowController;
        private BattleSceneCombatBootstrap combatBootstrap;
        private GameBootstrapper gameBootstrapper;
        private DefinitionDatabase definitionDatabase;
        private CharacterDefinition mareaDefinition;
        private SaveService saveService;

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
            KragmorCombatConfig kragmorConfig =
                KragmorTestConfig.Create();
            createdObjects.Add(kragmorConfig);
            SetField(bossDefinition, "combatConfig", kragmorConfig);

            mareaDefinition =
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
            MareaBluefangCombatConfig config =
                MareaBluefangTestConfig.Create();
            createdObjects.Add(config);
            SetField(mareaDefinition, "combatConfig", config);

            definitionDatabase = ScriptableObject.CreateInstance<
                DefinitionDatabase>();
            createdObjects.Add(definitionDatabase);
            SetField(
                definitionDatabase,
                "characters",
                new[] { mareaDefinition });
            definitionDatabase.Initialize();

            GameObject gameBootstrapRoot = new GameObject(
                "BattleSceneCombatBootstrapTests.GameBootstrapper");
            gameBootstrapRoot.SetActive(false);
            createdObjects.Add(gameBootstrapRoot);
            gameBootstrapper =
                gameBootstrapRoot.AddComponent<GameBootstrapper>();
            SetField(
                gameBootstrapper,
                "definitionDatabase",
                definitionDatabase);
            SetAutoProperty(
                typeof(GameBootstrapper),
                "Instance",
                target: null,
                value: gameBootstrapper);
            SetAutoProperty(
                typeof(GameBootstrapper),
                "RandomSource",
                gameBootstrapper,
                new SeededRandomSource(1));
            InstallProfile(ProfileWithMarea());

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
            BattleResultBalanceDefinition resultBalance =
                CreateResultBalanceDefinition();
            createdObjects.Add(resultBalance);
            SetField(
                combatBootstrap,
                "fallbackResultBalanceDefinition",
                resultBalance);
        }

        [TearDown]
        public void TearDown()
        {
            SetAutoProperty(
                typeof(GameBootstrapper),
                "Instance",
                target: null,
                value: null);
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
            Assert.That(root.GetComponent<BattleHudController>(), Is.Null);

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
            Assert.That(composition.Party.Characters[0].CharacterId,
                Is.EqualTo("character_marea_bluefang"));
            Assert.That(composition.Party.Characters[0].MaxHp,
                Is.EqualTo(900));
            Assert.That(composition.Party.Characters[0].Attack,
                Is.EqualTo(180d));
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
            Assert.That(composition.Boss.Resources.Get(
                    WaterElementResource.Id).MaxAmount,
                Is.EqualTo(5));
            Assert.That(composition.Boss.Resources.GetAmount(
                    WaterElementResource.Id),
                Is.Zero);
            Assert.That(composition.ResultBalance.RemainingTurnBonusRate,
                Is.EqualTo(0.30m));
            Assert.That(
                composition.KragmorConfig,
                Is.SameAs(((BossDefinition)GetField(
                    combatBootstrap,
                    "fallbackBossDefinition")).CombatConfig));
            Assert.That(composition.ActiveBindings[0].CharacterId,
                Is.EqualTo(mareaDefinition.Id));
        }

        [Test]
        public void HudPresenceDoesNotChangeProductionComposition()
        {
            BattleHudController hudController =
                root.AddComponent<BattleHudController>();
            SetField(
                hudController,
                "battleFlowController",
                flowController);
            InvokePrivate(hudController, "OnEnable");

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");
            InvokePrivate(hudController, "Start");

            Assert.That(combatBootstrap.HasInitializedCombat, Is.True);
            Assert.That(hudController.IsRuntimeConnected, Is.True);
            Assert.That(hudController.Coordinator,
                Is.SameAs(flowController.Coordinator));
            Assert.That(hudController.CombatBridge,
                Is.SameAs(combatBootstrap.CombatComposition.Bridge));
            InvokePrivate(hudController, "OnDisable");
        }

        [Test]
        public void ActiveProfilePresetLevelAndNonZeroSlotReachBattleState()
        {
            ProfileSaveData profile = ProfileWithMarea(
                activePresetIndex: 0,
                partySlotIndex: 2,
                level: 37);
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "inactive_fixture",
                Level = 99,
                Awakening = 0
            });
            profile.Party.Presets[1].CharacterSlotIds[0] =
                "inactive_fixture";
            InstallProfile(profile);
            CharacterStatValues expected = CharacterStatCalculator.Calculate(
                mareaDefinition,
                37);

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedCombat, Is.True);
            Assert.That(
                combatBootstrap.CombatComposition.Party.Characters,
                Has.Count.EqualTo(1));
            CharacterBattleState character =
                combatBootstrap.CombatComposition.Party.Characters[0];
            Assert.That(character.CharacterId,
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(character.PartySlotIndex, Is.EqualTo(2));
            Assert.That(character.MaxHp, Is.EqualTo(expected.MaxHp));
            Assert.That(character.Attack, Is.EqualTo(expected.Attack));
            Assert.That(
                combatBootstrap.CombatComposition.ActiveBindings[0]
                    .PartySlotIndex,
                Is.EqualTo(2));
        }

        [Test]
        public void EmptyPartyStopsBeforeCompositionAndFlowInitialization()
        {
            InstallProfile(SaveTestDataBuilder.Valid());
            LogAssert.Expect(
                LogType.Error,
                new Regex("Status=EmptyParty"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedFlow, Is.False);
            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(combatBootstrap.CombatComposition, Is.Null);
            Assert.That(flowController.Context, Is.Null);
        }

        [Test]
        public void InvalidPartyStopsBeforeCompositionAndFlowInitialization()
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "missing_definition",
                Level = 1,
                Awakening = 0
            });
            profile.Party.Presets[0].CharacterSlotIds[0] =
                "missing_definition";
            InstallProfile(profile);
            LogAssert.Expect(
                LogType.Error,
                new Regex("Status=InvalidParty.*CharacterDefinition"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedFlow, Is.False);
            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(combatBootstrap.CombatComposition, Is.Null);
            Assert.That(flowController.Context, Is.Null);
        }

        [Test]
        public void MissingSaveServiceStopsInitializationWithoutFallback()
        {
            SetAutoProperty(
                typeof(GameBootstrapper),
                "SaveService",
                gameBootstrapper,
                value: null);
            LogAssert.Expect(
                LogType.Error,
                new Regex("SaveService with a current profile is required"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(combatBootstrap.CombatComposition, Is.Null);
            Assert.That(flowController.Context, Is.Null);
        }

        [Test]
        public void UnsupportedCharacterCombatConfigFailsAtomically()
        {
            KragmorCombatConfig unsupported = KragmorTestConfig.Create();
            createdObjects.Add(unsupported);
            SetField(mareaDefinition, "combatConfig", unsupported);
            LogAssert.Expect(
                LogType.Exception,
                new Regex("unsupported CombatConfig"));
            LogAssert.Expect(
                LogType.Error,
                new Regex("Battle Flow initialization failed"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedFlow, Is.False);
            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(combatBootstrap.CombatComposition, Is.Null);
            Assert.That(flowController.Context, Is.Null);
        }

        [Test]
        public void RunningBattleKeepsPartySnapshotAfterProfileChanges()
        {
            InstallProfile(ProfileWithMarea(
                activePresetIndex: 0,
                partySlotIndex: 2,
                level: 37));
            InvokePrivate(combatBootstrap, "InitializeFlowOnce");
            CharacterBattleState battleCharacter =
                combatBootstrap.CombatComposition.Party.Characters[0];

            SaveTransactionResult transaction =
                saveService.ExecuteTransaction(profile =>
                {
                    profile.Party.Presets[0].CharacterSlotIds[2] =
                        string.Empty;
                });

            Assert.That(transaction.IsSuccess, Is.True);
            Assert.That(
                saveService.GetCurrentProfileSnapshot()
                    .Party.Presets[0].CharacterSlotIds[2],
                Is.Empty);
            Assert.That(
                combatBootstrap.CombatComposition.Party.Characters,
                Has.Count.EqualTo(1));
            Assert.That(
                combatBootstrap.CombatComposition.Party.Characters[0],
                Is.SameAs(battleCharacter));
            Assert.That(battleCharacter.PartySlotIndex, Is.EqualTo(2));
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

        [Test]
        public void MissingMareaConfigDoesNotInitialize()
        {
            SetField(mareaDefinition, "combatConfig", null);
            LogAssert.Expect(
                LogType.Exception,
                new Regex("has no CombatConfig"));
            LogAssert.Expect(
                LogType.Error,
                new Regex("Battle Flow initialization failed"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(flowController.Context, Is.Null);
        }

        [Test]
        public void InvalidMareaConfigDoesNotInitialize()
        {
            MareaBluefangCombatConfig invalid =
                MareaBluefangTestConfig.Create(
                    waterElementMaxAmount: 0);
            createdObjects.Add(invalid);
            SetField(mareaDefinition, "combatConfig", invalid);
            LogAssert.Expect(
                LogType.Exception,
                new Regex("Marea's CombatConfig is invalid"));
            LogAssert.Expect(
                LogType.Error,
                new Regex("Battle Flow initialization failed"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(flowController.Context, Is.Null);
        }

        [Test]
        public void WrongKragmorConfigTypeDoesNotInitialize()
        {
            BossDefinition boss = (BossDefinition)GetField(
                combatBootstrap,
                "fallbackBossDefinition");
            MareaBluefangCombatConfig wrong =
                MareaBluefangTestConfig.Create();
            createdObjects.Add(wrong);
            SetField(boss, "combatConfig", wrong);
            LogAssert.Expect(
                LogType.Error,
                new Regex("Kragmor requires a KragmorCombatConfig"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(flowController.Context, Is.Null);
        }

        [Test]
        public void InvalidKragmorConfigDoesNotInitialize()
        {
            BossDefinition boss = (BossDefinition)GetField(
                combatBootstrap,
                "fallbackBossDefinition");
            KragmorCombatConfig invalid = KragmorTestConfig.Create(
                maximumRockCount: -1);
            createdObjects.Add(invalid);
            SetField(boss, "combatConfig", invalid);
            LogAssert.Expect(
                LogType.Error,
                new Regex("Kragmor combat config is invalid"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(flowController.Context, Is.Null);
        }

        [Test]
        public void MissingResultBalanceDoesNotInitialize()
        {
            SetField(
                combatBootstrap,
                "fallbackResultBalanceDefinition",
                null);
            LogAssert.Expect(
                LogType.Error,
                new Regex("No BattleResultBalanceDefinition is available"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(flowController.Context, Is.Null);
        }

        [Test]
        public void InvalidResultBalanceDoesNotInitialize()
        {
            BattleResultBalanceDefinition definition =
                (BattleResultBalanceDefinition)GetField(
                    combatBootstrap,
                    "fallbackResultBalanceDefinition");
            SetField(definition, "remainingTurnBonusPercent", 29);
            LogAssert.Expect(
                LogType.Error,
                new Regex("Battle result balance is invalid"));

            InvokePrivate(combatBootstrap, "InitializeFlowOnce");

            Assert.That(combatBootstrap.HasInitializedCombat, Is.False);
            Assert.That(flowController.Context, Is.Null);
        }

        private static ProfileSaveData ProfileWithMarea(
            int activePresetIndex = 0,
            int partySlotIndex = 0,
            int level = 1)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = MareaBluefangRules.CharacterId,
                Level = level,
                Awakening = 0
            });
            profile.Party.ActivePresetIndex = activePresetIndex;
            profile.Party.Presets[activePresetIndex]
                .CharacterSlotIds[partySlotIndex] =
                    MareaBluefangRules.CharacterId;
            return profile;
        }

        private void InstallProfile(ProfileSaveData profile)
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
            saveService = SaveServiceTestFactory.Create(repository);
            SaveLoadResult load = saveService.LoadOrCreate("ignored");
            Assert.That(load.IsSuccess, Is.True, load.Message);
            SetAutoProperty(
                typeof(GameBootstrapper),
                "SaveService",
                gameBootstrapper,
                saveService);
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

        private static BattleResultBalanceDefinition
            CreateResultBalanceDefinition()
        {
            BattleResultBalanceDefinition definition =
                ScriptableObject.CreateInstance<
                    BattleResultBalanceDefinition>();
            SetField(
                definition,
                "remainingTurnBonusPercent",
                BattleResultBalanceDefaults.RemainingTurnBonusPercent);
            SetField(
                definition,
                "gradeThresholds",
                BattleResultBalanceDefaults.CreateGradeThresholds());
            SetField(
                definition,
                "firstGradeRewards",
                BattleResultBalanceDefaults.CreateFirstGradeRewards());
            SetField(
                definition,
                "difficultyRepeatRewards",
                BattleResultBalanceDefaults
                    .CreateDifficultyRepeatRewards());
            return definition;
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

        private static void SetAutoProperty(
            Type declaringType,
            string propertyName,
            object target,
            object value)
        {
            FieldInfo backingField = declaringType.GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.NonPublic
                    | (target == null
                        ? BindingFlags.Static
                        : BindingFlags.Instance));
            Assert.That(backingField, Is.Not.Null);
            backingField.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field.GetValue(target);
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(
                target.GetType().FullName,
                fieldName);
        }
    }
}
