using System;

namespace ValorChronicle.Audio.Core
{
    public static class AudioVolumeMath
    {
        public const float MutedDecibels = -80f;

        public static float PercentageToDecibels(int volume)
        {
            int normalized = Math.Max(0, Math.Min(100, volume));
            if (normalized == 0)
                return MutedDecibels;

            return 20f * (float)Math.Log10(normalized / 100f);
        }

        public static float DecibelsToLinear(float decibels)
        {
            if (decibels <= MutedDecibels)
                return 0f;

            return (float)Math.Pow(10d, decibels / 20d);
        }
    }
}
