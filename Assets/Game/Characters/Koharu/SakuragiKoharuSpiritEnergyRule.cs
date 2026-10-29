using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Koharu
{
    public sealed class SakuragiKoharuSpiritEnergyRule
        : ICombatTriggerRule
    {
        private const AttackTag MatchTierTags =
            AttackTag.Match3
            | AttackTag.Match4
            | AttackTag.Match5Plus;

        private readonly SakuragiKoharuBattleState battleState;
        private readonly SakuragiKoharuResolvedCombatRules combatRules;
        private readonly ActiveAbilityCooldownReductionHandle
            cooldownReductionHandle;
        private readonly HashSet<long> processedActionIds =
            new HashSet<long>();

        public SakuragiKoharuSpiritEnergyRule(
            SakuragiKoharuBattleState battleState,
            SakuragiKoharuCombatConfig config,
            int awakening,
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

        public IReadOnlyList<CombatAction> CreateDerivedActions(
            CombatActionTriggerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!TryResolveSpiritEnergyGain(context, out int gainAmount))
            {
                return Array.Empty<CombatAction>();
            }

            if (!cooldownReductionHandle.IsBound)
            {
                throw new InvalidOperationException(
                    "Koharu's active ability cooldown reducer must be bound "
                        + "before match damage can grant Spirit Energy.");
            }

            if (!processedActionIds.Add(context.ActionId))
            {
                return Array.Empty<CombatAction>();
            }

            battleState.GainSpiritEnergy(gainAmount);
            SakuragiKoharuSpiritCooldownReductionRule
                .ApplyAvailableReductions(
                    battleState,
                    cooldownReductionHandle,
                    combatRules.SpiritEnergyCostPerCooldownReduction);
            return Array.Empty<CombatAction>();
        }

        private bool TryResolveSpiritEnergyGain(
            CombatActionTriggerContext context,
            out int gainAmount)
        {
            gainAmount = 0;
            if (!(context.CompletedResult is DamageActionResult result)
                || !(result.Action is DamageAction damage)
                || damage.Origin != ActionOrigin.Match
                || damage.RootActionId != damage.ActionId
                || damage.SourceActionId.HasValue
                || damage.ContextRequest.AttackType != AttackType.Match
                || damage.ContextRequest.AttackElement != ElementType.Grass
                || !ReferenceEquals(
                    damage.ContextRequest.Attacker,
                    battleState.Owner)
                || !ReferenceEquals(
                    damage.ContextRequest.Party,
                    context.Party)
                || !ReferenceEquals(
                    damage.ContextRequest.TargetBoss,
                    context.Boss)
                || !ContainsOwnerAtExpectedSlot(context.Party))
            {
                return false;
            }

            switch (damage.ContextRequest.AttackTags & MatchTierTags)
            {
                case AttackTag.Match3:
                    gainAmount = combatRules.Match3SpiritEnergyGain;
                    return true;
                case AttackTag.Match4:
                    gainAmount = combatRules.Match4SpiritEnergyGain;
                    return true;
                case AttackTag.Match5Plus:
                    gainAmount = combatRules.Match5SpiritEnergyGain;
                    return true;
                default:
                    return false;
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
