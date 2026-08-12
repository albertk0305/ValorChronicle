using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class MareaBluefangBattleFlowIntegrationTests
    {
        [Test]
        public void RegistrationResolvesMareaAndLeavesUnknownUnregistered()
        {
            var matchProviders = new MatchEventActionProviderRegistry();
            var activeProviders = new ActiveAbilityActionProviderRegistry();

            MareaBluefangCombatProviderRegistration.Register(
                matchProviders,
                activeProviders);

            Assert.That(matchProviders.Count, Is.EqualTo(1));
            Assert.That(matchProviders.TryResolve(
                MareaBluefangRules.CharacterId,
                out IMatchEventActionProvider matchProvider), Is.True);
            Assert.That(matchProvider,
                Is.TypeOf<MareaBluefangMatchActionProvider>());
            Assert.That(matchProviders.TryResolve(
                "character_unknown",
                out _), Is.False);
            Assert.That(activeProviders.Count, Is.EqualTo(1));
            Assert.That(activeProviders.TryResolve(
                MareaBluefangRules.CharacterId,
                MareaBluefangRules.ActiveAbilityId,
                out IActiveAbilityActionProvider activeProvider), Is.True);
            Assert.That(activeProvider,
                Is.TypeOf<MareaBluefangActiveActionProvider>());
            Assert.That(activeProviders.TryResolve(
                "character_unknown",
                "active_unknown",
                out _), Is.False);
        }

        [Test]
        public void MatchExecutionUsesAscendingSlotsAndCompletesAfterAllCalls()
        {
            PartyBattleState party = new PartyBattleState(new[]
            {
                Character("water_4", 4),
                Character("water_0", 0),
                Character("water_2", 2),
                Character("fire_1", 1, ElementType.Fire)
            });
            BossBattleState boss = Boss();
            BattleFlowCoordinator coordinator = BeginMatchResolution(
                WaterMatch(3));
            var calls = new List<int>();
            var inFlightDuringCalls = new List<bool>();
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                calls.Add(context.Character.PartySlotIndex);
                inFlightDuringCalls.Add(ReferenceEquals(
                    coordinator.CurrentMatchEventExecution,
                    context.Execution));
                return Array.Empty<CombatAction>();
            });
            BattleFlowCombatBridge bridge = CreateBridge(
                coordinator,
                party,
                boss,
                provider,
                new CombatActionIdSequence());
            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(calls, Is.EqualTo(new[] { 0, 2, 4 }));
            Assert.That(inFlightDuringCalls.All(value => value), Is.True);
            Assert.That(coordinator.CurrentMatchEventExecution, Is.Null);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(bridge.LastMatchExecutionResult, Is.Null);
        }

        [Test]
        public void UnregisteredMatchCharacterIsSkippedAndFlowContinues()
        {
            CharacterBattleState unknown = Character("unknown", 0);
            var party = new PartyBattleState(new[] { unknown });
            BossBattleState boss = Boss();
            BattleFlowCoordinator coordinator = BeginMatchResolution(
                WaterMatch(3));
            var actionIds = new CombatActionIdSequence();
            BattleFlowCombatBridge bridge = CreateBridge(
                coordinator,
                party,
                boss,
                new MatchEventActionProviderRegistry(),
                actionIds);
            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(actionIds.LastIssuedId, Is.Zero);
            Assert.That(bridge.LastMatchExecutionResult, Is.Null);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
        }

        [Test]
        public void LaterMareaSnapshotsWaterAfterEarlierCharacterExecution()
        {
            CharacterBattleState generator = Character("generator", 0);
            CharacterBattleState marea = Marea(1);
            PartyBattleState party = new PartyBattleState(
                new[] { marea, generator });
            BossBattleState boss = Boss();
            ResourceState water = WaterElementResource.Register(
                boss.Resources);
            var matchProviders = new MatchEventActionProviderRegistry();
            matchProviders.Register(
                generator.CharacterId,
                new DelegateMatchEventActionProvider(context =>
                    new CombatAction[]
                    {
                        new AddResourceAction(
                            context.ActionIds.Next(),
                            ActionOrigin.Match,
                            context.Boss,
                            WaterElementResource.Id,
                            1)
                    }));
            int waterSeenByMarea = -1;
            matchProviders.Register(
                MareaBluefangRules.CharacterId,
                new ObservingMatchProvider(
                    new MareaBluefangMatchActionProvider(),
                    context => waterSeenByMarea = context.Boss.Resources
                        .Get(WaterElementResource.Id).CurrentAmount));
            BattleFlowCoordinator coordinator = BeginMatchResolution(
                WaterMatch(4));
            BattleFlowCombatBridge bridge = CreateBridge(
                coordinator,
                party,
                boss,
                matchProviders,
                new CombatActionIdSequence());
            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            DamageActionResult damage = bridge.LastMatchExecutionResult
                .ActionResults.OfType<DamageActionResult>().Single();
            Assert.That(waterSeenByMarea, Is.EqualTo(1));
            Assert.That(damage.Context.SkillCoefficient, Is.EqualTo(1.90d));
            Assert.That(damage.Context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.15d));
            Assert.That(damage.DamageResult.FinalDamage, Is.EqualTo(2185L));
            Assert.That(water.CurrentAmount, Is.EqualTo(2));
        }

        [Test]
        public void TriggerDerivedActionCompletesBeforeMareaProviderCall()
        {
            CharacterBattleState generator = Character("generator", 0);
            CharacterBattleState marea = Marea(1);
            PartyBattleState party = new PartyBattleState(
                new[] { generator, marea });
            BossBattleState boss = Boss();
            WaterElementResource.Register(boss.Resources);
            var actionIds = new CombatActionIdSequence();
            var matchProviders = new MatchEventActionProviderRegistry();
            matchProviders.Register(
                generator.CharacterId,
                new DelegateMatchEventActionProvider(context =>
                    new CombatAction[]
                    {
                        CombatIntegrationTestSupport.MatchDamage(context, 0d)
                    }));
            int waterSeenByMarea = -1;
            matchProviders.Register(
                MareaBluefangRules.CharacterId,
                new ObservingMatchProvider(
                    new MareaBluefangMatchActionProvider(),
                    context => waterSeenByMarea = context.Boss.Resources
                        .Get(WaterElementResource.Id).CurrentAmount));
            var derivedWater = new DelegateIntegrationTriggerRule(context =>
            {
                if (!(context.CompletedAction is DamageAction damage)
                    || !string.Equals(
                        damage.ContextRequest.Attacker.CharacterId,
                        generator.CharacterId,
                        StringComparison.Ordinal))
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    new AddResourceAction(
                        actionIds.Next(),
                        ActionOrigin.System,
                        boss,
                        WaterElementResource.Id,
                        1,
                        context.RootActionId,
                        context.ActionId)
                };
            });
            BattleFlowCoordinator coordinator = BeginMatchResolution(
                WaterMatch(4));
            BattleFlowCombatBridge bridge = CreateBridge(
                coordinator,
                party,
                boss,
                matchProviders,
                actionIds,
                new[] { derivedWater });
            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(waterSeenByMarea, Is.EqualTo(1));
            Assert.That(bridge.LastMatchExecutionResult.ActionResults[0],
                Is.TypeOf<DamageActionResult>());
            Assert.That(bridge.LastMatchExecutionResult.ActionResults[1],
                Is.TypeOf<AddResourceActionResult>());
            DamageActionResult mareaDamage = bridge.LastMatchExecutionResult
                .ActionResults.OfType<DamageActionResult>().Last();
            Assert.That(mareaDamage.Context.SkillCoefficient,
                Is.EqualTo(1.90d));
            Assert.That(mareaDamage.Context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.15d));
        }

        [Test]
        public void LethalEarlierCharacterSkipsMareaAndRemainingFlow()
        {
            CharacterBattleState killer = Character(
                "killer",
                0,
                ElementType.Water,
                1000d);
            CharacterBattleState marea = Marea(1);
            PartyBattleState party = new PartyBattleState(
                new[] { killer, marea });
            BossBattleState boss = Boss(50);
            WaterElementResource.Register(boss.Resources);
            int mareaCalls = 0;
            var matchProviders = new MatchEventActionProviderRegistry();
            matchProviders.Register(
                killer.CharacterId,
                new DelegateMatchEventActionProvider(context =>
                    new CombatAction[]
                    {
                        CombatIntegrationTestSupport.MatchDamage(context, 1d)
                    }));
            matchProviders.Register(
                MareaBluefangRules.CharacterId,
                new ObservingMatchProvider(
                    new MareaBluefangMatchActionProvider(),
                    context => mareaCalls++));
            BattleFlowCoordinator coordinator = BeginMatchResolution(
                WaterMatch(3));
            var actionIds = new CombatActionIdSequence();
            BattleFlowCombatBridge bridge = CreateBridge(
                coordinator,
                party,
                boss,
                matchProviders,
                actionIds);
            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(mareaCalls, Is.Zero);
            Assert.That(actionIds.LastIssuedId, Is.EqualTo(1));
            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(coordinator.PendingMatchEventCount, Is.Zero);
            Assert.That(coordinator.HasMatchEventInFlight, Is.False);
            Assert.That(bridge.LastMatchExecutionResult.CompletedActionCount,
                Is.EqualTo(1));
            Assert.That(bridge.LastMatchExecutionResult.StoppedEarly, Is.True);
            Assert.That(
                bridge.LastMatchExecutionResult.ClearedRemainingActions,
                Is.True);
            Assert.That(bridge.ResolveBossAction(1), Is.False);
        }

        [Test]
        public void MissingActiveProviderDoesNotConsumeCooldownOrCreateAction()
        {
            CharacterBattleState marea = Marea(0);
            PartyBattleState party = new PartyBattleState(new[] { marea });
            BossBattleState boss = Boss();
            var coordinator = new BattleFlowCoordinator(
                25,
                new[] { MareaBluefangRules.ActiveCooldownTurns });
            var actionIds = new CombatActionIdSequence();
            BattleFlowCombatBridge bridge = CreateBridge(
                coordinator,
                party,
                boss,
                new MatchEventActionProviderRegistry(),
                actionIds,
                Array.Empty<ICombatTriggerRule>(),
                MareaBinding(),
                new ActiveAbilityActionProviderRegistry());
            coordinator.StartBattle();

            int notificationCount = 0;
            bridge.CombatActionsApplied += () => notificationCount++;
            Assert.That(bridge.CanUseActive(0), Is.False);
            Assert.That(bridge.TryUseActive(0), Is.False);

            ActiveAbilityRuntimeState runtime =
                coordinator.Context.ActiveAbilities[0];
            Assert.That(runtime.RemainingCooldown, Is.Zero);
            Assert.That(runtime.UsedThisTurn, Is.False);
            Assert.That(actionIds.LastIssuedId, Is.Zero);
            Assert.That(marea.Effects.Count, Is.Zero);
            Assert.That(bridge.LastActiveExecutionResult, Is.Null);
            Assert.That(notificationCount, Is.Zero);
        }

        [Test]
        public void ActiveAvailabilityQueryIsSideEffectFreeAndSharedWithUse()
        {
            IntegratedBattle battle = CreateIntegratedMareaBattle(2);
            Assert.That(battle.Bridge.CanUseActive(0), Is.False);
            battle.Coordinator.StartBattle();

            ActiveAbilityRuntimeState runtime =
                battle.Coordinator.Context.ActiveAbilities[0];
            long actionIdBefore = battle.ActionIds.LastIssuedId;
            Assert.That(battle.Bridge.CanUseActive(0), Is.True);
            Assert.That(battle.Bridge.CanUseActive(0), Is.True);
            Assert.That(runtime.RemainingCooldown, Is.Zero);
            Assert.That(runtime.UsedThisTurn, Is.False);
            Assert.That(battle.Marea.Effects.Count, Is.Zero);
            Assert.That(battle.ActionIds.LastIssuedId,
                Is.EqualTo(actionIdBefore));

            int notificationCount = 0;
            battle.Bridge.CombatActionsApplied += () =>
            {
                notificationCount++;
                Assert.That(runtime.RemainingCooldown, Is.EqualTo(8));
                Assert.That(runtime.UsedThisTurn, Is.True);
                Assert.That(battle.Marea.Effects.Count, Is.EqualTo(1));
            };

            Assert.That(battle.Bridge.TryUseActive(0), Is.True);
            Assert.That(notificationCount, Is.EqualTo(1));
            Assert.That(battle.Bridge.CanUseActive(0), Is.False);
            Assert.That(battle.Bridge.TryUseActive(0), Is.False);
            Assert.That(notificationCount, Is.EqualTo(1));

            Assert.That(battle.Coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(battle.Bridge.CanUseActive(0), Is.False);
        }

        [Test]
        public void ActiveAvailabilityRejectsMissingBindingAndInvalidIndex()
        {
            CharacterBattleState marea = Marea(0);
            var party = new PartyBattleState(new[] { marea });
            BossBattleState boss = Boss();
            var coordinator = new BattleFlowCoordinator(
                25,
                new[]
                {
                    MareaBluefangRules.ActiveCooldownTurns,
                    MareaBluefangRules.ActiveCooldownTurns
                });
            var activeProviders = new ActiveAbilityActionProviderRegistry();
            activeProviders.Register(
                MareaBluefangRules.CharacterId,
                MareaBluefangRules.ActiveAbilityId,
                new MareaBluefangActiveActionProvider());
            BattleFlowCombatBridge bridge = CreateBridge(
                coordinator,
                party,
                boss,
                new MatchEventActionProviderRegistry(),
                new CombatActionIdSequence(),
                Array.Empty<ICombatTriggerRule>(),
                MareaBinding(),
                activeProviders);
            coordinator.StartBattle();

            Assert.That(bridge.CanUseActive(0), Is.True);
            Assert.That(bridge.CanUseActive(1), Is.False);
            Assert.That(bridge.CanUseActive(-1), Is.False);
            Assert.That(bridge.CanUseActive(2), Is.False);
            Assert.That(coordinator.Context.ActiveAbilities[1]
                .RemainingCooldown, Is.Zero);
            Assert.That(coordinator.Context.ActiveAbilities[1]
                .UsedThisTurn, Is.False);
        }

        [Test]
        public void ActiveExecutesImmediatelyAndSameTurnMatchSeesBuff()
        {
            IntegratedBattle battle = CreateIntegratedMareaBattle(2);
            battle.Coordinator.StartBattle();

            Assert.That(battle.Bridge.TryUseActive(0), Is.True);
            Assert.That(battle.Marea.Effects.FindByEffectId(
                MareaBluefangRules.ActiveEffectId).Count, Is.EqualTo(1));
            Assert.That(battle.Coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(8));
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(battle.Bridge.LastActiveExecutionResult
                .ActionResults.Single(), Is.TypeOf<ApplyEffectActionResult>());
            long idsAfterFirstUse = battle.ActionIds.LastIssuedId;

            Assert.That(battle.Bridge.TryUseActive(0), Is.False);
            Assert.That(battle.ActionIds.LastIssuedId,
                Is.EqualTo(idsAfterFirstUse));
            Assert.That(battle.Coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(8));

            DamageActionResult damage = ResolveOneMareaMatch(battle);

            Assert.That(damage.Context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.25d));
            Assert.That(damage.Context.DealtDamageIncreaseRateSum, Is.Zero);
            Assert.That(damage.DamageResult.FinalDamage, Is.EqualTo(1125L));
            Assert.That(battle.Water.CurrentAmount, Is.EqualTo(1));
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);
            Assert.That(battle.Marea.Effects.GetActiveEffects()[0]
                .RemainingTurns, Is.EqualTo(2));
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(battle.ActionIds.LastIssuedId, Is.EqualTo(3));
        }

        [Test]
        public void FlowTurnEndKeepsActiveForThreeAttackTurns()
        {
            IntegratedBattle battle = CreateIntegratedMareaBattle(4);
            battle.Coordinator.StartBattle();
            Assert.That(battle.Bridge.TryUseActive(0), Is.True);
            var elementRates = new List<double>();

            for (int turn = 1; turn <= 4; turn++)
            {
                DamageActionResult damage = ResolveOneMareaMatch(battle);
                elementRates.Add(
                    damage.Context.ElementDamageIncreaseRateSum);
                Assert.That(battle.Bridge.ResolveBossAction(turn), Is.True);
            }

            Assert.That(elementRates,
                Is.EqualTo(new[] { 0.25d, 0.25d, 0.25d, 0d }));
            Assert.That(battle.Marea.Effects.Count, Is.Zero);
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
        }

        private static DamageActionResult ResolveOneMareaMatch(
            IntegratedBattle battle)
        {
            Assert.That(battle.Coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(battle.Coordinator.NotifyBoardActionResolved(
                WaterMatch(3),
                true), Is.True);
            Assert.That(battle.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);
            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.True);
            return battle.Bridge.LastMatchExecutionResult.ActionResults
                .OfType<DamageActionResult>()
                .Single();
        }

        private static IntegratedBattle CreateIntegratedMareaBattle(
            int turnLimit)
        {
            CharacterBattleState marea = Marea(0);
            var party = new PartyBattleState(new[] { marea });
            BossBattleState boss = Boss(1000000);
            ResourceState water = WaterElementResource.Register(
                boss.Resources);
            var coordinator = new BattleFlowCoordinator(
                turnLimit,
                new[] { MareaBluefangRules.ActiveCooldownTurns });
            var actionIds = new CombatActionIdSequence();
            var matchProviders = new MatchEventActionProviderRegistry();
            var activeProviders = new ActiveAbilityActionProviderRegistry();
            MareaBluefangCombatProviderRegistration.Register(
                matchProviders,
                activeProviders);
            BattleFlowCombatBridge bridge = CreateBridge(
                coordinator,
                party,
                boss,
                matchProviders,
                actionIds,
                Array.Empty<ICombatTriggerRule>(),
                MareaBinding(),
                activeProviders);
            return new IntegratedBattle(
                marea,
                water,
                coordinator,
                bridge,
                actionIds);
        }

        private static BattleFlowCombatBridge CreateBridge(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            IMatchEventActionProvider matchProvider,
            CombatActionIdSequence actionIds,
            IReadOnlyList<ICombatTriggerRule> triggerRules = null,
            IReadOnlyList<ActiveAbilityBinding> activeBindings = null,
            ActiveAbilityActionProviderRegistry activeProviders = null)
        {
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(123)),
                new CombatTriggerResolver(
                    triggerRules ?? Array.Empty<ICombatTriggerRule>()));
            if (activeBindings == null)
            {
                return new BattleFlowCombatBridge(
                    coordinator,
                    party,
                    boss,
                    executor,
                    matchProvider,
                    EmptyBossProvider(),
                    actionIds);
            }

            return new BattleFlowCombatBridge(
                coordinator,
                party,
                boss,
                executor,
                matchProvider,
                EmptyBossProvider(),
                actionIds,
                activeBindings,
                activeProviders);
        }

        private static IReadOnlyList<ActiveAbilityBinding> MareaBinding()
        {
            return new[]
            {
                new ActiveAbilityBinding(
                    0,
                    0,
                    MareaBluefangRules.CharacterId,
                    MareaBluefangRules.ActiveAbilityId)
            };
        }

        private static IBossCombatActionProvider EmptyBossProvider()
        {
            return new DelegateBossCombatActionProvider(
                context => Array.Empty<CombatAction>());
        }

        private static BattleFlowCoordinator BeginMatchResolution(
            BoardCascadeResult cascade)
        {
            var coordinator = new BattleFlowCoordinator(25);
            coordinator.StartBattle();
            coordinator.TryBeginBoardResolution();
            coordinator.NotifyBoardActionResolved(cascade, true);
            return coordinator;
        }

        private static BoardCascadeResult WaterMatch(int blockCount)
        {
            var positions = new BoardPosition[blockCount];
            for (int index = 0; index < blockCount; index++)
            {
                positions[index] = new BoardPosition(index, 0);
            }

            return BattleFlowTestSupport.CreateCascade(new[]
            {
                BattleFlowTestSupport.Match(ElementType.Water, positions)
            });
        }

        private static CharacterBattleState Marea(int slot)
        {
            return Character(
                MareaBluefangRules.CharacterId,
                slot,
                ElementType.Water,
                1000d);
        }

        private static CharacterBattleState Character(
            string id,
            int slot,
            ElementType element = ElementType.Water,
            double attack = 100d)
        {
            return new CharacterBattleState(
                id,
                slot,
                element,
                1000,
                attack);
        }

        private static BossBattleState Boss(long hp = 100000)
        {
            return new BossBattleState(
                "boss",
                ElementType.Water,
                hp,
                100d);
        }

        private sealed class ObservingMatchProvider
            : IMatchEventActionProvider
        {
            private readonly IMatchEventActionProvider inner;
            private readonly Action<MatchEventActionContext> observe;

            public ObservingMatchProvider(
                IMatchEventActionProvider inner,
                Action<MatchEventActionContext> observe)
            {
                this.inner = inner;
                this.observe = observe;
            }

            public IReadOnlyList<CombatAction> CreateRootActions(
                MatchEventActionContext context)
            {
                observe(context);
                return inner.CreateRootActions(context);
            }
        }

        private sealed class IntegratedBattle
        {
            public IntegratedBattle(
                CharacterBattleState marea,
                ResourceState water,
                BattleFlowCoordinator coordinator,
                BattleFlowCombatBridge bridge,
                CombatActionIdSequence actionIds)
            {
                Marea = marea;
                Water = water;
                Coordinator = coordinator;
                Bridge = bridge;
                ActionIds = actionIds;
            }

            public CharacterBattleState Marea { get; }
            public ResourceState Water { get; }
            public BattleFlowCoordinator Coordinator { get; }
            public BattleFlowCombatBridge Bridge { get; }
            public CombatActionIdSequence ActionIds { get; }
        }
    }
}
