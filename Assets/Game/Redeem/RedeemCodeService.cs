using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Redeem
{
    public enum RedeemRewardType
    {
        GachaCurrency = 0
    }

    public enum RedeemCodeStatus
    {
        Success = 0,
        EmptyCode = 1,
        InvalidCode = 2,
        PersistenceFailed = 3
    }

    public sealed class RedeemCodeDefinition
    {
        public RedeemCodeDefinition(
            string code,
            RedeemRewardType rewardType,
            long amount)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("A redeem code is required.", nameof(code));
            if (amount <= 0L)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Code = code.Trim().ToUpperInvariant();
            RewardType = rewardType;
            Amount = amount;
        }

        public string Code { get; }
        public RedeemRewardType RewardType { get; }
        public long Amount { get; }
    }

    public sealed class RedeemCodeResult
    {
        public RedeemCodeResult(
            RedeemCodeStatus status,
            RedeemCodeDefinition definition = null,
            long currencyBefore = 0L,
            long currencyAfter = 0L,
            SaveTransactionResult transactionResult = null)
        {
            Status = status;
            Definition = definition;
            CurrencyBefore = currencyBefore;
            CurrencyAfter = currencyAfter;
            TransactionResult = transactionResult;
        }

        public RedeemCodeStatus Status { get; }
        public RedeemCodeDefinition Definition { get; }
        public long CurrencyBefore { get; }
        public long CurrencyAfter { get; }
        public SaveTransactionResult TransactionResult { get; }
        public bool IsSuccess => Status == RedeemCodeStatus.Success;
    }

    public interface IRedeemCodeService
    {
        RedeemCodeResult Redeem(string input);
    }

    public sealed class RedeemCodeService : IRedeemCodeService
    {
        public const string InitialCode = "NEAT3";
        public const long InitialRewardAmount = 10000L;

        private static readonly IReadOnlyDictionary<
            string,
            RedeemCodeDefinition> Definitions =
            new Dictionary<string, RedeemCodeDefinition>(
                StringComparer.OrdinalIgnoreCase)
            {
                {
                    InitialCode,
                    new RedeemCodeDefinition(
                        InitialCode,
                        RedeemRewardType.GachaCurrency,
                        InitialRewardAmount)
                }
            };

        private readonly SaveService saveService;

        public RedeemCodeService(SaveService saveService)
        {
            this.saveService = saveService
                ?? throw new ArgumentNullException(nameof(saveService));
        }

        public RedeemCodeResult Redeem(string input)
        {
            string normalized = Normalize(input);
            if (normalized.Length == 0)
            {
                return new RedeemCodeResult(RedeemCodeStatus.EmptyCode);
            }

            if (!Definitions.TryGetValue(
                    normalized,
                    out RedeemCodeDefinition definition))
            {
                return new RedeemCodeResult(RedeemCodeStatus.InvalidCode);
            }

            long currencyBefore = 0L;
            long currencyAfter = 0L;
            SaveTransactionResult transaction =
                saveService.ExecuteTransaction(profile =>
                {
                    CurrencySaveData currencies = profile?.Currencies
                        ?? throw new InvalidOperationException(
                            "Profile currencies are unavailable.");
                    if (currencies.GachaCurrency < 0L)
                    {
                        throw new InvalidOperationException(
                            "Gacha currency cannot be negative.");
                    }

                    currencyBefore = currencies.GachaCurrency;
                    currencyAfter = checked(
                        currencyBefore + definition.Amount);
                    currencies.GachaCurrency = currencyAfter;
                });

            return transaction.IsSuccess
                ? new RedeemCodeResult(
                    RedeemCodeStatus.Success,
                    definition,
                    currencyBefore,
                    currencyAfter,
                    transaction)
                : new RedeemCodeResult(
                    RedeemCodeStatus.PersistenceFailed,
                    definition,
                    currencyBefore,
                    currencyBefore,
                    transaction);
        }

        private static string Normalize(string input)
        {
            return string.IsNullOrWhiteSpace(input)
                ? string.Empty
                : input.Trim().ToUpperInvariant();
        }
    }
}
