using System;
using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [Serializable]
    public sealed class BossVisualStateEntry
    {
        [SerializeField]
        private string visualStateId;

        [SerializeField]
        private Sprite sprite;

        public string VisualStateId => visualStateId;
        public Sprite Sprite => sprite;
    }
}
