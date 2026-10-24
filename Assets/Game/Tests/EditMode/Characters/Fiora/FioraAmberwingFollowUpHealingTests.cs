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
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Fiora;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Fiora
{
    public sealed class FioraAmberwingFollowUpHealingTests
    {
        private FioraAmberwingCombatConfig config;

        [SetUp]
        public void SetUp()
        {
            config = FioraAmberwingTestConfig.Create();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void ProviderRequiresChargeNextTurnAndNonEmptyQueue()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            var provider = Provider(battle, awakening: 0);

            Assert.That(provider.CreateRootActions(
                Context(battle, turn: 1)), Is.Empty);
            battle.State.GrantFollowUpHeals(2, currentTurn: 1);
            Assert.That(provider.CreateRootActions(
                Context(battle, turn: 1)), Is.Empty);
            Assert.That(provider.CreateRootActions(
                Context(battle, turn: 2, hasMatchEvent: false)), Is.Empty);

            IReadOnlyList<CombatAction> actions =
                provider.CreateRootActions(Context(battle, turn: 2));

            Assert.That(actions.Count, Is.EqualTo(1));
            var heal = (HealAction)actions[0];
            Assert.That(heal.Origin, Is.EqualTo(ActionOrigin.System));
            Assert.That(heal.RootActionId, Is.EqualTo(heal.ActionId));
            Assert.That(heal.SourceActionId, Is.Null);
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(2));
        }

        [TestCase(0, 0.06d, 0d, 60L)]
        [TestCase(2, 0.06d, 0.05d, 63L)]
        [TestCase(5, 0.07d, 0.05d, 73L)]
        public void HealUsesResolvedCoefficientLocalIncreaseAndNoCombo(
            int awakening,
            double expectedCoefficient,
            double expectedLocalIncrease,
            long expectedHealing)
        {
            TestBattle battle = CreateBattle(awakening);
            battle.State.GrantFollowUpHeals(2, currentTurn: 1);
            HealAction action = (HealAction)Provider(battle, awakening)
                .CreateRootActions(Context(battle, turn: 2))[0];

            CombatActionExecutionResult result = Execute(
                battle,
                new CombatAction[] { action },
                turn: 2);
            var heal = result.ActionResults
                .OfType<HealActionResult>()
                .Single();

            Assert.That(action.ContextRequest.HealingCoefficient,
                Is.EqualTo(expectedCoefficient));
            Assert.That(action.ContextRequest.ActionLocalHealingIncreaseRate,
                Is.EqualTo(expectedLocalIncrease));
            Assert.That(action.ContextRequest.AppliesCombo, Is.False);
            Assert.That(heal.HealingResult.ComboMultiplier, Is.EqualTo(1d));
            Assert.That(heal.FinalHealing, Is.EqualTo(expectedHealing));
        }

        [Test]
        public void ChargeIsConsumedOnlyAfterCompletedHealEvenAtFullHp()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            battle.State.GrantFollowUpHeals(2, currentTurn: 1);
            IReadOnlyList<CombatAction> actions = Provider(battle, 0)
                .CreateRootActions(Context(battle, turn: 2));

            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(2));

            CombatActionExecutionResult result = Execute(
                battle,
                actions,
                turn: 2);
            HealActionResult heal = result.ActionResults
                .OfType<HealActionResult>()
                .Single();

            Assert.That(heal.AppliedHealing, Is.Zero);
            Assert.That(heal.OverhealAmount, Is.EqualTo(60L));
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(1));
            Assert.That(battle.State.LastFollowUpHealTurn, Is.EqualTo(2));
            Assert.That(battle.Party.Effects.GetActiveEffects(),
                Has.Count.EqualTo(1));
        }

        [Test]
        public void CompletedHealWithoutOverhealConsumesWithoutBlessing()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            var damage = new BossDamageAction(
                battle.ActionIds.Next(),
                new BossDamageContextBuildRequest(
                    battle.Boss,
                    battle.Party,
                    2d,
                    AttackTag.None));
            Execute(
                battle,
                new CombatAction[] { damage },
                turn: 1);
            battle.State.GrantFollowUpHeals(2, currentTurn: 1);
            IReadOnlyList<CombatAction> actions = Provider(battle, 0)
                .CreateRootActions(Context(battle, turn: 2));

            CombatActionExecutionResult result = Execute(
                battle,
                actions,
                turn: 2);
            HealActionResult heal = result.ActionResults
                .OfType<HealActionResult>()
                .Single();

            Assert.That(heal.AppliedHealing, Is.EqualTo(60L));
            Assert.That(heal.OverhealAmount, Is.Zero);
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(1));
            Assert.That(battle.Party.Effects.GetActiveEffects(), Is.Empty);
        }

        [Test]
        public void UnexecutedOrClearedHealPreservesCharge()
        {
            TestBattle battle = CreateBattle(awakening: 0, bossHp: 1);
            battle.State.GrantFollowUpHeals(2, currentTurn: 1);
            HealAction followUp = (HealAction)Provider(battle, 0)
                .CreateRootActions(Context(battle, turn: 2))[0];
            var lethal = new DamageAction(
                battle.ActionIds.Next(),
                ActionOrigin.System,
                new DamageContextBuildRequest(
                    battle.Fiora,
                    battle.Party,
                    battle.Boss,
                    ElementType.Fire,
                    AttackType.Active,
                    AttackTag.None,
                    1d,
                    false,
                    0,
                    false));

            CombatActionExecutionResult result = Execute(
                battle,
                new CombatAction[] { lethal, followUp },
                turn: 2);

            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.ActionResults.OfType<HealActionResult>(),
                Is.Empty);
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(2));
            Assert.That(battle.State.LastFollowUpHealTurn, Is.Null);
        }

        [Test]
        public void ActiveReuseAccumulatesCapsAndBlocksItsUseTurn()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            battle.State.GrantFollowUpHeals(2, currentTurn: 1);
            Assert.That(battle.State.TryConsumeFollowUpHeal(2), Is.True);
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(1));

            battle.State.GrantFollowUpHeals(2, currentTurn: 2);
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(3));
            Assert.That(battle.State.CanExecuteFollowUpHeal(2), Is.False);
            Assert.That(battle.State.CanExecuteFollowUpHeal(3), Is.True);

            battle.State.GrantFollowUpHeals(2, currentTurn: 3);
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(4));
            Assert.That(battle.State.CanExecuteFollowUpHeal(3), Is.False);
        }

        [Test]
        public void CompletedActionCannotConsumeTwiceOrRunTwiceInTurn()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            battle.State.GrantFollowUpHeals(2, currentTurn: 1);
            IReadOnlyList<CombatAction> actions = Provider(battle, 0)
                .CreateRootActions(Context(battle, turn: 2));

            Execute(battle, actions, turn: 2);

            Assert.That(battle.State.TryConsumeFollowUpHeal(2), Is.False);
            Assert.That(Provider(battle, 0).CreateRootActions(
                Context(battle, turn: 2)), Is.Empty);
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(1));
        }

        [Test]
        public void Awakening6ConversionIsSharedWithLaterHealingInTurn()
        {
            TestBattle battle = CreateBattle(awakening: 6);
            battle.State.GrantFollowUpHeals(2, currentTurn: 1);
            IReadOnlyList<CombatAction> followUp = Provider(battle, 6)
                .CreateRootActions(Context(battle, turn: 2));

            Execute(battle, followUp, turn: 2);
            long secondId = battle.ActionIds.Next();
            var secondHeal = new HealAction(
                secondId,
                ActionOrigin.System,
                new HealingContextBuildRequest(
                    battle.Fiora,
                    battle.Party,
                    0.01d,
                    false,
                    0));
            battle.State.RegisterPassiveEligibleHeal(
                secondId,
                FioraAmberwingHealingSource.Match);

            Execute(
                battle,
                new CombatAction[] { secondHeal },
                turn: 2);

            double[] magnitudes = battle.Party.Effects
                .GetActiveEffects()
                .Select(effect => effect.Magnitude)
                .ToArray();
            Assert.That(battle.State.LastAwakening6ConversionTurn,
                Is.EqualTo(2));
            Assert.That(magnitudes, Has.Length.EqualTo(2));
            Assert.That(magnitudes[0], Is.EqualTo(0.05d));
            Assert.That(magnitudes[1],
                Is.EqualTo(0.0055d).Within(0.000000001d));
        }

        [Test]
        public void RegistrarIncludesFollowUpProviderWithSharedState()
        {
            CharacterDefinition definition = CreateDefinition();
            try
            {
                TestBattle battle = CreateBattle(awakening: 6);
                var member = new BattlePartyMemberInput(
                    FioraAmberwingRules.CharacterId,
                    partySlotIndex: 0,
                    level: 100,
                    awakening: 6,
                    definition);

                CharacterCombatProviderRegistrationResult registration =
                    new FioraAmberwingCombatProviderRegistrar().Register(
                        member,
                        battle.Fiora,
                        battle.Boss,
                        battle.ActionIds,
                        new MatchEventActionProviderRegistry(),
                        new ActiveAbilityActionProviderRegistry());

                Assert.That(registration.PreMatchQueueActionProviders,
                    Has.Count.EqualTo(1));
                Assert.That(registration.PreMatchQueueActionProviders[0],
                    Is.TypeOf<FioraAmberwingFollowUpHealingProvider>());
                Assert.That(registration.CombatStatusSources.Single(),
                    Is.TypeOf<FioraAmberwingBattleState>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void BridgeCompletesHealAndBlessingBeforeFirstMatchEvent()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            battle.State.GrantFollowUpHeals(2, currentTurn: 0);
            var coordinator = new BattleFlowCoordinator(2);
            coordinator.StartBattle();
            coordinator.TryBeginBoardResolution();
            coordinator.NotifyBoardActionResolved(SingleMatchCascade(), true);
            var order = new List<string>();
            var bridge = new BattleFlowCombatBridge(
                coordinator,
                battle.Party,
                battle.Boss,
                battle.Executor,
                new DelegateMatchProvider(_ => Array.Empty<CombatAction>()),
                new DelegateBossProvider(_ => Array.Empty<CombatAction>()),
                battle.ActionIds,
                Array.Empty<ActiveAbilityBinding>(),
                new ActiveAbilityActionProviderRegistry(),
                boardMutationSink: null,
                difficultyId: null,
                resultBalance: null,
                boardElementConversionSink: null,
                preMatchQueueBindings: new[]
                {
                    new PreMatchQueueActionProviderBinding(
                        battle.Fiora,
                        Provider(battle, 0))
                });
            bridge.PreMatchQueueCombatActionStepApplied += step =>
                order.Add(step.ActionStep.Action is HealAction
                    ? "heal"
                    : "blessing");
            coordinator.MatchEventExecuting += _ => order.Add("match");

            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.True);
            while (bridge.TryGetNextPreMatchQueueCombatAction(out _, out _))
            {
                Assert.That(bridge.TryApplyNextPreMatchQueueCombatAction(
                    out _), Is.True);
            }
            Assert.That(bridge.TryCompletePreMatchQueueCombat(), Is.True);
            Assert.That(coordinator.TryBeginNextMatchEvent(out _), Is.True);

            Assert.That(order, Is.EqualTo(new[]
            {
                "heal", "blessing", "match"
            }));
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(1));
        }

        private FioraAmberwingFollowUpHealingProvider Provider(
            TestBattle battle,
            int awakening)
        {
            return new FioraAmberwingFollowUpHealingProvider(
                config,
                awakening,
                battle.State);
        }

        private static PreMatchQueueActionContext Context(
            TestBattle battle,
            int turn,
            bool hasMatchEvent = true)
        {
            IReadOnlyList<MatchEvent> events = hasMatchEvent
                ? MatchEventFactory.Create(SingleMatchCascade())
                : Array.Empty<MatchEvent>();
            return new PreMatchQueueActionContext(
                battle.Fiora,
                battle.Party,
                battle.Boss,
                turn,
                matchQueueId: 1,
                events,
                battle.ActionIds);
        }

        private static CombatActionExecutionResult Execute(
            TestBattle battle,
            IReadOnlyList<CombatAction> actions,
            int turn)
        {
            CombatActionExecutionSession session =
                battle.Executor.BeginExecution(actions, turn);
            while (session.TryExecuteNext(out _))
            {
            }

            return session.Result;
        }

        private TestBattle CreateBattle(
            int awakening,
            long bossHp = 1000000)
        {
            var fiora = new CharacterBattleState(
                FioraAmberwingRules.CharacterId,
                0,
                ElementType.Fire,
                1000,
                400d);
            var party = new PartyBattleState(new[] { fiora });
            var boss = new BossBattleState(
                "boss",
                ElementType.Grass,
                bossHp,
                100d);
            var ids = new CombatActionIdSequence();
            FioraAmberwingResolvedCombatRules rules =
                FioraAmberwingCombatRuleResolver.Resolve(config, awakening);
            var state = new FioraAmberwingBattleState(
                fiora,
                rules.ActiveFollowUpHealMaxCount);
            var trigger = new FioraAmberwingOverhealBlessingRule(
                state,
                config,
                awakening,
                ids);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(
                    new ICombatTriggerRule[] { trigger }));
            return new TestBattle(
                fiora,
                party,
                boss,
                ids,
                state,
                executor);
        }

        private CharacterDefinition CreateDefinition()
        {
            CharacterDefinition definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                FioraAmberwingRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Fire;
            serialized.FindProperty("level1Hp").intValue = 1450;
            serialized.FindProperty("level1Attack").intValue = 80;
            serialized.FindProperty("level100Hp").intValue = 5800;
            serialized.FindProperty("level100Attack").intValue = 400;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static BoardCascadeResult SingleMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(new[]
            {
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0),
                    new BoardPosition(2, 0))
            });
        }

        private sealed class TestBattle
        {
            public TestBattle(
                CharacterBattleState fiora,
                PartyBattleState party,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                FioraAmberwingBattleState state,
                CombatActionExecutor executor)
            {
                Fiora = fiora;
                Party = party;
                Boss = boss;
                ActionIds = actionIds;
                State = state;
                Executor = executor;
            }

            public CharacterBattleState Fiora { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public FioraAmberwingBattleState State { get; }
            public CombatActionExecutor Executor { get; }
        }

        private sealed class DelegateMatchProvider
            : IMatchEventActionProvider
        {
            private readonly Func<MatchEventActionContext,
                IReadOnlyList<CombatAction>> createActions;

            public DelegateMatchProvider(
                Func<MatchEventActionContext,
                    IReadOnlyList<CombatAction>> createActions)
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
            private readonly Func<BossCombatActionContext,
                IReadOnlyList<CombatAction>> createActions;

            public DelegateBossProvider(
                Func<BossCombatActionContext,
                    IReadOnlyList<CombatAction>> createActions)
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
