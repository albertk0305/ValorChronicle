using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Naira;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    internal static class NairaWindhornTestConfig
    {
        private const string AssetPath =
            "Assets/Data/CombatConfigs/Naira/"
            + "naira_windhorn_combat_config.asset";

        public static NairaWindhornCombatConfig Canonical =>
            AssetDatabase.LoadAssetAtPath<NairaWindhornCombatConfig>(
                AssetPath);

        public static NairaWindhornCombatConfig Create()
        {
            return ScriptableObject.CreateInstance<
                NairaWindhornCombatConfig>();
        }

        public static void SetDouble(
            NairaWindhornCombatConfig config,
            string propertyName,
            double value)
        {
            var serialized = new SerializedObject(config);
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            property.doubleValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(
            NairaWindhornCombatConfig config,
            string propertyName,
            int value)
        {
            var serialized = new SerializedObject(config);
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(
            NairaWindhornCombatConfig config,
            string propertyName,
            bool value)
        {
            var serialized = new SerializedObject(config);
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

