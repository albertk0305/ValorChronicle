using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode
{
    internal static class MareaBluefangTestConfig
    {
        private const string CanonicalPath =
            "Assets/Data/CombatConfigs/Marea/"
            + "marea_bluefang_combat_config.asset";

        public static MareaBluefangCombatConfig Canonical =>
            AssetDatabase.LoadAssetAtPath<MareaBluefangCombatConfig>(
                CanonicalPath);

        public static MareaBluefangCombatConfig Create(
            double match3Coefficient = 0.90d,
            double match4BaseCoefficient = 1.50d,
            double match4WaterElementBonusCoefficient = 0.40d,
            double match5BaseCoefficient = 2.40d,
            double match5CoefficientPerWaterElement = 1.40d,
            double passiveDealtDamageIncreaseRate = 0.15d,
            double activeWaterDamageIncreaseRate = 0.25d,
            int activeDurationTurns = 3,
            int activeCooldownTurns = 8,
            int waterElementMaxAmount = 5)
        {
            var config = ScriptableObject.CreateInstance<
                MareaBluefangCombatConfig>();
            var serialized = new SerializedObject(config);
            SetDouble(serialized, "match3Coefficient", match3Coefficient);
            SetDouble(
                serialized,
                "match4BaseCoefficient",
                match4BaseCoefficient);
            SetDouble(
                serialized,
                "match4WaterElementBonusCoefficient",
                match4WaterElementBonusCoefficient);
            SetDouble(
                serialized,
                "match5BaseCoefficient",
                match5BaseCoefficient);
            SetDouble(
                serialized,
                "match5CoefficientPerWaterElement",
                match5CoefficientPerWaterElement);
            SetDouble(
                serialized,
                "passiveDealtDamageIncreaseRate",
                passiveDealtDamageIncreaseRate);
            SetDouble(
                serialized,
                "activeWaterDamageIncreaseRate",
                activeWaterDamageIncreaseRate);
            serialized.FindProperty("activeDurationTurns").intValue =
                activeDurationTurns;
            serialized.FindProperty("activeCooldownTurns").intValue =
                activeCooldownTurns;
            serialized.FindProperty("waterElementMaxAmount").intValue =
                waterElementMaxAmount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        public static void Assign(
            CharacterDefinition character,
            MareaBluefangCombatConfig config)
        {
            var serialized = new SerializedObject(character);
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
