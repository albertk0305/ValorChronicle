using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ValorChronicle.Battle.Flow.Presentation
{
    [Serializable]
    public sealed class BattleCharacterSlotView
    {
        [SerializeField]
        private GameObject root = null;

        [SerializeField]
        private Button activeButton = null;

        [SerializeField]
        private Image characterImage = null;

        [SerializeField]
        private Image typeImage = null;

        [SerializeField]
        private TMP_Text cooldownText = null;

        [SerializeField]
        private Image cooldownOverlay = null;

        [SerializeField]
        private BattleStatusIconView[] statusSlots =
            Array.Empty<BattleStatusIconView>();

        [NonSerialized]
        private UnityAction activeListener;

        [NonSerialized]
        private string characterId;

        public GameObject Root => root;
        public Button ActiveButton => activeButton;
        public Image CharacterImage => characterImage;
        public Image TypeImage => typeImage;
        public TMP_Text CooldownText => cooldownText;
        public Image CooldownOverlay => cooldownOverlay;
        public string CharacterId => characterId;
        public int StatusSlotCount => statusSlots?.Length ?? 0;
        public bool HasActiveListener => activeListener != null;
        public bool IsConfigured =>
            root != null
            && activeButton != null
            && characterImage != null
            && typeImage != null
            && cooldownText != null
            && cooldownOverlay != null;

        public BattleStatusIconView GetStatusSlot(int index)
        {
            return statusSlots[index];
        }

        public void SetActiveListener(UnityAction listener)
        {
            ClearActiveListener();
            if (activeButton == null || listener == null)
            {
                return;
            }

            activeButton.onClick.AddListener(listener);
            activeListener = listener;
        }

        public void ClearActiveListener()
        {
            if (activeButton != null && activeListener != null)
            {
                activeButton.onClick.RemoveListener(activeListener);
            }

            activeListener = null;
        }

        public void SetCharacterAvailable(bool available)
        {
            if (activeButton != null)
            {
                activeButton.transition = Selectable.Transition.None;
                activeButton.interactable = available;
            }
        }

        public void RenderEmpty()
        {
            characterId = null;
            SetCharacterAvailable(false);
            if (characterImage != null)
            {
                characterImage.enabled = false;
            }

            if (typeImage != null)
            {
                typeImage.sprite = null;
                typeImage.gameObject.SetActive(false);
            }

            SetCooldown(0);
            HideStatusSlots();
        }

        public void RenderCharacter(
            string runtimeCharacterId,
            Sprite elementSprite,
            int remainingCooldown,
            bool activeAvailable)
        {
            characterId = runtimeCharacterId;
            if (characterImage != null)
            {
                characterImage.enabled = true;
            }

            if (typeImage != null)
            {
                typeImage.sprite = elementSprite;
                typeImage.gameObject.SetActive(elementSprite != null);
            }

            SetCooldown(remainingCooldown);
            SetCharacterAvailable(activeAvailable);
        }

        public void InitializeSafeDisplay()
        {
            RenderEmpty();
        }

        public void HideStatusSlots()
        {
            if (statusSlots == null)
            {
                return;
            }

            for (int index = 0; index < statusSlots.Length; index++)
            {
                statusSlots[index]?.SetVisible(false);
            }
        }

        private void SetCooldown(int remainingCooldown)
        {
            bool visible = remainingCooldown > 0;
            if (cooldownOverlay != null)
            {
                cooldownOverlay.raycastTarget = false;
                cooldownOverlay.gameObject.SetActive(visible);
            }

            if (cooldownText == null)
            {
                return;
            }

            cooldownText.text = visible
                ? remainingCooldown.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                : string.Empty;
            cooldownText.gameObject.SetActive(visible);
        }
    }
}
