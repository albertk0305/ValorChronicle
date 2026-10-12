using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Gacha
{
    public sealed class GachaPullResult
    {
        internal GachaPullResult(
            int index,
            string gachaId,
            GachaRewardKind rewardKind,
            string rewardId,
            long amount,
            bool wasGuaranteedByPity,
            int pityCountBefore,
            int pityCountAfter)
        {
            Index = index;
            GachaId = gachaId;
            RewardKind = rewardKind;
            RewardId = rewardId;
            Amount = amount;
            WasGuaranteedByPity = wasGuaranteedByPity;
            PityCountBefore = pityCountBefore;
            PityCountAfter = pityCountAfter;
        }

        public int Index { get; }
        public string GachaId { get; }
        public GachaRewardKind RewardKind { get; }
        public string RewardId { get; }
        public long Amount { get; }
        public bool WasGuaranteedByPity { get; }
        public int PityCountBefore { get; }
        public int PityCountAfter { get; }
    }

    public sealed class GachaCalculationResult
    {
        private readonly ReadOnlyCollection<GachaPullResult> pulls;
        private readonly string gachaId;
        private readonly int pityCount;
        private readonly long totalPullCount;

        internal GachaCalculationResult(
            GachaPullResult[] pulls,
            string gachaId,
            int pityCount,
            long totalPullCount)
        {
            if (pulls == null)
            {
                throw new ArgumentNullException(nameof(pulls));
            }

            var copy = (GachaPullResult[])pulls.Clone();
            this.pulls = Array.AsReadOnly(copy);
            this.gachaId = gachaId;
            this.pityCount = pityCount;
            this.totalPullCount = totalPullCount;
        }

        public IReadOnlyList<GachaPullResult> Pulls => pulls;

        public GachaStateSaveData ResultingState =>
            new GachaStateSaveData
            {
                GachaId = gachaId,
                PityCount = pityCount,
                TotalPullCount = totalPullCount
            };
    }
}
