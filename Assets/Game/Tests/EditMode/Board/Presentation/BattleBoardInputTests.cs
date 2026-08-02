using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BattleBoardInputTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private EventSystem eventSystem;
        private RectTransform boardRect;
        private BattleBoardView boardView;
        private BattleBoardController controller;
        private BattleBoardInput input;

        [SetUp]
        public void SetUp()
        {
            var bootstrapObject = new GameObject("TestBootstrapper");
            bootstrapObject.SetActive(false);
            createdObjects.Add(bootstrapObject);
            var bootstrapper =
                bootstrapObject.AddComponent<GameBootstrapper>();
            SetProperty(
                bootstrapper,
                "RandomSource",
                new SeededRandomSource(48271));
            SetStaticProperty(
                typeof(GameBootstrapper),
                "Instance",
                bootstrapper);

            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem));
            createdObjects.Add(eventSystemObject);
            eventSystem = eventSystemObject.GetComponent<EventSystem>();

            var canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            createdObjects.Add(canvasObject);
            canvasObject.GetComponent<Canvas>().renderMode =
                RenderMode.ScreenSpaceOverlay;

            var boardObject = new GameObject(
                "Blocks",
                typeof(RectTransform));
            boardObject.transform.SetParent(
                canvasObject.transform,
                false);
            boardRect = boardObject.GetComponent<RectTransform>();
            boardRect.sizeDelta = new Vector2(1080f, 900f);

            Sprite sprite = CreateSprite();
            var spriteSet = ScriptableObject.CreateInstance<
                BoardElementSpriteSet>();
            spriteSet.Configure(sprite, sprite, sprite, sprite, sprite);
            createdObjects.Add(spriteSet);

            var prefabObject = new GameObject(
                "BlockViewTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(BlockView));
            createdObjects.Add(prefabObject);
            prefabObject.GetComponent<BlockView>().Configure(
                prefabObject.GetComponent<Image>());
            prefabObject.SetActive(false);

            var pool = boardObject.AddComponent<BlockViewPool>();
            pool.Configure(
                prefabObject.GetComponent<BlockView>(),
                boardRect,
                BoardConstants.CellCount);
            boardView = boardObject.AddComponent<BattleBoardView>();
            boardView.Configure(spriteSet, pool);

            controller = boardObject.AddComponent<BattleBoardController>();
            SetField(controller, "boardView", boardView);
            SetAllDurationsToZero();

            input = boardObject.AddComponent<BattleBoardInput>();
            input.Configure(controller, boardRect, 54f);
        }

        [TearDown]
        public void TearDown()
        {
            SetStaticProperty(typeof(GameBootstrapper), "Instance", null);
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void BeginDrag_ControllerNotReadyDoesNotTrack()
        {
            var eventData = CreatePointerEvent(
                1,
                boardRect.gameObject,
                Vector2.zero);

            input.OnBeginDrag(eventData);

            Assert.That(input.IsTrackingPointer, Is.False);
            Assert.That(controller.LastSwapActionResult, Is.Null);
        }

        [UnityTest]
        public IEnumerator BeginDrag_NonBlockTargetDoesNotTrack()
        {
            yield return InitializeReadyBoard();
            var eventData = CreatePointerEvent(
                1,
                boardRect.gameObject,
                Vector2.zero);

            input.OnBeginDrag(eventData);

            Assert.That(input.IsTrackingPointer, Is.False);
        }

        [UnityTest]
        public IEnumerator BeginDrag_RuntimeIdMismatchDoesNotTrack()
        {
            yield return InitializeReadyBoard();
            BlockView view = GetView(new BoardPosition(2, 2));
            BoardBlock block = controller.CurrentBoard.Get(view.Position);
            view.Bind(
                new BoardBlock(
                    block.RuntimeId + 1000,
                    block.BlockType,
                    block.Element),
                view.Position,
                view.Image.sprite);
            PointerEventData eventData = CreatePointerEvent(
                1,
                view.gameObject,
                GetScreenPoint(view));

            input.OnBeginDrag(eventData);

            Assert.That(input.IsTrackingPointer, Is.False);
        }

        [UnityTest]
        public IEnumerator ValidBeginAndDragTrackOnePointerWithoutMovingView()
        {
            yield return InitializeReadyBoard();
            BlockView view = GetView(new BoardPosition(2, 2));
            Vector2 originalPosition = view.RectTransform.anchoredPosition;
            Vector2 screenPoint = GetScreenPoint(view);
            PointerEventData first = CreatePointerEvent(
                4,
                view.gameObject,
                screenPoint);
            PointerEventData second = CreatePointerEvent(
                5,
                view.gameObject,
                screenPoint + Vector2.right * 80f);

            input.OnBeginDrag(first);
            input.OnBeginDrag(second);
            input.OnDrag(second);

            Assert.That(input.IsTrackingPointer, Is.True);
            Assert.That(input.TrackedPointerId, Is.EqualTo(4));
            Assert.That(view.RectTransform.anchoredPosition,
                Is.EqualTo(originalPosition));

            input.OnEndDrag(second);
            Assert.That(input.IsTrackingPointer, Is.True);
            Assert.That(controller.LastSwapActionResult, Is.Null);
        }

        [UnityTest]
        public IEnumerator EndDrag_BelowDistanceOrOutsideBoardDoesNotRequest()
        {
            yield return InitializeReadyBoard();
            BlockView middle = GetView(new BoardPosition(2, 2));
            PerformDrag(middle, 1, new Vector2(53.9f, 0f));
            Assert.That(controller.LastSwapActionResult, Is.Null);

            BlockView edge = GetView(new BoardPosition(5, 2));
            PerformDrag(edge, 2, new Vector2(54f, 0f));
            Assert.That(controller.LastSwapActionResult, Is.Null);
        }

        [UnityTest]
        public IEnumerator Drag_ControllerBecomesUnavailableCancelsGesture()
        {
            yield return InitializeReadyBoard();
            BlockView view = GetView(new BoardPosition(2, 2));
            Vector2 point = GetScreenPoint(view);
            PointerEventData eventData = CreatePointerEvent(
                3,
                view.gameObject,
                point);
            input.OnBeginDrag(eventData);
            SetProperty(controller, "IsBoardReady", false);
            eventData.position = point + Vector2.right * 80f;

            input.OnDrag(eventData);
            input.OnEndDrag(eventData);

            Assert.That(input.IsTrackingPointer, Is.False);
            Assert.That(controller.LastSwapActionResult, Is.Null);
        }

        [UnityTest]
        public IEnumerator EndDrag_RequestsExactAdjacentSwapOnlyOnce()
        {
            yield return InitializeReadyBoard();
            var start = new BoardPosition(2, 2);
            BlockView view = GetView(start);

            PerformDrag(view, 7, new Vector2(80f, 30f));

            Assert.That(controller.LastSwapActionResult, Is.Not.Null);
            Assert.That(controller.LastSwapActionResult.Swap.First,
                Is.EqualTo(start));
            Assert.That(controller.LastSwapActionResult.Swap.Second,
                Is.EqualTo(new BoardPosition(3, 2)));
            BoardSwapActionResult result = controller.LastSwapActionResult;
            PointerEventData repeatedEnd = CreatePointerEvent(
                7,
                view.gameObject,
                GetScreenPoint(view) + Vector2.right * 80f);
            input.OnEndDrag(repeatedEnd);
            Assert.That(controller.LastSwapActionResult, Is.SameAs(result));
        }

        [UnityTest]
        public IEnumerator EndDrag_RevalidatesRuntimeIdBeforeRequest()
        {
            yield return InitializeReadyBoard();
            BlockView view = GetView(new BoardPosition(2, 2));
            Vector2 point = GetScreenPoint(view);
            PointerEventData eventData = CreatePointerEvent(
                9,
                view.gameObject,
                point);
            input.OnBeginDrag(eventData);
            view.SetLogicalPosition(new BoardPosition(1, 2));
            eventData.position = point + Vector2.right * 80f;

            input.OnEndDrag(eventData);

            Assert.That(input.IsTrackingPointer, Is.False);
            Assert.That(controller.LastSwapActionResult, Is.Null);
        }

        [UnityTest]
        public IEnumerator DisableCancelsAndCanceledGestureCanBeReused()
        {
            yield return InitializeReadyBoard();
            BlockView view = GetView(new BoardPosition(2, 2));
            Vector2 point = GetScreenPoint(view);
            PointerEventData first = CreatePointerEvent(
                10,
                view.gameObject,
                point);
            input.OnBeginDrag(first);
            Assert.That(input.IsTrackingPointer, Is.True);
            Assert.That(input.TrackedPointerId, Is.EqualTo(10));

            InvokePrivate(input, "OnDisable");

            Assert.That(input.IsTrackingPointer, Is.False);
            Assert.That(input.TrackedPointerId, Is.Zero);

            PointerEventData second = CreatePointerEvent(
                11,
                view.gameObject,
                point);
            input.OnBeginDrag(second);
            Assert.That(input.IsTrackingPointer, Is.True);
            Assert.That(input.TrackedPointerId, Is.EqualTo(11));
            second.position = point + Vector2.up * 20f;
            input.OnEndDrag(second);
            Assert.That(input.IsTrackingPointer, Is.False);
            Assert.That(controller.LastSwapActionResult, Is.Null);

            PointerEventData third = CreatePointerEvent(
                12,
                view.gameObject,
                point);
            input.OnBeginDrag(third);
            Assert.That(input.IsTrackingPointer, Is.True);
        }

        [Test]
        public void Configure_RejectsMissingReferencesAndInvalidDistance()
        {
            Assert.Throws<ArgumentNullException>(() =>
                input.Configure(null, boardRect, 54f));
            Assert.Throws<ArgumentNullException>(() =>
                input.Configure(controller, null, 54f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                input.Configure(controller, boardRect, 0f));
        }

        private IEnumerator InitializeReadyBoard()
        {
            controller.Initialize();
            yield return null;
            Assert.That(controller.CanAcceptBoardInput, Is.True);
        }

        private void PerformDrag(
            BlockView view,
            int pointerId,
            Vector2 localDelta)
        {
            Vector2 start = GetScreenPoint(view);
            PointerEventData eventData = CreatePointerEvent(
                pointerId,
                view.gameObject,
                start);
            input.OnBeginDrag(eventData);
            eventData.position = start + localDelta;
            input.OnDrag(eventData);
            input.OnEndDrag(eventData);
        }

        private PointerEventData CreatePointerEvent(
            int pointerId,
            GameObject target,
            Vector2 position)
        {
            var eventData = new PointerEventData(eventSystem)
            {
                pointerId = pointerId,
                position = position,
                pointerPress = target,
                pointerPressRaycast = new RaycastResult
                {
                    gameObject = target,
                },
            };
            return eventData;
        }

        private BlockView GetView(BoardPosition position)
        {
            long runtimeId = controller.CurrentBoard.Get(position).RuntimeId;
            Assert.That(
                boardView.TryGetView(runtimeId, out BlockView view),
                Is.True);
            return view;
        }

        private static Vector2 GetScreenPoint(BlockView view)
        {
            return RectTransformUtility.WorldToScreenPoint(
                null,
                view.RectTransform.position);
        }

        private Sprite CreateSprite()
        {
            var texture = new Texture2D(2, 2);
            createdObjects.Add(texture);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            createdObjects.Add(sprite);
            return sprite;
        }

        private void SetAllDurationsToZero()
        {
            SetField(controller, "initialDropDuration", 0f);
            SetField(controller, "initialDropColumnStagger", 0f);
            SetField(controller, "swapDuration", 0f);
            SetField(controller, "removalDuration", 0f);
            SetField(controller, "collapseDuration", 0f);
            SetField(controller, "refillDuration", 0f);
            SetField(controller, "shuffleDuration", 0f);
        }

        private static void SetField(
            object target,
            string name,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static void SetProperty(
            object target,
            string name,
            object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                name,
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, name);
            property.SetValue(target, value);
        }

        private static void SetStaticProperty(
            Type type,
            string name,
            object value)
        {
            PropertyInfo property = type.GetProperty(
                name,
                BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, name);
            property.SetValue(null, value);
        }
    }
}
