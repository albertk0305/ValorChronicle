using System;
using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [Serializable]
    public sealed class BossDifficultyStats
    {
        [SerializeField]
        private string difficultyId;

        [SerializeField]
        private long maxHp;

        [SerializeField]
        private double attack;

        public BossDifficultyStats(
            string difficultyId,
            long maxHp,
            double attack)
        {
            this.difficultyId = difficultyId;
            this.maxHp = maxHp;
            this.attack = attack;
        }

        public string DifficultyId => difficultyId;
        public long MaxHp => maxHp;
        public double Attack => attack;
    }
}
