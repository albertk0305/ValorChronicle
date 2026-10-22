using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public interface IBattleBoardElementConversionSink
    {
        /// <summary>
        /// True only while both the authoritative Board and its presentation
        /// can accept an immediate, synchronous conversion.
        /// </summary>
        bool CanAcceptElementConversion { get; }

        /// <summary>
        /// Applies a conversion atomically. Returning false must leave the
        /// authoritative Board unchanged; an unsafe presentation must also
        /// make <see cref="CanAcceptElementConversion"/> return false.
        /// </summary>
        bool TryConvertElements(
            ElementType targetElement,
            int requestedCount,
            out BoardElementConversionResult result);
    }
}
