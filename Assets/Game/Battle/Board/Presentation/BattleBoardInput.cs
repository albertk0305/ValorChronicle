using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BattleBoardInput : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField]
        private BattleBoardController controller;

        [SerializeField]
        private RectTransform boardRect;

        [SerializeField]
        private float minimumDragDistance = 54f;

        private bool isTrackingPointer;
        private int trackedPointerId;
        private BoardPosition startPosition;
        private long startRuntimeId;
        private BlockView startView;
        private Vector2 startLocalPoint;

        public bool IsTrackingPointer => isTrackingPointer;
        public int TrackedPointerId => trackedPointerId;

        public void Configure(
            BattleBoardController targetController,
            RectTransform targetBoardRect,
            float dragDistance)
        {
            if (targetController == null)
            {
                throw new ArgumentNullException(nameof(targetController));
            }

            if (targetBoardRect == null)
            {
                throw new ArgumentNullException(nameof(targetBoardRect));
            }

            ValidateMinimumDragDistance(dragDistance);
            controller = targetController;
            boardRect = targetBoardRect;
            minimumDragDistance = dragDistance;
            ResetGesture();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (isTrackingPointer
                || !HasUsableConfiguration()
                || !controller.CanAcceptBoardInput
                || !TryGetPressedBlockView(eventData, out BlockView view)
                || !IsCurrentBoardView(view)
                || !TryGetLocalPoint(eventData, out Vector2 localPoint))
            {
                return;
            }

            isTrackingPointer = true;
            trackedPointerId = eventData.pointerId;
            startPosition = view.Position;
            startRuntimeId = view.RuntimeId;
            startView = view;
            startLocalPoint = localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (!isTrackingPointer || eventData.pointerId != trackedPointerId)
            {
                return;
            }

            if (!controller.CanAcceptBoardInput)
            {
                ResetGesture();
                return;
            }

            if (!TryGetLocalPoint(eventData, out _))
            {
                ResetGesture();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (!isTrackingPointer || eventData.pointerId != trackedPointerId)
            {
                return;
            }

            bool hasEndPoint = TryGetLocalPoint(
                eventData,
                out Vector2 endLocalPoint);
            BoardPosition capturedPosition = startPosition;
            long capturedRuntimeId = startRuntimeId;
            BlockView capturedView = startView;
            Vector2 capturedStartPoint = startLocalPoint;
            ResetGesture();

            if (!hasEndPoint
                || !controller.CanAcceptBoardInput
                || !IsCurrentBoardView(
                    capturedView,
                    capturedPosition,
                    capturedRuntimeId))
            {
                return;
            }

            Vector2 dragDelta = endLocalPoint - capturedStartPoint;
            if (BoardDragSwapPlanner.TryCreateSwap(
                capturedPosition,
                dragDelta,
                minimumDragDistance,
                out BoardSwap swap))
            {
                controller.TryExecuteSwap(swap);
            }
        }

        private void OnDisable()
        {
            ResetGesture();
        }

        private bool HasUsableConfiguration()
        {
            return controller != null
                && boardRect != null
                && minimumDragDistance > 0f
                && !float.IsNaN(minimumDragDistance)
                && !float.IsInfinity(minimumDragDistance);
        }

        private bool TryGetPressedBlockView(
            PointerEventData eventData,
            out BlockView view)
        {
            GameObject pressedObject =
                eventData.pointerPressRaycast.gameObject;
            if (pressedObject == null)
            {
                pressedObject = eventData.pointerPress;
            }

            view = pressedObject != null
                ? pressedObject.GetComponentInParent<BlockView>()
                : null;
            return view != null
                && view.transform.IsChildOf(boardRect);
        }

        private bool IsCurrentBoardView(BlockView view)
        {
            return view != null
                && IsCurrentBoardView(
                    view,
                    view.Position,
                    view.RuntimeId);
        }

        private bool IsCurrentBoardView(
            BlockView view,
            BoardPosition position,
            long runtimeId)
        {
            if (view == null
                || view.Position != position
                || view.RuntimeId != runtimeId
                || controller.CurrentBoard == null)
            {
                return false;
            }

            BoardBlock block = controller.CurrentBoard.Get(position);
            return block != null && block.RuntimeId == runtimeId;
        }

        private bool TryGetLocalPoint(
            PointerEventData eventData,
            out Vector2 localPoint)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                boardRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);
        }

        private void ResetGesture()
        {
            isTrackingPointer = false;
            trackedPointerId = 0;
            startPosition = default(BoardPosition);
            startRuntimeId = 0;
            startView = null;
            startLocalPoint = Vector2.zero;
        }

        private static void ValidateMinimumDragDistance(float value)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Minimum drag distance must be a positive finite value.");
            }
        }
    }
}
