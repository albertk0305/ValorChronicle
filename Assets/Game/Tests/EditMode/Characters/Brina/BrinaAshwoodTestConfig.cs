using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Brina;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    internal static class BrinaAshwoodTestConfig
    {
        public static BrinaAshwoodCombatConfig Create()
        {
            return ScriptableObject.CreateInstance<
                BrinaAshwoodCombatConfig>();
        }

        public static void SetDouble(
            BrinaAshwoodCombatConfig config,
            string propertyName,
            double value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(propertyName).doubleValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(
            BrinaAshwoodCombatConfig config,
            string propertyName,
            int value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
