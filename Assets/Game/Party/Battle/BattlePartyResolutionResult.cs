using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Party.Battle
{
    public enum BattlePartyResolutionStatus
    {
        Success = 0,
        EmptyParty = 1,
        InvalidParty = 2
    }

    public sealed class BattlePartyResolutionResult
    {
        private static readonly ReadOnlyCollection<BattlePartyMemberInput>
            EmptyMembers = Array.AsReadOnly(
                Array.Empty<BattlePartyMemberInput>());

        private readonly ReadOnlyCollection<BattlePartyMemberInput> members;

        private BattlePartyResolutionResult(
            BattlePartyResolutionStatus status,
            IReadOnlyList<BattlePartyMemberInput> members,
            string errorMessage)
        {
            Status = status;
            ErrorMessage = errorMessage ?? string.Empty;

            if (members == null || members.Count == 0)
            {
                this.members = EmptyMembers;
                return;
            }

            var copy = new BattlePartyMemberInput[members.Count];
            for (int index = 0; index < members.Count; index++)
            {
                copy[index] = members[index];
            }

            this.members = Array.AsReadOnly(copy);
        }

        public BattlePartyResolutionStatus Status { get; }
        public bool IsSuccess =>
            Status == BattlePartyResolutionStatus.Success;
        public IReadOnlyList<BattlePartyMemberInput> Members => members;
        public string ErrorMessage { get; }

        internal static BattlePartyResolutionResult Succeeded(
            IReadOnlyList<BattlePartyMemberInput> members)
        {
            return new BattlePartyResolutionResult(
                BattlePartyResolutionStatus.Success,
                members,
                string.Empty);
        }

        internal static BattlePartyResolutionResult EmptyParty()
        {
            return new BattlePartyResolutionResult(
                BattlePartyResolutionStatus.EmptyParty,
                EmptyMembers,
                "The active party contains no characters.");
        }

        internal static BattlePartyResolutionResult InvalidParty(
            string errorMessage)
        {
            return new BattlePartyResolutionResult(
                BattlePartyResolutionStatus.InvalidParty,
                EmptyMembers,
                errorMessage);
        }
    }
}
