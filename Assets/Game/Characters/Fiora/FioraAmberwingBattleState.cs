using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Fiora
{
    public enum FioraAmberwingHealingSource
    {
        Match,
        Active,
        FollowUp
    }

    public sealed class FioraAmberwingBattleState
        : ICharacterCombatStatusSource
    {
        private readonly Dictionary<long, FioraAmberwingHealingSource>
            passiveEligibleHeals =
                new Dictionary<long, FioraAmberwingHealingSource>();
        private int remainingFollowUpHeals;

        public FioraAmberwingBattleState(
            CharacterBattleState owner,
            int followUpHealMaxCount)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (!string.Equals(owner.CharacterId, FioraAmberwingRules.CharacterId, StringComparison.Ordinal))
                throw new ArgumentException("Fiora battle state requires Fiora Amberwing as its owner.", nameof(owner));
            if (followUpHealMaxCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(followUpHealMaxCount));

            FollowUpHealMaxCount = followUpHealMaxCount;
        }

        public CharacterBattleState Owner { get; }
        public int RemainingFollowUpHeals => remainingFollowUpHeals;
        public int FollowUpHealMaxCount { get; }
        public int? FirstFollowUpHealAvailableTurn { get; private set; }
        public int? LastFollowUpHealTurn { get; private set; }
        public int? LastActiveUseTurn { get; private set; }
        public int? LastAwakening6ConversionTurn { get; private set; }
        public string StatusId => FioraAmberwingRules.FollowUpHealingStatusId;
        public string CharacterId => Owner.CharacterId;
        public int PartySlotIndex => Owner.PartySlotIndex;
        public int CurrentCount => RemainingFollowUpHeals;
        public bool IsVisible => RemainingFollowUpHeals > 0;

        public void RegisterPassiveEligibleHeal(
            long actionId,
            FioraAmberwingHealingSource source)
        {
            if (actionId <= 0) throw new ArgumentOutOfRangeException(nameof(actionId));
            if (!Enum.IsDefined(typeof(FioraAmberwingHealingSource), source))
                throw new ArgumentOutOfRangeException(nameof(source));
            if (passiveEligibleHeals.ContainsKey(actionId))
                throw new InvalidOperationException($"Fiora healing action '{actionId}' was already registered.");
            passiveEligibleHeals.Add(actionId, source);
        }

        public bool TryTakePassiveEligibleHeal(
            long actionId,
            out FioraAmberwingHealingSource source)
        {
            if (!passiveEligibleHeals.TryGetValue(actionId, out source))
                return false;
            passiveEligibleHeals.Remove(actionId);
            return true;
        }

        public void GrantFollowUpHeals(int count, int currentTurn)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (currentTurn < 0) throw new ArgumentOutOfRangeException(nameof(currentTurn));

            bool wasEmpty = remainingFollowUpHeals == 0;
            remainingFollowUpHeals = Math.Min(
                checked(remainingFollowUpHeals + count),
                FollowUpHealMaxCount);
            LastActiveUseTurn = currentTurn;
            if (wasEmpty && remainingFollowUpHeals > 0)
                FirstFollowUpHealAvailableTurn = checked(currentTurn + 1);
        }

        public bool CanExecuteFollowUpHeal(int currentTurn)
        {
            if (currentTurn < 0)
                throw new ArgumentOutOfRangeException(nameof(currentTurn));

            return remainingFollowUpHeals > 0
                && FirstFollowUpHealAvailableTurn.HasValue
                && currentTurn >= FirstFollowUpHealAvailableTurn.Value
                && LastActiveUseTurn != currentTurn
                && LastFollowUpHealTurn != currentTurn;
        }

        public bool TryConsumeFollowUpHeal(int currentTurn)
        {
            if (currentTurn < 0) throw new ArgumentOutOfRangeException(nameof(currentTurn));
            if (!CanExecuteFollowUpHeal(currentTurn))
                return false;

            remainingFollowUpHeals--;
            LastFollowUpHealTurn = currentTurn;
            if (remainingFollowUpHeals == 0)
                FirstFollowUpHealAvailableTurn = null;
            return true;
        }

        public bool TryUseAwakening6Conversion(int currentTurn)
        {
            if (currentTurn < 0) throw new ArgumentOutOfRangeException(nameof(currentTurn));
            if (LastAwakening6ConversionTurn == currentTurn) return false;
            LastAwakening6ConversionTurn = currentTurn;
            return true;
        }
    }
}
