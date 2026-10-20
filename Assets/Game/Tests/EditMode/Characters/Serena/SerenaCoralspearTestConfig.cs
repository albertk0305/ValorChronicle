using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Serena;

namespace ValorChronicle.Tests.EditMode.Characters.Serena
{
    internal static class SerenaCoralspearTestConfig
    {
        public static SerenaCoralspearCombatConfig Create()
        {
            return ScriptableObject.CreateInstance<
                SerenaCoralspearCombatConfig>();
        }

        public static void SetDouble(
            SerenaCoralspearCombatConfig config,
            string propertyName,
            double value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(propertyName).doubleValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(
            SerenaCoralspearCombatConfig config,
            string propertyName,
            int value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
