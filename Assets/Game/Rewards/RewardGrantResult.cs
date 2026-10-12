using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Gacha;

namespace ValorChronicle.Rewards
{
    public enum RewardGrantKind
    {
        NewCharacter = 0,
        CharacterAwakeningIncreased = 1,
        MaxAwakeningCharacterConvertedToHeroTokens = 2,
        NewRelicInstanceCreated = 3,
        RelicAwakeningIncreased = 4,
        CurrencyGranted = 5
    }

    public sealed class RewardGrantRequest
    {
        public RewardGrantRequest(
            int index,
            GachaRewardKind rewardKind,
            string rewardId,
            long amount)
            : this(index, rewardKind, rewardId, amount, null)
        {
        }

        private RewardGrantRequest(
            int index,
            GachaRewardKind rewardKind,
            string rewardId,
            long amount,
            GachaPullResult sourcePull)
        {
            Index = index;
            RewardKind = rewardKind;
            RewardId = rewardId;
            Amount = amount;
            SourcePull = sourcePull;
        }

        public int Index { get; }
        public GachaRewardKind RewardKind { get; }
        public string RewardId { get; }
        public long Amount { get; }
        public GachaPullResult SourcePull { get; }

        public static RewardGrantRequest FromGachaPull(
            GachaPullResult pull)
        {
            if (pull == null)
            {
                throw new ArgumentNullException(nameof(pull));
            }

            return new RewardGrantRequest(
                pull.Index,
                pull.RewardKind,
                pull.RewardId,
                pull.Amount,
                pull);
        }
    }

    public sealed class RewardGrantResult
    {
        internal RewardGrantResult(
            RewardGrantRequest request,
            RewardGrantKind grantKind,
            string contentId,
            string relicInstanceId,
            int? previousAwakening,
            int? newAwakening,
            CurrencyGrantResult currencyGrant,
            bool relicCreatedAfterMaxAwakening)
        {
            Request = request;
            GrantKind = grantKind;
            ContentId = contentId;
            RelicInstanceId = relicInstanceId;
            PreviousAwakening = previousAwakening;
            NewAwakening = newAwakening;
            CurrencyGrant = currencyGrant;
            RelicCreatedAfterMaxAwakening =
                relicCreatedAfterMaxAwakening;
        }

        public RewardGrantRequest Request { get; }
        public int Index => Request.Index;
        public GachaRewardKind OriginalRewardKind => Request.RewardKind;
        public string OriginalRewardId => Request.RewardId;
        public long OriginalAmount => Request.Amount;
        public GachaPullResult SourcePull => Request.SourcePull;
        public RewardGrantKind GrantKind { get; }
        public string ContentId { get; }
        public string RelicInstanceId { get; }
        public int? PreviousAwakening { get; }
        public int? NewAwakening { get; }
        public CurrencyGrantResult CurrencyGrant { get; }
        public bool RelicCreatedAfterMaxAwakening { get; }
    }

    public sealed class RewardGrantBatchResult
    {
        private readonly ReadOnlyCollection<RewardGrantResult> grants;

        internal RewardGrantBatchResult(RewardGrantResult[] grants)
        {
            if (grants == null)
            {
                throw new ArgumentNullException(nameof(grants));
            }

            this.grants = Array.AsReadOnly(
                (RewardGrantResult[])grants.Clone());
        }

        public IReadOnlyList<RewardGrantResult> Grants => grants;
    }
}
