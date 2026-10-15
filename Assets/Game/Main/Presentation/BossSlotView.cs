using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ValorChronicle.Main.Presentation
{
    public sealed class BossSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image bossFaceImage = null;
        [SerializeField] private TMP_Text bossNameText = null;
        [SerializeField] private Image bossType = null;
        [SerializeField] private TMP_Text bestScoreText = null;
        [SerializeField] private Image bestGrade = null;
        [SerializeField] private Image touchPanel = null;

        private Action<string> selectionRequested;

        public string BossId { get; private set; }
        public Image BossFaceImage => bossFaceImage;
        public TMP_Text BossNameText => bossNameText;
        public Image BossType => bossType;
        public TMP_Text BestScoreText => bestScoreText;
        public Image BestGrade => bestGrade;
        public bool IsBound => !string.IsNullOrEmpty(BossId);
        public bool IsConfigured =>
            bossFaceImage != null
            && bossNameText != null
            && bossType != null
            && bestScoreText != null
            && bestGrade != null
            && touchPanel != null;

        public void Configure(
            Image configuredBossFaceImage,
            TMP_Text configuredBossNameText,
            Image configuredBossType,
            TMP_Text configuredBestScoreText,
            Image configuredBestGrade,
            Image configuredTouchPanel)
        {
            bossFaceImage = configuredBossFaceImage;
            bossNameText = configuredBossNameText;
            bossType = configuredBossType;
            bestScoreText = configuredBestScoreText;
            bestGrade = configuredBestGrade;
            touchPanel = configuredTouchPanel;
        }

        public void Bind(
            BossSelectionItem item,
            Action<string> onSelectionRequested)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            BossId = item.BossId;
            selectionRequested = onSelectionRequested;
            bossNameText.text = item.DisplayName;
            bestScoreText.text = item.BestScoreText;
            SetOptionalSprite(bossFaceImage, item.FaceSprite);
            SetOptionalSprite(bossType, item.ElementSprite);
            SetOptionalSprite(bestGrade, item.BestGradeSprite);
            touchPanel.raycastTarget = true;
            gameObject.SetActive(true);
        }

        public void Unbind()
        {
            BossId = null;
            selectionRequested = null;
            if (touchPanel != null)
            {
                touchPanel.raycastTarget = false;
            }

            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsBound
                || eventData == null
                || eventData.dragging
                || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            selectionRequested?.Invoke(BossId);
        }

        private static void SetOptionalSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.gameObject.SetActive(sprite != null);
        }
    }
}
