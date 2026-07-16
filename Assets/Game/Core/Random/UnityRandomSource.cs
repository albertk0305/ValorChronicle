using System;

namespace ValorChronicle.Core.Random
{
    public sealed class UnityRandomSource : IRandomSource
    {
        private const int FloatSampleCount = 1 << 24;
        private const float InverseFloatSampleCount = 1f / FloatSampleCount;

        public int Next(int minInclusive, int maxExclusive)
        {
            ValidateRange(minInclusive, maxExclusive);
            return UnityEngine.Random.Range(minInclusive, maxExclusive);
        }

        public float NextFloat()
        {
            int sample = UnityEngine.Random.Range(0, FloatSampleCount);
            return sample * InverseFloatSampleCount;
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
