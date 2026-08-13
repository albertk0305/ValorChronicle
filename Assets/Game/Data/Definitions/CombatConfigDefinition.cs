using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    public abstract class CombatConfigDefinition : ScriptableObject
    {
        public abstract bool TryValidate(out string errorMessage);
    }
}
