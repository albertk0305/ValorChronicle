using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Party.Roster;
using Object = UnityEngine.Object;

namespace ValorChronicle.Tests.PlayMode
{
    public sealed class CharacterRosterInteractionPlayModeTests
    {
        private readonly List<GameObject> roots = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int index = roots.Count - 1; index >= 0; index--)
            {
                if (roots[index] != null)
                {
                    Object.Destroy(roots[index]);
                }
            }

            roots.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CellRaycastClickAndDrag_UseButtonAboveScrollOverlay()
        {
            Fixture fixture = CreateFixture();
            yield return null;
            Canvas.ForceUpdateCanvases();

            fixture.Grid.Configure(
                fixture.ScrollRect,
                fixture.Viewport,
                fixture.Content,
                fixture.InitialCells,
                new Vector2(200f, 200f),
                new Vector2(15f, 65f));
            fixture.Grid.Configure(
                fixture.ScrollRect,
                fixture.Viewport,
                fixture.Content,
                fixture.InitialCells,
                new Vector2(200f, 200f),
                new Vector2(15f, 65f));
            fixture.Grid.SetItems(
                Roster(100),
                string.Empty,
                _ => null,
                _ => null,
                resetScroll: true);
            Assert.That(fixture.Grid.ContentHeight,
                Is.GreaterThan(fixture.Viewport.rect.height));
            Canvas.ForceUpdateCanvases();

            CharacterRosterCellView cell = fixture.Grid.PoolCells.First(
                item => item.CharacterId == "character_002");
            cell.CharacterImage.color = Color.white;
            Canvas.ForceUpdateCanvases();
            Assert.That(cell.SelectedBorder.activeSelf, Is.True);
            Assert.That(cell.CharacterId, Is.EqualTo("character_002"));
            Bounds firstRowBounds = RectTransformUtility
                .CalculateRelativeRectTransformBounds(
                    fixture.Viewport,
                    cell.transform);
            Assert.That(
                firstRowBounds.max.y,
                Is.LessThanOrEqualTo(fixture.Viewport.rect.yMax + 0.01f));

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                null,
                cell.Button.transform.position);
            var pointer = new PointerEventData(fixture.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = screenPoint,
                pressPosition = screenPoint
            };
            var raycasts = new List<RaycastResult>();
            fixture.Raycaster.Raycast(pointer, raycasts);

            Assert.That(raycasts, Is.Not.Empty);
            Assert.That(
                raycasts[0].gameObject,
                Is.EqualTo(cell.Button.gameObject),
                "The behind-content ScrollPanel must not intercept a cell click.");
            Assert.That(
                raycasts.Any(result =>
                    result.gameObject == fixture.ScrollOverlay.gameObject),
                Is.True,
                "The scroll overlay remains available behind cells.");

            int clickCount = 0;
            string requestedId = string.Empty;
            fixture.Grid.CharacterRequested += characterId =>
            {
                clickCount++;
                requestedId = characterId;
            };

            ExecuteEvents.ExecuteHierarchy(
                raycasts[0].gameObject,
                pointer,
                ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(
                raycasts[0].gameObject,
                pointer,
                ExecuteEvents.pointerUpHandler);
            GameObject clickHandler = ExecuteEvents.GetEventHandler<
                IPointerClickHandler>(raycasts[0].gameObject);
            ExecuteEvents.Execute(
                clickHandler,
                pointer,
                ExecuteEvents.pointerClickHandler);

            Assert.That(clickCount, Is.EqualTo(1));
            Assert.That(requestedId, Is.EqualTo("character_002"));

            float offsetBeforeDrag = fixture.Grid.ScrollOffset;
            var drag = new PointerEventData(fixture.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = screenPoint,
                pressPosition = screenPoint
            };
            GameObject dragHandler = ExecuteEvents.GetEventHandler<
                IDragHandler>(raycasts[0].gameObject);
            Assert.That(
                dragHandler,
                Is.EqualTo(fixture.Viewport.gameObject));
            drag.pointerDrag = dragHandler;
            ExecuteEvents.ExecuteHierarchy(
                raycasts[0].gameObject,
                drag,
                ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(
                dragHandler,
                drag,
                ExecuteEvents.beginDragHandler);
            drag.delta = new Vector2(0f, -250f);
            drag.position += drag.delta;
            drag.dragging = true;
            drag.eligibleForClick = false;
            ExecuteEvents.Execute(
                dragHandler,
                drag,
                ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(
                dragHandler,
                drag,
                ExecuteEvents.endDragHandler);
            ExecuteEvents.ExecuteHierarchy(
                raycasts[0].gameObject,
                drag,
                ExecuteEvents.pointerUpHandler);

            Assert.That(
                fixture.Grid.ScrollOffset,
                Is.GreaterThan(offsetBeforeDrag));
            Assert.That(clickCount, Is.EqualTo(1));

            fixture.Grid.SetScrollOffsetForTesting(0f);
            Drag(fixture, fixture.ScrollOverlay.gameObject, -250f);
            Assert.That(fixture.Grid.ScrollOffset, Is.GreaterThan(0f));
            Assert.That(clickCount, Is.EqualTo(1));
            fixture.Grid.SetScrollOffsetForTesting(float.MaxValue);
            Assert.That(fixture.Grid.PoolCells.Any(item =>
                item.IsBound
                && item.CharacterId == "character_099"), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SingleItemRoster_IsClampedForCharacterAndParty()
        {
            Fixture[] fixtures =
            {
                CreateFixture(),
                CreatePartyFixture()
            };
            yield return null;

            for (int index = 0; index < fixtures.Length; index++)
            {
                Fixture fixture = fixtures[index];
                fixture.Grid.SetItems(
                    Roster(1),
                    string.Empty,
                    _ => null,
                    _ => null,
                    resetScroll: true);
                Canvas.ForceUpdateCanvases();
                AssertCommonScrollPolicy(fixture.ScrollRect);
                Assert.That(fixture.Grid.ContentHeight,
                    Is.LessThanOrEqualTo(
                        fixture.Viewport.rect.height + 0.01f));

                CharacterRosterCellView cell = fixture.Grid.PoolCells
                    .Single(item => item.IsBound);
                Vector2 before = fixture.Content.anchoredPosition;
                Drag(fixture, cell.Button.gameObject, -250f);
                Assert.That(fixture.Grid.ScrollOffset, Is.Zero);
                Assert.That(fixture.Content.anchoredPosition,
                    Is.EqualTo(before));

                Drag(fixture, fixture.ScrollOverlay.gameObject, -250f);
                Assert.That(fixture.Grid.ScrollOffset, Is.Zero);
                Assert.That(fixture.Content.anchoredPosition,
                    Is.EqualTo(before));

                int clickCount = 0;
                string clickedId = string.Empty;
                fixture.Grid.CharacterRequested += characterId =>
                {
                    clickCount++;
                    clickedId = characterId;
                };
                cell.Button.onClick.Invoke();
                Assert.That(clickCount, Is.EqualTo(1));
                Assert.That(clickedId, Is.EqualTo("character_000"));
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RuntimeAddThenConfigure_DoesNotLogIncompleteWarnings()
        {
            Fixture fixture = CreateFixture();

            yield return null;

            Assert.That(fixture.InitialCells[0].IsConfigured, Is.True);
            Assert.That(fixture.Grid.IsConfigured, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PartyStyleViewport_CellClickAndDragBothReachTargets()
        {
            Fixture fixture = CreatePartyFixture();
            yield return null;

            fixture.Grid.SetItems(
                Roster(100),
                string.Empty,
                _ => null,
                _ => null,
                resetScroll: true);
            Assert.That(fixture.Grid.ContentHeight,
                Is.GreaterThan(fixture.Viewport.rect.height));
            CharacterRosterCellView cell = fixture.Grid.PoolCells.First(
                item => item.CharacterId == "character_002");
            cell.SetAlwaysShowBorder(true);
            cell.CharacterImage.color = Color.white;
            Canvas.ForceUpdateCanvases();

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                null,
                cell.Button.transform.position);
            var pointer = new PointerEventData(fixture.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = screenPoint,
                pressPosition = screenPoint
            };
            var raycasts = new List<RaycastResult>();
            fixture.Raycaster.Raycast(pointer, raycasts);
            Assert.That(raycasts, Is.Not.Empty);
            Assert.That(raycasts[0].gameObject,
                Is.EqualTo(cell.Button.gameObject));

            int clickCount = 0;
            string clickedId = string.Empty;
            fixture.Grid.CharacterRequested += characterId =>
            {
                clickCount++;
                clickedId = characterId;
            };
            ExecuteEvents.ExecuteHierarchy(
                cell.Button.gameObject,
                pointer,
                ExecuteEvents.pointerClickHandler);
            Assert.That(clickCount, Is.EqualTo(1));
            Assert.That(clickedId, Is.EqualTo("character_002"));

            float offsetBefore = fixture.Grid.ScrollOffset;
            var drag = new PointerEventData(fixture.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = screenPoint,
                pressPosition = screenPoint,
                eligibleForClick = false
            };
            GameObject dragHandler = ExecuteEvents.GetEventHandler<
                IDragHandler>(cell.Button.gameObject);
            Assert.That(dragHandler,
                Is.EqualTo(fixture.Viewport.gameObject));
            drag.pointerDrag = dragHandler;
            ExecuteEvents.ExecuteHierarchy(
                cell.Button.gameObject,
                drag,
                ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(
                dragHandler,
                drag,
                ExecuteEvents.beginDragHandler);
            drag.delta = new Vector2(0f, -250f);
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
                cell.Button.gameObject,
                drag,
                ExecuteEvents.pointerUpHandler);

            Assert.That(fixture.Grid.ScrollOffset,
                Is.GreaterThan(offsetBefore));
            Assert.That(clickCount, Is.EqualTo(1));
            Assert.That(cell.SelectedBorder.activeSelf, Is.True);

            fixture.Grid.SetScrollOffsetForTesting(0f);
            Drag(fixture, fixture.ScrollOverlay.gameObject, -250f);
            Assert.That(fixture.Grid.ScrollOffset, Is.GreaterThan(0f));
            Assert.That(clickCount, Is.EqualTo(1));

            fixture.Grid.SetScrollOffsetForTesting(float.MaxValue);
            Assert.That(fixture.Grid.PoolCells.Any(item =>
                item.IsBound
                && item.CharacterId == "character_099"), Is.True);
            Assert.That(
                fixture.Grid.ScrollOffset,
                Is.EqualTo(
                    fixture.Grid.ContentHeight
                        - fixture.Viewport.rect.height).Within(0.01f));
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertCommonScrollPolicy(ScrollRect scrollRect)
        {
            Assert.That(scrollRect.movementType,
                Is.EqualTo(ScrollRect.MovementType.Clamped));
            Assert.That(scrollRect.elasticity, Is.EqualTo(0.1f));
            Assert.That(scrollRect.inertia, Is.True);
            Assert.That(scrollRect.decelerationRate, Is.EqualTo(0.135f));
            Assert.That(scrollRect.scrollSensitivity, Is.EqualTo(1f));
            Assert.That(scrollRect.horizontal, Is.False);
            Assert.That(scrollRect.vertical, Is.True);
        }

        private static void Drag(
            Fixture fixture,
            GameObject source,
            float verticalDelta)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(
                null,
                source.transform.position);
            var pointer = new PointerEventData(fixture.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = point,
                pressPosition = point,
                eligibleForClick = false
            };
            GameObject dragHandler = ExecuteEvents.GetEventHandler<
                IDragHandler>(source);
            Assert.That(dragHandler,
                Is.EqualTo(fixture.Viewport.gameObject));
            pointer.pointerDrag = dragHandler;
            ExecuteEvents.ExecuteHierarchy(
                source,
                pointer,
                ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(
                dragHandler,
                pointer,
                ExecuteEvents.beginDragHandler);
            pointer.delta = new Vector2(0f, verticalDelta);
            pointer.position += pointer.delta;
            pointer.dragging = true;
            ExecuteEvents.Execute(
                dragHandler,
                pointer,
                ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(
                dragHandler,
                pointer,
                ExecuteEvents.endDragHandler);
            ExecuteEvents.ExecuteHierarchy(
                source,
                pointer,
                ExecuteEvents.pointerUpHandler);
        }

        private Fixture CreateFixture()
        {
            var canvasRoot = new GameObject(
                "CharacterRosterInteractionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            roots.Add(canvasRoot);
            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            GraphicRaycaster raycaster = canvasRoot.GetComponent<
                GraphicRaycaster>();

            var eventSystemRoot = new GameObject(
                "CharacterRosterInteractionEventSystem",
                typeof(EventSystem));
            roots.Add(eventSystemRoot);
            EventSystem eventSystem = eventSystemRoot.GetComponent<
                EventSystem>();

            RectTransform viewport = Rect("CharacterSlots", canvasRoot.transform);
            viewport.sizeDelta = new Vector2(1070f, 1400f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            Image overlay = Image("ScrollPanel", viewport);
            RectTransform overlayRect = (RectTransform)overlay.transform;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            overlay.raycastTarget = true;

            RectTransform content = Rect("CharacterSelectSlots", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.SetAsLastSibling();
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            CharacterRosterCellView cell = Cell(content);
            var grid = viewport.gameObject.AddComponent<
                VirtualizedCharacterGridView>();
            var cells = new[] { cell };
            grid.Configure(
                scrollRect,
                viewport,
                content,
                cells,
                new Vector2(200f, 200f),
                new Vector2(15f, 65f));

            return new Fixture(
                viewport,
                content,
                overlay,
                scrollRect,
                grid,
                cells,
                eventSystem,
                raycaster);
        }

        private Fixture CreatePartyFixture()
        {
            var canvasRoot = new GameObject(
                "PartyRosterInteractionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            roots.Add(canvasRoot);
            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            GraphicRaycaster raycaster = canvasRoot.GetComponent<
                GraphicRaycaster>();

            var eventSystemRoot = new GameObject(
                "PartyRosterInteractionEventSystem",
                typeof(EventSystem));
            roots.Add(eventSystemRoot);
            EventSystem eventSystem = eventSystemRoot.GetComponent<
                EventSystem>();

            RectTransform viewport = Rect(
                "CharacterScrollPanel",
                canvasRoot.transform);
            viewport.sizeDelta = new Vector2(1080f, 1000f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            RectTransform content = Rect("Characters", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            CharacterRosterCellView cell = Cell(content);
            cell.SetAlwaysShowBorder(true);
            var grid = viewport.gameObject.AddComponent<
                VirtualizedCharacterGridView>();
            var cells = new[] { cell };
            grid.Configure(
                scrollRect,
                viewport,
                content,
                cells,
                new Vector2(200f, 200f),
                new Vector2(10f, 10f));

            return new Fixture(
                viewport,
                content,
                viewportImage,
                scrollRect,
                grid,
                cells,
                eventSystem,
                raycaster);
        }

        private static CharacterRosterCellView Cell(RectTransform content)
        {
            RectTransform root = Rect("CharacterSlot", content);
            RectTransform buttonRoot = Rect("CharacterButton", root);
            buttonRoot.sizeDelta = new Vector2(200f, 200f);
            Image face = buttonRoot.gameObject.AddComponent<Image>();
            var button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            Image type = Image("TypeImage", root);
            type.raycastTarget = false;
            GameObject border = Image("Border", root).gameObject;
            border.GetComponent<Image>().raycastTarget = false;
            var cell = root.gameObject.AddComponent<CharacterRosterCellView>();
            cell.Configure(
                button,
                face,
                type,
                border,
                configuredAlwaysShowBorder: true);
            return cell;
        }

        private static IReadOnlyList<CharacterRosterEntry> Roster(int count)
        {
            var roster = new List<CharacterRosterEntry>(count);
            for (int index = 0; index < count; index++)
            {
                roster.Add(new CharacterRosterEntry(
                    $"character_{index:D3}",
                    ElementType.Water,
                    1,
                    0,
                    1,
                    1,
                    index));
            }

            return roster;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static Image Image(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent);
            return rect.gameObject.AddComponent<Image>();
        }

        private sealed class Fixture
        {
            public Fixture(
                RectTransform viewport,
                RectTransform content,
                Image scrollOverlay,
                ScrollRect scrollRect,
                VirtualizedCharacterGridView grid,
                CharacterRosterCellView[] initialCells,
                EventSystem eventSystem,
                GraphicRaycaster raycaster)
            {
                Viewport = viewport;
                Content = content;
                ScrollOverlay = scrollOverlay;
                ScrollRect = scrollRect;
                Grid = grid;
                InitialCells = initialCells;
                EventSystem = eventSystem;
                Raycaster = raycaster;
            }

            public RectTransform Viewport { get; }
            public RectTransform Content { get; }
            public Image ScrollOverlay { get; }
            public ScrollRect ScrollRect { get; }
            public VirtualizedCharacterGridView Grid { get; }
            public CharacterRosterCellView[] InitialCells { get; }
            public EventSystem EventSystem { get; }
            public GraphicRaycaster Raycaster { get; }
        }
    }
}
