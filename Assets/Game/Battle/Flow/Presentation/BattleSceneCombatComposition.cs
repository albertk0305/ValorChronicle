using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatComposition
    {
        private readonly IReadOnlyList<ActiveAbilityBinding> activeBindings;

        public BattleSceneCombatComposition(
            CharacterDefinition mareaDefinition,
            int mareaLevel,
            BossDefinition bossDefinition,
            BossDifficultyStats bossDifficultyStats,
            IRandomSource randomSource)
        {
            if (mareaDefinition == null)
            {
                throw new ArgumentNullException(nameof(mareaDefinition));
            }

            if (!string.Equals(
                    mareaDefinition.Id,
                    MareaBluefangRules.CharacterId,
                    StringComparison.Ordinal)
                || mareaDefinition.Element != ElementType.Water)
            {
                throw new ArgumentException(
                    "Marea's canonical Water CharacterDefinition is required.",
                    nameof(mareaDefinition));
            }

            if (bossDefinition == null)
            {
                throw new ArgumentNullException(nameof(bossDefinition));
            }

            if (bossDifficultyStats == null)
            {
                throw new ArgumentNullException(
                    nameof(bossDifficultyStats));
            }

            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            Marea = CharacterBattleStateFactory.Create(
                mareaDefinition,
                mareaLevel,
                partySlotIndex: 0);
            Boss = new BossBattleState(
                bossDefinition.Id,
                bossDefinition.Element,
                bossDifficultyStats.MaxHp,
                bossDifficultyStats.Attack);
            WaterElement = WaterElementResource.Register(Boss.Resources);
            Party = new PartyBattleState(new[] { Marea });

            ActionIds = new CombatActionIdSequence();
            KragmorRuntimeState = new KragmorBattleRuntimeState();
            KragmorDefenseEffectFactory.InitializeBattle(
                Boss,
                KragmorRuntimeState,
                ActionIds);
            TriggerResolver = new CombatTriggerResolver(
                Array.Empty<ICombatTriggerRule>());
            Executor = new CombatActionExecutor(
                Boss,
                Party,
                new DamageContextFactory(randomSource),
                TriggerResolver);

            MatchProviders = new MatchEventActionProviderRegistry();
            ActiveProviders = new ActiveAbilityActionProviderRegistry();
            BossActionProvider = new KragmorBossCombatActionProvider(
                KragmorRuntimeState);
            MareaBluefangCombatProviderRegistration.Register(
                MatchProviders,
                ActiveProviders);
            activeBindings = Array.AsReadOnly(new[]
            {
                new ActiveAbilityBinding(
                    activeAbilityIndex: 0,
                    partySlotIndex: Marea.PartySlotIndex,
                    characterId: Marea.CharacterId,
                    activeAbilityId: MareaBluefangRules.ActiveAbilityId)
            });
            FlowSetup = new BattleFlowSetup(
                bossDefinition.TurnLimit,
                new[] { MareaBluefangRules.ActiveCooldownTurns });
        }

        public BattleFlowSetup FlowSetup { get; }
        public CharacterBattleState Marea { get; }
        public PartyBattleState Party { get; }
        public BossBattleState Boss { get; }
        public ResourceState WaterElement { get; }
        public CombatActionIdSequence ActionIds { get; }
        public CombatTriggerResolver TriggerResolver { get; }
        public CombatActionExecutor Executor { get; }
        public MatchEventActionProviderRegistry MatchProviders { get; }
        public ActiveAbilityActionProviderRegistry ActiveProviders { get; }
        public KragmorBattleRuntimeState KragmorRuntimeState { get; }
        public KragmorBossCombatActionProvider BossActionProvider { get; }
        public IReadOnlyList<ActiveAbilityBinding> ActiveBindings =>
            activeBindings;
        public BattleFlowCombatBridge Bridge { get; private set; }

        public BattleFlowCombatBridge CreateBridge(
            BattleFlowCoordinator coordinator)
        {
            return CreateBridge(coordinator, boardMutationSink: null);
        }

        public BattleFlowCombatBridge CreateBridge(
            BattleFlowCoordinator coordinator,
            IBattleBoardMutationSink boardMutationSink)
        {
            if (coordinator == null)
            {
                throw new ArgumentNullException(nameof(coordinator));
            }

            if (Bridge != null)
            {
                throw new InvalidOperationException(
                    "Combat bridge was already created.");
            }

            Bridge = new BattleFlowCombatBridge(
                coordinator,
                Party,
                Boss,
                Executor,
                MatchProviders,
                BossActionProvider,
                ActionIds,
                activeBindings,
                ActiveProviders,
                boardMutationSink);
            return Bridge;
        }
    }
}
