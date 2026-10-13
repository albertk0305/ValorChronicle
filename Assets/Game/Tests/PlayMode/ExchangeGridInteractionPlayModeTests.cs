using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Exchange;
using ValorChronicle.Exchange.Presentation;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.PlayMode
{
    public sealed class ExchangeGridInteractionPlayModeTests
    {
        private readonly List<UnityEngine.Object> created =
            new List<UnityEngine.Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                    UnityEngine.Object.Destroy(created[index]);
            }

            created.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClickSelectsOnceAndDragOnlyScrolls()
        {
            Fixture fixture = CreateFixture(11);
            int selectionCount = 0;
            fixture.Grid.SelectionRequested += (_, __) =>
                selectionCount++;
            yield return null;
            Canvas.ForceUpdateCanvases();

            ExchangeSlotView slot = fixture.Grid.PoolSlots[0];
            Vector2 point = RectTransformUtility.WorldToScreenPoint(
                null,
                slot.Button.transform.position);
            var click = new PointerEventData(fixture.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = point,
                pressPosition = point
            };
            ExecuteEvents.ExecuteHierarchy(
                slot.Button.gameObject,
                click,
                ExecuteEvents.pointerClickHandler);
            Assert.That(selectionCount, Is.EqualTo(1));

            float before = fixture.Grid.ScrollOffset;
            var drag = new PointerEventData(fixture.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = point,
                pressPosition = point,
                eligibleForClick = false
            };
            GameObject dragHandler = ExecuteEvents.GetEventHandler<
                IDragHandler>(slot.Button.gameObject);
            Assert.That(dragHandler,
                Is.EqualTo(fixture.Viewport.gameObject));
            drag.pointerDrag = dragHandler;
            ExecuteEvents.ExecuteHierarchy(
                slot.Button.gameObject,
                drag,
                ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(
                dragHandler,
                drag,
                ExecuteEvents.beginDragHandler);
            drag.delta = new Vector2(0f, 250f);
            drag.position += drag.delta;
            drag.dragging = true;
            ExecuteEvents.Execute(
                dragHandler,
                drag,
                ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(
                dragHandler,
                drag,
                ExecuteEvents.endDragHandler);
            ExecuteEvents.ExecuteHierarchy(
                slot.Button.gameObject,
                drag,
                ExecuteEvents.pointerUpHandler);

            Assert.That(fixture.Grid.ScrollOffset,
                Is.GreaterThan(before));
            Assert.That(selectionCount, Is.EqualTo(1));
            Assert.That(fixture.EventSystem.pixelDragThreshold,
                Is.EqualTo(10));

            SetScrollOffset(
                fixture.Grid,
                fixture.Viewport,
                float.MaxValue);
            Assert.That(fixture.Grid.PoolSlots.ExistsBound(
                "character_010"), Is.True);

            fixture.Grid.SetInteractionEnabled(false);
            ExchangeSlotView lockedSlot = fixture.Grid.PoolSlots.FirstBound();
            lockedSlot.Button.onClick.Invoke();
            Assert.That(selectionCount, Is.EqualTo(1));
            Assert.That(lockedSlot.Button.interactable, Is.False);
            Assert.That(fixture.Viewport.GetComponent<ScrollRect>().enabled,
                Is.False);

            fixture.Grid.SetInteractionEnabled(true);
            Assert.That(lockedSlot.Button.interactable, Is.True);
            Assert.That(fixture.Viewport.GetComponent<ScrollRect>().enabled,
                Is.True);
        }

        [UnityTest]
        public IEnumerator BoundSlotsRemainVisibleAfterInactiveRootActivation()
        {
            Fixture fixture = CreateFixture(
                characterCount: 11,
                bindWhileInactive: true);
            Assert.That(fixture.Root.activeInHierarchy, Is.False);
            Assert.That(fixture.Grid.ItemCount, Is.EqualTo(11));

            fixture.Root.SetActive(true);
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(fixture.Grid.VisibleKind,
                Is.EqualTo(ExchangeContentKind.Character));
            Assert.That(fixture.Grid.IsAtTop, Is.True);
            Assert.That(fixture.Grid.BoundItemCount, Is.GreaterThan(0));
            Assert.That(fixture.Grid.PoolSlots.Count(slot =>
                slot.IsBound && slot.gameObject.activeSelf),
                Is.EqualTo(fixture.Grid.BoundItemCount));
        }

        [UnityTest]
        public IEnumerator EmptyGridRemainsSafeAfterInactiveRootActivation()
        {
            Fixture fixture = CreateFixture(
                characterCount: 0,
                bindWhileInactive: true);

            fixture.Root.SetActive(true);
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(fixture.Grid.VisibleKind,
                Is.EqualTo(ExchangeContentKind.Character));
            Assert.That(fixture.Grid.ItemCount, Is.Zero);
            Assert.That(fixture.Grid.BoundItemCount, Is.Zero);
            Assert.That(fixture.Grid.PoolSlots.Any(slot => slot.IsBound),
                Is.False);
            Assert.That(fixture.Grid.IsAtTop, Is.True);
        }

        private Fixture CreateFixture(
            int characterCount,
            bool bindWhileInactive = false)
        {
            var canvasRoot = new GameObject(
                "ExchangeInteractionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            created.Add(canvasRoot);
            if (bindWhileInactive)
                canvasRoot.SetActive(false);
            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            EventSystem eventSystem = UnityEngine.Object
                .FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventRoot = new GameObject(
                    "ExchangeInteractionEventSystem",
                    typeof(EventSystem));
                created.Add(eventRoot);
                eventSystem = eventRoot.GetComponent<EventSystem>();
            }
            eventSystem.pixelDragThreshold = 10;

            RectTransform viewport = Rect("ScrollPanel", canvasRoot.transform);
            viewport.sizeDelta = new Vector2(1080f, 300f);
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            RectTransform content = Rect("Slots", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            RectTransform background = Rect("SlotsPanel", content);
            background.gameObject.AddComponent<Image>().raycastTarget = false;
            var slots = new ExchangeSlotView[10];
            for (int index = 0; index < slots.Length; index++)
                slots[index] = Slot(content, index);

            var grid = viewport.gameObject.AddComponent<
                VirtualizedExchangeGridView>();
            grid.Configure(
                scrollRect,
                viewport,
                content,
                background,
                slots,
                new Vector2(100f, 100f),
                new Vector2(430f, 100f),
                0f,
                0f,
                0f);

            TestData data = CreateData(characterCount);
            ProfileSaveData profile = new NewProfileFactory().Create(
                "exchange_interaction",
                1L);
            profile.Currencies.HeroTokens = 1000L;
            grid.BindCharacters(
                ExchangeCatalogBuilder.Build(
                    data.Database,
                    data.Catalog,
                    profile),
                data.Resolver,
                resetScroll: true);
            return new Fixture(canvasRoot, viewport, grid, eventSystem);
        }

        private TestData CreateData(int count)
        {
            Sprite face = CreateSprite();
            var definitions = new CharacterDefinition[count];
            var presentations = new CharacterPresentationDefinition[count];
            var ids = new string[count];
            for (int index = 0; index < count; index++)
            {
                string id = $"character_{index:D3}";
                ids[index] = id;
                CharacterDefinition definition = ScriptableObject
                    .CreateInstance<CharacterDefinition>();
                created.Add(definition);
                SetField(definition, "id", id);
                definitions[index] = definition;
                CharacterPresentationDefinition presentation =
                    ScriptableObject.CreateInstance<
                        CharacterPresentationDefinition>();
                created.Add(presentation);
                SetField(presentation, "characterId", id);
                SetField(presentation, "faceSprite", face);
                presentations[index] = presentation;
            }

            var presentationCatalog = ScriptableObject
                .CreateInstance<CharacterPresentationCatalog>();
            created.Add(presentationCatalog);
            SetField(presentationCatalog, "definitions", presentations);
            presentationCatalog.Initialize();
            var catalog = ScriptableObject
                .CreateInstance<ExchangeCatalogDefinition>();
            created.Add(catalog);
            SetField(catalog, "characterIds", ids);
            var database = ScriptableObject
                .CreateInstance<DefinitionDatabase>();
            created.Add(database);
            SetField(database, "characters", definitions);
            SetField(database, "exchangeCatalog", catalog);
            database.Initialize();
            return new TestData(
                database,
                catalog,
                new ExchangeSpriteResolver(
                    presentationCatalog,
                    database,
                    face,
                    null));
        }

        private ExchangeSlotView Slot(RectTransform content, int index)
        {
            RectTransform root = Rect($"Slot ({index})", content);
            Image face = Image("Button", root);
            var button = face.gameObject.AddComponent<Button>();
            GameObject awakening = Image("AwakeningIcon", root).gameObject;
            TMP_Text awakeningText = Text("AwakeningText", root);
            Image price = Image("PriceIcon", root);
            TMP_Text priceText = Text("PriceText", root);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            var slot = root.gameObject.AddComponent<ExchangeSlotView>();
            slot.Configure(
                button,
                face,
                awakening,
                awakeningText,
                price,
                priceText,
                group);
            return slot;
        }

        private Sprite CreateSprite()
        {
            var texture = new Texture2D(2, 2);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            created.Add(texture);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            created.Add(sprite);
            return sprite;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static Image Image(string name, Transform parent)
        {
            return Rect(name, parent).gameObject.AddComponent<Image>();
        }

        private static TMP_Text Text(string name, Transform parent)
        {
            return Rect(name, parent).gameObject
                .AddComponent<TextMeshProUGUI>();
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance
                        | BindingFlags.NonPublic
                        | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(
                target.GetType().FullName,
                fieldName);
        }

        private static void SetScrollOffset(
            VirtualizedExchangeGridView grid,
            RectTransform viewport,
            float offset)
        {
            RectTransform content = viewport.GetComponent<ScrollRect>()
                .content;
            float maximum = Mathf.Max(
                0f,
                content.rect.height - viewport.rect.height);
            Vector2 position = content.anchoredPosition;
            position.y = Mathf.Clamp(offset, 0f, maximum);
            content.anchoredPosition = position;
            grid.RefreshCurrent(resetScroll: false);
        }

        private sealed class TestData
        {
            public TestData(
                DefinitionDatabase database,
                ExchangeCatalogDefinition catalog,
                ExchangeSpriteResolver resolver)
            {
                Database = database;
                Catalog = catalog;
                Resolver = resolver;
            }

            public DefinitionDatabase Database { get; }
            public ExchangeCatalogDefinition Catalog { get; }
            public ExchangeSpriteResolver Resolver { get; }
        }

        private sealed class Fixture
        {
            public Fixture(
                GameObject root,
                RectTransform viewport,
                VirtualizedExchangeGridView grid,
                EventSystem eventSystem)
            {
                Root = root;
                Viewport = viewport;
                Grid = grid;
                EventSystem = eventSystem;
            }

            public GameObject Root { get; }
            public RectTransform Viewport { get; }
            public VirtualizedExchangeGridView Grid { get; }
            public EventSystem EventSystem { get; }
        }
    }

    internal static class ExchangeSlotViewListExtensions
    {
        public static ExchangeSlotView FirstBound(
            this IReadOnlyList<ExchangeSlotView> slots)
        {
            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index].IsBound)
                    return slots[index];
            }

            throw new InvalidOperationException(
                "No bound Exchange slot was found.");
        }

        public static bool ExistsBound(
            this IReadOnlyList<ExchangeSlotView> slots,
            string contentId)
        {
            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index].IsBound
                    && string.Equals(
                        slots[index].ContentId,
                        contentId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
