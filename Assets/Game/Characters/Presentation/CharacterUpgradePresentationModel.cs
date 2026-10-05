using UnityEngine;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterUpgradePresentationModel
    {
        public CharacterUpgradePresentationModel(
            string characterId,
            string displayName,
            Sprite fullArtSprite,
            Sprite elementIcon,
            int level,
            int awakening,
            long maxHp,
            long attack,
            long battleRecords,
            long nextLevelCost,
            bool showLevelUpCost,
            bool canLevelUp)
        {
            CharacterId = characterId;
            DisplayName = displayName;
            FullArtSprite = fullArtSprite;
            ElementIcon = elementIcon;
            Level = level;
            Awakening = awakening;
            MaxHp = maxHp;
            Attack = attack;
            BattleRecords = battleRecords;
            NextLevelCost = nextLevelCost;
            ShowLevelUpCost = showLevelUpCost;
            CanLevelUp = canLevelUp;
        }

        public string CharacterId { get; }
        public string DisplayName { get; }
        public Sprite FullArtSprite { get; }
        public Sprite ElementIcon { get; }
        public int Level { get; }
        public int Awakening { get; }
        public long MaxHp { get; }
        public long Attack { get; }
        public long BattleRecords { get; }
        public long NextLevelCost { get; }
        public bool ShowLevelUpCost { get; }
        public bool CanLevelUp { get; }
    }
}
