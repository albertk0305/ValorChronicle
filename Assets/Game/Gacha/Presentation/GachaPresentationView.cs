using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Gacha.Presentation
{
    public sealed class GachaIndividualDisplayEntry
    {
        public GachaIndividualDisplayEntry(
            int index,
            GachaResultDisplayKind kind,
            Sprite primarySprite,
            long itemAmount,
            Sprite overlaySprite,
            string overlayText)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (!Enum.IsDefined(typeof(GachaResultDisplayKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            Index = index;
            Kind = kind;
            PrimarySprite = primarySprite
                ?? throw new ArgumentNullException(nameof(primarySprite));
            if (kind == GachaResultDisplayKind.Item && itemAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemAmount));
            }

            ItemAmount = itemAmount;
            OverlaySprite = overlaySprite;
            OverlayText = overlayText ?? string.Empty;
        }

        public int Index { get; }
        public GachaResultDisplayKind Kind { get; }
        public Sprite PrimarySprite { get; }
        public long ItemAmount { get; }
        public Sprite OverlaySprite { get; }
        public string OverlayText { get; }
        public bool HasOverlay => OverlaySprite != null;
    }

    public sealed class GachaPresentationView : MonoBehaviour
    {
        private RectTransform presentationRoot;
        private GameObject characterRoot;
        private Image characterImage;
        private GameObject characterOverlayRoot;
        private Image characterOverlayImage;
        private TMP_Text characterOverlayText;
        private GameObject artifactOrItemRoot;
        private GameObject artifactRoot;
        private Image artifactImage;
        private GameObject itemRoot;
        private Image itemImage;
        private TMP_Text itemText;
        private GameObject artifactOverlayRoot;
        private Image artifactOverlayImage;
        private TMP_Text artifactOverlayText;
        private GameObject resultRoot;
        private GameObject curtainRoot;
        private RectTransform leftCurtain;
        private RectTransform rightCurtain;
        private Button touchButton;
        private Button skipButton;
        private Vector2 leftClosedPosition;
        private Vector2 rightClosedPosition;

        public bool IsConfigured =>
            presentationRoot != null
            && characterRoot != null
            && characterImage != null
            && characterOverlayRoot != null
            && characterOverlayImage != null
            && characterOverlayText != null
            && artifactOrItemRoot != null
            && artifactRoot != null
            && artifactImage != null
            && itemRoot != null
            && itemImage != null
            && itemText != null
            && artifactOverlayRoot != null
            && artifactOverlayImage != null
            && artifactOverlayText != null
            && resultRoot != null
            && curtainRoot != null
            && leftCurtain != null
            && rightCurtain != null
            && touchButton != null
            && skipButton != null;
        public GameObject PresentationRoot =>
            presentationRoot != null ? presentationRoot.gameObject : null;
        public GameObject CharacterRoot => characterRoot;
        public GameObject ArtifactOrItemRoot => artifactOrItemRoot;
        public GameObject CurtainRoot => curtainRoot;
        public Button TouchButton => touchButton;
        public Button SkipButton => skipButton;
        public Image CharacterImage => characterImage;
        public Image ArtifactImage => artifactImage;
        public TMP_Text ItemText => itemText;
        public TMP_Text CharacterOverlayText => characterOverlayText;
        public TMP_Text ArtifactOverlayText => artifactOverlayText;
        public bool IsSkipVisible =>
            skipButton != null && skipButton.gameObject.activeSelf;

        public void Configure(
            RectTransform configuredPresentationRoot,
            GameObject configuredCharacterRoot,
            Image configuredCharacterImage,
            GameObject configuredCharacterOverlayRoot,
            Image configuredCharacterOverlayImage,
            TMP_Text configuredCharacterOverlayText,
            GameObject configuredArtifactOrItemRoot,
            GameObject configuredArtifactRoot,
            Image configuredArtifactImage,
            GameObject configuredItemRoot,
            Image configuredItemImage,
            TMP_Text configuredItemText,
            GameObject configuredArtifactOverlayRoot,
            Image configuredArtifactOverlayImage,
            TMP_Text configuredArtifactOverlayText,
            GameObject configuredResultRoot,
            GameObject configuredCurtainRoot,
            RectTransform configuredLeftCurtain,
            RectTransform configuredRightCurtain,
            Button configuredTouchButton,
            Button configuredSkipButton)
        {
            presentationRoot = configuredPresentationRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredPresentationRoot));
            characterRoot = configuredCharacterRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredCharacterRoot));
            characterImage = configuredCharacterImage
                ?? throw new ArgumentNullException(
                    nameof(configuredCharacterImage));
            characterOverlayRoot = configuredCharacterOverlayRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredCharacterOverlayRoot));
            characterOverlayImage = configuredCharacterOverlayImage
                ?? throw new ArgumentNullException(
                    nameof(configuredCharacterOverlayImage));
            characterOverlayText = configuredCharacterOverlayText
                ?? throw new ArgumentNullException(
                    nameof(configuredCharacterOverlayText));
            artifactOrItemRoot = configuredArtifactOrItemRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredArtifactOrItemRoot));
            artifactRoot = configuredArtifactRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredArtifactRoot));
            artifactImage = configuredArtifactImage
                ?? throw new ArgumentNullException(
                    nameof(configuredArtifactImage));
            itemRoot = configuredItemRoot
                ?? throw new ArgumentNullException(nameof(configuredItemRoot));
            itemImage = configuredItemImage
                ?? throw new ArgumentNullException(nameof(configuredItemImage));
            itemText = configuredItemText
                ?? throw new ArgumentNullException(nameof(configuredItemText));
            artifactOverlayRoot = configuredArtifactOverlayRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredArtifactOverlayRoot));
            artifactOverlayImage = configuredArtifactOverlayImage
                ?? throw new ArgumentNullException(
                    nameof(configuredArtifactOverlayImage));
            artifactOverlayText = configuredArtifactOverlayText
                ?? throw new ArgumentNullException(
                    nameof(configuredArtifactOverlayText));
            resultRoot = configuredResultRoot
                ?? throw new ArgumentNullException(nameof(configuredResultRoot));
            curtainRoot = configuredCurtainRoot
                ?? throw new ArgumentNullException(nameof(configuredCurtainRoot));
            leftCurtain = configuredLeftCurtain
                ?? throw new ArgumentNullException(nameof(configuredLeftCurtain));
            rightCurtain = configuredRightCurtain
                ?? throw new ArgumentNullException(nameof(configuredRightCurtain));
            touchButton = configuredTouchButton
                ?? throw new ArgumentNullException(nameof(configuredTouchButton));
            skipButton = configuredSkipButton
                ?? throw new ArgumentNullException(nameof(configuredSkipButton));

            leftClosedPosition = leftCurtain.anchoredPosition;
            rightClosedPosition = rightCurtain.anchoredPosition;
            ResetToIdle();
        }

        public void Prepare(GachaIndividualDisplayEntry firstEntry)
        {
            EnsureConfigured();
            presentationRoot.gameObject.SetActive(true);
            resultRoot.SetActive(false);
            touchButton.gameObject.SetActive(true);
            SetSkipVisible(false);
            Bind(firstEntry);
            SetCurtainProgress(0f);
        }

        public void Bind(GachaIndividualDisplayEntry entry)
        {
            EnsureConfigured();
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            ClearIndividual();
            switch (entry.Kind)
            {
                case GachaResultDisplayKind.Character:
                    BindImage(characterImage, entry.PrimarySprite);
                    BindOverlay(
                        characterOverlayRoot,
                        characterOverlayImage,
                        characterOverlayText,
                        entry);
                    characterRoot.SetActive(true);
                    break;
                case GachaResultDisplayKind.Relic:
                    BindImage(artifactImage, entry.PrimarySprite);
                    artifactRoot.SetActive(true);
                    BindOverlay(
                        artifactOverlayRoot,
                        artifactOverlayImage,
                        artifactOverlayText,
                        entry);
                    artifactOrItemRoot.SetActive(true);
                    break;
                case GachaResultDisplayKind.Item:
                    BindImage(itemImage, entry.PrimarySprite);
                    itemText.text = "+" + entry.ItemAmount.ToString(
                        "N0",
                        CultureInfo.InvariantCulture);
                    itemRoot.SetActive(true);
                    artifactOrItemRoot.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry));
            }
        }

        public void SetCurtainProgress(float openProgress)
        {
            EnsureConfigured();
            Canvas.ForceUpdateCanvases();
            float progress = Mathf.Clamp01(openProgress);
            float rootWidth = Mathf.Max(1f, presentationRoot.rect.width);
            float leftDistance = rootWidth + leftCurtain.rect.width;
            float rightDistance = rootWidth + rightCurtain.rect.width;
            leftCurtain.anchoredPosition = Vector2.Lerp(
                leftClosedPosition,
                leftClosedPosition + Vector2.left * leftDistance,
                progress);
            rightCurtain.anchoredPosition = Vector2.Lerp(
                rightClosedPosition,
                rightClosedPosition + Vector2.right * rightDistance,
                progress);
            curtainRoot.SetActive(true);
        }

        public void PrepareFinalSummary()
        {
            ClearIndividual();
            curtainRoot.SetActive(false);
            SetSkipVisible(false);
        }

        public void SetSkipVisible(bool visible)
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(visible);
            }
        }

        public void ResetToIdle()
        {
            ClearIndividual();
            if (resultRoot != null)
            {
                resultRoot.SetActive(false);
            }

            if (leftCurtain != null)
            {
                leftCurtain.anchoredPosition = leftClosedPosition;
            }

            if (rightCurtain != null)
            {
                rightCurtain.anchoredPosition = rightClosedPosition;
            }

            curtainRoot?.SetActive(false);
            SetSkipVisible(false);
            if (presentationRoot != null)
            {
                presentationRoot.gameObject.SetActive(false);
            }
        }

        private void ClearIndividual()
        {
            characterRoot?.SetActive(false);
            artifactOrItemRoot?.SetActive(false);
            artifactRoot?.SetActive(false);
            itemRoot?.SetActive(false);
            characterOverlayRoot?.SetActive(false);
            artifactOverlayRoot?.SetActive(false);
            if (characterImage != null)
            {
                characterImage.sprite = null;
            }

            if (artifactImage != null)
            {
                artifactImage.sprite = null;
            }

            if (itemImage != null)
            {
                itemImage.sprite = null;
            }

            if (itemText != null)
            {
                itemText.text = string.Empty;
            }

            if (characterOverlayText != null)
            {
                characterOverlayText.text = string.Empty;
            }

            if (artifactOverlayText != null)
            {
                artifactOverlayText.text = string.Empty;
            }
        }

        private static void BindImage(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
        }

        private static void BindOverlay(
            GameObject root,
            Image image,
            TMP_Text text,
            GachaIndividualDisplayEntry entry)
        {
            if (!entry.HasOverlay)
            {
                root.SetActive(false);
                return;
            }

            BindImage(image, entry.OverlaySprite);
            text.text = entry.OverlayText;
            root.SetActive(true);
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "GachaPresentationView must be configured before use.");
            }
        }
    }
}
