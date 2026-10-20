using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Serena
{
    public sealed class SerenaCoralspearBattleState
        : ICharacterCombatStatusSource
    {
        private int empoweredChaseCharges;

        public SerenaCoralspearBattleState(
            CharacterBattleState owner,
            int empoweredChaseMaxCharges,
            int initialEmpoweredChaseCharges = 0)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (!string.Equals(
                owner.CharacterId,
                SerenaCoralspearRules.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Serena battle state requires Serena Coralspear as its "
                        + "owner.",
                    nameof(owner));
            }

            if (empoweredChaseMaxCharges <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(empoweredChaseMaxCharges),
                    empoweredChaseMaxCharges,
                    "Empowered chase maximum charges must be positive.");
            }

            if (initialEmpoweredChaseCharges < 0
                || initialEmpoweredChaseCharges
                    > empoweredChaseMaxCharges)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialEmpoweredChaseCharges),
                    initialEmpoweredChaseCharges,
                    "Initial empowered chase charges must be between zero "
                        + "and the configured maximum.");
            }

            Owner = owner;
            EmpoweredChaseMaxCharges = empoweredChaseMaxCharges;
            empoweredChaseCharges = initialEmpoweredChaseCharges;
        }

        public CharacterBattleState Owner { get; }
        public int EmpoweredChaseCharges => empoweredChaseCharges;
        public int EmpoweredChaseMaxCharges { get; }
        public bool HasEmpoweredChase => empoweredChaseCharges > 0;
        public string StatusId =>
            SerenaCoralspearRules.EmpoweredChaseStatusId;
        public string CharacterId => Owner.CharacterId;
        public int PartySlotIndex => Owner.PartySlotIndex;
        public int CurrentCount => EmpoweredChaseCharges;
        public bool IsVisible => HasEmpoweredChase;

        public bool TryGrantEmpoweredChase()
        {
            if (empoweredChaseCharges >= EmpoweredChaseMaxCharges)
            {
                return false;
            }

            empoweredChaseCharges++;
            return true;
        }

        public bool TryConsumeEmpoweredChase()
        {
            if (empoweredChaseCharges == 0)
            {
                return false;
            }

            empoweredChaseCharges--;
            return true;
        }
    }
}
