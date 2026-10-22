using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Miyuki
{
    internal static class ShimotsukiMiyukiTestConfig
    {
        public static ShimotsukiMiyukiCombatConfig Create(
            double match3Coefficient = 0.55d,
            double match4Coefficient = 0.90d,
            double match5Coefficient = 1.45d,
            int match3WaterAmount = 1,
            int match4WaterAmount = 2,
            int match5WaterAmount = 3,
            double passiveRate = 0.08d,
            double enhancedPassiveRate = 0.10d,
            double match5Rate = 0.12d,
            int match5Duration = 2,
            double enhancedMatch3 = 0.60d,
            double enhancedMatch4 = 1.00d,
            int activeCooldown = 8,
            int activeConversionCount = 4,
            int enhancedActiveConversionCount = 5,
            int awakening6ExtraWaterAmount = 1,
            int waterMaximum = 5)
        {
            var config = ScriptableObject.CreateInstance<
                ShimotsukiMiyukiCombatConfig>();
            var serialized = new SerializedObject(config);
            Set(serialized, "match3Coefficient", match3Coefficient);
            Set(serialized, "match4Coefficient", match4Coefficient);
            Set(serialized, "match5Coefficient", match5Coefficient);
            Set(serialized, "match3WaterAmount", match3WaterAmount);
            Set(serialized, "match4WaterAmount", match4WaterAmount);
            Set(serialized, "match5WaterAmount", match5WaterAmount);
            Set(serialized, "passiveWaterDamageIncreaseRate", passiveRate);
            Set(serialized, "enhancedPassiveWaterDamageIncreaseRate", enhancedPassiveRate);
            Set(serialized, "match5WaterDamageIncreaseRate", match5Rate);
            Set(serialized, "match5EffectDurationTurns", match5Duration);
            Set(serialized, "enhancedMatch3Coefficient", enhancedMatch3);
            Set(serialized, "enhancedMatch4Coefficient", enhancedMatch4);
            Set(serialized, "activeCooldownTurns", activeCooldown);
            Set(serialized, "activeBlockConversionCount", activeConversionCount);
            Set(serialized, "enhancedActiveBlockConversionCount", enhancedActiveConversionCount);
            Set(serialized, "awakening6ExtraWaterAmount", awakening6ExtraWaterAmount);
            Set(serialized, "waterElementMaxAmount", waterMaximum);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        public static CharacterDefinition Definition(
            ShimotsukiMiyukiCombatConfig config)
        {
            var definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                ShimotsukiMiyukiRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Water;
            serialized.FindProperty("level1Hp").intValue = 1100;
            serialized.FindProperty("level1Attack").intValue = 120;
            serialized.FindProperty("level100Hp").intValue = 4300;
            serialized.FindProperty("level100Attack").intValue = 650;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void Set(
            SerializedObject serialized,
            string property,
            double value)
        {
            serialized.FindProperty(property).doubleValue = value;
        }

        private static void Set(
            SerializedObject serialized,
            string property,
            int value)
        {
            serialized.FindProperty(property).intValue = value;
        }
    }
}
