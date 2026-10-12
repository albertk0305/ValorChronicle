using System;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Rewards
{
    public enum RewardCurrencyKind
    {
        HeroTokens = 0,
        RelicTokens = 1,
        BattleRecords = 2
    }

    public sealed class CurrencyGrantResult
    {
        internal CurrencyGrantResult(
            RewardCurrencyKind currencyKind,
            long amount,
            long previousAmount,
            long newAmount)
        {
            CurrencyKind = currencyKind;
            Amount = amount;
            PreviousAmount = previousAmount;
            NewAmount = newAmount;
        }

        public RewardCurrencyKind CurrencyKind { get; }
        public long Amount { get; }
        public long PreviousAmount { get; }
        public long NewAmount { get; }
    }

    public static class CurrencyProfileUpdater
    {
        public static CurrencyGrantResult Add(
            ProfileSaveData profile,
            RewardCurrencyKind currencyKind,
            long amount)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (profile.Currencies == null)
            {
                throw new ArgumentException(
                    "Profile currencies are required.",
                    nameof(profile));
            }

            if (!Enum.IsDefined(typeof(RewardCurrencyKind), currencyKind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currencyKind),
                    currencyKind,
                    "Currency kind is invalid.");
            }

            if (amount <= 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Currency grant amount must be positive.");
            }

            long previousAmount = GetAmount(
                profile.Currencies,
                currencyKind);
            if (previousAmount < 0L)
            {
                throw new ArgumentException(
                    "Profile currency amount cannot be negative.",
                    nameof(profile));
            }

            long newAmount = checked(previousAmount + amount);
            SetAmount(profile.Currencies, currencyKind, newAmount);
            return new CurrencyGrantResult(
                currencyKind,
                amount,
                previousAmount,
                newAmount);
        }

        private static long GetAmount(
            CurrencySaveData currencies,
            RewardCurrencyKind currencyKind)
        {
            switch (currencyKind)
            {
                case RewardCurrencyKind.HeroTokens:
                    return currencies.HeroTokens;
                case RewardCurrencyKind.RelicTokens:
                    return currencies.RelicTokens;
                case RewardCurrencyKind.BattleRecords:
                    return currencies.BattleRecords;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(currencyKind));
            }
        }

        private static void SetAmount(
            CurrencySaveData currencies,
            RewardCurrencyKind currencyKind,
            long amount)
        {
            switch (currencyKind)
            {
                case RewardCurrencyKind.HeroTokens:
                    currencies.HeroTokens = amount;
                    break;
                case RewardCurrencyKind.RelicTokens:
                    currencies.RelicTokens = amount;
                    break;
                case RewardCurrencyKind.BattleRecords:
                    currencies.BattleRecords = amount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(currencyKind));
            }
        }
    }
}
