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
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatComposition : IDisposable
    {
        private readonly IReadOnlyList<ActiveAbilityBinding> activeBindings;

        public BattleSceneCombatComposition(
            IReadOnlyList<BattlePartyMemberInput> partyMembers,
            CharacterCombatProviderRegistrarCatalog characterRegistrars,
            BossDefinition bossDefinition,
            KragmorCombatConfig kragmorConfig,
            BossDifficultyStats bossDifficultyStats,
            IRandomSource randomSource,
            BattleResultBalance resultBalance)
            : this(
                partyMembers,
                characterRegistrars,
                bossDefinition,
                kragmorConfig,
                bossDifficultyStats,
                randomSource,
                resultBalance,
                saveService: null)
        {
        }

        public BattleSceneCombatComposition(
            IReadOnlyList<BattlePartyMemberInput> partyMembers,
            CharacterCombatProviderRegistrarCatalog characterRegistrars,
            BossDefinition bossDefinition,
            KragmorCombatConfig kragmorConfig,
            BossDifficultyStats bossDifficultyStats,
            IRandomSource randomSource,
            BattleResultBalance resultBalance,
            SaveService saveService)
        {
            if (partyMembers == null)
            {
                throw new ArgumentNullException(nameof(partyMembers));
            }

            if (partyMembers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partyMembers),
                    partyMembers.Count,
                    "Battle party cannot be empty.");
            }

            if (characterRegistrars == null)
            {
                throw new ArgumentNullException(
                    nameof(characterRegistrars));
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

            CharacterBuildResolver buildResolver =
                CharacterBuildResolverFactory.CreateDefault();
            var characterStates = new CharacterBattleState[
                partyMembers.Count];
            for (int index = 0; index < partyMembers.Count; index++)
            {
                BattlePartyMemberInput member = partyMembers[index];
                if (member == null)
                {
                    throw new ArgumentException(
                        $"Battle party member {index} is null.",
                        nameof(partyMembers));
                }

                ResolvedCharacterBuild build = buildResolver.Resolve(
                    member.CharacterDefinition,
                    member.Level,
                    member.Awakening);
                characterStates[index] = CharacterBattleStateFactory.Create(
                    member.CharacterDefinition,
                    build,
                    member.PartySlotIndex);
            }

            Party = new PartyBattleState(characterStates);
            Boss = new BossBattleState(
                bossDefinition.Id,
                bossDefinition.Element,
                bossDifficultyStats.MaxHp,
                bossDifficultyStats.Attack);
            DifficultyId = bossDifficultyStats.DifficultyId;
            ResultBalance = resultBalance;

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

            var membersBySlot = new Dictionary<int, BattlePartyMemberInput>(
                partyMembers.Count);
            for (int index = 0; index < partyMembers.Count; index++)
            {
                BattlePartyMemberInput member = partyMembers[index];
                membersBySlot.Add(member.PartySlotIndex, member);
            }

            var bindings = new List<ActiveAbilityBinding>(
                partyMembers.Count);
            var cooldowns = new List<int>(partyMembers.Count);
            for (int index = 0; index < Party.Characters.Count; index++)
            {
                CharacterBattleState character = Party.Characters[index];
                BattlePartyMemberInput member =
                    membersBySlot[character.PartySlotIndex];
                CharacterCombatProviderRegistrationResult registration =
                    characterRegistrars.Register(
                        member,
                        character,
                        Boss,
                        MatchProviders,
                        ActiveProviders);

                if (!MatchProviders.TryResolve(
                    character.CharacterId,
                    out _))
                {
                    throw new InvalidOperationException(
                        $"The registrar for '{character.CharacterId}' did "
                            + "not register a match provider.");
                }

                if (!registration.HasActiveAbility)
                {
                    continue;
                }

                if (!ActiveProviders.TryResolve(
                    character.CharacterId,
                    registration.ActiveAbilityId,
                    out _))
                {
                    throw new InvalidOperationException(
                        $"The registrar for '{character.CharacterId}' did "
                            + "not register its active provider.");
                }

                bindings.Add(new ActiveAbilityBinding(
                    activeAbilityIndex: bindings.Count,
                    partySlotIndex: character.PartySlotIndex,
                    characterId: character.CharacterId,
                    activeAbilityId: registration.ActiveAbilityId));
                cooldowns.Add(registration.ActiveCooldownTurns);
            }

            activeBindings = Array.AsReadOnly(bindings.ToArray());
            FlowSetup = new BattleFlowSetup(
                bossDefinition.TurnLimit,
                cooldowns);
        }

        public BattleFlowSetup FlowSetup { get; }
        public string DifficultyId { get; }
        public BattleResultBalance ResultBalance { get; }
        public KragmorCombatConfig KragmorConfig { get; }
        public PartyBattleState Party { get; }
        public BossBattleState Boss { get; }
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
