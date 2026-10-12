using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    public enum GachaKind
    {
        Character = 0,
        Relic = 1
    }

    public enum GachaRewardKind
    {
        RandomCharacter = 0,
        RandomRelic = 1,
        HeroTokens = 2,
        RelicTokens = 3,
        BattleRecords = 4
    }

    [Serializable]
    public sealed class GachaRewardEntry
    {
        [SerializeField]
        private GachaRewardKind rewardKind;

        [SerializeField]
        private long quantity;

        [SerializeField]
        private int weight;

        public GachaRewardKind RewardKind => rewardKind;
        public long Quantity => quantity;
        public int Weight => weight;
    }

    [CreateAssetMenu(
        fileName = "GachaDefinition",
        menuName = "Valor Chronicle/Definitions/Gacha")]
    public sealed class GachaDefinition : GameDefinition
    {
        public const int RequiredTotalWeight = 1000;

        [SerializeField]
        private GachaKind gachaKind;

        [SerializeField]
        private long singlePullCost;

        [SerializeField]
        private long tenPullCost;

        [SerializeField]
        private int pityPullCount;

        [SerializeField]
        private GachaRewardEntry[] rewardEntries =
            Array.Empty<GachaRewardEntry>();

        [SerializeField]
        private string[] characterPoolIds = Array.Empty<string>();

        [SerializeField]
        private string[] relicPoolIds = Array.Empty<string>();

        public GachaKind GachaKind => gachaKind;
        public long SinglePullCost => singlePullCost;
        public long TenPullCost => tenPullCost;
        public int PityPullCount => pityPullCount;
        public IReadOnlyList<GachaRewardEntry> RewardEntries =>
            Array.AsReadOnly(
                rewardEntries ?? Array.Empty<GachaRewardEntry>());
        public IReadOnlyList<string> CharacterPoolIds =>
            Array.AsReadOnly(
                characterPoolIds ?? Array.Empty<string>());
        public IReadOnlyList<string> RelicPoolIds =>
            Array.AsReadOnly(relicPoolIds ?? Array.Empty<string>());
    }
}
