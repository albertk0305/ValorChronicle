using UnityEngine;

namespace ValorChronicle.Main.Presentation
{
    public sealed class BossSelectionItem
    {
        public BossSelectionItem(
            string bossId,
            string displayName,
            Sprite faceSprite,
            Sprite elementSprite,
            string bestScoreText,
            Sprite bestGradeSprite)
        {
            BossId = bossId;
            DisplayName = displayName;
            FaceSprite = faceSprite;
            ElementSprite = elementSprite;
            BestScoreText = bestScoreText;
            BestGradeSprite = bestGradeSprite;
        }

        public string BossId { get; }
        public string DisplayName { get; }
        public Sprite FaceSprite { get; }
        public Sprite ElementSprite { get; }
        public string BestScoreText { get; }
        public Sprite BestGradeSprite { get; }
    }
}
