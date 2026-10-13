using System;

namespace ValorChronicle.Exchange.Persistence
{
    public sealed class ExchangeTransactionRequest
    {
        public ExchangeTransactionRequest(
            ExchangeContentKind contentKind,
            string contentId)
        {
            ContentKind = contentKind;
            ContentId = contentId;
        }

        public ExchangeContentKind ContentKind { get; }
        public string ContentId { get; }
    }
}
