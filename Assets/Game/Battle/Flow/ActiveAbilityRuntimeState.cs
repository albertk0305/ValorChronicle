using System;

namespace ValorChronicle.Battle.Flow
{
    public sealed class ActiveAbilityRuntimeState
    {
        internal ActiveAbilityRuntimeState(int cooldownTurns)
        {
            if (cooldownTurns < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cooldownTurns),
                    cooldownTurns,
                    "Cooldown turns cannot be negative.");
            }

            CooldownTurns = cooldownTurns;
            RemainingCooldown = 0;
            UsedThisTurn = false;
        }

        public int CooldownTurns { get; }
        public int RemainingCooldown { get; private set; }
        public bool UsedThisTurn { get; private set; }
        public bool CanUse => RemainingCooldown == 0 && !UsedThisTurn;

        public int ReduceRemainingCooldown(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Cooldown reduction amount must be positive.");
            }

            int appliedAmount = Math.Min(RemainingCooldown, amount);
            RemainingCooldown -= appliedAmount;
            return appliedAmount;
        }

        internal void BeginTurn()
        {
            if (RemainingCooldown > 0)
            {
                RemainingCooldown--;
            }

            UsedThisTurn = false;
        }

        internal bool TryUse()
        {
            if (!CanUse)
            {
                return false;
            }

            RemainingCooldown = CooldownTurns;
            UsedThisTurn = true;
            return true;
        }

        internal bool TryRollbackUse()
        {
            if (!UsedThisTurn || RemainingCooldown != CooldownTurns)
            {
                return false;
            }

            RemainingCooldown = 0;
            UsedThisTurn = false;
            return true;
        }
    }
}
