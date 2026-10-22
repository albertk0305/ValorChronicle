using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiBattleState
        : ICharacterCombatStatusSource
    {
        private int frostEchoCount;

        public ShimotsukiMiyukiBattleState(
            CharacterBattleState owner,
            bool isFrostEchoEnabled)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (!string.Equals(
                owner.CharacterId,
                ShimotsukiMiyukiRules.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Miyuki battle state requires Shimotsuki Miyuki as its owner.",
                    nameof(owner));
            }

            IsFrostEchoEnabled = isFrostEchoEnabled;
        }

        public CharacterBattleState Owner { get; }
        public bool IsFrostEchoEnabled { get; }
        public int FrostEchoCount => frostEchoCount;
        public bool HasFrostEcho => frostEchoCount > 0;
        public string StatusId => ShimotsukiMiyukiRules.FrostEchoStatusId;
        public string CharacterId => Owner.CharacterId;
        public int PartySlotIndex => Owner.PartySlotIndex;
        public int CurrentCount => FrostEchoCount;
        public bool IsVisible => HasFrostEcho;

        public bool TryGrantFrostEcho()
        {
            if (!IsFrostEchoEnabled || frostEchoCount != 0)
            {
                return false;
            }

            frostEchoCount = 1;
            return true;
        }

        public bool TryConsumeFrostEcho()
        {
            if (!IsFrostEchoEnabled || frostEchoCount == 0)
            {
                return false;
            }

            frostEchoCount = 0;
            return true;
        }
    }
}
