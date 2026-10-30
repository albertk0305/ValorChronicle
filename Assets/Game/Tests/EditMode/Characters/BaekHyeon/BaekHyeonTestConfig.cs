using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    internal static class BaekHyeonTestConfig
    {
        public static ValorChronicle.Characters.BaekHyeon.
            BaekHyeonCombatConfig Create()
        {
            return ScriptableObject.CreateInstance<
                ValorChronicle.Characters.BaekHyeon.
                    BaekHyeonCombatConfig>();
        }

        public static void SetField(
            ValorChronicle.Characters.BaekHyeon.
                BaekHyeonCombatConfig config,
            string fieldName,
            object value)
        {
            FieldInfo field = typeof(
                ValorChronicle.Characters.BaekHyeon.
                    BaekHyeonCombatConfig).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(config, value);
        }
    }
}
