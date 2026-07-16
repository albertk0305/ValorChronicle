using System;

namespace ValorChronicle.Core.Random
{
    public sealed class SeededRandomSource : IRandomSource
    {
        private const int FloatSampleCount = 1 << 24;
        private const float InverseFloatSampleCount = 1f / FloatSampleCount;

        private readonly System.Random random;

        public SeededRandomSource(int seed)
        {
            random = new System.Random(seed);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            ValidateRange(minInclusive, maxExclusive);
            return random.Next(minInclusive, maxExclusive);
        }

        public float NextFloat()
        {
            return random.Next(0, FloatSampleCount) * InverseFloatSampleCount;
        }

        private static void ValidateRange(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive),
                    maxExclusive,
                    "Maximum must be greater than minimum.");
            }
        }
    }
}
