using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Battle.Flow.Presentation
{
    [Serializable]
    public sealed class BattleBossIntentSlotView
    {
        [SerializeField]
        private GameObject root = null;

        [SerializeField]
        private Image actionImage = null;

        [SerializeField]
        private TMP_Text remainingTurnText = null;

        [NonSerialized]
        private Sprite defaultActionSprite;

        [NonSerialized]
        private bool defaultActionSpriteCached;

        public GameObject Root => root;
        public Image ActionImage => actionImage;
        public TMP_Text RemainingTurnText => remainingTurnText;
        public bool IsConfigured =>
            root != null && actionImage != null
            && remainingTurnText != null;

        public void SetVisible(bool visible)
        {
            CacheDefaultActionSprite();
            if (root != null)
            {
                root.SetActive(visible);
            }

            if (!visible && remainingTurnText != null)
            {
                remainingTurnText.text = string.Empty;
            }
        }

        public void Render(Sprite actionSprite, int turnsUntilAction)
        {
            if (turnsUntilAction <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnsUntilAction),
                    turnsUntilAction,
                    "Turns until action must be positive.");
            }

            CacheDefaultActionSprite();
            if (actionImage != null)
            {
                actionImage.sprite = actionSprite != null
                    ? actionSprite
                    : defaultActionSprite;
            }

            if (remainingTurnText != null)
            {
                remainingTurnText.text = turnsUntilAction.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            if (root != null)
            {
                root.SetActive(true);
            }
        }

        private void CacheDefaultActionSprite()
        {
            if (defaultActionSpriteCached)
            {
                return;
            }

            defaultActionSpriteCached = true;
            defaultActionSprite = actionImage != null
                ? actionImage.sprite
                : null;
        }
    }
}
