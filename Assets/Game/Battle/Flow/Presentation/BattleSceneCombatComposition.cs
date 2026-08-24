using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Results;
using ValorChronicle.Battle.Results.Persistence;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatComposition : IDisposable
    {
        private readonly IReadOnlyList<ActiveAbilityBinding> activeBindings;

        public BattleSceneCombatComposition(
            CharacterDefinition mareaDefinition,
            MareaBluefangCombatConfig mareaConfig,
            int mareaLevel,
            BossDefinition bossDefinition,
            KragmorCombatConfig kragmorConfig,
            BossDifficultyStats bossDifficultyStats,
            IRandomSource randomSource,
            BattleResultBalance resultBalance)
            : this(
                mareaDefinition,
                mareaConfig,
                mareaLevel,
                bossDefinition,
                kragmorConfig,
                bossDifficultyStats,
                randomSource,
                resultBalance,
                saveService: null)
        {
        }

        public BattleSceneCombatComposition(
            CharacterDefinition mareaDefinition,
            MareaBluefangCombatConfig mareaConfig,
            int mareaLevel,
            BossDefinition bossDefinition,
            KragmorCombatConfig kragmorConfig,
            BossDifficultyStats bossDifficultyStats,
            IRandomSource randomSource,
            BattleResultBalance resultBalance,
            SaveService saveService)
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

            if (mareaConfig == null)
            {
                throw new ArgumentNullException(nameof(mareaConfig));
            }

            if (!ReferenceEquals(mareaDefinition.CombatConfig, mareaConfig))
            {
                throw new ArgumentException(
                    "Marea config must be the config referenced by the "
                        + "CharacterDefinition.",
                    nameof(mareaConfig));
            }

            if (!mareaConfig.TryValidate(out string configError))
            {
                throw new ArgumentException(
                    configError,
                    nameof(mareaConfig));
            }

            if (bossDefinition == null)
            {
                throw new ArgumentNullException(nameof(bossDefinition));
            }

            if (kragmorConfig == null)
            {
                throw new ArgumentNullException(nameof(kragmorConfig));
            }

            if (!ReferenceEquals(
                bossDefinition.CombatConfig,
                kragmorConfig))
            {
                throw new ArgumentException(
                    "Kragmor config must be the config referenced by the "
                        + "BossDefinition.",
                    nameof(kragmorConfig));
            }

            if (!kragmorConfig.TryValidate(out string kragmorConfigError))
            {
                throw new ArgumentException(
                    kragmorConfigError,
                    nameof(kragmorConfig));
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

            if (resultBalance == null)
            {
                throw new ArgumentNullException(nameof(resultBalance));
            }

            BattleResultBalanceValidator.ValidateOrThrow(resultBalance);
            if (saveService != null)
            {
                ResultPersistenceService =
                    new BattleResultPersistenceService(saveService);
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
            DifficultyId = bossDifficultyStats.DifficultyId;
            ResultBalance = resultBalance;
            MareaConfig = mareaConfig;
            WaterElement = WaterElementResource.Register(
                Boss.Resources,
                mareaConfig.WaterElementMaxAmount);
            Party = new PartyBattleState(new[] { Marea });

            ActionIds = new CombatActionIdSequence();
            KragmorConfig = kragmorConfig;
            KragmorRuntimeState = new KragmorBattleRuntimeState(
                kragmorConfig);
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
                ActiveProviders,
                mareaConfig);
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
                new[] { mareaConfig.ActiveCooldownTurns });
        }

        public BattleFlowSetup FlowSetup { get; }
        public string DifficultyId { get; }
        public BattleResultBalance ResultBalance { get; }
        public CharacterBattleState Marea { get; }
        public MareaBluefangCombatConfig MareaConfig { get; }
        public KragmorCombatConfig KragmorConfig { get; }
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
        public IKragmorBossIntentSource BossIntentSource =>
            BossActionProvider;
        public IBossVisualStateSource BossVisualStateSource =>
            KragmorRuntimeState;
        public IReadOnlyList<ActiveAbilityBinding> ActiveBindings =>
            activeBindings;
        public BattleFlowCombatBridge Bridge { get; private set; }
        public BattleResultPersistenceService ResultPersistenceService
        {
            get;
        }
        public BattleResultPersistenceCoordinator ResultPersistenceCoordinator
        {
            get;
            private set;
        }

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
                boardMutationSink,
                DifficultyId,
                ResultBalance);
            if (ResultPersistenceService != null)
            {
                ResultPersistenceCoordinator =
                    new BattleResultPersistenceCoordinator(
                        Bridge,
                        ResultPersistenceService);
            }

            return Bridge;
        }

        public void Dispose()
        {
            ResultPersistenceCoordinator?.Dispose();
        }
    }
}
