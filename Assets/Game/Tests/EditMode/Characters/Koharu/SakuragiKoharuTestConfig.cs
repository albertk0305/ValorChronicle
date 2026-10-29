using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Koharu;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Koharu
{
    internal static class SakuragiKoharuTestConfig
    {
        public static SakuragiKoharuCombatConfig Create(
            double match3Coefficient = 0.50d,
            double match4Coefficient = 0.80d,
            double match5Coefficient = 1.30d,
            int match3SpiritGain = 1,
            int match4SpiritGain = 2,
            int match5SpiritGain = 3,
            double match5DamageIncreaseRate = 0.12d,
            int match5DurationTurns = 2,
            double awakening3Match3Coefficient = 0.55d,
            double awakening3Match4Coefficient = 0.90d,
            double awakening4Match5Coefficient = 1.45d,
            double awakening4Match5DamageIncreaseRate = 0.15d,
            int spiritCost = 6,
            int awakening5SpiritCost = 5,
            double awakening6DamageIncreaseRate = 0.10d,
            int awakening6DurationTurns = 1,
            int activeCooldownTurns = 4,
            int activeBlockConversionCount = 2)
        {
            var config = ScriptableObject.CreateInstance<
                SakuragiKoharuCombatConfig>();
            var serialized = new SerializedObject(config);
            Set(serialized, "match3Coefficient", match3Coefficient);
            Set(serialized, "match4Coefficient", match4Coefficient);
            Set(serialized, "match5Coefficient", match5Coefficient);
            Set(serialized, "match3SpiritEnergyGain", match3SpiritGain);
            Set(serialized, "match4SpiritEnergyGain", match4SpiritGain);
            Set(serialized, "match5SpiritEnergyGain", match5SpiritGain);
            Set(serialized,
                "match5AdditionalAndChaseDamageIncreaseRate",
                match5DamageIncreaseRate);
            Set(serialized,
                "match5EffectDurationTurns",
                match5DurationTurns);
            Set(serialized,
                "awakening3Match3Coefficient",
                awakening3Match3Coefficient);
            Set(serialized,
                "awakening3Match4Coefficient",
                awakening3Match4Coefficient);
            Set(serialized,
                "awakening4Match5Coefficient",
                awakening4Match5Coefficient);
            Set(serialized,
                "awakening4Match5AdditionalAndChaseDamageIncreaseRate",
                awakening4Match5DamageIncreaseRate);
            Set(serialized,
                "spiritEnergyCostPerCooldownReduction",
                spiritCost);
            Set(serialized,
                "awakening5SpiritEnergyCostPerCooldownReduction",
                awakening5SpiritCost);
            Set(serialized,
                "awakening6AdditionalAndChaseDamageIncreaseRate",
                awakening6DamageIncreaseRate);
            Set(serialized,
                "awakening6EffectDurationTurns",
                awakening6DurationTurns);
            Set(serialized, "activeCooldownTurns", activeCooldownTurns);
            Set(serialized,
                "activeBlockConversionCount",
                activeBlockConversionCount);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        public static CharacterDefinition Definition(
            SakuragiKoharuCombatConfig config)
        {
            var definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                SakuragiKoharuRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Grass;
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
            string propertyName,
            double value)
        {
            serialized.FindProperty(propertyName).doubleValue = value;
        }

        private static void Set(
            SerializedObject serialized,
            string propertyName,
            int value)
        {
            serialized.FindProperty(propertyName).intValue = value;
        }
    }
}
