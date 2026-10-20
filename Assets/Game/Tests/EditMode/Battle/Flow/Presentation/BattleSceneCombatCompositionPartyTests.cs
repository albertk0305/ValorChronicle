using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Relics.Stats;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatCompositionPartyTests
    {
        private const string TriggerInputResourceId = "trigger_input";
        private const string TriggerOutputResourceId = "trigger_output";

        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

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
        public void CompositionCreatesSortedPartyProvidersBindingsAndCooldowns()
        {
            BattlePartyMemberInput memberA = Member(
                "a",
                slot: 0,
                level: 10,
                awakening: 1,
                cooldown: 3);
            BattlePartyMemberInput memberB = Member(
                "b",
                slot: 2,
                level: 20,
                awakening: 2,
                cooldown: 5);
            BattlePartyMemberInput memberC = Member(
                "c",
                slot: 4,
                level: 30,
                awakening: 3,
                cooldown: 7);
            var inputs = new[] { memberC, memberA, memberB };

            using BattleSceneCombatComposition composition = Compose(inputs);

            Assert.That(composition.Party.Characters, Has.Count.EqualTo(3));
            Assert.That(
                composition.Party.Characters.Select(
                    character => character.CharacterId),
                Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(
                composition.Party.Characters.Select(
                    character => character.PartySlotIndex),
                Is.EqualTo(new[] { 0, 2, 4 }));
            Assert.That(composition.MatchProviders.Count, Is.EqualTo(3));
            Assert.That(composition.ActiveProviders.Count, Is.EqualTo(3));
            Assert.That(composition.ActiveBindings, Has.Count.EqualTo(3));
            Assert.That(
                composition.ActiveBindings.Select(
                    binding => binding.ActiveAbilityIndex),
                Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(
                composition.ActiveBindings.Select(
                    binding => binding.PartySlotIndex),
                Is.EqualTo(new[] { 0, 2, 4 }));
            Assert.That(
                composition.FlowSetup.ActiveAbilityCooldowns,
                Is.EqualTo(new[] { 3, 5, 7 }));
            foreach (BattlePartyMemberInput member in inputs)
            {
                Assert.That(composition.MatchProviders.TryResolve(
                    member.CharacterId,
                    out _), Is.True);
                Assert.That(composition.ActiveProviders.TryResolve(
                    member.CharacterId,
                    $"{member.CharacterId}_active",
                    out _), Is.True);
            }

            Assert.That(inputs, Is.EqualTo(
                new[] { memberC, memberA, memberB }));
            Assert.That(memberA.Awakening, Is.EqualTo(1));
            Assert.That(memberB.Awakening, Is.EqualTo(2));
            Assert.That(memberC.Awakening, Is.EqualTo(3));
            Assert.That(composition.Boss.Resources.Count, Is.Zero);
        }

        [Test]
        public void CompositionPassesOneBattleActionIdSequenceToRegistrars()
        {
            var registrar = new TestCharacterCombatProviderRegistrar();
            using BattleSceneCombatComposition composition = Compose(
                new[]
                {
                    Member("a", 0, 1, 0, 3),
                    Member("b", 2, 1, 0, 5)
                },
                registrar);

            Assert.That(registrar.ReceivedActionIds,
                Has.Count.EqualTo(2));
            foreach (CombatActionIdSequence received in
                registrar.ReceivedActionIds)
            {
                Assert.That(received, Is.SameAs(composition.ActionIds));
            }
        }

        [Test]
        public void CompositionCollectsRulesInSlotOrderAndExecutesDerivations()
        {
            var registrar = new TestCharacterCombatProviderRegistrar(
                "left",
                "right");
            registrar.ResourceRequirements.Add(
                new CombatResourceRequirement("shared_charge", 2));
            using BattleSceneCombatComposition composition = Compose(
                new[]
                {
                    Member("right", 4, 1, 0, 5),
                    Member("left", 1, 1, 0, 3)
                },
                registrar);
            PrepareTriggerResources(composition, inputAmount: 1);
            long rootActionId = composition.ActionIds.Next();

            CombatActionExecutionResult result = composition.Executor.Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    Consumption(composition, rootActionId)
                }));

            Assert.That(
                registrar.CreatedRules.Select(
                    rule => rule.Owner.PartySlotIndex),
                Is.EqualTo(new[] { 1, 4 }));
            Assert.That(registrar.MatchedPartySlots,
                Is.EqualTo(new[] { 1, 4 }));
            Assert.That(composition.Boss.Resources.GetAmount(
                TriggerOutputResourceId), Is.EqualTo(2));
            Assert.That(composition.Boss.Resources.Get(
                    "shared_charge").MaxAmount,
                Is.EqualTo(2));
            Assert.That(result.ActionResults, Has.Count.EqualTo(3));
            Assert.That(result.ActionResults[0].Action,
                Is.TypeOf<ConsumeResourceAction>());
            Assert.That(result.ActionResults[1].Action,
                Is.TypeOf<AddResourceAction>());
            Assert.That(result.ActionResults[2].Action,
                Is.TypeOf<AddResourceAction>());

            var actionIds = new HashSet<long>();
            for (int index = 0; index < result.ActionResults.Count; index++)
            {
                Assert.That(actionIds.Add(
                    result.ActionResults[index].Action.ActionId), Is.True);
            }

            for (int index = 1; index < result.ActionResults.Count; index++)
            {
                CombatAction derived = result.ActionResults[index].Action;
                Assert.That(derived.RootActionId, Is.EqualTo(rootActionId));
                Assert.That(derived.SourceActionId,
                    Is.EqualTo(rootActionId));
            }

            foreach (ResourceConsumptionTestTriggerRule rule in
                registrar.CreatedRules)
            {
                Assert.That(rule.ActionIds,
                    Is.SameAs(composition.ActionIds));
                Assert.That(rule.Boss, Is.SameAs(composition.Boss));
                Assert.That(
                    composition.Party.Characters.Contains(rule.Owner),
                    Is.True);
            }
        }

        [Test]
        public void CompositionCollectsRulesFromDifferentRegistrarsInSlotOrder()
        {
            var matchedPartySlots = new List<int>();
            var firstRegistrar = new TestCharacterCombatProviderRegistrar(
                typeof(TestCharacterCombatConfig),
                matchedPartySlots,
                "first");
            var secondRegistrar = new TestCharacterCombatProviderRegistrar(
                typeof(AlternateTestCharacterCombatConfig),
                matchedPartySlots,
                "second");
            using BattleSceneCombatComposition composition =
                ComposeWithRegistrars(
                    new[]
                    {
                        AlternateMember("second", slot: 3, cooldown: 5),
                        Member("first", 1, 1, 0, 3)
                    },
                    new ICharacterCombatProviderRegistrar[]
                    {
                        firstRegistrar,
                        secondRegistrar
                    });
            PrepareTriggerResources(composition, inputAmount: 1);

            composition.Executor.Execute(new CombatActionQueue(
                new CombatAction[]
                {
                    Consumption(
                        composition,
                        composition.ActionIds.Next())
                }));

            Assert.That(matchedPartySlots, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(composition.Boss.Resources.GetAmount(
                TriggerOutputResourceId), Is.EqualTo(2));
        }

        [Test]
        public void CompositionEvaluatesEachConsumptionAndQueuesItsDerivationNext()
        {
            var registrar = new TestCharacterCombatProviderRegistrar("owner");
            using BattleSceneCombatComposition composition = Compose(
                new[] { Member("owner", 2, 1, 0, 3) },
                registrar);
            PrepareTriggerResources(composition, inputAmount: 2);
            long firstRootId = composition.ActionIds.Next();
            long secondRootId = composition.ActionIds.Next();

            CombatActionExecutionResult result = composition.Executor.Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    Consumption(composition, firstRootId),
                    Consumption(composition, secondRootId)
                }));

            Assert.That(result.ActionResults, Has.Count.EqualTo(4));
            Assert.That(
                result.ActionResults.Select(item => item.Action.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(ConsumeResourceAction),
                    typeof(AddResourceAction),
                    typeof(ConsumeResourceAction),
                    typeof(AddResourceAction)
                }));
            Assert.That(result.ActionResults[0].Action.ActionId,
                Is.EqualTo(firstRootId));
            Assert.That(result.ActionResults[1].Action.SourceActionId,
                Is.EqualTo(firstRootId));
            Assert.That(result.ActionResults[2].Action.ActionId,
                Is.EqualTo(secondRootId));
            Assert.That(result.ActionResults[3].Action.SourceActionId,
                Is.EqualTo(secondRootId));
            Assert.That(registrar.MatchedPartySlots,
                Is.EqualTo(new[] { 2, 2 }));
            Assert.That(composition.Boss.Resources.GetAmount(
                TriggerOutputResourceId), Is.EqualTo(2));
        }

        [Test]
        public void CompositionDoesNotInstallRuleForCharacterOutsideParty()
        {
            var registrar = new TestCharacterCombatProviderRegistrar(
                "not_in_party");
            using BattleSceneCombatComposition composition = Compose(
                new[] { Member("present", 0, 1, 0, 3) },
                registrar);
            PrepareTriggerResources(composition, inputAmount: 1);

            CombatActionExecutionResult result = composition.Executor.Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    Consumption(
                        composition,
                        composition.ActionIds.Next())
                }));

            Assert.That(registrar.CreatedRules, Is.Empty);
            Assert.That(result.ActionResults, Has.Count.EqualTo(1));
            Assert.That(composition.Boss.Resources.GetAmount(
                TriggerOutputResourceId), Is.Zero);
        }

        [Test]
        public void SeparateCompositionsReceiveSeparateRuleInstances()
        {
            var registrar = new TestCharacterCombatProviderRegistrar("owner");
            using BattleSceneCombatComposition first = Compose(
                new[] { Member("owner", 0, 1, 0, 3) },
                registrar);
            using BattleSceneCombatComposition second = Compose(
                new[] { Member("owner", 0, 1, 0, 3) },
                registrar);
            PrepareTriggerResources(second, inputAmount: 1);
            ResourceConsumptionTestTriggerRule firstRule =
                registrar.CreatedRules[0];
            ResourceConsumptionTestTriggerRule secondRule =
                registrar.CreatedRules[1];

            second.Executor.Execute(new CombatActionQueue(new CombatAction[]
            {
                Consumption(second, second.ActionIds.Next())
            }));

            Assert.That(firstRule, Is.Not.SameAs(secondRule));
            Assert.That(firstRule.EvaluationCount, Is.Zero);
            Assert.That(secondRule.EvaluationCount, Is.GreaterThan(0));
            Assert.That(firstRule.Boss, Is.SameAs(first.Boss));
            Assert.That(secondRule.Boss, Is.SameAs(second.Boss));
        }

        [Test]
        public void CompositionMergesSharedAndDistinctResourceRequirements()
        {
            var firstRegistrar = new TestCharacterCombatProviderRegistrar();
            firstRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement("shared", 5));
            firstRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement("first_only", 3));
            firstRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement("shared", 5));
            var secondRegistrar = new TestCharacterCombatProviderRegistrar(
                typeof(AlternateTestCharacterCombatConfig));
            secondRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement("shared", 5));
            secondRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement("second_only", 4));

            using BattleSceneCombatComposition composition =
                ComposeWithRegistrars(
                    new[]
                    {
                        AlternateMember("second", slot: 4, cooldown: 5),
                        Member("first", 0, 1, 0, 3)
                    },
                    new ICharacterCombatProviderRegistrar[]
                    {
                        firstRegistrar,
                        secondRegistrar
                    });

            Assert.That(composition.Boss.Resources.Count, Is.EqualTo(3));
            Assert.That(composition.Boss.Resources.Get("shared").MaxAmount,
                Is.EqualTo(5));
            Assert.That(composition.Boss.Resources.Get("first_only")
                .MaxAmount, Is.EqualTo(3));
            Assert.That(composition.Boss.Resources.Get("second_only")
                .MaxAmount, Is.EqualTo(4));
            Assert.That(composition.Boss.Resources.GetAmount("shared"),
                Is.Zero);
        }

        [Test]
        public void CompositionRejectsConflictingResourceMaximums()
        {
            var firstRegistrar = new TestCharacterCombatProviderRegistrar();
            firstRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement("shared", 5));
            var secondRegistrar = new TestCharacterCombatProviderRegistrar(
                typeof(AlternateTestCharacterCombatConfig));
            secondRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement("shared", 7));

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() =>
                    ComposeWithRegistrars(
                        new[]
                        {
                            Member("first", 0, 1, 0, 3),
                            AlternateMember(
                                "second",
                                slot: 1,
                                cooldown: 5)
                        },
                        new ICharacterCombatProviderRegistrar[]
                        {
                            firstRegistrar,
                            secondRegistrar
                        }));

            Assert.That(exception.Message, Does.Contain("shared"));
            Assert.That(exception.Message, Does.Contain("5"));
            Assert.That(exception.Message, Does.Contain("7"));
            Assert.That(firstRegistrar.ReceivedActionIds, Has.Count.EqualTo(1));
            Assert.That(secondRegistrar.ReceivedActionIds,
                Has.Count.EqualTo(1));
        }

        [Test]
        public void ResourceMergeIsIndependentOfPartySlotOrder()
        {
            var firstRegistrar = new TestCharacterCombatProviderRegistrar();
            firstRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement("z_resource", 5));
            var secondRegistrar = new TestCharacterCombatProviderRegistrar(
                typeof(AlternateTestCharacterCombatConfig));
            secondRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement("a_resource", 3));
            var registrars = new ICharacterCombatProviderRegistrar[]
            {
                firstRegistrar,
                secondRegistrar
            };

            using BattleSceneCombatComposition first =
                ComposeWithRegistrars(
                    new[]
                    {
                        Member("first_a", 0, 1, 0, 3),
                        AlternateMember("second_a", 4, 5)
                    },
                    registrars);
            using BattleSceneCombatComposition second =
                ComposeWithRegistrars(
                    new[]
                    {
                        Member("first_b", 4, 1, 0, 3),
                        AlternateMember("second_b", 0, 5)
                    },
                    registrars);

            IReadOnlyList<ResourceState> firstResources =
                first.Boss.Resources.GetAll();
            IReadOnlyList<ResourceState> secondResources =
                second.Boss.Resources.GetAll();
            Assert.That(
                secondResources.Select(resource => resource.ResourceId),
                Is.EqualTo(firstResources.Select(
                    resource => resource.ResourceId)));
            Assert.That(
                secondResources.Select(resource => resource.MaxAmount),
                Is.EqualTo(firstResources.Select(
                    resource => resource.MaxAmount)));
        }

        [Test]
        public void SeparateCompositionsOwnIndependentResourceStates()
        {
            var firstRegistrar = new TestCharacterCombatProviderRegistrar();
            firstRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement(
                    WaterElementResource.Id,
                    WaterElementResource.MaxAmount));
            var secondRegistrar = new TestCharacterCombatProviderRegistrar();
            secondRegistrar.ResourceRequirements.Add(
                new CombatResourceRequirement(
                    WaterElementResource.Id,
                    WaterElementResource.MaxAmount));
            using BattleSceneCombatComposition first = Compose(
                new[] { Member("first", 0, 1, 0, 3) },
                firstRegistrar);
            using BattleSceneCombatComposition second = Compose(
                new[] { Member("second", 0, 1, 0, 3) },
                secondRegistrar);

            first.Boss.Resources.Add(WaterElementResource.Id, 2);

            Assert.That(first.Boss.Resources.GetAmount(
                    WaterElementResource.Id),
                Is.EqualTo(2));
            Assert.That(second.Boss.Resources.GetAmount(
                WaterElementResource.Id), Is.Zero);
            Assert.That(first.Boss.Resources.Get(WaterElementResource.Id),
                Is.Not.SameAs(second.Boss.Resources.Get(
                    WaterElementResource.Id)));
        }

        [Test]
        public void Composition_MaxHpRelicChangesOnlyEquippedMemberAndFlowsToHealingAndShield()
        {
            BattlePartyMemberInput normal = Member(
                "normal",
                slot: 0,
                level: 1,
                awakening: 0,
                cooldown: 3);
            TestCharacterCombatConfig config =
                Config<TestCharacterCombatConfig>();
            config.Initialize(hasActiveAbility: true, activeCooldownTurns: 3);
            CharacterDefinition boostedDefinition = Definition(
                "boosted",
                ElementType.Fire,
                config);
            var boosted = new BattlePartyMemberInput(
                "boosted",
                partySlotIndex: 1,
                level: 1,
                awakening: 0,
                boostedDefinition,
                new RelicStatBonusSummary(0.10d, 0d));

            using BattleSceneCombatComposition composition = Compose(
                new[] { normal, boosted });
            CharacterBattleState normalState = composition.Party.Characters[0];
            CharacterBattleState boostedState = composition.Party.Characters[1];

            Assert.That(normalState.MaxHp, Is.EqualTo(100));
            Assert.That(boostedState.MaxHp, Is.EqualTo(110));
            Assert.That(composition.Party.MaxHp, Is.EqualTo(210));
            Assert.That(normalState.Attack, Is.EqualTo(boostedState.Attack));
            Assert.That(boostedState.PermanentAttackIncreaseRate, Is.Zero);

            HealingContext healing = HealingContextFactory.Build(
                new HealingContextBuildRequest(
                    boostedState,
                    composition.Party,
                    healingCoefficient: 0.5d,
                    appliesCombo: false,
                    finalComboCount: 0));
            ShieldGenerationContext shield =
                ShieldGenerationContextFactory.Build(
                    new ShieldGenerationContextBuildRequest(
                        boostedState,
                        composition.Party,
                        shieldCoefficient: 0.5d,
                        appliesCombo: false,
                        finalComboCount: 0));

            Assert.That(healing.SourceMaxHp, Is.EqualTo(110));
            Assert.That(healing.HealingIncreaseRateSum, Is.Zero);
            Assert.That(
                HealingCalculator.Calculate(healing).FinalHealing,
                Is.EqualTo(55));
            Assert.That(shield.SourceMaxHp, Is.EqualTo(110));
            Assert.That(shield.ShieldAmountIncreaseRateSum, Is.Zero);
            Assert.That(
                ShieldGenerationCalculator.Calculate(shield).FinalShieldAmount,
                Is.EqualTo(55));
        }

        [Test]
        public void Composition_AttackRelicBecomesPermanentRateWithoutChangingBaseAttack()
        {
            TestCharacterCombatConfig config =
                Config<TestCharacterCombatConfig>();
            config.Initialize(hasActiveAbility: true, activeCooldownTurns: 3);
            CharacterDefinition definition = Definition(
                "sword_user",
                ElementType.Fire,
                config);
            var member = new BattlePartyMemberInput(
                "sword_user",
                partySlotIndex: 0,
                level: 1,
                awakening: 0,
                definition,
                new RelicStatBonusSummary(0d, 0.10d));

            using BattleSceneCombatComposition composition = Compose(
                new[] { member });
            CharacterBattleState state = composition.Party.Characters[0];

            Assert.That(state.Attack, Is.EqualTo(10d));
            Assert.That(state.PermanentAttackIncreaseRate,
                Is.EqualTo(0.10d));
        }

        [Test]
        public void ActiveAbilityUsesDenseIndexWithoutChangingOtherCooldowns()
        {
            using BattleSceneCombatComposition composition = Compose(
                new[]
                {
                    Member("a", 0, 1, 0, 3),
                    Member("b", 2, 1, 0, 5),
                    Member("c", 4, 1, 0, 7)
                });
            var coordinator = new BattleFlowCoordinator(
                composition.FlowSetup.TurnLimit,
                composition.FlowSetup.ActiveAbilityCooldowns);
            var bridge = composition.CreateBridge(coordinator);
            ActiveAbilityBinding slotTwoBinding =
                composition.ActiveBindings.Single(
                    binding => binding.PartySlotIndex == 2);

            Assert.That(coordinator.StartBattle(), Is.True);
            Assert.That(
                bridge.TryUseActive(slotTwoBinding.ActiveAbilityIndex),
                Is.True);

            Assert.That(
                coordinator.Context.ActiveAbilities.Select(
                    active => active.RemainingCooldown),
                Is.EqualTo(new[] { 0, 5, 0 }));
        }

        [Test]
        public void CharacterWithoutActiveStillRegistersMatchProvider()
        {
            using BattleSceneCombatComposition composition = Compose(
                new[]
                {
                    Member("active", 0, 1, 0, 4),
                    MemberWithoutActive("passive", 3)
                });

            Assert.That(composition.MatchProviders.Count, Is.EqualTo(2));
            Assert.That(composition.ActiveProviders.Count, Is.EqualTo(1));
            Assert.That(composition.ActiveBindings, Has.Count.EqualTo(1));
            Assert.That(
                composition.ActiveBindings[0].PartySlotIndex,
                Is.EqualTo(0));
            Assert.That(
                composition.FlowSetup.ActiveAbilityCooldowns,
                Is.EqualTo(new[] { 4 }));
        }

        [Test]
        public void UnsupportedCombatConfigFailsCompositionExplicitly()
        {
            CharacterDefinition definition = Definition(
                "unsupported",
                ElementType.Fire,
                Config<UnsupportedTestCombatConfig>());
            var input = new BattlePartyMemberInput(
                definition.Id,
                partySlotIndex: 0,
                level: 1,
                awakening: 0,
                characterDefinition: definition);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => Compose(
                    new[] { input }));

            Assert.That(exception.Message, Does.Contain("unsupported"));
            Assert.That(exception.Message, Does.Contain(
                nameof(UnsupportedTestCombatConfig)));
        }

        [Test]
        public void EmptyPartyIsRejectedBeforeRuntimeConstruction()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Compose(
                Array.Empty<BattlePartyMemberInput>()));
        }

        private BattleSceneCombatComposition Compose(
            IReadOnlyList<BattlePartyMemberInput> members,
            ICharacterCombatProviderRegistrar registrar = null)
        {
            return ComposeWithRegistrars(
                members,
                new ICharacterCombatProviderRegistrar[]
                {
                    registrar
                        ?? new TestCharacterCombatProviderRegistrar()
                });
        }

        private BattleSceneCombatComposition ComposeWithRegistrars(
            IReadOnlyList<BattlePartyMemberInput> members,
            IReadOnlyList<ICharacterCombatProviderRegistrar> registrars)
        {
            BossDefinition boss = Boss(out KragmorCombatConfig bossConfig);
            return new BattleSceneCombatComposition(
                members,
                new CharacterCombatProviderRegistrarCatalog(registrars),
                boss,
                bossConfig,
                new BossDifficultyStats("difficulty_test", 10000, 100),
                new SeededRandomSource(1),
                BattleResultBalanceDefaults.Create());
        }

        private static void PrepareTriggerResources(
            BattleSceneCombatComposition composition,
            int inputAmount)
        {
            composition.Boss.Resources.Register(
                TriggerInputResourceId,
                maxAmount: 10);
            composition.Boss.Resources.Register(
                TriggerOutputResourceId,
                maxAmount: 10);
            composition.Boss.Resources.Add(
                TriggerInputResourceId,
                inputAmount);
        }

        private static ConsumeResourceAction Consumption(
            BattleSceneCombatComposition composition,
            long actionId)
        {
            return new ConsumeResourceAction(
                actionId,
                ActionOrigin.System,
                composition.Boss,
                TriggerInputResourceId,
                consumerId: "test_consumer",
                mode: ResourceConsumptionMode.Amount,
                amount: 1);
        }

        private BattlePartyMemberInput Member(
            string id,
            int slot,
            int level,
            int awakening,
            int cooldown)
        {
            TestCharacterCombatConfig config =
                Config<TestCharacterCombatConfig>();
            config.Initialize(hasActiveAbility: true, cooldown);
            CharacterDefinition definition = Definition(
                id,
                ElementType.Fire,
                config);
            return new BattlePartyMemberInput(
                id,
                slot,
                level,
                awakening,
                definition);
        }

        private BattlePartyMemberInput MemberWithoutActive(
            string id,
            int slot)
        {
            TestCharacterCombatConfig config =
                Config<TestCharacterCombatConfig>();
            config.Initialize(
                hasActiveAbility: false,
                activeCooldownTurns: 0);
            CharacterDefinition definition = Definition(
                id,
                ElementType.Water,
                config);
            return new BattlePartyMemberInput(
                id,
                slot,
                level: 1,
                awakening: 0,
                definition);
        }

        private BattlePartyMemberInput AlternateMember(
            string id,
            int slot,
            int cooldown)
        {
            AlternateTestCharacterCombatConfig config =
                Config<AlternateTestCharacterCombatConfig>();
            config.Initialize(
                hasActiveAbility: true,
                activeCooldownTurns: cooldown);
            CharacterDefinition definition = Definition(
                id,
                ElementType.Water,
                config);
            return new BattlePartyMemberInput(
                id,
                slot,
                level: 1,
                awakening: 0,
                definition);
        }

        private CharacterDefinition Definition(
            string id,
            ElementType element,
            CombatConfigDefinition config)
        {
            var definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("element").enumValueIndex = (int)element;
            serialized.FindProperty("level1Hp").intValue = 100;
            serialized.FindProperty("level1Attack").intValue = 10;
            serialized.FindProperty("level100Hp").intValue = 1000;
            serialized.FindProperty("level100Attack").intValue = 100;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private BossDefinition Boss(out KragmorCombatConfig config)
        {
            var boss = ScriptableObject.CreateInstance<BossDefinition>();
            createdObjects.Add(boss);
            config = KragmorTestConfig.Create();
            createdObjects.Add(config);
            var serialized = new SerializedObject(boss);
            serialized.FindProperty("id").stringValue = "test_boss";
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Grass;
            serialized.FindProperty("turnLimit").intValue = 25;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return boss;
        }

        private TConfig Config<TConfig>()
            where TConfig : CombatConfigDefinition
        {
            TConfig config = ScriptableObject.CreateInstance<TConfig>();
            createdObjects.Add(config);
            return config;
        }

        private sealed class TestCharacterCombatProviderRegistrar
            : ICharacterCombatProviderRegistrar
        {
            private readonly Type supportedCombatConfigType;
            private readonly HashSet<string> triggerOwnerIds;

            public TestCharacterCombatProviderRegistrar(
                params string[] triggerOwnerIds)
                : this(
                    typeof(TestCharacterCombatConfig),
                    new List<int>(),
                    triggerOwnerIds)
            {
            }

            public TestCharacterCombatProviderRegistrar(
                Type supportedCombatConfigType,
                params string[] triggerOwnerIds)
                : this(
                    supportedCombatConfigType,
                    new List<int>(),
                    triggerOwnerIds)
            {
            }

            public TestCharacterCombatProviderRegistrar(
                Type supportedCombatConfigType,
                List<int> matchedPartySlots,
                params string[] triggerOwnerIds)
            {
                this.supportedCombatConfigType = supportedCombatConfigType;
                this.triggerOwnerIds = new HashSet<string>(
                    triggerOwnerIds ?? Array.Empty<string>(),
                    StringComparer.Ordinal);
                MatchedPartySlots = matchedPartySlots
                    ?? throw new ArgumentNullException(
                        nameof(matchedPartySlots));
            }

            public List<CombatActionIdSequence> ReceivedActionIds { get; } =
                new List<CombatActionIdSequence>();
            public List<ResourceConsumptionTestTriggerRule> CreatedRules
                { get; } = new List<ResourceConsumptionTestTriggerRule>();
            public List<int> MatchedPartySlots { get; }
            public List<CombatResourceRequirement> ResourceRequirements
                { get; } = new List<CombatResourceRequirement>();

            public Type SupportedCombatConfigType =>
                supportedCombatConfigType;
            public string CharacterId => "test_character";

            public CharacterCombatProviderRegistrationResult Register(
                BattlePartyMemberInput member,
                CharacterBattleState character,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders)
            {
                ReceivedActionIds.Add(actionIds);
                var config = (ITestCharacterCombatConfig)
                    member.CharacterDefinition.CombatConfig;
                matchProviders.Register(
                    member.CharacterId,
                    new EmptyMatchProvider());
                IReadOnlyList<ICombatTriggerRule> triggerRules =
                    Array.Empty<ICombatTriggerRule>();
                if (triggerOwnerIds.Contains(member.CharacterId))
                {
                    var rule = new ResourceConsumptionTestTriggerRule(
                        character,
                        boss,
                        actionIds,
                        MatchedPartySlots);
                    CreatedRules.Add(rule);
                    triggerRules = new ICombatTriggerRule[] { rule };
                }

                if (!config.HasActiveAbility)
                {
                    return CharacterCombatProviderRegistrationResult
                        .WithoutActiveAbility(
                            triggerRules,
                            ResourceRequirements);
                }

                string activeAbilityId = $"{member.CharacterId}_active";
                activeProviders.Register(
                    member.CharacterId,
                    activeAbilityId,
                    new EmptyActiveProvider());
                return CharacterCombatProviderRegistrationResult
                    .WithActiveAbility(
                        activeAbilityId,
                        config.ActiveCooldownTurns,
                        triggerRules,
                        ResourceRequirements);
            }
        }

        private sealed class ResourceConsumptionTestTriggerRule
            : ICombatTriggerRule
        {
            private readonly List<int> matchedPartySlots;

            public ResourceConsumptionTestTriggerRule(
                CharacterBattleState owner,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                List<int> matchedPartySlots)
            {
                Owner = owner;
                Boss = boss;
                ActionIds = actionIds;
                this.matchedPartySlots = matchedPartySlots;
            }

            public CharacterBattleState Owner { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public int EvaluationCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                EvaluationCount++;
                if (!(context.CompletedResult is
                        ConsumeResourceActionResult consumption)
                    || consumption.ConsumptionRecord == null
                    || !string.Equals(
                        consumption.ConsumptionRecord.ResourceId,
                        TriggerInputResourceId,
                        StringComparison.Ordinal))
                {
                    return Array.Empty<CombatAction>();
                }

                matchedPartySlots.Add(Owner.PartySlotIndex);
                return new CombatAction[]
                {
                    new AddResourceAction(
                        ActionIds.Next(),
                        ActionOrigin.System,
                        Boss,
                        TriggerOutputResourceId,
                        amount: 1,
                        rootActionId: context.RootActionId,
                        sourceActionId: context.ActionId)
                };
            }
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

        private sealed class EmptyActiveProvider
            : IActiveAbilityActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                ActiveAbilityActionContext context)
            {
                return new CombatAction[]
                {
                    new HealAction(
                        context.ActionIds.Next(),
                        ActionOrigin.Active,
                        new HealingContextBuildRequest(
                            context.Character,
                            context.Party,
                            healingCoefficient: 0d,
                            appliesCombo: false,
                            finalComboCount: 0))
                };
            }
        }

        private interface ITestCharacterCombatConfig
        {
            bool HasActiveAbility { get; }
            int ActiveCooldownTurns { get; }
        }

        private sealed class TestCharacterCombatConfig
            : CombatConfigDefinition, ITestCharacterCombatConfig
        {
            public bool HasActiveAbility { get; private set; }
            public int ActiveCooldownTurns { get; private set; }

            public void Initialize(
                bool hasActiveAbility,
                int activeCooldownTurns)
            {
                HasActiveAbility = hasActiveAbility;
                ActiveCooldownTurns = activeCooldownTurns;
            }

            public override bool TryValidate(out string errorMessage)
            {
                errorMessage = string.Empty;
                return ActiveCooldownTurns >= 0;
            }
        }

        private sealed class AlternateTestCharacterCombatConfig
            : CombatConfigDefinition, ITestCharacterCombatConfig
        {
            public bool HasActiveAbility { get; private set; }
            public int ActiveCooldownTurns { get; private set; }

            public void Initialize(
                bool hasActiveAbility,
                int activeCooldownTurns)
            {
                HasActiveAbility = hasActiveAbility;
                ActiveCooldownTurns = activeCooldownTurns;
            }

            public override bool TryValidate(out string errorMessage)
            {
                errorMessage = string.Empty;
                return ActiveCooldownTurns >= 0;
            }
        }

        private sealed class UnsupportedTestCombatConfig
            : CombatConfigDefinition
        {
            public override bool TryValidate(out string errorMessage)
            {
                errorMessage = string.Empty;
                return true;
            }
        }
    }
}
