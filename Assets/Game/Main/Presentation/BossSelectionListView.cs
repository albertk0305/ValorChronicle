using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Battle.Results;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Main.Presentation
{
    public sealed class BossSelectionListView : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect = null;
        [SerializeField] private RectTransform content = null;
        [SerializeField] private BossSlotView[] initialSlots =
            Array.Empty<BossSlotView>();
        [SerializeField] private ElementIconSet elementIconSet = null;
        [SerializeField] private Sprite gradeC = null;
        [SerializeField] private Sprite gradeB = null;
        [SerializeField] private Sprite gradeA = null;
        [SerializeField] private Sprite gradeS = null;
        [SerializeField] private Sprite gradeSS = null;
        [SerializeField] private Sprite gradeSSS = null;
        [SerializeField] private float slotHeight = 300f;
        [SerializeField] private float spacing = 35f;
        [SerializeField] private float verticalPadding = 45f;

        private readonly List<BossSlotView> slots =
            new List<BossSlotView>();
        private bool poolInitialized;

        public event Action<string> SelectionRequested;

        public int PoolCount => slots.Count;
        public int ActiveSlotCount { get; private set; }
        public ScrollRect ScrollRect => scrollRect;
        public RectTransform Content => content;
        public IReadOnlyList<BossSlotView> Slots => slots;
        public ElementIconSet ElementIconSet => elementIconSet;
        public bool IsConfigured =>
            scrollRect != null
            && scrollRect.viewport != null
            && content != null
            && initialSlots != null
            && initialSlots.Length > 0
            && elementIconSet != null;

        public void Configure(
            ScrollRect configuredScrollRect,
            RectTransform configuredContent,
            BossSlotView[] configuredInitialSlots,
            ElementIconSet configuredElementIconSet,
            Sprite configuredGradeC = null,
            Sprite configuredGradeB = null,
            Sprite configuredGradeA = null,
            Sprite configuredGradeS = null,
            Sprite configuredGradeSS = null,
            Sprite configuredGradeSSS = null)
        {
            scrollRect = configuredScrollRect;
            content = configuredContent;
            initialSlots = configuredInitialSlots ?? Array.Empty<BossSlotView>();
            elementIconSet = configuredElementIconSet;
            gradeC = configuredGradeC;
            gradeB = configuredGradeB;
            gradeA = configuredGradeA;
            gradeS = configuredGradeS;
            gradeSS = configuredGradeSS;
            gradeSSS = configuredGradeSSS;
            poolInitialized = false;
            slots.Clear();
        }

        public void Populate(
            DefinitionDatabase database,
            LocalizationService localization,
            ProfileSaveData profile)
        {
            Show(BuildItems(database, localization, profile));
        }

        public void Refresh(
            DefinitionDatabase database,
            LocalizationService localization,
            ProfileSaveData profile)
        {
            Refresh(BuildItems(database, localization, profile));
        }

        public void Refresh(IReadOnlyList<BossSelectionItem> items)
        {
            float scrollPosition = scrollRect.verticalNormalizedPosition;
            ShowItems(items, resetScroll: false);
            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = scrollPosition;
        }

        public IReadOnlyList<BossSelectionItem> BuildItems(
            DefinitionDatabase database,
            LocalizationService localization,
            ProfileSaveData profile)
        {
            return BossSelectionListBuilder.Build(
                    database,
                    localization,
                    elementIconSet,
                    profile,
                    ResolveGradeSprite,
                    this);
        }

        public void Show(IReadOnlyList<BossSelectionItem> items)
        {
            ShowItems(items, resetScroll: true);
        }

        private void ShowItems(
            IReadOnlyList<BossSelectionItem> items,
            bool resetScroll)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            InitializePool();
            EnsurePoolSize(items.Count);
            for (int index = 0; index < slots.Count; index++)
            {
                BossSlotView slot = slots[index];
                if (index < items.Count)
                {
                    PositionSlot(slot, index);
                    slot.Bind(items[index], HandleSelectionRequested);
                }
                else
                {
                    slot.Unbind();
                }
            }

            ActiveSlotCount = Mathf.Min(items.Count, slots.Count);
            ResizeContent(items.Count);
            if (resetScroll)
            {
                ResetScrollToTop();
            }
        }

        public Sprite ResolveGradeSprite(string gradeId)
        {
            switch (gradeId)
            {
                case BattleGradeIds.C:
                    return gradeC;
                case BattleGradeIds.B:
                    return gradeB;
                case BattleGradeIds.A:
                    return gradeA;
                case BattleGradeIds.S:
                    return gradeS;
                case BattleGradeIds.SS:
                    return gradeSS;
                case BattleGradeIds.SSS:
                    return gradeSSS;
                default:
                    return null;
            }
        }

        private void InitializePool()
        {
            if (poolInitialized)
            {
                return;
            }

            poolInitialized = true;
            for (int index = 0; index < initialSlots.Length; index++)
            {
                BossSlotView slot = initialSlots[index];
                if (slot != null && !slots.Contains(slot))
                {
                    slots.Add(slot);
                }
            }
        }

        private void EnsurePoolSize(int requiredCount)
        {
            if (requiredCount <= slots.Count)
            {
                return;
            }

            if (slots.Count == 0)
            {
                GameLogger.Error(
                    "[BossSelectionListView] A slot template is required.",
                    this);
                return;
            }

            BossSlotView template = slots[0];
            while (slots.Count < requiredCount)
            {
                BossSlotView clone = Instantiate(template, content);
                clone.name = $"Slot ({slots.Count})";
                clone.Unbind();
                slots.Add(clone);
            }
        }

        private void PositionSlot(BossSlotView slot, int index)
        {
            var rect = (RectTransform)slot.transform;
            rect.SetParent(content, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(
                0f,
                -verticalPadding
                    - (slotHeight * 0.5f)
                    - (index * (slotHeight + spacing)));
            rect.SetSiblingIndex(index);
        }

        private void ResizeContent(int itemCount)
        {
            float requiredHeight = verticalPadding * 2f;
            if (itemCount > 0)
            {
                requiredHeight += itemCount * slotHeight;
                requiredHeight += (itemCount - 1) * spacing;
            }

            float viewportHeight = scrollRect.viewport.rect.height;
            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(requiredHeight, viewportHeight));
        }

        private void ResetScrollToTop()
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void HandleSelectionRequested(string bossId)
        {
            SelectionRequested?.Invoke(bossId);
        }
    }
}
