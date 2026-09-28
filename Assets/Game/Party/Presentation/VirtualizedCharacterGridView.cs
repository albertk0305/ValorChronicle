using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Roster;

namespace ValorChronicle.Party.Presentation
{
    public sealed class VirtualizedCharacterGridView : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect = null;
        [SerializeField] private RectTransform viewport = null;
        [SerializeField] private RectTransform content = null;
        [SerializeField]
        private CharacterRosterCellView[] initialCells =
            Array.Empty<CharacterRosterCellView>();
        [SerializeField] private int columnCount = 5;
        [SerializeField] private int bufferRows = 1;
        [SerializeField] private Vector2 cellSize = new Vector2(100f, 100f);
        [SerializeField] private Vector2 spacing = new Vector2(110f, 110f);
        [SerializeField] private float topPadding = 11f;
        [SerializeField] private float bottomPadding = 11f;

        private readonly List<CharacterRosterCellView> pool =
            new List<CharacterRosterCellView>();
        private IReadOnlyList<CharacterRosterEntry> items =
            Array.Empty<CharacterRosterEntry>();
        private Func<string, Sprite> faceResolver;
        private Func<ElementType, Sprite> elementIconResolver;
        private string selectedCharacterId = string.Empty;
        private int firstBoundRow = -1;
        private bool interactionEnabled = true;
        private bool initialized;

        public event Action<string> CharacterRequested;

        public IReadOnlyList<CharacterRosterCellView> PoolCells => pool;
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
        public bool IsConfigured =>
            scrollRect != null
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
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(HandleScrollChanged);
            }
        }

        private void OnDisable()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(
                    HandleScrollChanged);
            }
        }

        public void SetItems(
            IReadOnlyList<CharacterRosterEntry> newItems,
            string previewCharacterId,
            Func<string, Sprite> newFaceResolver,
            Func<ElementType, Sprite> newElementIconResolver,
            bool resetScroll)
        {
            items = newItems
                ?? throw new ArgumentNullException(nameof(newItems));
            selectedCharacterId = previewCharacterId ?? string.Empty;
            faceResolver = newFaceResolver;
            elementIconResolver = newElementIconResolver;
            InitializeIfNeeded();
            UpdateContentHeight();
            if (resetScroll)
            {
                ScrollToTop();
            }

            firstBoundRow = -1;
            RebindVisibleCells(force: true);
        }

        public void RefreshSelection(string previewCharacterId)
        {
            selectedCharacterId = previewCharacterId ?? string.Empty;
            RebindVisibleCells(force: true);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
            for (int index = 0; index < pool.Count; index++)
            {
                CharacterRosterCellView cell = pool[index];
                cell.Button.interactable = enabled && cell.IsBound;
            }

            if (scrollRect != null)
            {
                scrollRect.enabled = enabled;
            }
        }

        public void Clear()
        {
            items = Array.Empty<CharacterRosterEntry>();
            selectedCharacterId = string.Empty;
            faceResolver = null;
            elementIconResolver = null;
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

            if (!initialized)
            {
                pool.Clear();
                for (int index = 0; index < initialCells.Length; index++)
                {
                    AddPoolCell(initialCells[index]);
                }

                initialized = true;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.viewport = viewport;
                scrollRect.content = content;
            }

            EnsurePoolCapacity();
            HideAllCells();
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
            CharacterRosterCellView template = initialCells[0];
            while (pool.Count < requiredCellCount)
            {
                CharacterRosterCellView clone = Instantiate(
                    template,
                    content);
                clone.name = $"Character (Virtual {pool.Count})";
                AddPoolCell(clone);
            }
        }

        private void AddPoolCell(CharacterRosterCellView cell)
        {
            pool.Add(cell);
            cell.Clicked += HandleCellClicked;
        }

        private void HandleScrollChanged(Vector2 _)
        {
            RebindVisibleCells(force: false);
        }

        private void HandleCellClicked(string characterId)
        {
            CharacterRequested?.Invoke(characterId);
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
                CharacterRosterCellView cell = pool[poolIndex];
                if (dataIndex >= items.Count
                    || dataIndex >= range.LastRowExclusive * columnCount)
                {
                    cell.HideAndClear();
                    continue;
                }

                CharacterRosterEntry entry = items[dataIndex];
                PositionCell(cell, dataIndex);
                cell.Bind(
                    entry,
                    faceResolver?.Invoke(entry.CharacterId),
                    elementIconResolver?.Invoke(entry.Element),
                    string.Equals(
                        selectedCharacterId,
                        entry.CharacterId,
                        StringComparison.Ordinal));
                cell.Button.interactable = interactionEnabled;
                BoundItemCount++;
            }
        }

        private void PositionCell(
            CharacterRosterCellView cell,
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
            if (!IsConfigured)
            {
                GameLogger.Warning(
                    "[VirtualizedCharacterGridView] ScrollRect, viewport, "
                        + "content, initial cells, and five-column layout "
                        + "configuration are required.",
                    this);
            }
        }
    }
}
