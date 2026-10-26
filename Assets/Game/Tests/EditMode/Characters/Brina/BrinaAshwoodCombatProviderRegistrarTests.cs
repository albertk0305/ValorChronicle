using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodCombatProviderRegistrarTests
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

        [TestCase(0, 8)]
        [TestCase(4, 8)]
        [TestCase(5, 7)]
        [TestCase(6, 7)]
        public void RegisterReturnsCompleteContract(
            int awakening,
            int expectedCooldown)
        {
            RegistrationFixture fixture = Register(
                awakening,
                slot: 1);
            CharacterCombatProviderRegistrationResult result =
                fixture.Result;

            Assert.That(fixture.Registrar.CharacterId,
                Is.EqualTo(BrinaAshwoodRules.CharacterId));
            Assert.That(fixture.Registrar.SupportedCombatConfigType,
                Is.EqualTo(typeof(BrinaAshwoodCombatConfig)));
            Assert.That(result.HasActiveAbility, Is.True);
            Assert.That(result.ActiveAbilityId,
                Is.EqualTo(BrinaAshwoodRules.ActiveAbilityId));
            Assert.That(result.ActiveCooldownTurns,
                Is.EqualTo(expectedCooldown));
            Assert.That(result.TriggerRules.Count, Is.EqualTo(1));
            Assert.That(result.TriggerRules[0],
                Is.TypeOf<BrinaAshwoodMatchCompletionRule>());
            Assert.That(result.ResourceRequirements.Count, Is.Zero);
            Assert.That(result.CombatStatusSources.Count, Is.EqualTo(1));
            Assert.That(result.CombatStatusSources[0],
                Is.TypeOf<BrinaAshwoodBattleState>());
            Assert.That(result.InitialPartyEffects.Count, Is.Zero);
            Assert.That(result.PreMatchQueueActionProviders.Count, Is.Zero);
            Assert.That(result.TriggerRules, Has.None.Null);
            Assert.That(result.CombatStatusSources, Has.None.Null);

            Assert.That(fixture.MatchProviders.TryResolve(
                BrinaAshwoodRules.CharacterId,
                out IMatchEventActionProvider matchProvider), Is.True);
            Assert.That(matchProvider,
                Is.TypeOf<BrinaAshwoodMatchActionProvider>());
            Assert.That(fixture.ActiveProviders.TryResolve(
                BrinaAshwoodRules.CharacterId,
                BrinaAshwoodRules.ActiveAbilityId,
                out IActiveAbilityActionProvider activeProvider), Is.True);
            Assert.That(activeProvider,
                Is.TypeOf<BrinaAshwoodActiveActionProvider>());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        public void StatusUsesActualPartySlotAndStartsVisibleAtZero(int slot)
        {
            RegistrationFixture fixture = Register(0, slot);
            ICharacterCombatStatusSource status =
                fixture.Result.CombatStatusSources.Single();

            Assert.That(status.StatusId,
                Is.EqualTo(BrinaAshwoodRules.WoodcutStatusId));
            Assert.That(status.CharacterId,
                Is.EqualTo(BrinaAshwoodRules.CharacterId));
            Assert.That(status.PartySlotIndex, Is.EqualTo(slot));
            Assert.That(status.CurrentCount, Is.Zero);
            Assert.That(status.IsVisible, Is.True);
        }

        [Test]
        public void TriggerAndStatusShareOneBattleState()
        {
            RegistrationFixture fixture = Register(0, slot: 0);
            var status = (BrinaAshwoodBattleState)fixture.Result
                .CombatStatusSources.Single();
            var trigger = (BrinaAshwoodMatchCompletionRule)fixture.Result
                .TriggerRules.Single();
            FieldInfo field = typeof(BrinaAshwoodMatchCompletionRule)
                .GetField(
                    "battleState",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            Assert.That(field.GetValue(trigger), Is.SameAs(status));

            fixture.ExecuteMatch(BoardMatchTier.Three, playerTurn: 1);

            Assert.That(status.CurrentCount, Is.EqualTo(1));
        }

        [Test]
        public void SeparateRegistrationsHaveIsolatedBattleStates()
        {
            RegistrationFixture first = Register(6, slot: 0);
            RegistrationFixture second = Register(6, slot: 0);
            var firstState = (BrinaAshwoodBattleState)first.Result
                .CombatStatusSources.Single();
            var secondState = (BrinaAshwoodBattleState)second.Result
                .CombatStatusSources.Single();

            first.ExecuteMatch(BoardMatchTier.Three, playerTurn: 3);

            Assert.That(firstState, Is.Not.SameAs(secondState));
            Assert.That(firstState.WoodcutCount, Is.EqualTo(2));
            Assert.That(firstState.LastWoodcutGainTurn, Is.EqualTo(3));
            Assert.That(secondState.WoodcutCount, Is.Zero);
            Assert.That(secondState.LastWoodcutGainTurn, Is.Null);
            Assert.That(second.Character.Effects.Count, Is.Zero);
        }

        [Test]
        public void MatchTriggerAdditionalAndStatusRunAsOneFlow()
        {
            RegistrationFixture fixture = Register(0, slot: 0);
            var state = (BrinaAshwoodBattleState)fixture.Result
                .CombatStatusSources.Single();
            state.GainWoodcut(9);

            CombatActionExecutionResult execution = fixture.ExecuteMatch(
                BoardMatchTier.Four,
                playerTurn: 1);

            Assert.That(execution.ActionResults.Count, Is.EqualTo(2));
            DamageAction match = (DamageAction)execution
                .ActionResults[0].Action;
            DamageAction additional = (DamageAction)execution
                .ActionResults[1].Action;
            Assert.That(state.CurrentCount, Is.EqualTo(1));
            Assert.That(additional.ContextRequest.AppliesCombo, Is.False);
            Assert.That(additional.RootActionId,
                Is.EqualTo(match.ActionId));
            Assert.That(additional.SourceActionId,
                Is.EqualTo(match.ActionId));
        }

        [Test]
        public void RegisteredActiveAndAwakeningSixAffectSameTurnMatch()
        {
            RegistrationFixture fixture = Register(6, slot: 0);
            var state = (BrinaAshwoodBattleState)fixture.Result
                .CombatStatusSources.Single();

            fixture.ExecuteActive();
            fixture.ExecuteMatch(BoardMatchTier.Three, playerTurn: 4);

            Assert.That(state.CurrentCount, Is.EqualTo(3));
            Assert.That(state.LastWoodcutGainTurn, Is.EqualTo(4));
        }

        [Test]
        public void AllRegisteredActionsUseSharedIdSequence()
        {
            RegistrationFixture fixture = Register(0, slot: 0);
            var state = (BrinaAshwoodBattleState)fixture.Result
                .CombatStatusSources.Single();
            state.GainWoodcut(9);

            CombatActionExecutionResult active = fixture.ExecuteActive();
            CombatActionExecutionResult match = fixture.ExecuteMatch(
                BoardMatchTier.Three,
                playerTurn: 1);
            long[] ids = active.ActionResults.Concat(match.ActionResults)
                .Select(item => item.Action.ActionId)
                .ToArray();

            Assert.That(ids, Is.EqualTo(new long[] { 1L, 2L, 3L }));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            Assert.That(fixture.ActionIds.LastIssuedId, Is.EqualTo(3L));
        }

        [Test]
        public void LethalMatchDoesNotRunRegisteredTrigger()
        {
            RegistrationFixture fixture = Register(
                0,
                slot: 0,
                bossHp: 1000L);
            var state = (BrinaAshwoodBattleState)fixture.Result
                .CombatStatusSources.Single();

            CombatActionExecutionResult execution = fixture.ExecuteMatch(
                BoardMatchTier.Three,
                playerTurn: 1);

            Assert.That(execution.BossDefeated, Is.True);
            Assert.That(execution.CompletedActionCount, Is.EqualTo(1));
            Assert.That(state.CurrentCount, Is.Zero);
        }

        [Test]
        public void RegisterRejectsMismatchedMemberStateAndConfig()
        {
            BrinaAshwoodCombatConfig config = Config();
            CharacterDefinition definition = Definition(config);
            var registrar = new BrinaAshwoodCombatProviderRegistrar();
            BossBattleState boss = Boss();

            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(definition, 0, slot: 1),
                    Character(slot: 0),
                    boss,
                    new CombatActionIdSequence(),
                    new MatchEventActionProviderRegistry(),
                    new ActiveAbilityActionProviderRegistry()));

            CharacterDefinition wrongConfig = Definition(null);
            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(wrongConfig, 0, slot: 0),
                    Character(slot: 0),
                    boss,
                    new CombatActionIdSequence(),
                    new MatchEventActionProviderRegistry(),
                    new ActiveAbilityActionProviderRegistry()));

            BrinaAshwoodCombatConfig invalidConfig = Config();
            BrinaAshwoodTestConfig.SetInt(
                invalidConfig,
                "woodcutTriggerThreshold",
                0);
            CharacterDefinition invalidDefinition = Definition(
                invalidConfig);
            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(invalidDefinition, 0, slot: 0),
                    Character(slot: 0),
                    boss,
                    new CombatActionIdSequence(),
                    new MatchEventActionProviderRegistry(),
                    new ActiveAbilityActionProviderRegistry()));
        }

        private RegistrationFixture Register(
            int awakening,
            int slot,
            long bossHp = 1000000L)
        {
            BrinaAshwoodCombatConfig config = Config();
            CharacterDefinition definition = Definition(config);
            CharacterBattleState character = Character(slot);
            BossBattleState boss = Boss(bossHp);
            var actionIds = new CombatActionIdSequence();
            var matchProviders = new MatchEventActionProviderRegistry();
            var activeProviders = new ActiveAbilityActionProviderRegistry();
            var registrar = new BrinaAshwoodCombatProviderRegistrar();
            CharacterCombatProviderRegistrationResult result =
                registrar.Register(
                    Member(definition, awakening, slot),
                    character,
                    boss,
                    actionIds,
                    matchProviders,
                    activeProviders);
            return new RegistrationFixture(
                registrar,
                result,
                character,
                boss,
                actionIds,
                matchProviders,
                activeProviders);
        }

        private BrinaAshwoodCombatConfig Config()
        {
            BrinaAshwoodCombatConfig config =
                BrinaAshwoodTestConfig.Create();
            createdObjects.Add(config);
            return config;
        }

        private CharacterDefinition Definition(
            CombatConfigDefinition config)
        {
            CharacterDefinition definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                BrinaAshwoodRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Grass;
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
            int slot)
        {
            return new BattlePartyMemberInput(
                BrinaAshwoodRules.CharacterId,
                slot,
                level: 1,
                awakening,
                definition);
        }

        private static CharacterBattleState Character(int slot)
        {
            return new CharacterBattleState(
                BrinaAshwoodRules.CharacterId,
                slot,
                ElementType.Grass,
                1000L,
                1000d);
        }

        private static BossBattleState Boss(long hp = 1000000L)
        {
            return new BossBattleState(
                "registrar_test_boss",
                ElementType.Grass,
                hp,
                0d);
        }

        private sealed class RegistrationFixture
        {
            private readonly CombatActionExecutor executor;

            public RegistrationFixture(
                BrinaAshwoodCombatProviderRegistrar registrar,
                CharacterCombatProviderRegistrationResult result,
                CharacterBattleState character,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders)
            {
                Registrar = registrar;
                Result = result;
                Character = character;
                Boss = boss;
                ActionIds = actionIds;
                MatchProviders = matchProviders;
                ActiveProviders = activeProviders;
                Party = new PartyBattleState(new[] { character });
                executor = new CombatActionExecutor(
                    boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(result.TriggerRules));
            }

            public BrinaAshwoodCombatProviderRegistrar Registrar { get; }
            public CharacterCombatProviderRegistrationResult Result { get; }
            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public MatchEventActionProviderRegistry MatchProviders { get; }
            public ActiveAbilityActionProviderRegistry ActiveProviders { get; }

            public CombatActionExecutionResult ExecuteActive()
            {
                ActiveProviders.TryResolve(
                    BrinaAshwoodRules.CharacterId,
                    BrinaAshwoodRules.ActiveAbilityId,
                    out IActiveAbilityActionProvider provider);
                var active = (BrinaAshwoodActiveActionProvider)provider;
                return Execute(active.CreateActions(Character, ActionIds), 0);
            }

            public CombatActionExecutionResult ExecuteMatch(
                BoardMatchTier tier,
                int playerTurn)
            {
                MatchProviders.TryResolve(
                    BrinaAshwoodRules.CharacterId,
                    out IMatchEventActionProvider provider);
                var match = (BrinaAshwoodMatchActionProvider)provider;
                return Execute(
                    match.CreateActions(
                        Character,
                        Party,
                        Boss,
                        tier,
                        finalComboCount: 1,
                        actionIds: ActionIds),
                    playerTurn);
            }

            private CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions,
                int playerTurn)
            {
                CombatActionExecutionSession session =
                    executor.BeginExecution(actions, playerTurn);
                while (session.TryExecuteNext(out _))
                {
                }

                return session.Result;
            }
        }
    }
}
