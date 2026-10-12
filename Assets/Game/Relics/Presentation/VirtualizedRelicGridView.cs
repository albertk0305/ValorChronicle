using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Logging;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Relics.Inventory;

namespace ValorChronicle.Relics.Presentation
{
    public sealed class VirtualizedRelicGridView : MonoBehaviour
    {
        private ScrollRect scrollRect;
        private RectTransform viewport;
        private RectTransform content;
        private RelicInventoryCellView[] initialCells =
            Array.Empty<RelicInventoryCellView>();
        private int columnCount = 5;
        private int bufferRows;
        private Vector2 cellSize = new Vector2(185f, 185f);
        private Vector2 spacing = new Vector2(25f, 25f);
        private float topPadding = 15f;
        private float bottomPadding = 15f;

        private readonly List<RelicInventoryCellView> pool =
            new List<RelicInventoryCellView>();
        private readonly List<RelicInventoryCellView> runtimeCreatedCells =
            new List<RelicInventoryCellView>();
        private IReadOnlyList<RelicInventoryEntry> items =
            Array.Empty<RelicInventoryEntry>();
        private Func<string, Sprite> iconResolver;
        private int firstBoundRow = -1;
        private bool initialized;
        private bool scrollSubscribed;

        public event Action<string> RelicRequested;

        public IReadOnlyList<RelicInventoryCellView> PoolCells => pool;
        public IReadOnlyList<RelicInventoryEntry> Items => items;
        public int ItemCount => items.Count;
        public int PoolSize => pool.Count;
        public int FirstBoundRow => Math.Max(0, firstBoundRow);
        public int BoundItemCount { get; private set; }
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
            && initialCells != null
            && initialCells.Length > 0
            && Array.TrueForAll(initialCells, cell => cell != null)
            && columnCount == 5
            && bufferRows >= 0
            && cellSize.x > 0f
            && cellSize.y > 0f
            && spacing.x >= 0f
            && spacing.y >= 0f
            && topPadding >= 0f
            && bottomPadding >= 0f;

        private void OnEnable()
        {
            InitializeIfNeeded();
            SubscribeScroll();
        }

        private void OnDisable()
        {
            UnsubscribeScroll();
        }

        public void Configure(
            ScrollRect configuredScrollRect,
            RectTransform configuredViewport,
            RectTransform configuredContent,
            RelicInventoryCellView[] configuredInitialCells,
            Vector2 configuredCellSize,
            Vector2 configuredSpacing,
            float configuredTopPadding = 15f,
            float configuredBottomPadding = 15f,
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
            initialCells = configuredInitialCells
                ?? Array.Empty<RelicInventoryCellView>();
            columnCount = 5;
            bufferRows = configuredBufferRows;
            cellSize = configuredCellSize;
            spacing = configuredSpacing;
            topPadding = configuredTopPadding;
            bottomPadding = configuredBottomPadding;
            initialized = false;
            InitializeIfNeeded();
            if (isActiveAndEnabled)
            {
                SubscribeScroll();
            }
        }

        public void SetItems(
            IReadOnlyList<RelicInventoryEntry> newItems,
            Func<string, Sprite> newIconResolver,
            bool resetScroll)
        {
            items = newItems
                ?? throw new ArgumentNullException(nameof(newItems));
            iconResolver = newIconResolver;
            InitializeIfNeeded();
            UpdateContentHeight();
            if (resetScroll)
            {
                ScrollToTop();
            }

            firstBoundRow = -1;
            RebindVisibleCells(force: true);
        }

        public void Clear()
        {
            items = Array.Empty<RelicInventoryEntry>();
            iconResolver = null;
            firstBoundRow = -1;
            BoundItemCount = 0;
            InitializeIfNeeded();
            UpdateContentHeight();
            ScrollToTop();
            HideAllCells();
        }

        public void SetScrollOffsetForTesting(float offset)
        {
            InitializeIfNeeded();
            float maximum = Mathf.Max(
                0f,
                content.rect.height - viewport.rect.height);
            Vector2 position = content.anchoredPosition;
            position.y = Mathf.Clamp(offset, 0f, maximum);
            content.anchoredPosition = position;
            RebindVisibleCells(force: true);
        }

        private void InitializeIfNeeded()
        {
            if (!IsConfigured)
            {
                return;
            }

            ApplyScrollInteractionPolicy();
            if (!initialized)
            {
                RemovePoolCellListeners();
                pool.Clear();
                for (int index = 0; index < initialCells.Length; index++)
                {
                    AddPoolCellIfMissing(initialCells[index]);
                }

                for (int index = runtimeCreatedCells.Count - 1;
                    index >= 0;
                    index--)
                {
                    if (runtimeCreatedCells[index] == null)
                    {
                        runtimeCreatedCells.RemoveAt(index);
                    }
                }

                for (int index = 0;
                    index < runtimeCreatedCells.Count;
                    index++)
                {
                    RelicInventoryCellView runtimeCell =
                        runtimeCreatedCells[index];
                    if (runtimeCell.transform.parent != content)
                    {
                        runtimeCell.transform.SetParent(content, false);
                    }

                    AddPoolCellIfMissing(runtimeCell);
                }

                initialized = true;
                scrollRect.viewport = viewport;
                scrollRect.content = content;
            }

            EnsurePoolCapacity();
            HideAllCells();
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
            int requiredRows =
                VirtualizedGridLayout.GetRequiredPoolRowCount(
                    viewportHeight,
                    cellSize.y,
                    rowStride,
                    bufferRows);
            int requiredCellCount = requiredRows * columnCount;
            RelicInventoryCellView template = initialCells[0];
            while (pool.Count < requiredCellCount)
            {
                RelicInventoryCellView clone = Instantiate(
                    template,
                    content);
                clone.name = $"Relic (Virtual {runtimeCreatedCells.Count})";
                clone.Configure(
                    clone.Button,
                    clone.IconImage,
                    clone.Border,
                    clone.AwakeningText);
                runtimeCreatedCells.Add(clone);
                AddPoolCellIfMissing(clone);
            }
        }

        private void AddPoolCellIfMissing(RelicInventoryCellView cell)
        {
            if (pool.Contains(cell))
            {
                return;
            }

            pool.Add(cell);
            cell.Clicked += HandleCellClicked;
        }

        private void RemovePoolCellListeners()
        {
            for (int index = 0; index < pool.Count; index++)
            {
                if (pool[index] != null)
                {
                    pool[index].Clicked -= HandleCellClicked;
                }
            }
        }

        private void HandleScrollChanged(Vector2 _)
        {
            RebindVisibleCells(force: false);
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

        private void HandleCellClicked(string instanceId)
        {
            RelicRequested?.Invoke(instanceId);
        }

        private void UpdateContentHeight()
        {
            float height = VirtualizedGridLayout.GetContentHeight(
                items.Count,
                columnCount,
                Mathf.Max(1f, viewport.rect.height),
                cellSize.y,
                spacing.y,
                topPadding,
                bottomPadding);
            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                height);
        }

        private void ScrollToTop()
        {
            scrollRect.StopMovement();
            Vector2 position = content.anchoredPosition;
            position.y = 0f;
            content.anchoredPosition = position;
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void RebindVisibleCells(bool force)
        {
            if (!initialized)
            {
                return;
            }

            VirtualizedGridRange range =
                VirtualizedGridLayout.GetBoundRowRange(
                    items.Count,
                    columnCount,
                    ScrollOffset,
                    Mathf.Max(1f, viewport.rect.height),
                    cellSize.y,
                    spacing.y,
                    topPadding,
                    bufferRows);
            if (!force && range.FirstRow == firstBoundRow)
            {
                return;
            }

            firstBoundRow = range.FirstRow;
            BoundItemCount = 0;
            int firstDataIndex = range.FirstRow * columnCount;
            for (int poolIndex = 0;
                poolIndex < pool.Count;
                poolIndex++)
            {
                int dataIndex = firstDataIndex + poolIndex;
                RelicInventoryCellView cell = pool[poolIndex];
                if (dataIndex >= items.Count
                    || dataIndex
                        >= range.LastRowExclusive * columnCount)
                {
                    cell.HideAndClear();
                    continue;
                }

                RelicInventoryEntry entry = items[dataIndex];
                PositionCell(cell, dataIndex);
                cell.Bind(
                    entry,
                    iconResolver?.Invoke(entry.RelicDefinitionId));
                BoundItemCount++;
            }
        }

        private void PositionCell(
            RelicInventoryCellView cell,
            int dataIndex)
        {
            int row = dataIndex / columnCount;
            int column = dataIndex % columnCount;
            float columnStride = cellSize.x + spacing.x;
            float rowStride = cellSize.y + spacing.y;
            float centeredColumn = column - ((columnCount - 1) * 0.5f);
            RectTransform rect = (RectTransform)cell.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = cellSize;
            rect.anchoredPosition = new Vector2(
                centeredColumn * columnStride,
                -(topPadding + (cellSize.y * 0.5f)
                    + (row * rowStride)));
        }

        private void HideAllCells()
        {
            for (int index = 0; index < pool.Count; index++)
            {
                pool[index].HideAndClear();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!initialized || viewport == null || content == null)
            {
                return;
            }

            EnsurePoolCapacity();
            UpdateContentHeight();
            firstBoundRow = -1;
            RebindVisibleCells(force: true);
        }

        private void OnValidate()
        {
            if (Application.isPlaying || IsConfigured)
            {
                return;
            }

            GameLogger.Warning(
                "[VirtualizedRelicGridView] ScrollRect, viewport, "
                    + "content, cells, and five-column layout are required.",
                this);
        }
    }
}
