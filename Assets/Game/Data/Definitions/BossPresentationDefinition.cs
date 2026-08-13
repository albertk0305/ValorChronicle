using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "BossPresentationDefinition",
        menuName = "Valor Chronicle/Definitions/Boss Presentation")]
    public sealed class BossPresentationDefinition : ScriptableObject
    {
        [SerializeField]
        private string bossId;

        [SerializeField]
        private Sprite defaultSprite;

        [SerializeField]
        private BossVisualStateEntry[] stateVisuals =
            Array.Empty<BossVisualStateEntry>();

        public string BossId => bossId;
        public Sprite DefaultSprite => defaultSprite;
        public IReadOnlyList<BossVisualStateEntry> StateVisuals =>
            Array.AsReadOnly(
                stateVisuals ?? Array.Empty<BossVisualStateEntry>());

        public bool TryGetStateSprite(
            string visualStateId,
            out Sprite sprite)
        {
            if (string.IsNullOrEmpty(visualStateId))
            {
                sprite = null;
                return false;
            }

            IReadOnlyList<BossVisualStateEntry> visuals = StateVisuals;
            for (int index = 0; index < visuals.Count; index++)
            {
                BossVisualStateEntry entry = visuals[index];
                if (entry != null
                    && string.Equals(
                        entry.VisualStateId,
                        visualStateId,
                        StringComparison.Ordinal))
                {
                    sprite = entry.Sprite;
                    return sprite != null;
                }
            }

            sprite = null;
            return false;
        }
    }
}
