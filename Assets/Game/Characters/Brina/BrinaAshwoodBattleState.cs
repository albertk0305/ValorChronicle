using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Brina
{
    public sealed class BrinaAshwoodBattleState
        : ICharacterCombatStatusSource
    {
        private int woodcutCount;
        private int? lastWoodcutGainTurn;

        public BrinaAshwoodBattleState(
            CharacterBattleState owner,
            int woodcutTriggerThreshold)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (!string.Equals(
                owner.CharacterId,
                BrinaAshwoodRules.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Brina battle state requires Brina Ashwood as its "
                        + "owner.",
                    nameof(owner));
            }

            if (woodcutTriggerThreshold <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(woodcutTriggerThreshold));
            }

            Owner = owner;
            WoodcutTriggerThreshold = woodcutTriggerThreshold;
        }

        public CharacterBattleState Owner { get; }
        public int WoodcutCount => woodcutCount;
        public int WoodcutTriggerThreshold { get; }
        public int? LastWoodcutGainTurn => lastWoodcutGainTurn;
        public string StatusId => BrinaAshwoodRules.WoodcutStatusId;
        public string CharacterId => Owner.CharacterId;
        public int PartySlotIndex => Owner.PartySlotIndex;
        public int CurrentCount => woodcutCount;
        public bool IsVisible => true;

        public bool TryMarkFirstWoodcutGainForTurn(int playerTurnNumber)
        {
            if (playerTurnNumber < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerTurnNumber));
            }

            if (lastWoodcutGainTurn == playerTurnNumber)
            {
                return false;
            }

            lastWoodcutGainTurn = playerTurnNumber;
            return true;
        }

        public int GainWoodcut(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            long total = (long)woodcutCount + amount;
            int triggerCount = checked(
                (int)(total / WoodcutTriggerThreshold));
            woodcutCount = (int)(total % WoodcutTriggerThreshold);
            return triggerCount;
        }
    }
}
