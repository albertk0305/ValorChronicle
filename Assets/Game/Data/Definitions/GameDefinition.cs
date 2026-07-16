using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    public abstract class GameDefinition : ScriptableObject
    {
        [SerializeField]
        private string id;

        public string Id => id;
    }
}
