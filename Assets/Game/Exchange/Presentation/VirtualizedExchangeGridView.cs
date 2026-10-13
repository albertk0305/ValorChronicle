using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Exchange.Presentation
{
    public sealed class VirtualizedExchangeGridView : MonoBehaviour
    {
        public const int ColumnCount = 2;

        private ScrollRect scrollRect;
        private RectTransform viewport;
        private RectTransform content;
        private RectTransform contentBackground;
        private ExchangeSlotView[] initialSlots =
            Array.Empty<ExchangeSlotView>();
        private int bufferRows;
        private Vector2 cellSize = new Vector2(100f, 100f);
        private Vector2 spacing = new Vector2(430f, 200f);
        private float topPadding = 120f;
        private float bottomPadding = 210f;
        private float horizontalOffset = -115f;

        private readonly List<ExchangeSlotView> pool =
            new List<ExchangeSlotView>();
        private readonly List<ExchangeSlotView> runtimeCreatedSlots =
            new List<ExchangeSlotView>();
        private IReadOnlyList<ExchangeListItem> items =
            Array.Empty<ExchangeListItem>();
        private ExchangeSpriteResolver spriteResolver;
        private ExchangeContentKind visibleKind;
        private int firstBoundRow = -1;
        private bool initialized;
        private bool scrollSubscribed;
        private bool interactionEnabled = true;

        public event Action<ExchangeContentKind, string> SelectionRequested;

        public IReadOnlyList<ExchangeSlotView> PoolSlots => pool;
        public int ItemCount => items.Count;
        public int PoolSize => pool.Count;
        public int FirstBoundRow => Math.Max(0, firstBoundRow);
        public int BoundItemCount { get; private set; }
        public ExchangeContentKind VisibleKind => visibleKind;
        public float ScrollOffset => content == null
            ? 0f
            : Mathf.Max(0f, content.anchoredPosition.y);
        public float ContentHeight => content == null
            ? 0f
            : content.rect.height;
        public bool IsAtTop => ScrollOffset <= 0.01f;
        public bool IsConfigured => scrollRect != null
            && viewport != null
            && content != null
            && initialSlots != null
            && initialSlots.Length > 0
            && Array.TrueForAll(initialSlots, slot => slot != null)
            && bufferRows >= 0
            && cellSize.x > 0f
            && cellSize.y > 0f
            && spacing.x >= 0f
            && spacing.y >= 0f
            && topPadding >= 0f
            && bottomPadding >= 0f;
        public bool IsInteractionEnabled => interactionEnabled;

        private void OnEnable()
        {
            InitializeIfNeeded();
            SubscribeScroll();
            RebindVisibleSlots(force: true);
        }

        private void OnDisable()
        {
            UnsubscribeScroll();
        }

        private void OnDestroy()
        {
            UnsubscribeScroll();
            RemovePoolListeners();
        }

        public void Configure(
            ScrollRect configuredScrollRect,
            RectTransform configuredViewport,
            RectTransform configuredContent,
            RectTransform configuredContentBackground,
            ExchangeSlotView[] configuredInitialSlots,
            Vector2 configuredCellSize,
            Vector2 configuredSpacing,
            float configuredTopPadding,
            float configuredBottomPadding,
            float configuredHorizontalOffset,
            int configuredBufferRows = 0)
        {
            UnsubscribeScroll();
            scrollRect = configuredScrollRect
                ?? throw new ArgumentNullException(
                    nameof(configuredScrollRect));
            viewport = configuredViewport
                ?? throw new ArgumentNullException(
                    nameof(configuredViewport));
            content = configuredContent
                ?? throw new ArgumentNullException(
                    nameof(configuredContent));
            contentBackground = configuredContentBackground;
            initialSlots = configuredInitialSlots
                ?? Array.Empty<ExchangeSlotView>();
            cellSize = configuredCellSize;
            spacing = configuredSpacing;
            topPadding = configuredTopPadding;
            bottomPadding = configuredBottomPadding;
            horizontalOffset = configuredHorizontalOffset;
            bufferRows = configuredBufferRows;
            initialized = false;
            InitializeIfNeeded();
            if (isActiveAndEnabled)
                SubscribeScroll();
        }

        public void BindCharacters(
            ExchangeCatalogData catalog,
            ExchangeSpriteResolver resolver,
            bool resetScroll)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            SetItems(
                catalog.Characters,
                ExchangeContentKind.Character,
                resolver,
                resetScroll);
        }

        public void BindRelics(
            ExchangeCatalogData catalog,
            ExchangeSpriteResolver resolver,
            bool resetScroll)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            SetItems(
                catalog.Relics,
                ExchangeContentKind.Relic,
                resolver,
                resetScroll);
        }

        public void RefreshCurrent(bool resetScroll)
        {
            InitializeIfNeeded();
            UpdateContentHeight();
            if (resetScroll)
                ScrollToTop();
            else
                ClampScrollOffset();

            firstBoundRow = -1;
            RebindVisibleSlots(force: true);
        }

        public void Clear(bool resetScroll = true)
        {
            items = Array.Empty<ExchangeListItem>();
            spriteResolver = null;
            firstBoundRow = -1;
            BoundItemCount = 0;
            InitializeIfNeeded();
            UpdateContentHeight();
            if (resetScroll)
                ScrollToTop();
            else
                ClampScrollOffset();
            HideAllSlots();
        }

        public void ScrollToTop()
        {
            if (!IsConfigured)
                return;

            scrollRect.StopMovement();
            Vector2 position = content.anchoredPosition;
            position.y = 0f;
            content.anchoredPosition = position;
            scrollRect.verticalNormalizedPosition = 1f;
            firstBoundRow = -1;
            RebindVisibleSlots(force: true);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
            if (scrollRect != null)
                scrollRect.enabled = enabled;
            for (int index = 0; index < pool.Count; index++)
                pool[index].SetInputEnabled(enabled);
        }

        private void SetItems<T>(
            IReadOnlyList<T> newItems,
            ExchangeContentKind kind,
            ExchangeSpriteResolver resolver,
            bool resetScroll)
            where T : ExchangeListItem
        {
            if (newItems == null)
                throw new ArgumentNullException(nameof(newItems));

            var itemReferences = new ExchangeListItem[newItems.Count];
            for (int index = 0; index < newItems.Count; index++)
                itemReferences[index] = newItems[index];

            items = itemReferences;
            visibleKind = kind;
            spriteResolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
            InitializeIfNeeded();
            UpdateContentHeight();
            if (resetScroll)
                ScrollToTop();
            else
                ClampScrollOffset();

            firstBoundRow = -1;
            RebindVisibleSlots(force: true);
        }

        private void InitializeIfNeeded()
        {
            if (!IsConfigured)
                return;

            ApplyScrollInteractionPolicy();
            if (!initialized)
            {
                RemovePoolListeners();
                pool.Clear();
                for (int index = 0; index < initialSlots.Length; index++)
                    AddPoolSlotIfMissing(initialSlots[index]);

                for (int index = runtimeCreatedSlots.Count - 1;
                    index >= 0;
                    index--)
                {
                    if (runtimeCreatedSlots[index] == null)
                        runtimeCreatedSlots.RemoveAt(index);
                }

                for (int index = 0;
                    index < runtimeCreatedSlots.Count;
                    index++)
                {
                    ExchangeSlotView slot = runtimeCreatedSlots[index];
                    if (slot.transform.parent != content)
                        slot.transform.SetParent(content, false);
                    AddPoolSlotIfMissing(slot);
                }

                initialized = true;
                scrollRect.viewport = viewport;
                scrollRect.content = content;
            }

            EnsurePoolCapacity();
            HideAllSlots();
        }

        private void ApplyScrollInteractionPolicy()
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 1f;
        }

        private void EnsurePoolCapacity()
        {
            float rowStride = cellSize.y + spacing.y;
            float viewportHeight = Mathf.Max(1f, viewport.rect.height);
            int requiredRows = VirtualizedGridLayout
                .GetRequiredPoolRowCount(
                    viewportHeight,
                    cellSize.y,
                    rowStride,
                    bufferRows);
            int requiredSlotCount = requiredRows * ColumnCount;
            ExchangeSlotView template = initialSlots[0];
            while (pool.Count < requiredSlotCount)
            {
                ExchangeSlotView clone = Instantiate(template, content);
                clone.name =
                    $"Slot (Virtual {runtimeCreatedSlots.Count})";
                clone.Configure(
                    clone.Button,
                    clone.ContentImage,
                    clone.AwakeningIcon,
                    clone.AwakeningText,
                    clone.PriceIcon,
                    clone.PriceText,
                    clone.CanvasGroup);
                runtimeCreatedSlots.Add(clone);
                AddPoolSlotIfMissing(clone);
            }
        }

        private void AddPoolSlotIfMissing(ExchangeSlotView slot)
        {
            if (pool.Contains(slot))
                return;

            pool.Add(slot);
            slot.Clicked += HandleSlotClicked;
        }

        private void RemovePoolListeners()
        {
            for (int index = 0; index < pool.Count; index++)
            {
                if (pool[index] != null)
                    pool[index].Clicked -= HandleSlotClicked;
            }
        }

        private void HandleSlotClicked(
            ExchangeContentKind kind,
            string contentId)
        {
            SelectionRequested?.Invoke(kind, contentId);
        }

        private void SubscribeScroll()
        {
            if (!scrollSubscribed && scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(HandleScrollChanged);
                scrollSubscribed = true;
            }
        }

        private void UnsubscribeScroll()
        {
            if (scrollSubscribed && scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(
                    HandleScrollChanged);
            }

            scrollSubscribed = false;
        }

        private void HandleScrollChanged(Vector2 _)
        {
            RebindVisibleSlots(force: false);
        }

        private void UpdateContentHeight()
        {
            float height = VirtualizedGridLayout.GetContentHeight(
                items.Count,
                ColumnCount,
                Mathf.Max(1f, viewport.rect.height),
                cellSize.y,
                spacing.y,
                topPadding,
                bottomPadding);
            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                height);
            if (contentBackground != null)
            {
                contentBackground.anchorMin = Vector2.zero;
                contentBackground.anchorMax = Vector2.one;
                contentBackground.pivot = new Vector2(0.5f, 0.5f);
                contentBackground.anchoredPosition = Vector2.zero;
                contentBackground.sizeDelta = Vector2.zero;
                contentBackground.SetAsFirstSibling();
            }
        }

        private void ClampScrollOffset()
        {
            float maximum = Mathf.Max(
                0f,
                content.rect.height - viewport.rect.height);
            Vector2 position = content.anchoredPosition;
            position.y = Mathf.Clamp(position.y, 0f, maximum);
            content.anchoredPosition = position;
        }

        private void RebindVisibleSlots(bool force)
        {
            if (!initialized)
                return;

            VirtualizedGridRange range = VirtualizedGridLayout
                .GetBoundRowRange(
                    items.Count,
                    ColumnCount,
                    ScrollOffset,
                    Mathf.Max(1f, viewport.rect.height),
                    cellSize.y,
                    spacing.y,
                    topPadding,
                    bufferRows);
            if (!force && range.FirstRow == firstBoundRow)
                return;

            firstBoundRow = range.FirstRow;
            BoundItemCount = 0;
            int firstDataIndex = range.FirstRow * ColumnCount;
            for (int poolIndex = 0;
                poolIndex < pool.Count;
                poolIndex++)
            {
                int dataIndex = firstDataIndex + poolIndex;
                ExchangeSlotView slot = pool[poolIndex];
                if (dataIndex >= items.Count
                    || dataIndex
                        >= range.LastRowExclusive * ColumnCount)
                {
                    slot.HideAndClear();
                    continue;
                }

                ExchangeListItem item = items[dataIndex];
                PositionSlot(slot, dataIndex);
                slot.Bind(
                    item,
                    spriteResolver.ResolveContent(item),
                    spriteResolver.ResolveCurrency(item.CurrencyKind));
                slot.SetInputEnabled(interactionEnabled);
                BoundItemCount++;
            }
        }

        private void PositionSlot(ExchangeSlotView slot, int dataIndex)
        {
            int row = dataIndex / ColumnCount;
            int column = dataIndex % ColumnCount;
            float columnStride = cellSize.x + spacing.x;
            float rowStride = cellSize.y + spacing.y;
            float centeredColumn = column
                - ((ColumnCount - 1) * 0.5f);
            RectTransform rect = (RectTransform)slot.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = cellSize;
            rect.anchoredPosition = new Vector2(
                horizontalOffset + (centeredColumn * columnStride),
                -(topPadding + (cellSize.y * 0.5f)
                    + (row * rowStride)));
        }

        private void HideAllSlots()
        {
            for (int index = 0; index < pool.Count; index++)
                pool[index].HideAndClear();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!initialized || viewport == null || content == null)
                return;

            EnsurePoolCapacity();
            UpdateContentHeight();
            ClampScrollOffset();
            firstBoundRow = -1;
            RebindVisibleSlots(force: true);
        }
    }
}
