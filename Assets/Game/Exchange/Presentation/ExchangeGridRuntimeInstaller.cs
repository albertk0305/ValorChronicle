using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Exchange.Presentation
{
    public static class ExchangeGridRuntimeInstaller
    {
        public static readonly Vector2 SceneCellSize =
            new Vector2(100f, 100f);
        public static readonly Vector2 SceneSpacing =
            new Vector2(430f, 200f);
        public const float SceneTopPadding = 120f;
        public const float SceneBottomPadding = 210f;
        public const float SceneHorizontalOffset = -115f;

        public static VirtualizedExchangeGridView Install(
            RectTransform exchangeRoot)
        {
            if (exchangeRoot == null)
                throw new ArgumentNullException(nameof(exchangeRoot));

            RectTransform viewport = RequireRect(
                exchangeRoot,
                "ScrollPanel");
            RectTransform content = FindContent(exchangeRoot, viewport);
            RectTransform contentBackground = RequireRect(
                content,
                "SlotsPanel");
            Image viewportImage = RequireComponent<Image>(viewport);
            viewportImage.raycastTarget = true;
            GetOrAdd<RectMask2D>(viewport.gameObject);
            ScrollRect scrollRect = GetOrAdd<ScrollRect>(
                viewport.gameObject);

            if (content.parent != viewport)
                content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(
                0f,
                Mathf.Max(1f, viewport.rect.height));
            contentBackground.anchorMin = Vector2.zero;
            contentBackground.anchorMax = Vector2.one;
            contentBackground.pivot = new Vector2(0.5f, 0.5f);
            contentBackground.anchoredPosition = Vector2.zero;
            contentBackground.sizeDelta = Vector2.zero;
            contentBackground.SetAsFirstSibling();

            var slots = new List<ExchangeSlotView>();
            for (int index = 0; index < content.childCount; index++)
            {
                Transform child = content.GetChild(index);
                if (!IsSlotName(child.name))
                    continue;

                slots.Add(InstallSlot((RectTransform)child));
            }

            if (slots.Count == 0)
            {
                throw new InvalidOperationException(
                    "Exchange Slots requires at least one Slot child.");
            }

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            var grid = GetOrAdd<VirtualizedExchangeGridView>(
                viewport.gameObject);
            grid.Configure(
                scrollRect,
                viewport,
                content,
                contentBackground,
                slots.ToArray(),
                SceneCellSize,
                SceneSpacing,
                SceneTopPadding,
                SceneBottomPadding,
                SceneHorizontalOffset);
            return grid;
        }

        private static ExchangeSlotView InstallSlot(RectTransform root)
        {
            RectTransform buttonRoot = RequireRect(root, "Button");
            Button button = RequireComponent<Button>(buttonRoot);
            Image contentImage = RequireComponent<Image>(buttonRoot);
            RectTransform awakeningIcon = RequireRect(
                root,
                "AwakeningIcon");
            Image awakeningImage = RequireComponent<Image>(awakeningIcon);
            awakeningImage.raycastTarget = false;
            TMP_Text awakeningText = RequireComponent<TMP_Text>(RequireRect(
                root,
                "AwakeningText"));
            awakeningText.raycastTarget = false;
            Image priceIcon = RequireComponent<Image>(RequireRect(
                root,
                "PriceIcon"));
            priceIcon.raycastTarget = false;
            TMP_Text priceText = RequireComponent<TMP_Text>(RequireRect(
                root,
                "PriceText"));
            priceText.raycastTarget = false;
            RequireComponent<Image>(RequireRect(root, "Border"))
                .raycastTarget = false;
            RequireComponent<Image>(RequireRect(root, "BackgroundPanel"))
                .raycastTarget = false;
            var slot = GetOrAdd<ExchangeSlotView>(root.gameObject);
            slot.Configure(
                button,
                contentImage,
                awakeningIcon.gameObject,
                awakeningText,
                priceIcon,
                priceText,
                GetOrAdd<CanvasGroup>(root.gameObject));
            return slot;
        }

        private static bool IsSlotName(string objectName)
        {
            return string.Equals(
                    objectName,
                    "Slot",
                    StringComparison.Ordinal)
                || objectName.StartsWith(
                    "Slot (",
                    StringComparison.Ordinal);
        }

        private static RectTransform RequireRect(
            Transform root,
            string path)
        {
            Transform found = root.Find(path);
            return found as RectTransform
                ?? throw new InvalidOperationException(
                    $"Required Exchange object is missing: {path}.");
        }

        private static RectTransform FindContent(
            Transform exchangeRoot,
            Transform viewport)
        {
            Transform found = exchangeRoot.Find("Slots")
                ?? viewport.Find("Slots");
            return found as RectTransform
                ?? throw new InvalidOperationException(
                    "Required Exchange object is missing: Slots.");
        }

        private static T RequireComponent<T>(Component target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : throw new InvalidOperationException(
                    $"'{target.name}' requires {typeof(T).Name}.");
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
