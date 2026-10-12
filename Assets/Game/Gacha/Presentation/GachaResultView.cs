using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Gacha.Presentation
{
    public enum GachaResultDisplayKind
    {
        Character = 0,
        Relic = 1,
        Item = 2
    }

    public sealed class GachaResultDisplayEntry
    {
        public GachaResultDisplayEntry(
            int index,
            GachaResultDisplayKind kind,
            Sprite primarySprite,
            long itemAmount,
            Sprite awakeningSprite)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (!Enum.IsDefined(typeof(GachaResultDisplayKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            PrimarySprite = primarySprite
                ?? throw new ArgumentNullException(nameof(primarySprite));
            if (kind == GachaResultDisplayKind.Item && itemAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemAmount));
            }

            Index = index;
            Kind = kind;
            ItemAmount = itemAmount;
            AwakeningSprite = awakeningSprite;
        }

        public int Index { get; }
        public GachaResultDisplayKind Kind { get; }
        public Sprite PrimarySprite { get; }
        public long ItemAmount { get; }
        public Sprite AwakeningSprite { get; }
    }

    public sealed class GachaResultSlotView : MonoBehaviour
    {
        private GameObject contentRoot;
        private GameObject itemRoot;
        private Image contentImage;
        private Image itemImage;
        private TMP_Text itemText;
        private Image awakeningImage;

        public GameObject ContentRoot => contentRoot;
        public GameObject ItemRoot => itemRoot;
        public Image ContentImage => contentImage;
        public Image ItemImage => itemImage;
        public TMP_Text ItemText => itemText;
        public Image AwakeningImage => awakeningImage;
        public bool IsConfigured =>
            contentRoot != null
            && itemRoot != null
            && contentImage != null
            && itemImage != null
            && itemText != null
            && awakeningImage != null;

        public void Configure(
            GameObject configuredContentRoot,
            GameObject configuredItemRoot,
            Image configuredContentImage,
            Image configuredItemImage,
            TMP_Text configuredItemText,
            Image configuredAwakeningImage)
        {
            contentRoot = configuredContentRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredContentRoot));
            itemRoot = configuredItemRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredItemRoot));
            contentImage = configuredContentImage
                ?? throw new ArgumentNullException(
                    nameof(configuredContentImage));
            itemImage = configuredItemImage
                ?? throw new ArgumentNullException(
                    nameof(configuredItemImage));
            itemText = configuredItemText
                ?? throw new ArgumentNullException(
                    nameof(configuredItemText));
            awakeningImage = configuredAwakeningImage
                ?? throw new ArgumentNullException(
                    nameof(configuredAwakeningImage));
            Clear();
        }

        public void Bind(GachaResultDisplayEntry entry)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "GachaResultSlotView must be configured before use.");
            }

            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            ClearVisuals();
            if (entry.Kind == GachaResultDisplayKind.Item)
            {
                itemImage.sprite = entry.PrimarySprite;
                itemImage.preserveAspect = true;
                itemText.text = "+" + entry.ItemAmount.ToString(
                    "N0",
                    CultureInfo.InvariantCulture);
                itemRoot.SetActive(true);
            }
            else
            {
                contentImage.sprite = entry.PrimarySprite;
                contentImage.preserveAspect = true;
                contentRoot.SetActive(true);
            }

            if (entry.AwakeningSprite != null)
            {
                awakeningImage.sprite = entry.AwakeningSprite;
                awakeningImage.preserveAspect = true;
                awakeningImage.gameObject.SetActive(true);
            }

            gameObject.SetActive(true);
        }

        public void Clear()
        {
            ClearVisuals();
            gameObject.SetActive(false);
        }

        private void ClearVisuals()
        {
            if (contentImage != null)
            {
                contentImage.sprite = null;
            }

            if (itemImage != null)
            {
                itemImage.sprite = null;
            }

            if (itemText != null)
            {
                itemText.text = string.Empty;
            }

            if (awakeningImage != null)
            {
                awakeningImage.sprite = null;
                awakeningImage.gameObject.SetActive(false);
            }

            contentRoot?.SetActive(false);
            itemRoot?.SetActive(false);
        }
    }

    public sealed class GachaResultView : MonoBehaviour
    {
        public const int MaximumResultCount = 10;

        private GameObject presentationRoot;
        private GameObject characterRoot;
        private GameObject artifactOrItemRoot;
        private GameObject resultRoot;
        private GameObject curtainRoot;
        private GameObject skipButtonRoot;
        private Button touchButton;
        private GachaResultSlotView[] slots =
            Array.Empty<GachaResultSlotView>();
        private bool subscribed;
        private bool closeInputEnabled = true;

        public event Action CloseRequested;

        public GameObject PresentationRoot => presentationRoot;
        public GameObject ResultRoot => resultRoot;
        public IReadOnlyList<GachaResultSlotView> Slots => slots;
        public bool IsOpen =>
            presentationRoot != null && presentationRoot.activeSelf;
        public bool IsConfigured =>
            presentationRoot != null
            && characterRoot != null
            && artifactOrItemRoot != null
            && resultRoot != null
            && curtainRoot != null
            && skipButtonRoot != null
            && touchButton != null
            && slots.Length == MaximumResultCount
            && Array.TrueForAll(slots, slot =>
                slot != null && slot.IsConfigured);
        public bool CloseInputEnabled => closeInputEnabled;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            GameObject configuredPresentationRoot,
            GameObject configuredCharacterRoot,
            GameObject configuredArtifactOrItemRoot,
            GameObject configuredResultRoot,
            GameObject configuredCurtainRoot,
            GameObject configuredSkipButtonRoot,
            Button configuredTouchButton,
            GachaResultSlotView[] configuredSlots)
        {
            Unsubscribe();
            presentationRoot = configuredPresentationRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredPresentationRoot));
            characterRoot = configuredCharacterRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredCharacterRoot));
            artifactOrItemRoot = configuredArtifactOrItemRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredArtifactOrItemRoot));
            resultRoot = configuredResultRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredResultRoot));
            curtainRoot = configuredCurtainRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredCurtainRoot));
            skipButtonRoot = configuredSkipButtonRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredSkipButtonRoot));
            touchButton = configuredTouchButton
                ?? throw new ArgumentNullException(
                    nameof(configuredTouchButton));
            if (configuredSlots == null
                || configuredSlots.Length != MaximumResultCount
                || Array.Exists(configuredSlots, slot => slot == null))
            {
                throw new ArgumentException(
                    "Exactly ten Gacha result slots are required.",
                    nameof(configuredSlots));
            }

            slots = (GachaResultSlotView[])configuredSlots.Clone();
            closeInputEnabled = true;
            Close();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Show(IReadOnlyList<GachaResultDisplayEntry> entries)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "GachaResultView must be configured before use.");
            }

            if (entries == null
                || (entries.Count != 1
                    && entries.Count != MaximumResultCount))
            {
                throw new ArgumentException(
                    "Gacha results must contain one or ten entries.",
                    nameof(entries));
            }

            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index] == null
                    || entries[index].Index != index)
                {
                    throw new ArgumentException(
                        "Gacha result indices must be contiguous and ordered.",
                        nameof(entries));
                }
            }

            ResetSlots();
            characterRoot.SetActive(false);
            artifactOrItemRoot.SetActive(false);
            curtainRoot.SetActive(false);
            skipButtonRoot.SetActive(false);
            resultRoot.SetActive(true);
            for (int index = 0; index < entries.Count; index++)
            {
                slots[index].Bind(entries[index]);
            }

            presentationRoot.SetActive(true);
            touchButton.gameObject.SetActive(true);
        }

        public void Close()
        {
            ResetSlots();
            if (resultRoot != null)
            {
                resultRoot.SetActive(false);
            }

            if (characterRoot != null)
            {
                characterRoot.SetActive(false);
            }

            if (artifactOrItemRoot != null)
            {
                artifactOrItemRoot.SetActive(false);
            }

            if (curtainRoot != null)
            {
                curtainRoot.SetActive(false);
            }

            if (skipButtonRoot != null)
            {
                skipButtonRoot.SetActive(false);
            }

            if (presentationRoot != null)
            {
                presentationRoot.SetActive(false);
            }
        }

        public void SetCloseInputEnabled(bool enabled)
        {
            if (closeInputEnabled == enabled)
            {
                return;
            }

            Unsubscribe();
            closeInputEnabled = enabled;
            if (enabled && isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void ResetSlots()
        {
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index]?.Clear();
            }
        }

        private void Subscribe()
        {
            if (subscribed || !IsConfigured || !closeInputEnabled)
            {
                return;
            }

            touchButton.onClick.AddListener(HandleCloseRequested);
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            touchButton.onClick.RemoveListener(HandleCloseRequested);
            subscribed = false;
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }
    }
}
