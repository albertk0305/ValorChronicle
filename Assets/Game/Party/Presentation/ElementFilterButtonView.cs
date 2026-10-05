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

        private bool buttonSubscribed;

        private void OnEnable()
        {
            SubscribeButton();
        }

        private void OnDisable()
        {
            UnsubscribeButton();
        }

        public void Configure(
            ElementType configuredElement,
            Button configuredButton,
            Image configuredSelectionTarget)
        {
            UnsubscribeButton();
            element = configuredElement;
            button = configuredButton;
            selectionTarget = configuredSelectionTarget;
            if (isActiveAndEnabled)
            {
                SubscribeButton();
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
