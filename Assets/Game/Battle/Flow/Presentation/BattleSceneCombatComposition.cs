using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
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
        private const long InitialPartyEffectRuntimeIdBase = 1L << 62;

        private readonly IReadOnlyList<ActiveAbilityBinding> activeBindings;
        private readonly IReadOnlyList<ICharacterCombatStatusSource>
            combatStatusSources;
        private readonly IReadOnlyList<PreMatchQueueActionProviderBinding>
            preMatchQueueBindings;

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
                    member.Awakening,
                    member.RelicStatBonuses);
                characterStates[index] = CharacterBattleStateFactory.Create(
                    member.CharacterDefinition,
                    build,
                    member.PartySlotIndex,
                    member.RelicStatBonuses);
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
            var triggerRules = new List<ICombatTriggerRule>();
            var resourceRequirements =
                new List<CombatResourceRequirement>();
            var statusSources =
                new List<ICharacterCombatStatusSource>();
            var initialPartyEffects =
                new List<InitialPartyEffectDefinition>();
            var preMatchBindings =
                new List<PreMatchQueueActionProviderBinding>();
            var statusSourceKeys = new HashSet<string>(
                StringComparer.Ordinal);
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
                        ActionIds,
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

                IReadOnlyList<ICombatTriggerRule> registeredRules =
                    registration.TriggerRules
                    ?? throw new InvalidOperationException(
                        $"The registrar for '{character.CharacterId}' "
                            + "returned a null trigger rule collection.");
                for (int ruleIndex = 0;
                    ruleIndex < registeredRules.Count;
                    ruleIndex++)
                {
                    ICombatTriggerRule rule = registeredRules[ruleIndex]
                        ?? throw new InvalidOperationException(
                            $"The registrar for '{character.CharacterId}' "
                                + "returned a null trigger rule at index "
                                + $"{ruleIndex}.");
                    triggerRules.Add(rule);
                }

                IReadOnlyList<CombatResourceRequirement>
                    registeredResourceRequirements =
                        registration.ResourceRequirements
                        ?? throw new InvalidOperationException(
                            $"The registrar for '{character.CharacterId}' "
                                + "returned a null resource requirement "
                                + "collection.");
                for (int requirementIndex = 0;
                    requirementIndex
                        < registeredResourceRequirements.Count;
                    requirementIndex++)
                {
                    CombatResourceRequirement requirement =
                        registeredResourceRequirements[requirementIndex]
                        ?? throw new InvalidOperationException(
                            $"The registrar for "
                                + $"'{character.CharacterId}' returned a "
                                + "null resource requirement at index "
                                + $"{requirementIndex}.");
                    resourceRequirements.Add(requirement);
                }

                IReadOnlyList<ICharacterCombatStatusSource>
                    registeredStatusSources =
                        registration.CombatStatusSources
                        ?? throw new InvalidOperationException(
                            $"The registrar for '{character.CharacterId}' "
                                + "returned a null combat status source "
                                + "collection.");
                for (int statusIndex = 0;
                    statusIndex < registeredStatusSources.Count;
                    statusIndex++)
                {
                    ICharacterCombatStatusSource source =
                        registeredStatusSources[statusIndex]
                        ?? throw new InvalidOperationException(
                            $"The registrar for "
                                + $"'{character.CharacterId}' returned a "
                                + "null combat status source at index "
                                + $"{statusIndex}.");
                    if (!string.Equals(
                            source.CharacterId,
                            character.CharacterId,
                            StringComparison.Ordinal)
                        || source.PartySlotIndex
                            != character.PartySlotIndex)
                    {
                        throw new InvalidOperationException(
                            $"Combat status source '{source.StatusId}' must "
                                + "match its registered character and party "
                                + "slot.");
                    }

                    if (string.IsNullOrWhiteSpace(source.StatusId))
                    {
                        throw new InvalidOperationException(
                            $"The registrar for '{character.CharacterId}' "
                                + "returned a combat status source without "
                                + "an ID.");
                    }

                    string sourceKey = source.PartySlotIndex.ToString(
                            System.Globalization.CultureInfo.InvariantCulture)
                        + "\u001f"
                        + source.StatusId;
                    if (!statusSourceKeys.Add(sourceKey))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate combat status source "
                                + $"'{source.StatusId}' for party slot "
                                + $"{source.PartySlotIndex}.");
                    }

                    statusSources.Add(source);
                }

                IReadOnlyList<InitialPartyEffectDefinition>
                    registeredInitialEffects =
                        registration.InitialPartyEffects
                        ?? throw new InvalidOperationException(
                            $"The registrar for '{character.CharacterId}' "
                                + "returned a null initial Party Effect "
                                + "collection.");
                for (int effectIndex = 0;
                    effectIndex < registeredInitialEffects.Count;
                    effectIndex++)
                {
                    InitialPartyEffectDefinition effect =
                        registeredInitialEffects[effectIndex]
                        ?? throw new InvalidOperationException(
                            $"The registrar for "
                                + $"'{character.CharacterId}' returned a "
                                + "null initial Party Effect at index "
                                + $"{effectIndex}.");
                    if (!string.Equals(
                            effect.SourceId,
                            character.CharacterId,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Initial Party Effect '{effect.EffectId}' must "
                                + "use its registered character ID as the "
                                + "source ID.");
                    }

                    initialPartyEffects.Add(effect);
                }

                IReadOnlyList<IPreMatchQueueActionProvider>
                    registeredPreMatchProviders =
                        registration.PreMatchQueueActionProviders
                        ?? throw new InvalidOperationException(
                            $"The registrar for '{character.CharacterId}' "
                                + "returned a null pre-match queue provider "
                                + "collection.");
                for (int providerIndex = 0;
                    providerIndex < registeredPreMatchProviders.Count;
                    providerIndex++)
                {
                    IPreMatchQueueActionProvider provider =
                        registeredPreMatchProviders[providerIndex]
                        ?? throw new InvalidOperationException(
                            $"The registrar for "
                                + $"'{character.CharacterId}' returned a "
                                + "null pre-match queue provider at index "
                                + $"{providerIndex}.");
                    preMatchBindings.Add(
                        new PreMatchQueueActionProviderBinding(
                            character,
                            provider));
                }

                if (!registration.HasActiveAbility)
                {
                    continue;
                }

                bool hasCombatProvider = ActiveProviders.TryResolve(
                    character.CharacterId,
                    registration.ActiveAbilityId,
                    out _);
                bool hasBoardProvider = ActiveProviders.TryResolveBoard(
                    character.CharacterId,
                    registration.ActiveAbilityId,
                    out _);
                if (!hasCombatProvider && !hasBoardProvider)
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

            ApplyInitialPartyEffects(initialPartyEffects);

            IReadOnlyList<CombatResourceRequirement>
                mergedResourceRequirements =
                    CombatResourceRequirementMerger.Merge(
                        resourceRequirements);
            for (int index = 0;
                index < mergedResourceRequirements.Count;
                index++)
            {
                CombatResourceRequirement requirement =
                    mergedResourceRequirements[index];
                if (Boss.Resources.TryGet(requirement.ResourceId, out _))
                {
                    throw new InvalidOperationException(
                        $"Combat resource '{requirement.ResourceId}' was "
                            + "registered before shared resource "
                            + "requirements were applied.");
                }
            }

            for (int index = 0;
                index < mergedResourceRequirements.Count;
                index++)
            {
                CombatResourceRequirement requirement =
                    mergedResourceRequirements[index];
                Boss.Resources.Register(
                    requirement.ResourceId,
                    requirement.MaxAmount);
            }

            TriggerResolver = new CombatTriggerResolver(triggerRules);
            Executor = new CombatActionExecutor(
                Boss,
                Party,
                new DamageContextFactory(randomSource),
                TriggerResolver);
            activeBindings = Array.AsReadOnly(bindings.ToArray());
            combatStatusSources = Array.AsReadOnly(
                statusSources.ToArray());
            preMatchQueueBindings = Array.AsReadOnly(
                preMatchBindings.ToArray());
            FlowSetup = new BattleFlowSetup(
                bossDefinition.TurnLimit,
                cooldowns);
        }

        private void ApplyInitialPartyEffects(
            IReadOnlyList<InitialPartyEffectDefinition> definitions)
        {
            var instances = new EffectInstance[definitions.Count];
            for (int index = 0; index < definitions.Count; index++)
            {
                long identity = checked(
                    InitialPartyEffectRuntimeIdBase + index);
                instances[index] = definitions[index].CreateInstance(
                    identity,
                    identity);
            }

            for (int index = 0; index < instances.Length; index++)
            {
                Party.Effects.ApplyEffect(instances[index]);
            }
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
        public IReadOnlyList<ICharacterCombatStatusSource>
            CombatStatusSources => combatStatusSources;
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
                ResultBalance,
                preMatchQueueBindings: preMatchQueueBindings);
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
