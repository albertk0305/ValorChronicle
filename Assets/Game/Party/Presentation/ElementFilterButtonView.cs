using System;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Party.Presentation
{
    public sealed class ElementFilterButtonView : MonoBehaviour
    {
        [SerializeField] private ElementType element;
        [SerializeField] private Button button = null;
        [SerializeField] private Image selectionTarget = null;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor =
            new Color(0.55f, 0.55f, 0.55f, 1f);

        public event Action<ElementType> Clicked;

        public ElementType Element => element;
        public Button Button => button;
        public bool IsSelected { get; private set; }
        public bool IsConfigured => button != null && selectionTarget != null;

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

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (button != null)
            {
                button.interactable = true;
            }

            if (selectionTarget != null)
            {
                selectionTarget.color = selected
                    ? selectedColor
                    : normalColor;
            }
        }

        private void HandleClick()
        {
            if (button != null && button.interactable)
            {
                Clicked?.Invoke(element);
            }
        }

        private void OnValidate()
        {
            if (!IsConfigured)
            {
                GameLogger.Warning(
                    $"[ElementFilterButtonView] '{name}' is incomplete.",
                    this);
            }
        }
    }
}
