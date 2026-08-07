using System;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class CharacterBattleState
    {
        public const int MinimumPartySlotIndex = 0;
        public const int MaximumPartySlotIndex = 4;

        public CharacterBattleState(
            string characterId,
            int partySlotIndex,
            ElementType element,
            long maxHp,
            double attack)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "Character ID cannot be null or whitespace.",
                    nameof(characterId));
            }

            if (partySlotIndex < MinimumPartySlotIndex
                || partySlotIndex > MaximumPartySlotIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partySlotIndex),
                    partySlotIndex,
                    $"Party slot index must be between "
                        + $"{MinimumPartySlotIndex} and "
                        + $"{MaximumPartySlotIndex}.");
            }

            ValidateElement(element);
            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHp),
                    maxHp,
                    "Maximum HP must be positive.");
            }

            if (double.IsNaN(attack)
                || double.IsInfinity(attack)
                || attack < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attack),
                    attack,
                    "Attack must be finite and non-negative.");
            }

            CharacterId = characterId;
            PartySlotIndex = partySlotIndex;
            Element = element;
            MaxHp = maxHp;
            Attack = attack;
            Effects = new EffectCollection();
        }

        public string CharacterId { get; }
        public int PartySlotIndex { get; }
        public ElementType Element { get; }
        public long MaxHp { get; }
        public double Attack { get; }
        public EffectCollection Effects { get; }

        private static void ValidateElement(ElementType element)
        {
            if (!Enum.IsDefined(typeof(ElementType), element))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(element),
                    element,
                    "Element must be a defined value.");
            }
        }
    }
}
