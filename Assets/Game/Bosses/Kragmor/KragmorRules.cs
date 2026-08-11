using System;

namespace ValorChronicle.Bosses.Kragmor
{
    public static class KragmorRules
    {
        public const string BossId = "kragmor";
        public const string VolcanicCarapaceEffectId =
            "effect_kragmor_volcanic_carapace";
        public const string CoreCompressionEffectId =
            "effect_kragmor_core_compression";
        public const string CoreExposureEffectId =
            "effect_kragmor_core_exposure";
        public const int PatternCount = 4;
        public const double ColossusIronFistCoefficient = 0.75d;
        public const double RockshardEruptionCoefficient = 0.65d;
        public const int RockshardRockCreationCount = 3;
        public const int MaximumRockCount = 6;
        public const double EarthCollapseCoefficient = 2.40d;
        public const double VolcanicCarapaceReductionRate = 0.10d;
        public const double CoreCompressionReductionRate = 0.20d;
        public const double CoreExposureIncreaseRate = 0.30d;

        private static readonly KragmorBossIntent[] Intents =
        {
            new KragmorBossIntent(
                KragmorActionKind.ColossusIronFist,
                true,
                false,
                ColossusIronFistCoefficient),
            new KragmorBossIntent(
                KragmorActionKind.RockshardEruption,
                true,
                false,
                RockshardEruptionCoefficient),
            new KragmorBossIntent(
                KragmorActionKind.CoreCompression,
                false,
                false,
                0d),
            new KragmorBossIntent(
                KragmorActionKind.EarthCollapse,
                true,
                true,
                EarthCollapseCoefficient)
        };

        public static KragmorBossIntent GetIntent(
            KragmorActionKind actionKind)
        {
            if (!Enum.IsDefined(typeof(KragmorActionKind), actionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(actionKind));
            }

            return Intents[(int)actionKind];
        }
    }
}
