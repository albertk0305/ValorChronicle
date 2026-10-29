using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Koharu
{
    public sealed class SakuragiKoharuBattleState
        : ICharacterCombatStatusSource
    {
        private long spiritEnergy;

        public SakuragiKoharuBattleState(CharacterBattleState owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (!string.Equals(
                owner.CharacterId,
                SakuragiKoharuRules.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Koharu battle state requires Sakuragi Koharu as its "
                        + "owner.",
                    nameof(owner));
            }
        }

        public CharacterBattleState Owner { get; }
        public long SpiritEnergy => spiritEnergy;
        public string StatusId => SakuragiKoharuRules.SpiritEnergyStatusId;
        public string CharacterId => Owner.CharacterId;
        public int PartySlotIndex => Owner.PartySlotIndex;
        public int CurrentCount => spiritEnergy >= int.MaxValue
            ? int.MaxValue
            : (int)spiritEnergy;
        public bool IsVisible => true;

        public long GainSpiritEnergy(long amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Spirit Energy gain must be positive.");
            }

            spiritEnergy = checked(spiritEnergy + amount);
            return spiritEnergy;
        }

        public bool TryConsumeSpiritEnergy(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Spirit Energy consumption must be positive.");
            }

            if (spiritEnergy < amount)
            {
                return false;
            }

            spiritEnergy -= amount;
            return true;
        }
    }
}
