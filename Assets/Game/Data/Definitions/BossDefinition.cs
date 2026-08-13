using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "BossDefinition",
        menuName = "Valor Chronicle/Definitions/Boss")]
    public sealed class BossDefinition : GameDefinition
    {
        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private ElementType element;

        [SerializeField]
        private int turnLimit;

        [SerializeField]
        private string[] actionOrSkillIds = Array.Empty<string>();

        [SerializeField]
        private BossDifficultyStats[] difficultyStats =
            Array.Empty<BossDifficultyStats>();

        [SerializeField]
        private CombatConfigDefinition combatConfig;

        public string DisplayNameKey => displayNameKey;
        public ElementType Element => element;
        public int TurnLimit => turnLimit;
        public IReadOnlyList<string> ActionOrSkillIds =>
            Array.AsReadOnly(actionOrSkillIds ?? Array.Empty<string>());
        public IReadOnlyList<BossDifficultyStats> DifficultyStats =>
            Array.AsReadOnly(
                difficultyStats ?? Array.Empty<BossDifficultyStats>());
        public CombatConfigDefinition CombatConfig => combatConfig;

        public bool TryGetDifficultyStats(
            string difficultyId,
            out BossDifficultyStats stats)
        {
            if (string.IsNullOrWhiteSpace(difficultyId))
            {
                stats = null;
                return false;
            }

            IReadOnlyList<BossDifficultyStats> availableStats =
                DifficultyStats;
            for (int index = 0; index < availableStats.Count; index++)
            {
                BossDifficultyStats candidate = availableStats[index];
                if (candidate != null
                    && string.Equals(
                        candidate.DifficultyId,
                        difficultyId,
                        StringComparison.Ordinal))
                {
                    stats = candidate;
                    return true;
                }
            }

            stats = null;
            return false;
        }
    }
}
