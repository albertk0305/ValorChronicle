using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class ActiveAbilityCombatExecutor
    {
        private readonly BattleFlowCoordinator coordinator;
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;
        private readonly CombatActionExecutor executor;
        private readonly CombatActionIdSequence actionIds;
        private readonly ActiveAbilityBinding[] bindingsByIndex;
        private readonly ActiveAbilityActionProviderRegistry providers;

        public ActiveAbilityCombatExecutor(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            CombatActionIdSequence actionIds,
            IReadOnlyList<ActiveAbilityBinding> bindings,
            ActiveAbilityActionProviderRegistry providers)
        {
            this.coordinator = coordinator
                ?? throw new ArgumentNullException(nameof(coordinator));
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
            this.providers = providers
                ?? throw new ArgumentNullException(nameof(providers));
            bindingsByIndex = CreateBindings(bindings);
        }

        public bool TryExecute(
            int activeAbilityIndex,
            out CombatActionExecutionResult result)
        {
            result = null;
            if (coordinator.Context.Result != BattleResultKind.None
                || coordinator.Context.Phase != BattlePhase.ActiveInput
                || activeAbilityIndex < 0
                || activeAbilityIndex >= bindingsByIndex.Length)
            {
                return false;
            }

            ActiveAbilityBinding binding =
                bindingsByIndex[activeAbilityIndex];
            CharacterBattleState character = binding != null
                ? FindBoundCharacter(binding)
                : null;
            if (character == null
                || !providers.TryResolve(
                    binding.CharacterId,
                    binding.ActiveAbilityId,
                    out IActiveAbilityActionProvider provider))
            {
                return false;
            }

            ActiveAbilityRuntimeState runtime =
                coordinator.Context.ActiveAbilities[activeAbilityIndex];
            if (!runtime.CanUse)
            {
                return false;
            }

            var context = new ActiveAbilityActionContext(
                binding,
                character,
                party,
                boss,
                actionIds);
            long idBeforeProvider = actionIds.LastIssuedId;
            IReadOnlyList<CombatAction> rootActions =
                provider.CreateRootActions(context);
            ValidateRootActions(rootActions, idBeforeProvider);
            if (!coordinator.TryUseActiveAbility(activeAbilityIndex))
            {
                throw new InvalidOperationException(
                    "Active runtime changed after prevalidation.");
            }

            result = executor.Execute(new CombatActionQueue(rootActions));
            return true;
        }

        private ActiveAbilityBinding[] CreateBindings(
            IReadOnlyList<ActiveAbilityBinding> bindings)
        {
            if (bindings == null)
            {
                throw new ArgumentNullException(nameof(bindings));
            }

            var result = new ActiveAbilityBinding[
                coordinator.Context.ActiveAbilities.Count];
            for (int index = 0; index < bindings.Count; index++)
            {
                ActiveAbilityBinding binding = bindings[index]
                    ?? throw new ArgumentException(
                        "Active bindings cannot contain null.",
                        nameof(bindings));
                if (binding.ActiveAbilityIndex >= result.Length)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(bindings),
                        binding.ActiveAbilityIndex,
                        "Active binding index is outside runtime state.");
                }

                if (result[binding.ActiveAbilityIndex] != null)
                {
                    throw new ArgumentException(
                        $"Duplicate active binding index: "
                            + $"{binding.ActiveAbilityIndex}.",
                        nameof(bindings));
                }

                if (FindBoundCharacter(binding) == null)
                {
                    throw new InvalidOperationException(
                        "Active binding must match its party slot and "
                            + "character ID.");
                }

                result[binding.ActiveAbilityIndex] = binding;
            }

            return result;
        }

        private CharacterBattleState FindBoundCharacter(
            ActiveAbilityBinding binding)
        {
            for (int index = 0; index < party.Characters.Count; index++)
            {
                CharacterBattleState character = party.Characters[index];
                if (character.PartySlotIndex == binding.PartySlotIndex)
                {
                    return string.Equals(
                        character.CharacterId,
                        binding.CharacterId,
                        StringComparison.Ordinal)
                            ? character
                            : null;
                }
            }

            return null;
        }

        private void ValidateRootActions(
            IReadOnlyList<CombatAction> actions,
            long idBeforeProvider)
        {
            if (actions == null || actions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Active providers must return at least one root action.");
            }

            long allocatedCount = actionIds.LastIssuedId - idBeforeProvider;
            if (allocatedCount != actions.Count)
            {
                throw new InvalidOperationException(
                    "Active providers must allocate exactly one "
                        + "battle-scoped ID per returned action.");
            }

            for (int actionIndex = 0;
                actionIndex < actions.Count;
                actionIndex++)
            {
                CombatAction action = actions[actionIndex]
                    ?? throw new InvalidOperationException(
                        "Active providers cannot return null actions.");
                long expectedActionId = checked(
                    idBeforeProvider + actionIndex + 1);
                if (action.ActionId != expectedActionId
                    || action.RootActionId != action.ActionId
                    || action.SourceActionId.HasValue
                    || action.Origin != ActionOrigin.Active)
                {
                    throw new InvalidOperationException(
                        "Active root actions must preserve provider order, "
                            + "use new battle-scoped IDs, have no source, "
                            + "and use Active origin.");
                }
            }
        }
    }
}
