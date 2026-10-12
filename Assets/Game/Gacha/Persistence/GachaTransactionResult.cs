using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Rewards;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Gacha.Persistence
{
    public enum GachaTransactionStatus
    {
        Success = 0,
        InvalidGachaId = 1,
        UnsupportedPullCount = 2,
        DefinitionNotFound = 3,
        InvalidDefinition = 4,
        InsufficientGachaCurrency = 5,
        InvalidProfile = 6,
        InvalidGachaState = 7,
        CalculationFailed = 8,
        RewardGrantFailed = 9,
        CurrencyDeductionFailed = 10,
        PersistenceFailed = 11,
        PersistedResultMismatch = 12
    }

    public sealed class GachaTransactionStateResult
    {
        internal GachaTransactionStateResult(
            string gachaId,
            int pityCount,
            long totalPullCount)
        {
            GachaId = gachaId;
            PityCount = pityCount;
            TotalPullCount = totalPullCount;
        }

        public string GachaId { get; }
        public int PityCount { get; }
        public long TotalPullCount { get; }
    }

    public sealed class GachaTransactionPullResult
    {
        internal GachaTransactionPullResult(
            GachaPullResult pull,
            RewardGrantResult grant)
        {
            if (pull == null)
            {
                throw new ArgumentNullException(nameof(pull));
            }

            if (grant == null)
            {
                throw new ArgumentNullException(nameof(grant));
            }

            if (pull.Index != grant.Index)
            {
                throw new ArgumentException(
                    "Pull and grant indices must match.",
                    nameof(grant));
            }

            Index = pull.Index;
            RewardKind = pull.RewardKind;
            RewardId = pull.RewardId;
            Amount = pull.Amount;
            WasGuaranteedByPity = pull.WasGuaranteedByPity;
            PityCountBefore = pull.PityCountBefore;
            PityCountAfter = pull.PityCountAfter;
            GrantKind = grant.GrantKind;
            ContentId = grant.ContentId;
            RelicInstanceId = grant.RelicInstanceId;
            PreviousAwakening = grant.PreviousAwakening;
            NewAwakening = grant.NewAwakening;
            RelicCreatedAfterMaxAwakening =
                grant.RelicCreatedAfterMaxAwakening;
            GrantedCurrencyKind = grant.CurrencyGrant?.CurrencyKind;
            GrantedCurrencyAmount = grant.CurrencyGrant?.Amount ?? 0L;
        }

        public int Index { get; }
        public GachaRewardKind RewardKind { get; }
        public string RewardId { get; }
        public long Amount { get; }
        public bool WasGuaranteedByPity { get; }
        public int PityCountBefore { get; }
        public int PityCountAfter { get; }
        public RewardGrantKind GrantKind { get; }
        public string ContentId { get; }
        public string RelicInstanceId { get; }
        public int? PreviousAwakening { get; }
        public int? NewAwakening { get; }
        public bool RelicCreatedAfterMaxAwakening { get; }
        public RewardCurrencyKind? GrantedCurrencyKind { get; }
        public long GrantedCurrencyAmount { get; }
    }

    public sealed class GachaTransactionResult
    {
        private readonly ReadOnlyCollection<GachaTransactionPullResult>
            pulls;

        internal GachaTransactionResult(
            string gachaId,
            GachaKind gachaKind,
            int pullCount,
            long goldSpent,
            long goldBefore,
            long goldAfter,
            GachaTransactionPullResult[] pulls,
            GachaTransactionStateResult resultingState,
            bool wasSanitized)
        {
            GachaId = gachaId;
            GachaKind = gachaKind;
            PullCount = pullCount;
            GoldSpent = goldSpent;
            GoldBefore = goldBefore;
            GoldAfter = goldAfter;
            this.pulls = Array.AsReadOnly(
                (GachaTransactionPullResult[])pulls.Clone());
            ResultingState = resultingState
                ?? throw new ArgumentNullException(nameof(resultingState));
            WasSanitized = wasSanitized;
        }

        public string GachaId { get; }
        public GachaKind GachaKind { get; }
        public int PullCount { get; }
        public long GoldSpent { get; }
        public long GoldBefore { get; }
        public long GoldAfter { get; }
        public IReadOnlyList<GachaTransactionPullResult> Pulls => pulls;
        public GachaTransactionStateResult ResultingState { get; }
        public bool WasSanitized { get; }
    }

    public sealed class GachaTransactionOperationResult
    {
        internal GachaTransactionOperationResult(
            GachaTransactionStatus status,
            GachaTransactionResult transactionResult,
            SaveTransactionResult saveTransactionResult,
            Exception exception,
            string message)
        {
            Status = status;
            TransactionResult = transactionResult;
            SaveTransactionResult = saveTransactionResult;
            Exception = exception;
            Message = message ?? string.Empty;
        }

        public GachaTransactionStatus Status { get; }
        public bool IsSuccess => Status == GachaTransactionStatus.Success;
        public bool WasPersisted => SaveTransactionResult?.IsSuccess == true;
        public GachaTransactionResult TransactionResult { get; }
        public SaveTransactionResult SaveTransactionResult { get; }
        public Exception Exception { get; }
        public string Message { get; }
    }
}
