namespace ValorChronicle.Core.Random
{
    public interface IRandomSource
    {
        /// <summary>
        /// Returns an integer greater than or equal to <paramref name="minInclusive"/>
        /// and less than <paramref name="maxExclusive"/>.
        /// </summary>
        int Next(int minInclusive, int maxExclusive);

        /// <summary>
        /// Returns a floating-point value greater than or equal to zero and less than one.
        /// </summary>
        float NextFloat();
    }
}
