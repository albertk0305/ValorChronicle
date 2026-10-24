using UnityEngine;

namespace ValorChronicle.Tests.EditMode.Characters.Fiora
{
    internal static class FioraAmberwingTestConfig
    {
        public static ValorChronicle.Characters.Fiora.FioraAmberwingCombatConfig Create()
        {
            return ScriptableObject.CreateInstance<
                ValorChronicle.Characters.Fiora.FioraAmberwingCombatConfig>();
        }
    }
}
