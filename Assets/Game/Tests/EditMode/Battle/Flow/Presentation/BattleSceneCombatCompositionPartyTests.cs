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
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatCompositionPartyTests
    {
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
            IReadOnlyList<BattlePartyMemberInput> members)
        {
            BossDefinition boss = Boss(out KragmorCombatConfig bossConfig);
            return new BattleSceneCombatComposition(
                members,
                new CharacterCombatProviderRegistrarCatalog(
                    new ICharacterCombatProviderRegistrar[]
                    {
                        new TestCharacterCombatProviderRegistrar()
                    }),
                boss,
                bossConfig,
                new BossDifficultyStats("difficulty_test", 10000, 100),
                new SeededRandomSource(1),
                BattleResultBalanceDefaults.Create());
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
            public Type SupportedCombatConfigType =>
                typeof(TestCharacterCombatConfig);

            public CharacterCombatProviderRegistrationResult Register(
                BattlePartyMemberInput member,
                CharacterBattleState character,
                BossBattleState boss,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders)
            {
                var config = (TestCharacterCombatConfig)
                    member.CharacterDefinition.CombatConfig;
                matchProviders.Register(
                    member.CharacterId,
                    new EmptyMatchProvider());
                if (!config.HasActiveAbility)
                {
                    return CharacterCombatProviderRegistrationResult
                        .WithoutActiveAbility();
                }

                string activeAbilityId = $"{member.CharacterId}_active";
                activeProviders.Register(
                    member.CharacterId,
                    activeAbilityId,
                    new EmptyActiveProvider());
                return CharacterCombatProviderRegistrationResult
                    .WithActiveAbility(
                        activeAbilityId,
                        config.ActiveCooldownTurns);
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

        private sealed class TestCharacterCombatConfig
            : CombatConfigDefinition
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
