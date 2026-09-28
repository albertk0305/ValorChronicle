using System;
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

        public event Action<string> Clicked;

        public string CharacterId { get; private set; } = string.Empty;
        public bool IsBound { get; private set; }
        public bool IsSelected { get; private set; }
        public Button Button => button;
        public Image CharacterImage => characterImage;
        public Image TypeImage => typeImage;
        public GameObject SelectedBorder => selectedBorder;
        public bool IsConfigured =>
            button != null
            && characterImage != null
            && typeImage != null
            && selectedBorder != null;

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
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
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = IsBound && selected;
            if (selectedBorder != null)
            {
                selectedBorder.SetActive(IsSelected);
            }
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

            gameObject.SetActive(false);
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
            if (!IsConfigured)
            {
                GameLogger.Warning(
                    $"[CharacterRosterCellView] '{name}' is incomplete.",
                    this);
            }
        }
    }
}
