using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Koharu
{
    public sealed class SakuragiKoharuActiveBoardPlanProvider
        : IActiveAbilityBoardPlanProvider,
          IActiveAbilityBoardSuccessCommitter,
          IActiveAbilityBoardSuccessActionProvider
    {
        private const AttackTypeMask EnhancedAttackTypes =
            AttackTypeMask.Additional | AttackTypeMask.Chase;

        private readonly SakuragiKoharuBattleState battleState;
        private readonly SakuragiKoharuResolvedCombatRules combatRules;
        private readonly ActiveAbilityCooldownReductionHandle
            cooldownReductionHandle;

        public SakuragiKoharuActiveBoardPlanProvider(
            SakuragiKoharuCombatConfig config,
            int awakening,
            SakuragiKoharuBattleState battleState,
            ActiveAbilityCooldownReductionHandle cooldownReductionHandle)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = SakuragiKoharuCombatRuleResolver.Resolve(
                config,
                awakening);
            this.cooldownReductionHandle = cooldownReductionHandle
                ?? throw new ArgumentNullException(
                    nameof(cooldownReductionHandle));
            ValidateHandleOwner(
                battleState.Owner,
                cooldownReductionHandle);
        }

        public int CooldownTurns => combatRules.ActiveCooldownTurns;

        public ActiveAbilityBoardElementConversionPlan CreateBoardPlan(
            ActiveAbilityBoardPlanContext context)
        {
            ValidatePlanContext(context);
            if (!cooldownReductionHandle.IsBound)
            {
                throw new InvalidOperationException(
                    "Koharu's active ability cooldown reducer must be bound "
                        + "before creating a Board plan.");
            }

            return new ActiveAbilityBoardElementConversionPlan(
                ElementType.Grass,
                combatRules.ActiveBlockConversionCount);
        }

        public void CommitBoardSuccess(
            ActiveAbilityBoardSuccessContext context)
        {
            ValidateSuccessContext(context);
            SakuragiKoharuSpiritCooldownReductionRule
                .ApplyAvailableReductions(
                    battleState,
                    cooldownReductionHandle,
                    combatRules.SpiritEnergyCostPerCooldownReduction);
        }

        public IReadOnlyList<CombatAction> CreateBoardSuccessActions(
            ActiveAbilityBoardSuccessActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ValidateSuccessContext(context.SuccessContext);
            if (!combatRules
                .HasAwakening6AdditionalAndChaseDamageIncrease)
            {
                return Array.Empty<CombatAction>();
            }

            long actionId = context.ActionIds.Next();
            var effect = new EffectInstance(
                actionId,
                SakuragiKoharuRules.Awakening6ActiveEffectId,
                battleState.Owner.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackTypeDamageIncrease,
                combatRules.Awakening6AdditionalAndChaseDamageIncreaseRate,
                combatRules.Awakening6EffectDurationTurns,
                actionId,
                EffectStackPolicy.RefreshDuration,
                attackTypeMask: EnhancedAttackTypes);
            return new CombatAction[]
            {
                new ApplyEffectAction(
                    actionId,
                    ActionOrigin.Active,
                    context.Party,
                    effect)
            };
        }

        private void ValidatePlanContext(
            ActiveAbilityBoardPlanContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            CharacterBattleState owner = battleState.Owner;
            if (!ReferenceEquals(context.Character, owner)
                || context.Character.Element != ElementType.Grass
                || context.Binding.PartySlotIndex != owner.PartySlotIndex
                || !string.Equals(
                    context.Binding.CharacterId,
                    SakuragiKoharuRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    context.Binding.ActiveAbilityId,
                    SakuragiKoharuRules.ActiveAbilityId,
                    StringComparison.Ordinal)
                || !ContainsOwnerAtExpectedSlot(context.Party))
            {
                throw new InvalidOperationException(
                    "Koharu Active context does not match its battle state, "
                        + "binding, and party slot.");
            }
        }

        private void ValidateSuccessContext(
            ActiveAbilityBoardSuccessContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ValidatePlanContext(context.PlanContext);
            if (!cooldownReductionHandle.IsBound
                || context.Plan.TargetElement != ElementType.Grass
                || context.Plan.RequestedCount
                    != combatRules.ActiveBlockConversionCount
                || !context.Result.Succeeded
                || context.Result.RequestedCount
                    != combatRules.ActiveBlockConversionCount
                || context.Result.ConvertedCount <= 0
                || context.Result.ConvertedCount
                    > combatRules.ActiveBlockConversionCount)
            {
                throw new InvalidOperationException(
                    "Koharu can commit only a valid successful Grass "
                        + "conversion with a bound cooldown reducer.");
            }

            for (int index = 0;
                index < context.Result.Entries.Count;
                index++)
            {
                BoardElementConversionEntry entry =
                    context.Result.Entries[index];
                if (entry.BlockType != BoardBlockType.Normal
                    || entry.PreviousElement == ElementType.Grass
                    || entry.NewElement != ElementType.Grass)
                {
                    throw new InvalidOperationException(
                        "Koharu can convert only non-Grass Normal blocks "
                            + "to Grass.");
                }
            }
        }

        private bool ContainsOwnerAtExpectedSlot(PartyBattleState party)
        {
            CharacterBattleState owner = battleState.Owner;
            for (int index = 0; index < party.Characters.Count; index++)
            {
                CharacterBattleState character = party.Characters[index];
                if (character.PartySlotIndex != owner.PartySlotIndex)
                {
                    continue;
                }

                return ReferenceEquals(character, owner)
                    && string.Equals(
                        character.CharacterId,
                        owner.CharacterId,
                        StringComparison.Ordinal);
            }

            return false;
        }

        private static void ValidateHandleOwner(
            CharacterBattleState owner,
            ActiveAbilityCooldownReductionHandle handle)
        {
            if (owner.Element != ElementType.Grass
                || !string.Equals(
                    owner.CharacterId,
                    SakuragiKoharuRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    owner.CharacterId,
                    handle.CharacterId,
                    StringComparison.Ordinal)
                || owner.PartySlotIndex != handle.PartySlotIndex)
            {
                throw new ArgumentException(
                    "Cooldown reduction handle must belong to Koharu's "
                        + "character and party slot.",
                    nameof(handle));
            }
        }
    }
}
