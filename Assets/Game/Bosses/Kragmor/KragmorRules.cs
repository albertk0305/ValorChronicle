namespace ValorChronicle.Bosses.Kragmor
{
    public static class KragmorRules
    {
        public const string BossId = "kragmor";
        public const string ColossusIronFistSkillId =
            "boss_action_kragmor_colossus_iron_fist";
        public const string RockshardEruptionSkillId =
            "boss_action_kragmor_rockshard_eruption";
        public const string CoreCompressionSkillId =
            "boss_action_kragmor_core_compression";
        public const string EarthCollapseSkillId =
            "boss_action_kragmor_earth_collapse";
        public const string VolcanicCarapaceEffectId =
            "effect_kragmor_volcanic_carapace";
        public const string CoreCompressionEffectId =
            "effect_kragmor_core_compression";
        public const string CoreExposureEffectId =
            "effect_kragmor_core_exposure";
        public const string DefaultVisualStateId = "kragmor_default";
        public const string CoreCompressionVisualStateId =
            "kragmor_core_compression";
        public const string CoreExposureVisualStateId =
            "kragmor_core_exposure";
        public const int PatternCount = 4;

        public static string GetSkillId(KragmorActionKind actionKind)
        {
            switch (actionKind)
            {
                case KragmorActionKind.ColossusIronFist:
                    return ColossusIronFistSkillId;
                case KragmorActionKind.RockshardEruption:
                    return RockshardEruptionSkillId;
                case KragmorActionKind.CoreCompression:
                    return CoreCompressionSkillId;
                case KragmorActionKind.EarthCollapse:
                    return EarthCollapseSkillId;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(actionKind),
                        actionKind,
                        "Unsupported Kragmor action kind.");
            }
        }

        public static string GetVisualStateId(
            KragmorDefenseState defenseState)
        {
            switch (defenseState)
            {
                case KragmorDefenseState.VolcanicCarapace:
                    return DefaultVisualStateId;
                case KragmorDefenseState.CoreCompression:
                    return CoreCompressionVisualStateId;
                case KragmorDefenseState.CoreExposure:
                    return CoreExposureVisualStateId;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(defenseState),
                        defenseState,
                        "Unsupported Kragmor defense state.");
            }
        }
    }
}
