using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Logging;
using ValorChronicle.Party.Roster;

namespace ValorChronicle.Party.Presentation
{
    public sealed class CharacterRosterCellView : MonoBehaviour
    {
        [SerializeField] private Button button = null;
        [SerializeField] private Image characterImage = null;
        [SerializeField] private Image typeImage = null;
        [SerializeField] private GameObject selectedBorder = null;
        [SerializeField] private TMP_Text levelText = null;
        [SerializeField] private TMP_Text awakeningText = null;
        [SerializeField] private bool alwaysShowBorder;

        private bool buttonSubscribed;

        public event Action<string> Clicked;

        public string CharacterId { get; private set; } = string.Empty;
        public bool IsBound { get; private set; }
        public bool IsSelected { get; private set; }
        public Button Button => button;
        public Image CharacterImage => characterImage;
        public Image TypeImage => typeImage;
        public GameObject SelectedBorder => selectedBorder;
        public TMP_Text LevelText => levelText;
        public TMP_Text AwakeningText => awakeningText;
        public bool AlwaysShowBorder => alwaysShowBorder;
        public bool IsConfigured =>
            button != null
            && characterImage != null
            && typeImage != null
            && selectedBorder != null;

        private void OnEnable()
        {
            SubscribeButton();
        }

        private void OnDisable()
        {
            UnsubscribeButton();
        }

        public void Configure(
            Button configuredButton,
            Image configuredCharacterImage,
            Image configuredTypeImage,
            GameObject configuredSelectedBorder,
            TMP_Text configuredLevelText = null,
            TMP_Text configuredAwakeningText = null,
            bool configuredAlwaysShowBorder = false)
        {
            UnsubscribeButton();
            button = configuredButton;
            characterImage = configuredCharacterImage;
            typeImage = configuredTypeImage;
            selectedBorder = configuredSelectedBorder;
            levelText = configuredLevelText;
            awakeningText = configuredAwakeningText;
            alwaysShowBorder = configuredAlwaysShowBorder;
            if (isActiveAndEnabled)
            {
                SubscribeButton();
            }
        }

        public void Bind(
            CharacterRosterEntry entry,
            Sprite faceSprite,
            Sprite elementIcon,
            bool selected)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    $"Character roster cell '{name}' is not configured.");
            }

            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            gameObject.SetActive(true);
            CharacterId = entry.CharacterId;
            IsBound = true;
            button.interactable = true;
            SetSprite(characterImage, faceSprite);
            SetSprite(typeImage, elementIcon);
            if (levelText != null)
            {
                levelText.text = entry.Level.ToString(
                    CultureInfo.InvariantCulture);
            }

            if (awakeningText != null)
            {
                awakeningText.text = entry.Awakening.ToString(
                    CultureInfo.InvariantCulture);
            }

            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = IsBound && selected;
            if (selectedBorder != null)
            {
                selectedBorder.SetActive(
                    IsBound && (alwaysShowBorder || IsSelected));
            }
        }

        public void SetAlwaysShowBorder(bool visible)
        {
            alwaysShowBorder = visible;
            SetSelected(IsSelected);
        }

        public void HideAndClear()
        {
            CharacterId = string.Empty;
            IsBound = false;
            IsSelected = false;
            if (button != null)
            {
                button.interactable = false;
            }

            if (characterImage != null)
            {
                SetSprite(characterImage, null);
            }

            if (typeImage != null)
            {
                SetSprite(typeImage, null);
            }

            if (selectedBorder != null)
            {
                selectedBorder.SetActive(false);
            }

            if (levelText != null)
            {
                levelText.text = string.Empty;
            }

            if (awakeningText != null)
            {
                awakeningText.text = string.Empty;
            }

            gameObject.SetActive(false);
        }

        private void SubscribeButton()
        {
            if (!buttonSubscribed && button != null)
            {
                button.onClick.AddListener(HandleClick);
                buttonSubscribed = true;
            }
        }

        private void UnsubscribeButton()
        {
            if (buttonSubscribed && button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }

            buttonSubscribed = false;
        }

        private void HandleClick()
        {
            if (IsBound && button != null && button.interactable)
            {
                Clicked?.Invoke(CharacterId);
            }
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            Color color = image.color;
            color.a = sprite == null ? 0f : 1f;
            image.color = color;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!IsConfigured)
            {
                GameLogger.Warning(
                    $"[CharacterRosterCellView] '{name}' is incomplete.",
                    this);
            }
        }
    }
}
