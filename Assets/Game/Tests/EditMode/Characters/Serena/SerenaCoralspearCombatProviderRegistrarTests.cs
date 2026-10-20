using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Tests.EditMode;

namespace ValorChronicle.Tests.EditMode.Characters.Serena
{
    public sealed class SerenaCoralspearCombatProviderRegistrarTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void RegisterReturnsCompleteSerenaContract()
        {
            SerenaCoralspearCombatConfig config = SerenaConfig();
            CharacterDefinition definition = Definition(config);
            var member = Member(definition, awakening: 5);
            CharacterBattleState serena = Character(
                SerenaCoralspearRules.CharacterId,
                slot: 1);
            BossBattleState boss = Boss();
            var actionIds = new CombatActionIdSequence();
            var matchProviders = new MatchEventActionProviderRegistry();
            var activeProviders = new ActiveAbilityActionProviderRegistry();
            CharacterRuntimeRegistrationCatalog runtimeCatalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            CharacterRuntimeRegistration runtimeRegistration =
                runtimeCatalog.Registrations.Single(item =>
                    item.CharacterId == SerenaCoralspearRules.CharacterId);
            CharacterCombatProviderRegistrarCatalog registrarCatalog =
                runtimeCatalog.CreateCombatRegistrarCatalog();

            CharacterCombatProviderRegistrationResult result =
                registrarCatalog.Register(
                    member,
                    serena,
                    boss,
                    actionIds,
                    matchProviders,
                    activeProviders);

            Assert.That(runtimeRegistration.CharacterId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(runtimeRegistration.SupportedCombatConfigType,
                Is.EqualTo(typeof(SerenaCoralspearCombatConfig)));
            Assert.That(matchProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                out IMatchEventActionProvider matchProvider), Is.True);
            Assert.That(matchProvider,
                Is.TypeOf<SerenaCoralspearMatchActionProvider>());
            Assert.That(activeProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                SerenaCoralspearRules.ActiveAbilityId,
                out IActiveAbilityActionProvider activeProvider), Is.True);
            Assert.That(activeProvider,
                Is.TypeOf<SerenaCoralspearActiveActionProvider>());
            Assert.That(result.HasActiveAbility, Is.True);
            Assert.That(result.ActiveAbilityId,
                Is.EqualTo(SerenaCoralspearRules.ActiveAbilityId));
            Assert.That(result.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(result.TriggerRules.Count, Is.EqualTo(2));
            Assert.That(result.TriggerRules[0],
                Is.TypeOf<SerenaCoralspearMatch5CompletionRule>());
            Assert.That(result.TriggerRules[1],
                Is.TypeOf<SerenaCoralspearPassiveChaseRule>());
            Assert.That(result.ResourceRequirements.Count, Is.EqualTo(1));
            Assert.That(result.ResourceRequirements[0].ResourceId,
                Is.EqualTo(WaterElementResource.Id));
            Assert.That(result.ResourceRequirements[0].MaxAmount,
                Is.EqualTo(WaterElementResource.MaxAmount));
            Assert.That(result.CombatStatusSources, Has.Count.EqualTo(1));
            ICharacterCombatStatusSource statusSource =
                result.CombatStatusSources[0];
            Assert.That(statusSource,
                Is.TypeOf<SerenaCoralspearBattleState>());
            Assert.That(statusSource.StatusId,
                Is.EqualTo(SerenaCoralspearRules.EmpoweredChaseStatusId));
            Assert.That(statusSource.CharacterId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(statusSource.PartySlotIndex, Is.EqualTo(1));
            Assert.That(statusSource.CurrentCount, Is.Zero);
            Assert.That(statusSource.IsVisible, Is.False);
            Assert.That(actionIds.LastIssuedId, Is.Zero);
            Assert.That(boss.Resources.TryGet(
                WaterElementResource.Id,
                out _), Is.False);
        }

        [Test]
        public void RegisteredRulesShareOneEmpoweredChaseState()
        {
            SerenaCoralspearCombatConfig serenaConfig = SerenaConfig();
            MareaBluefangCombatConfig mareaConfig = MareaConfig();
            CharacterDefinition definition = Definition(serenaConfig);
            CharacterBattleState marea = Character(
                MareaBluefangRules.CharacterId,
                slot: 0);
            CharacterBattleState serena = Character(
                SerenaCoralspearRules.CharacterId,
                slot: 1);
            var party = new PartyBattleState(new[] { marea, serena });
            BossBattleState boss = Boss();
            ResourceState water = WaterElementResource.Register(
                boss.Resources,
                WaterElementResource.MaxAmount);
            var actionIds = new CombatActionIdSequence();
            var matchProviders = new MatchEventActionProviderRegistry();
            var activeProviders = new ActiveAbilityActionProviderRegistry();
            CharacterCombatProviderRegistrationResult registration =
                new SerenaCoralspearCombatProviderRegistrar().Register(
                    Member(definition, awakening: 2),
                    serena,
                    boss,
                    actionIds,
                    matchProviders,
                    activeProviders);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(123)),
                new CombatTriggerResolver(registration.TriggerRules));
            ICharacterCombatStatusSource statusSource =
                registration.CombatStatusSources.Single();

            Assert.That(matchProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                out IMatchEventActionProvider provider), Is.True);
            var serenaProvider =
                (SerenaCoralspearMatchActionProvider)provider;
            executor.Execute(new CombatActionQueue(
                serenaProvider.CreateActions(
                    serena,
                    party,
                    boss,
                    BoardMatchTier.FiveOrMore,
                    finalComboCount: 1,
                    actionIds)));
            Assert.That(statusSource.CurrentCount, Is.EqualTo(1));
            Assert.That(statusSource.IsVisible, Is.True);

            executor.Execute(new CombatActionQueue(
                serenaProvider.CreateActions(
                    serena,
                    party,
                    boss,
                    BoardMatchTier.Three,
                    finalComboCount: 1,
                    actionIds)));
            Assert.That(water.CurrentAmount, Is.EqualTo(1));
            var mareaProvider = new MareaBluefangMatchActionProvider(
                mareaConfig);
            CombatActionExecutionResult empowered = executor.Execute(
                new CombatActionQueue(mareaProvider.CreateActions(
                    marea,
                    party,
                    boss,
                    BoardMatchTier.FiveOrMore,
                    finalComboCount: 1,
                    actionIds)));
            DamageActionResult empoweredChase = Chase(empowered);

            Assert.That(empoweredChase.Context
                    .AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.90d).Within(0.000000001d));
            Assert.That(empoweredChase.Action.RootActionId,
                Is.Not.EqualTo(empoweredChase.Action.ActionId));
            Assert.That(empoweredChase.Action.SourceActionId,
                Is.Not.Null);
            Assert.That(water.CurrentAmount, Is.Zero);
            Assert.That(statusSource.CurrentCount, Is.Zero);
            Assert.That(statusSource.IsVisible, Is.False);

            water.Add(1);
            CombatActionExecutionResult ordinary = executor.Execute(
                new CombatActionQueue(mareaProvider.CreateActions(
                    marea,
                    party,
                    boss,
                    BoardMatchTier.FiveOrMore,
                    finalComboCount: 1,
                    actionIds)));

            Assert.That(Chase(ordinary).Context
                    .AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.10d).Within(0.000000001d));
            long[] issuedIds = empowered.ActionResults
                .Concat(ordinary.ActionResults)
                .Select(item => item.Action.ActionId)
                .ToArray();
            Assert.That(issuedIds.Distinct().Count(),
                Is.EqualTo(issuedIds.Length));
        }

        [Test]
        public void MareaAndSerenaWaterRequirementsMergeOnce()
        {
            SerenaCoralspearCombatConfig serenaConfig = SerenaConfig();
            MareaBluefangCombatConfig mareaConfig = MareaConfig();
            BossBattleState boss = Boss();
            var actionIds = new CombatActionIdSequence();
            var matchProviders = new MatchEventActionProviderRegistry();
            var activeProviders = new ActiveAbilityActionProviderRegistry();
            CharacterCombatProviderRegistrarCatalog registrarCatalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateCombatRegistrarCatalog();
            CharacterCombatProviderRegistrationResult marea =
                registrarCatalog.Register(
                    Member(
                        Definition(
                            mareaConfig,
                            MareaBluefangRules.CharacterId),
                        awakening: 0,
                        MareaBluefangRules.CharacterId,
                        slot: 0),
                    Character(MareaBluefangRules.CharacterId, 0),
                    boss,
                    actionIds,
                    matchProviders,
                    activeProviders);
            CharacterCombatProviderRegistrationResult serena =
                registrarCatalog.Register(
                    Member(
                        Definition(serenaConfig),
                        awakening: 0,
                        slot: 1),
                    Character(SerenaCoralspearRules.CharacterId, 1),
                    boss,
                    actionIds,
                    matchProviders,
                    activeProviders);

            IReadOnlyList<CombatResourceRequirement> merged =
                CombatResourceRequirementMerger.Merge(
                    marea.ResourceRequirements
                        .Concat(serena.ResourceRequirements)
                        .ToArray());

            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0].ResourceId,
                Is.EqualTo(WaterElementResource.Id));
            Assert.That(merged[0].MaxAmount,
                Is.EqualTo(WaterElementResource.MaxAmount));
            Assert.That(matchProviders.Count, Is.EqualTo(2));
            Assert.That(activeProviders.Count, Is.EqualTo(2));
            Assert.That(marea.CombatStatusSources, Is.Empty);
            Assert.That(serena.CombatStatusSources, Has.Count.EqualTo(1));
        }

        [Test]
        public void DefaultCatalogComposesCanonicalSerenaBattle()
        {
            CharacterDefinition definition = AssetDatabase.LoadAssetAtPath<
                CharacterDefinition>(
                    "Assets/Data/Characters/serena_coralspear.asset");
            Assert.That(definition, Is.Not.Null);

            using BattleSceneCombatComposition composition =
                ComposeCanonical(new[]
                {
                    Member(definition, awakening: 0, slot: 0)
                });
            CharacterBattleState serena = composition.Party.Characters[0];
            Assert.That(serena.CharacterId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(serena.MaxHp, Is.EqualTo(870));
            Assert.That(serena.Attack, Is.EqualTo(170d));
            Assert.That(composition.MatchProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                out IMatchEventActionProvider match), Is.True);
            Assert.That(composition.ActiveProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                SerenaCoralspearRules.ActiveAbilityId,
                out IActiveAbilityActionProvider active), Is.True);
            Assert.That(composition.ActiveBindings.Count, Is.EqualTo(1));
            Assert.That(composition.CombatStatusSources,
                Has.Count.EqualTo(1));
            Assert.That(composition.CombatStatusSources[0].CharacterId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(composition.CombatStatusSources[0].PartySlotIndex,
                Is.Zero);

            ResourceState water = composition.Boss.Resources.Get(
                WaterElementResource.Id);
            Assert.That(water.MaxAmount,
                Is.EqualTo(WaterElementResource.MaxAmount));
            composition.Executor.Execute(new CombatActionQueue(
                ((SerenaCoralspearMatchActionProvider)match).CreateActions(
                    serena,
                    composition.Party,
                    composition.Boss,
                    BoardMatchTier.Three,
                    finalComboCount: 1,
                    composition.ActionIds)));
            Assert.That(water.CurrentAmount, Is.EqualTo(1));

            composition.Executor.Execute(new CombatActionQueue(
                ((SerenaCoralspearActiveActionProvider)active)
                    .CreateActions(serena, composition.ActionIds)));
            Assert.That(serena.Effects.FindByEffectId(
                    SerenaCoralspearRules.ActiveEffectId).Count,
                Is.EqualTo(1));
        }

        [Test]
        public void DefaultCatalogComposesCanonicalMareaAndSerenaBattle()
        {
            CharacterDefinition mareaDefinition =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                    "Assets/Data/Characters/marea_bluefang.asset");
            CharacterDefinition serenaDefinition =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                    "Assets/Data/Characters/serena_coralspear.asset");
            Assert.That(mareaDefinition, Is.Not.Null);
            Assert.That(serenaDefinition, Is.Not.Null);

            using BattleSceneCombatComposition composition =
                ComposeCanonical(new[]
                {
                    Member(
                        mareaDefinition,
                        awakening: 0,
                        MareaBluefangRules.CharacterId,
                        slot: 0),
                    Member(serenaDefinition, awakening: 0, slot: 1)
                });
            CharacterBattleState marea = composition.Party.Characters[0];
            CharacterBattleState serena = composition.Party.Characters[1];
            Assert.That(composition.Boss.Resources.GetAll().Count(item =>
                    item.ResourceId == WaterElementResource.Id),
                Is.EqualTo(1));
            ResourceState water = composition.Boss.Resources.Get(
                WaterElementResource.Id);
            Assert.That(water.MaxAmount,
                Is.EqualTo(WaterElementResource.MaxAmount));
            Assert.That(composition.MatchProviders.Count, Is.EqualTo(2));
            Assert.That(composition.ActiveProviders.Count, Is.EqualTo(2));
            Assert.That(composition.MatchProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                out IMatchEventActionProvider serenaMatch), Is.True);
            Assert.That(composition.MatchProviders.TryResolve(
                MareaBluefangRules.CharacterId,
                out IMatchEventActionProvider mareaMatch), Is.True);

            composition.Executor.Execute(new CombatActionQueue(
                ((SerenaCoralspearMatchActionProvider)serenaMatch)
                    .CreateActions(
                        serena,
                        composition.Party,
                        composition.Boss,
                        BoardMatchTier.FiveOrMore,
                        1,
                        composition.ActionIds)));
            composition.Executor.Execute(new CombatActionQueue(
                ((SerenaCoralspearMatchActionProvider)serenaMatch)
                    .CreateActions(
                        serena,
                        composition.Party,
                        composition.Boss,
                        BoardMatchTier.Three,
                        1,
                        composition.ActionIds)));
            Assert.That(water.CurrentAmount, Is.EqualTo(1));

            CombatActionExecutionResult result = composition.Executor.Execute(
                new CombatActionQueue(
                    ((MareaBluefangMatchActionProvider)mareaMatch)
                        .CreateActions(
                            marea,
                            composition.Party,
                            composition.Boss,
                            BoardMatchTier.FiveOrMore,
                            1,
                            composition.ActionIds)));
            DamageActionResult chase = Chase(result);
            Assert.That(water.CurrentAmount, Is.Zero);
            Assert.That(chase.Context.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.80d).Within(0.000000001d));
            long[] actionIds = result.ActionResults
                .Select(item => item.Action.ActionId)
                .ToArray();
            Assert.That(actionIds.Distinct().Count(),
                Is.EqualTo(actionIds.Length));
        }

        [Test]
        public void RegisteredActiveBuffsAdditionalButNotMatch()
        {
            SerenaCoralspearCombatConfig config = SerenaConfig();
            MareaBluefangCombatConfig mareaConfig = MareaConfig();
            CharacterDefinition definition = Definition(config);
            CharacterBattleState marea = Character(
                MareaBluefangRules.CharacterId,
                0);
            CharacterBattleState serena = Character(
                SerenaCoralspearRules.CharacterId,
                1);
            var party = new PartyBattleState(new[] { marea, serena });
            BossBattleState boss = Boss();
            ResourceState water = WaterElementResource.Register(
                boss.Resources,
                WaterElementResource.MaxAmount);
            var actionIds = new CombatActionIdSequence();
            var matchProviders = new MatchEventActionProviderRegistry();
            var activeProviders = new ActiveAbilityActionProviderRegistry();
            CharacterCombatProviderRegistrationResult registration =
                new SerenaCoralspearCombatProviderRegistrar().Register(
                    Member(definition, awakening: 2, slot: 1),
                    serena,
                    boss,
                    actionIds,
                    matchProviders,
                    activeProviders);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(123)),
                new CombatTriggerResolver(registration.TriggerRules));
            Assert.That(activeProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                SerenaCoralspearRules.ActiveAbilityId,
                out IActiveAbilityActionProvider active), Is.True);
            Assert.That(matchProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                out IMatchEventActionProvider match), Is.True);

            executor.Execute(new CombatActionQueue(
                ((SerenaCoralspearActiveActionProvider)active)
                    .CreateActions(serena, actionIds)));
            CombatActionExecutionResult mareaMatch = executor.Execute(
                new CombatActionQueue(
                    new MareaBluefangMatchActionProvider(mareaConfig)
                        .CreateActions(
                            marea,
                            party,
                            boss,
                            BoardMatchTier.Three,
                            1,
                            actionIds)));
            Assert.That(water.CurrentAmount, Is.EqualTo(1));
            Assert.That(mareaMatch.ActionResults
                    .OfType<DamageActionResult>()
                    .Single()
                    .Context.AttackTypeDamageIncreaseRateSum,
                Is.Zero);
            CombatActionExecutionResult result = executor.Execute(
                new CombatActionQueue(
                    ((SerenaCoralspearMatchActionProvider)match)
                        .CreateActions(
                            serena,
                            party,
                            boss,
                            BoardMatchTier.Four,
                            1,
                            actionIds)));
            DamageActionResult[] damage = result.ActionResults
                .OfType<DamageActionResult>()
                .ToArray();

            Assert.That(damage.Length, Is.EqualTo(2));
            Assert.That(damage[0].Action.Origin,
                Is.EqualTo(ActionOrigin.Match));
            Assert.That(damage[0].Context
                    .AttackTypeDamageIncreaseRateSum,
                Is.Zero);
            Assert.That(damage[1].Action.Origin,
                Is.EqualTo(ActionOrigin.Additional));
            Assert.That(damage[1].Context
                    .AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.45d).Within(0.000000001d));
            Assert.That(serena.Effects.Count, Is.EqualTo(1));
        }

        [Test]
        public void SeparateRegistrationsHaveIndependentBattleState()
        {
            SerenaCoralspearCombatConfig config = SerenaConfig();
            CharacterDefinition firstDefinition = Definition(config);
            SerenaCoralspearCombatConfig secondConfig = SerenaConfig();
            CharacterDefinition secondDefinition = Definition(secondConfig);

            IntegratedRegistration first = RegisterIntegrated(
                firstDefinition);
            IntegratedRegistration second = RegisterIntegrated(
                secondDefinition);

            first.ExecuteSerenaFiveMatch();
            DamageActionResult firstChase = first.ExecuteMareaFiveMatch();
            DamageActionResult secondChase = second.ExecuteMareaFiveMatch();

            Assert.That(firstChase.Context
                    .AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.80d).Within(0.000000001d));
            Assert.That(secondChase.Context
                    .AttackTypeDamageIncreaseRateSum,
                Is.Zero);
        }

        [Test]
        public void RegisterRejectsNullAndInvalidConfigInputs()
        {
            SerenaCoralspearCombatConfig validConfig = SerenaConfig();
            CharacterDefinition validDefinition = Definition(validConfig);
            var registrar = new SerenaCoralspearCombatProviderRegistrar();
            CharacterBattleState serena = Character(
                SerenaCoralspearRules.CharacterId,
                0);
            BossBattleState boss = Boss();
            var actionIds = new CombatActionIdSequence();
            var matchProviders = new MatchEventActionProviderRegistry();
            var activeProviders = new ActiveAbilityActionProviderRegistry();

            Assert.Throws<ArgumentNullException>(() => registrar.Register(
                null,
                serena,
                boss,
                actionIds,
                matchProviders,
                activeProviders));
            Assert.Throws<ArgumentNullException>(() => registrar.Register(
                Member(validDefinition, 0),
                serena,
                boss,
                actionIds,
                null,
                activeProviders));

            CharacterDefinition missingConfig = Definition(null);
            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(missingConfig, 0),
                    serena,
                    boss,
                    actionIds,
                    matchProviders,
                    activeProviders));

            CharacterDefinition wrongConfig = Definition(MareaConfig());
            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(wrongConfig, 0),
                    serena,
                    boss,
                    actionIds,
                    matchProviders,
                    activeProviders));

            SerenaCoralspearCombatConfig invalidConfig = SerenaConfig();
            var serialized = new SerializedObject(invalidConfig);
            serialized.FindProperty("activeCooldownTurns").intValue = -1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CharacterDefinition invalidDefinition = Definition(
                invalidConfig);

            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(invalidDefinition, 0),
                    serena,
                    boss,
                    actionIds,
                    matchProviders,
                    activeProviders));
        }

        private IntegratedRegistration RegisterIntegrated(
            CharacterDefinition definition)
        {
            CharacterBattleState marea = Character(
                MareaBluefangRules.CharacterId,
                0);
            CharacterBattleState serena = Character(
                SerenaCoralspearRules.CharacterId,
                1);
            var party = new PartyBattleState(new[] { marea, serena });
            BossBattleState boss = Boss();
            ResourceState water = WaterElementResource.Register(
                boss.Resources,
                WaterElementResource.MaxAmount);
            var actionIds = new CombatActionIdSequence();
            var matchProviders = new MatchEventActionProviderRegistry();
            CharacterCombatProviderRegistrationResult registration =
                new SerenaCoralspearCombatProviderRegistrar().Register(
                    Member(definition, 0, slot: 1),
                    serena,
                    boss,
                    actionIds,
                    matchProviders,
                    new ActiveAbilityActionProviderRegistry());
            matchProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                out IMatchEventActionProvider provider);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(123)),
                new CombatTriggerResolver(registration.TriggerRules));
            return new IntegratedRegistration(
                party,
                boss,
                water,
                actionIds,
                executor,
                (SerenaCoralspearMatchActionProvider)provider,
                new MareaBluefangMatchActionProvider(MareaConfig()),
                serena,
                marea);
        }

        private static BattleSceneCombatComposition ComposeCanonical(
            IReadOnlyList<BattlePartyMemberInput> members)
        {
            BossDefinition boss = AssetDatabase.LoadAssetAtPath<
                BossDefinition>("Assets/Data/Bosses/boss_kragmor.asset");
            Assert.That(boss, Is.Not.Null);
            var config = boss.CombatConfig as KragmorCombatConfig;
            Assert.That(config, Is.Not.Null);
            return new BattleSceneCombatComposition(
                members,
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateCombatRegistrarCatalog(),
                boss,
                config,
                new BossDifficultyStats(
                    "difficulty_serena_registration_test",
                    maxHp: 1000000,
                    attack: 100d),
                new SeededRandomSource(123),
                BattleResultBalanceDefaults.Create());
        }

        private SerenaCoralspearCombatConfig SerenaConfig()
        {
            SerenaCoralspearCombatConfig config =
                SerenaCoralspearTestConfig.Create();
            createdObjects.Add(config);
            return config;
        }

        private MareaBluefangCombatConfig MareaConfig()
        {
            MareaBluefangCombatConfig config =
                MareaBluefangTestConfig.Create();
            createdObjects.Add(config);
            return config;
        }

        private CharacterDefinition Definition(
            CombatConfigDefinition config,
            string characterId = SerenaCoralspearRules.CharacterId)
        {
            CharacterDefinition definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = characterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Water;
            serialized.FindProperty("level1Hp").intValue = 1000;
            serialized.FindProperty("level1Attack").intValue = 1000;
            serialized.FindProperty("level100Hp").intValue = 1000;
            serialized.FindProperty("level100Attack").intValue = 1000;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static BattlePartyMemberInput Member(
            CharacterDefinition definition,
            int awakening,
            string characterId = SerenaCoralspearRules.CharacterId,
            int slot = 1)
        {
            return new BattlePartyMemberInput(
                characterId,
                slot,
                level: 1,
                awakening,
                definition);
        }

        private static CharacterBattleState Character(string id, int slot)
        {
            return new CharacterBattleState(
                id,
                slot,
                ElementType.Water,
                maxHp: 1000,
                attack: 1000d);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "registrar_test_boss",
                ElementType.Water,
                maxHp: 1000000,
                attack: 100d);
        }

        private static DamageActionResult Chase(
            CombatActionExecutionResult result)
        {
            return result.ActionResults
                .OfType<DamageActionResult>()
                .Single(item => item.Action.Origin == ActionOrigin.Chase);
        }

        private sealed class IntegratedRegistration
        {
            private readonly PartyBattleState party;
            private readonly BossBattleState boss;
            private readonly ResourceState water;
            private readonly CombatActionIdSequence actionIds;
            private readonly CombatActionExecutor executor;
            private readonly SerenaCoralspearMatchActionProvider serenaProvider;
            private readonly MareaBluefangMatchActionProvider mareaProvider;
            private readonly CharacterBattleState serena;
            private readonly CharacterBattleState marea;

            public IntegratedRegistration(
                PartyBattleState party,
                BossBattleState boss,
                ResourceState water,
                CombatActionIdSequence actionIds,
                CombatActionExecutor executor,
                SerenaCoralspearMatchActionProvider serenaProvider,
                MareaBluefangMatchActionProvider mareaProvider,
                CharacterBattleState serena,
                CharacterBattleState marea)
            {
                this.party = party;
                this.boss = boss;
                this.water = water;
                this.actionIds = actionIds;
                this.executor = executor;
                this.serenaProvider = serenaProvider;
                this.mareaProvider = mareaProvider;
                this.serena = serena;
                this.marea = marea;
            }

            public void ExecuteSerenaFiveMatch()
            {
                executor.Execute(new CombatActionQueue(
                    serenaProvider.CreateActions(
                        serena,
                        party,
                        boss,
                        BoardMatchTier.FiveOrMore,
                        1,
                        actionIds)));
            }

            public DamageActionResult ExecuteMareaFiveMatch()
            {
                water.Add(1);
                return Chase(executor.Execute(new CombatActionQueue(
                    mareaProvider.CreateActions(
                        marea,
                        party,
                        boss,
                        BoardMatchTier.FiveOrMore,
                        1,
                        actionIds))));
            }
        }
    }
}
