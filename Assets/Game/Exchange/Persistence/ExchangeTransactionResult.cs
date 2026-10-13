using System;
using ValorChronicle.Rewards;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Exchange.Persistence
{
    public enum ExchangeTransactionStatus
    {
        Success = 0,
        InvalidRequest = 1,
        UnknownContent = 2,
        NotExchangeable = 3,
        InsufficientCurrency = 4,
        MaximumAwakening = 5,
        TransactionBusy = 6,
        InvalidProfile = 7,
        RewardGrantFailed = 8,
        PersistenceFailed = 9,
        PersistedStateMismatch = 10
    }

    public sealed class ExchangeTransactionResult
    {
        internal ExchangeTransactionResult(
            ExchangeContentKind contentKind,
            string contentId,
            ExchangeCurrencyKind currencyKind,
            long cost,
            long currencyBefore,
            long currencyAfter,
            RewardGrantResult grant,
            bool wasSanitized)
        {
            if (grant == null)
                throw new ArgumentNullException(nameof(grant));

            ContentKind = contentKind;
            ContentId = contentId;
            CurrencyKind = currencyKind;
            Cost = cost;
            CurrencyBefore = currencyBefore;
            CurrencyAfter = currencyAfter;
            GrantKind = grant.GrantKind;
            RelicInstanceId = grant.RelicInstanceId;
            PreviousAwakening = grant.PreviousAwakening;
            NewAwakening = grant.NewAwakening;
            IsNewCharacter = grant.GrantKind
                == RewardGrantKind.NewCharacter;
            IsNewRelicInstance = grant.GrantKind
                == RewardGrantKind.NewRelicInstanceCreated;
            RelicCreatedAfterMaxAwakening =
                grant.RelicCreatedAfterMaxAwakening;
            WasSanitized = wasSanitized;
        }

        public ExchangeContentKind ContentKind { get; }
        public string ContentId { get; }
        public ExchangeCurrencyKind CurrencyKind { get; }
        public long Cost { get; }
        public long CurrencyBefore { get; }
        public long CurrencyAfter { get; }
        public RewardGrantKind GrantKind { get; }
        public string RelicInstanceId { get; }
        public int? PreviousAwakening { get; }
        public int? NewAwakening { get; }
        public bool IsNewCharacter { get; }
        public bool IsNewRelicInstance { get; }
        public bool RelicCreatedAfterMaxAwakening { get; }
        public bool WasSanitized { get; }
    }

    public sealed class ExchangeTransactionOperationResult
    {
        internal ExchangeTransactionOperationResult(
            ExchangeTransactionStatus status,
            ExchangeTransactionResult transactionResult,
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

        public ExchangeTransactionStatus Status { get; }
        public bool IsSuccess => Status == ExchangeTransactionStatus.Success;
        public ExchangeTransactionResult TransactionResult { get; }
        public SaveTransactionResult SaveTransactionResult { get; }
        public bool WasPersisted => SaveTransactionResult?.IsSuccess == true;
        public Exception Exception { get; }
        public string Message { get; }
    }
}
