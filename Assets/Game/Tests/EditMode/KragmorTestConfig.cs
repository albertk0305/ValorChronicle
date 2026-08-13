using UnityEditor;
using UnityEngine;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode
{
    internal static class KragmorTestConfig
    {
        private const string CanonicalPath =
            "Assets/Data/CombatConfigs/Kragmor/"
            + "kragmor_combat_config.asset";

        public static KragmorCombatConfig Canonical =>
            AssetDatabase.LoadAssetAtPath<KragmorCombatConfig>(
                CanonicalPath);

        public static KragmorCombatConfig Create(
            double colossusIronFistCoefficient = 0.75d,
            double rockshardEruptionCoefficient = 0.65d,
            int rockshardRockCreationCount = 3,
            int maximumRockCount = 6,
            double earthCollapseCoefficient = 2.40d,
            double volcanicCarapaceReductionRate = 0.10d,
            double coreCompressionReductionRate = 0.20d,
            double coreExposureIncreaseRate = 0.30d)
        {
            var config = ScriptableObject.CreateInstance<
                KragmorCombatConfig>();
            var serialized = new SerializedObject(config);
            SetDouble(
                serialized,
                "colossusIronFistCoefficient",
                colossusIronFistCoefficient);
            SetDouble(
                serialized,
                "rockshardEruptionCoefficient",
                rockshardEruptionCoefficient);
            serialized.FindProperty("rockshardRockCreationCount").intValue =
                rockshardRockCreationCount;
            serialized.FindProperty("maximumRockCount").intValue =
                maximumRockCount;
            SetDouble(
                serialized,
                "earthCollapseCoefficient",
                earthCollapseCoefficient);
            SetDouble(
                serialized,
                "volcanicCarapaceReductionRate",
                volcanicCarapaceReductionRate);
            SetDouble(
                serialized,
                "coreCompressionReductionRate",
                coreCompressionReductionRate);
            SetDouble(
                serialized,
                "coreExposureIncreaseRate",
                coreExposureIncreaseRate);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        public static void Assign(
            BossDefinition boss,
            KragmorCombatConfig config)
        {
            var serialized = new SerializedObject(boss);
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetDouble(
            SerializedObject serialized,
            string propertyName,
            double value)
        {
            serialized.FindProperty(propertyName).doubleValue = value;
        }
    }
}
