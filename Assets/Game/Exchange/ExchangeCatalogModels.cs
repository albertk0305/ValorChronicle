using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Exchange
{
    public enum ExchangeContentKind
    {
        Character = 0,
        Relic = 1
    }

    public enum ExchangeCurrencyKind
    {
        HeroTokens = 0,
        RelicTokens = 1
    }

    [Flags]
    public enum ExchangeUnavailableReason
    {
        None = 0,
        InsufficientCurrency = 1 << 0,
        MaximumAwakening = 1 << 1
    }

    public enum ExchangeProgressKind
    {
        Unowned = 0,
        AwakeningIncrease = 1,
        NewInstanceAfterMaximumAwakening = 2,
        MaximumAwakening = 3
    }

    public sealed class ExchangeProgressDisplayData
    {
        internal ExchangeProgressDisplayData(
            ExchangeProgressKind kind,
            bool showAwakeningIcon,
            int? currentAwakening,
            int? resultingAwakening,
            int maximumAwakening)
        {
            Kind = kind;
            ShowAwakeningIcon = showAwakeningIcon;
            CurrentAwakening = currentAwakening;
            ResultingAwakening = resultingAwakening;
            MaximumAwakening = maximumAwakening;
        }

        public ExchangeProgressKind Kind { get; }
        public bool ShowAwakeningIcon { get; }
        public int? CurrentAwakening { get; }
        public int? ResultingAwakening { get; }
        public int MaximumAwakening { get; }
    }

    public abstract class ExchangeListItem
    {
        protected ExchangeListItem(
            ExchangeContentKind contentKind,
            string contentId,
            ExchangeCurrencyKind currencyKind,
            long cost,
            bool hasSufficientCurrency,
            ExchangeUnavailableReason unavailableReasons,
            ExchangeProgressDisplayData progress)
        {
            ContentKind = contentKind;
            ContentId = contentId;
            CurrencyKind = currencyKind;
            Cost = cost;
            HasSufficientCurrency = hasSufficientCurrency;
            UnavailableReasons = unavailableReasons;
            Progress = progress;
        }

        public ExchangeContentKind ContentKind { get; }
        public string ContentId { get; }
        public ExchangeCurrencyKind CurrencyKind { get; }
        public long Cost { get; }
        public bool HasSufficientCurrency { get; }
        public ExchangeUnavailableReason UnavailableReasons { get; }
        public bool CanExchange => UnavailableReasons
            == ExchangeUnavailableReason.None;
        public ExchangeProgressDisplayData Progress { get; }

        public ExchangeConfirmDisplayData CreateConfirmDisplayData()
        {
            return new ExchangeConfirmDisplayData(
                ContentKind,
                ContentId,
                CurrencyKind,
                Cost,
                UnavailableReasons,
                Progress);
        }
    }

    public sealed class CharacterExchangeListItem : ExchangeListItem
    {
        internal CharacterExchangeListItem(
            string characterId,
            bool isOwned,
            int currentAwakening,
            long cost,
            bool hasSufficientCurrency,
            ExchangeUnavailableReason unavailableReasons,
            ExchangeProgressDisplayData progress)
            : base(
                ExchangeContentKind.Character,
                characterId,
                ExchangeCurrencyKind.HeroTokens,
                cost,
                hasSufficientCurrency,
                unavailableReasons,
                progress)
        {
            CharacterId = characterId;
            IsOwned = isOwned;
            CurrentAwakening = currentAwakening;
        }

        public string CharacterId { get; }
        public bool IsOwned { get; }
        public int CurrentAwakening { get; }
        public int MaximumAwakening => SaveRules.CharacterMaxAwakening;
        public bool IsMaximumAwakening => IsOwned
            && CurrentAwakening >= MaximumAwakening;
    }

    public sealed class RelicExchangeListItem : ExchangeListItem
    {
        internal RelicExchangeListItem(
            string relicId,
            bool isOwned,
            string awakeningTargetInstanceId,
            int targetAwakening,
            bool willCreateNewInstance,
            long cost,
            bool hasSufficientCurrency,
            ExchangeUnavailableReason unavailableReasons,
            ExchangeProgressDisplayData progress)
            : base(
                ExchangeContentKind.Relic,
                relicId,
                ExchangeCurrencyKind.RelicTokens,
                cost,
                hasSufficientCurrency,
                unavailableReasons,
                progress)
        {
            RelicId = relicId;
            IsOwned = isOwned;
            AwakeningTargetInstanceId = awakeningTargetInstanceId
                ?? string.Empty;
            TargetAwakening = targetAwakening;
            WillCreateNewInstance = willCreateNewInstance;
        }

        public string RelicId { get; }
        public bool IsOwned { get; }
        public string AwakeningTargetInstanceId { get; }
        public int TargetAwakening { get; }
        public int MaximumAwakening => SaveRules.RelicMaxAwakening;
        public bool WillCreateNewInstance { get; }
    }

    public sealed class ExchangeConfirmDisplayData
    {
        internal ExchangeConfirmDisplayData(
            ExchangeContentKind contentKind,
            string contentId,
            ExchangeCurrencyKind currencyKind,
            long cost,
            ExchangeUnavailableReason unavailableReasons,
            ExchangeProgressDisplayData progress)
        {
            ContentKind = contentKind;
            ContentId = contentId;
            CurrencyKind = currencyKind;
            Cost = cost;
            UnavailableReasons = unavailableReasons;
            Progress = progress;
        }

        public ExchangeContentKind ContentKind { get; }
        public string ContentId { get; }
        public ExchangeCurrencyKind CurrencyKind { get; }
        public long Cost { get; }
        public ExchangeUnavailableReason UnavailableReasons { get; }
        public bool CanConfirm => UnavailableReasons
            == ExchangeUnavailableReason.None;
        public ExchangeProgressDisplayData Progress { get; }
    }

    public sealed class ExchangeCatalogData
    {
        internal ExchangeCatalogData(
            CharacterExchangeListItem[] characters,
            RelicExchangeListItem[] relics)
        {
            Characters = new ReadOnlyCollection<CharacterExchangeListItem>(
                (CharacterExchangeListItem[])characters.Clone());
            Relics = new ReadOnlyCollection<RelicExchangeListItem>(
                (RelicExchangeListItem[])relics.Clone());
        }

        public IReadOnlyList<CharacterExchangeListItem> Characters { get; }
        public IReadOnlyList<RelicExchangeListItem> Relics { get; }
    }
}
