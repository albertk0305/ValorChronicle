using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using ValorChronicle.Core.Logging;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BattleBoardView : MonoBehaviour
    {
        private const float AnchoredPositionTolerance = 0.01f;
        private const float ScaleTolerance = 0.0001f;

        [SerializeField]
        private BoardElementSpriteSet spriteSet;

        [SerializeField]
        private BlockViewPool pool;

        private readonly Dictionary<long, BlockView> viewsByRuntimeId =
            new Dictionary<long, BlockView>();
        private readonly List<RenderItem> renderItems =
            new List<RenderItem>(BoardConstants.CellCount);
        private readonly List<long> obsoleteRuntimeIds =
            new List<long>(BoardConstants.CellCount);
        private readonly BoardInitialDropPlanner initialDropPlanner =
            new BoardInitialDropPlanner();
        private readonly BoardSwapPresentationPlanner swapPlanner =
            new BoardSwapPresentationPlanner();
        private readonly BoardCascadeStepPresentationPlanner cascadePlanner =
            new BoardCascadeStepPresentationPlanner();
        private readonly BoardShufflePresentationPlanner shufflePlanner =
            new BoardShufflePresentationPlanner();
        private readonly List<InitialDropViewItem> initialDropViewItems =
            new List<InitialDropViewItem>(BoardConstants.CellCount);
        private readonly List<SwapViewItem> swapViewItems =
            new List<SwapViewItem>(2);
        private readonly List<RemovalViewItem> removalViewItems =
            new List<RemovalViewItem>(BoardConstants.CellCount);
        private readonly List<MoveViewItem> moveViewItems =
            new List<MoveViewItem>(BoardConstants.CellCount);
        private readonly List<SpawnViewItem> spawnViewItems =
            new List<SpawnViewItem>(BoardConstants.CellCount);
        private readonly List<ShuffleViewItem> shuffleViewItems =
            new List<ShuffleViewItem>(BoardConstants.CellCount);
        private readonly List<InputStateItem> inputStateItems =
            new List<InputStateItem>(BoardConstants.CellCount);
        private IReadOnlyDictionary<long, BlockView> readOnlyViews;
        private ActiveAnimation activeAnimation;
        private bool activeSwapRequiresBack;
        private bool activeSpawnInputEnabled;
        private BoardCascadeStepPresentationPlan activeCascadePlan;
        private BoardShufflePresentationPlan activeShufflePlan;
        private BoardState activeActionFinalBoard;
        private int animationVersion;

        public int ActiveViewCount => viewsByRuntimeId.Count;
        public bool IsAnimating { get; private set; }

        public IReadOnlyDictionary<long, BlockView> ActiveViews
        {
            get
            {
                if (readOnlyViews == null)
                {
                    readOnlyViews =
                        new ReadOnlyDictionary<long, BlockView>(
                            viewsByRuntimeId);
                }

                return readOnlyViews;
            }
        }

        public void Configure(
            BoardElementSpriteSet elementSpriteSet,
            BlockViewPool viewPool)
        {
            spriteSet = elementSpriteSet
                ?? throw new ArgumentNullException(nameof(elementSpriteSet));
            pool = viewPool
                ?? throw new ArgumentNullException(nameof(viewPool));
        }

        public void Render(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (IsAnimating)
            {
                throw new InvalidOperationException(
                    "The board cannot render while an animation is active.");
            }

            ValidateDependencies();
            CollectRenderItems(board);
            ReleaseObsoleteViews();

            for (int index = 0; index < renderItems.Count; index++)
            {
                RenderItem item = renderItems[index];
                if (!viewsByRuntimeId.TryGetValue(
                    item.Block.RuntimeId,
                    out BlockView view))
                {
                    view = pool.Acquire();
                    viewsByRuntimeId.Add(item.Block.RuntimeId, view);
                }

                view.Bind(item.Block, item.Position, item.Sprite);
                view.SetInputEnabled(true);
            }
        }

        public IEnumerator RenderInitialDrop(
            BoardState board,
            float duration,
            float columnStagger)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (duration < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    duration,
                    "Duration cannot be negative.");
            }

            if (columnStagger < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(columnStagger),
                    columnStagger,
                    "Column stagger cannot be negative.");
            }

            if (IsAnimating)
            {
                throw new InvalidOperationException(
                    "An initial board drop is already active.");
            }

            ValidateDependencies();
            IReadOnlyList<BoardInitialDropEntry> entries =
                initialDropPlanner.Build(board);
            CollectRenderItems(board);
            ReleaseObsoleteViews();
            PrepareInitialDropViews(entries);
            ValidateViewDictionaryIntegrity(
                board,
                "InitialDropPrepared",
                true);
            activeAnimation = ActiveAnimation.InitialDrop;
            IsAnimating = true;
            int version = ++animationVersion;

            return AnimateInitialDrop(duration, columnStagger, version);
        }

        public IEnumerator PlaySwapTransition(
            BoardState beforeBoard,
            BoardSwapActionResult result,
            float duration)
        {
            if (beforeBoard == null)
            {
                throw new ArgumentNullException(nameof(beforeBoard));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (duration < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    duration,
                    "Duration cannot be negative.");
            }

            if (IsAnimating)
            {
                throw new InvalidOperationException(
                    "A board animation is already active.");
            }

            BoardSwapPresentationPlan plan = swapPlanner.Build(
                beforeBoard,
                result);
            ValidateCurrentViewLayout(
                beforeBoard,
                "PlaySwapTransition.beforeBoard");

            if (plan.Motions.Count == 0)
            {
                return EmptyTransition();
            }

            PrepareSwapViews(plan);
            CaptureAndDisableInput();
            activeSwapRequiresBack = plan.RequiresSwapBack;
            activeAnimation = ActiveAnimation.Swap;
            IsAnimating = true;
            int version = ++animationVersion;

            return AnimateSwap(
                plan.RequiresSwapBack,
                result.SwappedBoard,
                plan.RequiresSwapBack
                    ? result.Board
                    : result.SwappedBoard,
                duration,
                version);
        }

        public IEnumerator PlayCascadeStep(
            BoardState beforeBoard,
            BoardCascadeStep step,
            float removalDuration,
            float collapseDuration,
            float refillDuration)
        {
            if (beforeBoard == null)
            {
                throw new ArgumentNullException(nameof(beforeBoard));
            }

            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            ValidateDuration(removalDuration, nameof(removalDuration));
            ValidateDuration(collapseDuration, nameof(collapseDuration));
            ValidateDuration(refillDuration, nameof(refillDuration));

            if (IsAnimating)
            {
                throw new InvalidOperationException(
                    "A board animation is already active.");
            }

            ValidateDependencies();
            BoardCascadeStepPresentationPlan plan = cascadePlanner.Build(
                beforeBoard,
                step);
            ValidateCurrentViewLayout(
                beforeBoard,
                "PlayCascadeStep.beforeBoard");
            PrepareCascadeViews(plan);
            activeSpawnInputEnabled = HasAnyInputEnabled();
            CaptureAndDisableInput();
            activeCascadePlan = plan;
            activeAnimation = ActiveAnimation.CascadeStep;
            IsAnimating = true;
            int version = ++animationVersion;

            return AnimateCascadeStep(
                plan,
                removalDuration,
                collapseDuration,
                refillDuration,
                version,
                "CascadeStep[0]");
        }

        public IEnumerator PlayShuffleTransition(
            BoardState beforeBoard,
            BoardShuffleResult result,
            float duration)
        {
            if (beforeBoard == null)
            {
                throw new ArgumentNullException(nameof(beforeBoard));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            ValidateDuration(duration, nameof(duration));
            if (IsAnimating)
            {
                throw new InvalidOperationException(
                    "A board animation is already active.");
            }

            ValidateDependencies();
            BoardShufflePresentationPlan plan = shufflePlanner.Build(
                beforeBoard,
                result);
            ValidateCurrentViewLayout(
                beforeBoard,
                "PlayShuffleTransition.beforeBoard");
            if (!plan.HasAnimation)
            {
                return EmptyTransition();
            }

            PrepareShuffleViews(plan);
            CaptureAndDisableInput();
            activeShufflePlan = plan;
            activeAnimation = ActiveAnimation.Shuffle;
            IsAnimating = true;
            int version = ++animationVersion;
            return AnimateShuffle(
                plan,
                duration,
                version,
                "Shuffle");
        }

        public IEnumerator PlaySwapActionSequence(
            BoardState beforeBoard,
            BoardSwapActionResult result,
            BoardActionPresentationTimings timings)
        {
            if (beforeBoard == null)
            {
                throw new ArgumentNullException(nameof(beforeBoard));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (timings == null)
            {
                throw new ArgumentNullException(nameof(timings));
            }

            if (IsAnimating)
            {
                throw new InvalidOperationException(
                    "A board animation is already active.");
            }

            ValidateDependencies();
            BuildActionPlans(
                beforeBoard,
                result,
                out BoardSwapPresentationPlan swapPlan,
                out List<BoardCascadeStepPresentationPlan> cascadePlans,
                out BoardShufflePresentationPlan shufflePlan);
            ValidateCurrentViewLayout(
                beforeBoard,
                "PlaySwapActionSequence.beforeBoard");
            if (swapPlan.Motions.Count == 0)
            {
                return EmptyTransition();
            }

            PrepareSwapViews(swapPlan);
            activeSpawnInputEnabled = HasAnyInputEnabled();
            CaptureAndDisableInput();
            activeActionFinalBoard = result.Board;
            activeAnimation = ActiveAnimation.ActionSequence;
            IsAnimating = true;
            int version = ++animationVersion;
            return AnimateActionSequence(
                result,
                swapPlan,
                cascadePlans,
                shufflePlan,
                timings,
                version);
        }

        public bool TryGetView(long runtimeId, out BlockView view)
        {
            return viewsByRuntimeId.TryGetValue(runtimeId, out view);
        }

        public bool MatchesBoard(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (IsAnimating || spriteSet == null || pool == null)
            {
                return false;
            }

            try
            {
                return TryValidateCurrentViewLayout(
                    board,
                    "MatchesBoard",
                    out _);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public bool TryValidateCurrentViewLayout(
            BoardState board,
            string context,
            out string failureReason)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (string.IsNullOrWhiteSpace(context))
            {
                throw new ArgumentException(
                    "A validation context is required.",
                    nameof(context));
            }

            return TryValidateCurrentViewLayoutCore(
                board,
                context,
                out failureReason);
        }

        private void OnDisable()
        {
            ActiveAnimation interruptedAnimation = activeAnimation;
            bool requiresSwapBack = activeSwapRequiresBack;
            animationVersion++;

            switch (interruptedAnimation)
            {
                case ActiveAnimation.InitialDrop:
                    CompleteInitialDrop();
                    break;
                case ActiveAnimation.Swap:
                    CompleteSwap(requiresSwapBack);
                    break;
                case ActiveAnimation.CascadeStep:
                    try
                    {
                        StabilizeCascadeStep();
                    }
                    catch (Exception stabilizationException)
                    {
                        LogSecondaryStabilizationFailure(
                            "CascadeStep.OnDisable",
                            stabilizationException);
                    }
                    finally
                    {
                        CompleteCascadeStepSafely();
                    }
                    break;
                case ActiveAnimation.Shuffle:
                    try
                    {
                        StabilizeShuffle();
                    }
                    catch (Exception stabilizationException)
                    {
                        LogSecondaryStabilizationFailure(
                            "Shuffle.OnDisable",
                            stabilizationException);
                    }
                    finally
                    {
                        CompleteShuffleSafely();
                    }
                    break;
                case ActiveAnimation.ActionSequence:
                    try
                    {
                        StabilizeActionSequence();
                    }
                    catch (Exception stabilizationException)
                    {
                        LogSecondaryStabilizationFailure(
                            "ActionSequence.OnDisable",
                            stabilizationException);
                    }
                    finally
                    {
                        CompleteActionSequenceSafely();
                    }
                    break;
                default:
                    RestoreInput();
                    IsAnimating = false;
                    break;
            }
        }

        private void ValidateDependencies()
        {
            if (spriteSet == null)
            {
                throw new InvalidOperationException(
                    "A BoardElementSpriteSet must be assigned.");
            }

            if (pool == null)
            {
                throw new InvalidOperationException(
                    "A BlockViewPool must be assigned.");
            }

            pool.Initialize();
        }

        private static void ValidateDuration(float duration, string name)
        {
            if (duration < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    duration,
                    "Duration cannot be negative.");
            }
        }

        private void BuildActionPlans(
            BoardState beforeBoard,
            BoardSwapActionResult result,
            out BoardSwapPresentationPlan swapPlan,
            out List<BoardCascadeStepPresentationPlan> cascadePlans,
            out BoardShufflePresentationPlan shufflePlan)
        {
            swapPlan = swapPlanner.Build(beforeBoard, result);
            cascadePlans = new List<BoardCascadeStepPresentationPlan>();
            shufflePlan = null;

            if (result.Status != BoardSwapActionStatus.Resolved)
            {
                return;
            }

            BoardState stepBefore = result.SwappedBoard;
            for (int index = 0; index < result.Cascade.Steps.Count; index++)
            {
                BoardCascadeStepPresentationPlan cascadePlan =
                    cascadePlanner.Build(
                        stepBefore,
                        result.Cascade.Steps[index]);
                cascadePlans.Add(cascadePlan);
                stepBefore = cascadePlan.Board;
            }

            ValidateBoardLayouts(
                stepBefore,
                result.Cascade.Board,
                "The last CascadeStep Board must match Cascade.Board.");
            shufflePlan = shufflePlanner.Build(
                result.Cascade.Board,
                result.Shuffle);
            ValidateBoardLayouts(
                shufflePlan.Board,
                result.Board,
                "The shuffle Board must match the action result Board.");
        }

        private static void ValidateBoardLayouts(
            BoardState expected,
            BoardState actual,
            string message)
        {
            if (expected == null || actual == null)
            {
                throw new InvalidOperationException(message);
            }

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock expectedBlock = expected.Get(position);
                BoardBlock actualBlock = actual.Get(position);
                if (!BlocksMatch(expectedBlock, actualBlock))
                {
                    throw new InvalidOperationException(
                        $"{message} Mismatch at {position}.");
                }
            }
        }

        private static bool BlocksMatch(BoardBlock expected, BoardBlock actual)
        {
            if (expected == null || actual == null)
            {
                return expected == null && actual == null;
            }

            return expected.RuntimeId == actual.RuntimeId
                && expected.BlockType == actual.BlockType
                && expected.Element == actual.Element;
        }

        private void CollectRenderItems(BoardState board)
        {
            renderItems.Clear();
            var runtimeIds = new HashSet<long>();

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = board.Get(position);
                    if (block == null)
                    {
                        continue;
                    }

                    if (block.BlockType != BoardBlockType.Normal
                        || !block.Element.HasValue)
                    {
                        throw new NotSupportedException(
                            $"Board block type {block.BlockType} at " +
                            $"{position} is not supported by BattleBoardView.");
                    }

                    if (!runtimeIds.Add(block.RuntimeId))
                    {
                        throw new InvalidOperationException(
                            $"RuntimeId {block.RuntimeId} appears more than " +
                            "once on the board.");
                    }

                    Sprite sprite =
                        spriteSet.GetSprite(block.Element.Value);
                    renderItems.Add(
                        new RenderItem(block, position, sprite));
                }
            }
        }

        private void ReleaseObsoleteViews()
        {
            obsoleteRuntimeIds.Clear();

            foreach (long runtimeId in viewsByRuntimeId.Keys)
            {
                bool remains = false;
                for (int index = 0; index < renderItems.Count; index++)
                {
                    if (renderItems[index].Block.RuntimeId == runtimeId)
                    {
                        remains = true;
                        break;
                    }
                }

                if (!remains)
                {
                    obsoleteRuntimeIds.Add(runtimeId);
                }
            }

            for (int index = 0; index < obsoleteRuntimeIds.Count; index++)
            {
                long runtimeId = obsoleteRuntimeIds[index];
                BlockView view = viewsByRuntimeId[runtimeId];
                viewsByRuntimeId.Remove(runtimeId);
                RemoveInputState(view);
                pool.Release(view);
            }
        }

        private void PrepareInitialDropViews(
            IReadOnlyList<BoardInitialDropEntry> entries)
        {
            initialDropViewItems.Clear();

            for (int index = 0; index < entries.Count; index++)
            {
                BoardInitialDropEntry entry = entries[index];
                RenderItem renderItem = renderItems[index];
                if (renderItem.Block.RuntimeId != entry.RuntimeId)
                {
                    throw new InvalidOperationException(
                        "Initial drop plan and render data are inconsistent.");
                }

                if (!viewsByRuntimeId.TryGetValue(
                    entry.RuntimeId,
                    out BlockView view))
                {
                    view = pool.Acquire();
                    viewsByRuntimeId.Add(entry.RuntimeId, view);
                }

                view.Bind(
                    renderItem.Block,
                    entry.Target,
                    renderItem.Sprite);
                view.SetInputEnabled(false);

                Vector2 source = BoardViewLayout.GetAnchoredPosition(
                    entry.SourceX,
                    entry.SourceY);
                Vector2 target = BoardViewLayout.GetAnchoredPosition(
                    entry.Target);
                view.SetAnchoredPosition(source);
                initialDropViewItems.Add(new InitialDropViewItem(
                    view,
                    entry.Target.X,
                    source,
                    target));
            }
        }

        private IEnumerator AnimateInitialDrop(
            float duration,
            float columnStagger,
            int version)
        {
            try
            {
                if (duration > 0f)
                {
                    float elapsed = 0f;
                    float totalDuration = duration
                        + ((BoardConstants.Width - 1) * columnStagger);

                    while (elapsed < totalDuration
                        && IsAnimationCurrent(
                            version,
                            ActiveAnimation.InitialDrop))
                    {
                        UpdateInitialDropPositions(
                            elapsed,
                            duration,
                            columnStagger);
                        yield return null;
                        elapsed += Time.deltaTime;
                    }
                }
            }
            finally
            {
                if (IsAnimationCurrent(
                    version,
                    ActiveAnimation.InitialDrop))
                {
                    CompleteInitialDrop();
                }
            }
        }

        private void UpdateInitialDropPositions(
            float elapsed,
            float duration,
            float columnStagger)
        {
            for (int index = 0; index < initialDropViewItems.Count; index++)
            {
                InitialDropViewItem item = initialDropViewItems[index];
                float delay = item.Column * columnStagger;
                float progress = Mathf.Clamp01((elapsed - delay) / duration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                item.View.SetAnchoredPosition(Vector2.LerpUnclamped(
                    item.Source,
                    item.Target,
                    easedProgress));
            }
        }

        private void CompleteInitialDrop()
        {
            for (int index = 0; index < initialDropViewItems.Count; index++)
            {
                InitialDropViewItem item = initialDropViewItems[index];
                item.View.SetAnchoredPosition(item.Target);
                item.View.SetInputEnabled(true);
            }

            initialDropViewItems.Clear();
            activeAnimation = ActiveAnimation.None;
            IsAnimating = false;
        }

        private void ValidateCurrentViewLayout(
            BoardState board,
            string context)
        {
            if (!TryValidateCurrentViewLayoutCore(
                board,
                context,
                out string failureReason))
            {
                throw new InvalidOperationException(failureReason);
            }
        }

        private bool TryValidateCurrentViewLayoutCore(
            BoardState board,
            string context,
            out string failureReason)
        {
            int occupiedCount = CountOccupiedBlocks(board);
            if (!TryValidateViewDictionaryIntegrity(
                null,
                context,
                false,
                occupiedCount,
                out failureReason))
            {
                return false;
            }

            var boardRuntimeIds = new HashSet<long>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = board.Get(position);
                    if (block == null)
                    {
                        continue;
                    }

                    if (!boardRuntimeIds.Add(block.RuntimeId))
                    {
                        failureReason = BuildDictionaryFailure(
                            context,
                            "BoardRuntimeIdDuplicate",
                            $"RuntimeId={block.RuntimeId}; Position={position}",
                            occupiedCount);
                        return false;
                    }

                    if (!viewsByRuntimeId.TryGetValue(
                        block.RuntimeId,
                        out BlockView view)
                        || view == null)
                    {
                        failureReason = BuildBlockFailure(
                            context,
                            "ViewMissing",
                            block,
                            position,
                            null,
                            occupiedCount);
                        return false;
                    }

                    if (block.BlockType != BoardBlockType.Normal)
                    {
                        failureReason = BuildBlockFailure(
                            context,
                            "BlockType",
                            block,
                            position,
                            view,
                            occupiedCount);
                        return false;
                    }

                    if (!block.Element.HasValue)
                    {
                        failureReason = BuildBlockFailure(
                            context,
                            "Element",
                            block,
                            position,
                            view,
                            occupiedCount);
                        return false;
                    }

                    Vector2 expectedAnchor =
                        BoardViewLayout.GetAnchoredPosition(position);
                    Sprite expectedSprite =
                        spriteSet.GetSprite(block.Element.Value);
                    if (view.RuntimeId != block.RuntimeId)
                    {
                        failureReason = BuildBlockFailure(
                            context,
                            "RuntimeId",
                            block,
                            position,
                            view,
                            occupiedCount);
                        return false;
                    }

                    if (view.Position != position)
                    {
                        failureReason = BuildBlockFailure(
                            context,
                            "Position",
                            block,
                            position,
                            view,
                            occupiedCount);
                        return false;
                    }

                    Vector2 anchorDelta =
                        view.RectTransform.anchoredPosition - expectedAnchor;
                    if (anchorDelta.sqrMagnitude
                        > AnchoredPositionTolerance
                            * AnchoredPositionTolerance)
                    {
                        failureReason = BuildBlockFailure(
                            context,
                            "AnchoredPosition",
                            block,
                            position,
                            view,
                            occupiedCount);
                        return false;
                    }

                    if (view.Image == null
                        || view.Image.sprite != expectedSprite)
                    {
                        failureReason = BuildBlockFailure(
                            context,
                            "Sprite",
                            block,
                            position,
                            view,
                            occupiedCount);
                        return false;
                    }

                    Vector3 scaleDelta =
                        view.RectTransform.localScale - Vector3.one;
                    if (scaleDelta.sqrMagnitude
                        > ScaleTolerance * ScaleTolerance)
                    {
                        failureReason = BuildBlockFailure(
                            context,
                            "Scale",
                            block,
                            position,
                            view,
                            occupiedCount);
                        return false;
                    }
                }
            }

            if (occupiedCount != viewsByRuntimeId.Count)
            {
                failureReason = BuildDictionaryFailure(
                    context,
                    "ViewCount",
                    $"Expected={occupiedCount}; Actual={viewsByRuntimeId.Count}",
                    occupiedCount);
                return false;
            }

            failureReason = null;
            return true;
        }

        private void ValidateViewDictionaryIntegrity(
            BoardState expectedBoard,
            string context,
            bool requireTargetRuntimeIds)
        {
            int occupiedCount = expectedBoard != null
                ? CountOccupiedBlocks(expectedBoard)
                : -1;
            if (!TryValidateViewDictionaryIntegrity(
                expectedBoard,
                context,
                requireTargetRuntimeIds,
                occupiedCount,
                out string failureReason))
            {
                throw new InvalidOperationException(failureReason);
            }
        }

        private bool TryValidateViewDictionaryIntegrity(
            BoardState expectedBoard,
            string context,
            bool requireTargetRuntimeIds,
            int occupiedCount,
            out string failureReason)
        {
            var keysByInstanceId = new Dictionary<int, List<long>>();
            var keysByViewRuntimeId = new Dictionary<long, List<long>>();
            foreach (KeyValuePair<long, BlockView> pair in viewsByRuntimeId)
            {
                BlockView view = pair.Value;
                if (view == null)
                {
                    failureReason = BuildDictionaryFailure(
                        context,
                        "NullView",
                        $"DictionaryKey={pair.Key}",
                        occupiedCount);
                    return false;
                }

                int instanceId = view.GetInstanceID();
                if (!keysByInstanceId.TryGetValue(
                    instanceId,
                    out List<long> instanceKeys))
                {
                    instanceKeys = new List<long>();
                    keysByInstanceId.Add(instanceId, instanceKeys);
                }

                instanceKeys.Add(pair.Key);
                if (!keysByViewRuntimeId.TryGetValue(
                    view.RuntimeId,
                    out List<long> runtimeIdKeys))
                {
                    runtimeIdKeys = new List<long>();
                    keysByViewRuntimeId.Add(view.RuntimeId, runtimeIdKeys);
                }

                runtimeIdKeys.Add(pair.Key);
            }

            foreach (KeyValuePair<int, List<long>> pair in keysByInstanceId)
            {
                if (pair.Value.Count <= 1)
                {
                    continue;
                }

                pair.Value.Sort();
                failureReason = BuildDictionaryFailure(
                    context,
                    "ViewAlias",
                    $"ViewInstanceId={pair.Key}; DictionaryKeys=" +
                    $"[{string.Join(",", pair.Value)}]",
                    occupiedCount);
                return false;
            }

            foreach (KeyValuePair<long, List<long>> pair
                in keysByViewRuntimeId)
            {
                if (pair.Value.Count <= 1)
                {
                    continue;
                }

                pair.Value.Sort();
                failureReason = BuildDictionaryFailure(
                    context,
                    "ViewRuntimeIdDuplicate",
                    $"ViewRuntimeId={pair.Key}; DictionaryKeys=" +
                    $"[{string.Join(",", pair.Value)}]",
                    occupiedCount);
                return false;
            }

            foreach (KeyValuePair<long, BlockView> pair in viewsByRuntimeId)
            {
                if (pair.Key == pair.Value.RuntimeId)
                {
                    continue;
                }

                failureReason = BuildDictionaryFailure(
                    context,
                    "DictionaryKeyRuntimeId",
                    $"DictionaryKey={pair.Key}; " +
                    $"ViewInstanceId={pair.Value.GetInstanceID()}; " +
                    $"ExpectedRuntimeId={pair.Key}; " +
                    $"ActualRuntimeId={pair.Value.RuntimeId}; " +
                    $"ViewRuntimeId={pair.Value.RuntimeId}; " +
                    $"ViewPosition={pair.Value.Position}",
                    occupiedCount);
                return false;
            }

            if (requireTargetRuntimeIds && expectedBoard != null)
            {
                var expectedRuntimeIds = new HashSet<long>();
                for (int index = 0;
                    index < BoardConstants.CellCount;
                    index++)
                {
                    BoardBlock block = expectedBoard.Get(
                        BoardPosition.FromIndex(index));
                    if (block != null)
                    {
                        expectedRuntimeIds.Add(block.RuntimeId);
                    }
                }

                foreach (long runtimeId in expectedRuntimeIds)
                {
                    if (!viewsByRuntimeId.ContainsKey(runtimeId))
                    {
                        failureReason = BuildDictionaryFailure(
                            context,
                            "TargetRuntimeIdMissing",
                            $"RuntimeId={runtimeId}",
                            occupiedCount);
                        return false;
                    }
                }

                foreach (long runtimeId in viewsByRuntimeId.Keys)
                {
                    if (!expectedRuntimeIds.Contains(runtimeId))
                    {
                        failureReason = BuildDictionaryFailure(
                            context,
                            "UnexpectedRuntimeId",
                            $"RuntimeId={runtimeId}",
                            occupiedCount);
                        return false;
                    }
                }
            }

            failureReason = null;
            return true;
        }

        private string BuildBlockFailure(
            string context,
            string field,
            BoardBlock expectedBlock,
            BoardPosition expectedPosition,
            BlockView actualView,
            int occupiedCount)
        {
            Vector2 expectedAnchor =
                BoardViewLayout.GetAnchoredPosition(expectedPosition);
            string expectedElement = expectedBlock.Element.HasValue
                ? expectedBlock.Element.Value.ToString()
                : "<none>";
            Sprite expectedSprite = expectedBlock.Element.HasValue
                && expectedBlock.BlockType == BoardBlockType.Normal
                ? spriteSet.GetSprite(expectedBlock.Element.Value)
                : null;
            var builder = new StringBuilder();
            builder.Append("Board view validation failed. Context=")
                .Append(context)
                .Append("; Field=").Append(field)
                .Append("; ExpectedRuntimeId=")
                .Append(expectedBlock.RuntimeId)
                .Append("; ExpectedPosition=").Append(expectedPosition)
                .Append("; ExpectedAnchoredPosition=(")
                .Append(expectedAnchor.x.ToString("G9"))
                .Append(", ")
                .Append(expectedAnchor.y.ToString("G9"))
                .Append(')')
                .Append("; ExpectedElement=").Append(expectedElement)
                .Append("; ExpectedSprite=")
                .Append(GetObjectName(expectedSprite))
                .Append("; ExpectedScale=").Append(Vector3.one);

            if (actualView == null)
            {
                builder.Append("; ActualView=<missing>");
            }
            else
            {
                Vector2 actualAnchor =
                    actualView.RectTransform.anchoredPosition;
                Vector2 anchorDelta = actualAnchor - expectedAnchor;
                builder.Append("; ActualViewInstanceId=")
                    .Append(actualView.GetInstanceID())
                    .Append("; ActualRuntimeId=")
                    .Append(actualView.RuntimeId)
                    .Append("; ActualPosition=")
                    .Append(actualView.Position)
                    .Append("; ActualAnchoredPosition=(")
                    .Append(actualAnchor.x.ToString("G9"))
                    .Append(", ")
                    .Append(actualAnchor.y.ToString("G9"))
                    .Append(')')
                    .Append("; AnchorDelta=(")
                    .Append(anchorDelta.x.ToString("G9"))
                    .Append(", ")
                    .Append(anchorDelta.y.ToString("G9"))
                    .Append(')')
                    .Append("; AnchorDeltaMagnitude=")
                    .Append(anchorDelta.magnitude.ToString("G9"))
                    .Append("; ActualSprite=")
                    .Append(GetObjectName(
                        actualView.Image != null
                            ? actualView.Image.sprite
                            : null))
                    .Append("; ActualScale=")
                    .Append(actualView.RectTransform.localScale)
                    .Append("; ActiveSelf=")
                    .Append(actualView.gameObject.activeSelf)
                    .Append("; RaycastTarget=")
                    .Append(actualView.Image != null
                        && actualView.Image.raycastTarget);
            }

            builder.Append("; ")
                .Append(BuildGlobalSummary(occupiedCount));
            return builder.ToString();
        }

        private string BuildDictionaryFailure(
            string context,
            string field,
            string detail,
            int occupiedCount)
        {
            return $"Board view dictionary integrity failed. " +
                $"Context={context}; Field={field}; {detail}; " +
                BuildGlobalSummary(occupiedCount);
        }

        private string BuildGlobalSummary(int occupiedCount)
        {
            var instanceIds = new HashSet<int>();
            foreach (BlockView view in viewsByRuntimeId.Values)
            {
                if (view != null)
                {
                    instanceIds.Add(view.GetInstanceID());
                }
            }

            string poolSummary = pool == null
                ? "Pool=<null>"
                : $"PoolTotal={pool.TotalCreatedCount}; " +
                    $"PoolActive={pool.ActiveCount}; " +
                    $"PoolAvailable={pool.AvailableCount}";
            return $"BoardOccupied={occupiedCount}; " +
                $"DictionaryCount={viewsByRuntimeId.Count}; " +
                $"UniqueViewInstances={instanceIds.Count}; " +
                $"{poolSummary}; IsAnimating={IsAnimating}; " +
                $"ActiveAnimation={activeAnimation}; " +
                $"AnimationVersion={animationVersion}";
        }

        private static int CountOccupiedBlocks(BoardState board)
        {
            int count = 0;
            for (int index = 0;
                index < BoardConstants.CellCount;
                index++)
            {
                if (board.Get(BoardPosition.FromIndex(index)) != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetObjectName(UnityEngine.Object target)
        {
            return target == null ? "<null>" : target.name;
        }

        private void PrepareSwapViews(BoardSwapPresentationPlan plan)
        {
            swapViewItems.Clear();
            for (int index = 0; index < plan.Motions.Count; index++)
            {
                BoardSwapViewMotion motion = plan.Motions[index];
                if (!viewsByRuntimeId.TryGetValue(
                    motion.RuntimeId,
                    out BlockView view))
                {
                    throw new InvalidOperationException(
                        $"No active BlockView exists for swap RuntimeId " +
                        $"{motion.RuntimeId}.");
                }

                swapViewItems.Add(new SwapViewItem(
                    view,
                    motion.From,
                    motion.To,
                    BoardViewLayout.GetAnchoredPosition(motion.From),
                    BoardViewLayout.GetAnchoredPosition(motion.To)));
            }
        }

        private void CaptureAndDisableInput()
        {
            inputStateItems.Clear();
            foreach (BlockView view in viewsByRuntimeId.Values)
            {
                inputStateItems.Add(new InputStateItem(
                    view,
                    view.IsInputEnabled));
                view.SetInputEnabled(false);
            }
        }

        private IEnumerator AnimateSwap(
            bool requiresSwapBack,
            BoardState forwardBoard,
            BoardState finalBoard,
            float duration,
            int version)
        {
            try
            {
                IEnumerator core = AnimateSwapCore(
                    requiresSwapBack,
                    forwardBoard,
                    "SwapForwardComplete",
                    duration,
                    version,
                    ActiveAnimation.Swap);
                while (core.MoveNext())
                {
                    yield return core.Current;
                }

                if (IsAnimationCurrent(version, ActiveAnimation.Swap))
                {
                    SnapSwapResult(requiresSwapBack);
                    ValidateCurrentViewLayout(
                        finalBoard,
                        requiresSwapBack
                            ? "NoMatchSwapBackComplete"
                            : "SwapForwardSettled");
                }
            }
            finally
            {
                if (IsAnimationCurrent(version, ActiveAnimation.Swap))
                {
                    CompleteSwap(requiresSwapBack);
                }
            }
        }

        private IEnumerator AnimateSwapCore(
            bool requiresSwapBack,
            BoardState forwardBoard,
            string forwardContext,
            float duration,
            int version,
            ActiveAnimation animation)
        {
            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration
                    && IsAnimationCurrent(version, animation))
                {
                    UpdateSwapPositions(elapsed / duration, false);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }

            if (!IsAnimationCurrent(version, animation))
            {
                yield break;
            }

            SnapSwapForward();
            ValidateCurrentViewLayout(forwardBoard, forwardContext);
            if (requiresSwapBack && duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration
                    && IsAnimationCurrent(version, animation))
                {
                    UpdateSwapPositions(elapsed / duration, true);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }
        }

        private bool IsAnimationCurrent(
            int version,
            ActiveAnimation animation)
        {
            return animationVersion == version
                && activeAnimation == animation;
        }

        private void UpdateSwapPositions(float progress, bool reverse)
        {
            float easedProgress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(progress));
            for (int index = 0; index < swapViewItems.Count; index++)
            {
                SwapViewItem item = swapViewItems[index];
                Vector2 from = reverse ? item.Target : item.Source;
                Vector2 to = reverse ? item.Source : item.Target;
                item.View.SetAnchoredPosition(Vector2.LerpUnclamped(
                    from,
                    to,
                    easedProgress));
            }
        }

        private void SnapSwapForward()
        {
            for (int index = 0; index < swapViewItems.Count; index++)
            {
                SwapViewItem item = swapViewItems[index];
                item.View.SetAnchoredPosition(item.Target);
                item.View.SetLogicalPosition(item.To);
            }
        }

        private void CompleteSwap(bool requiresSwapBack)
        {
            SnapSwapResult(requiresSwapBack);
            swapViewItems.Clear();
            RestoreInput();
            activeSwapRequiresBack = false;
            activeAnimation = ActiveAnimation.None;
            IsAnimating = false;
        }

        private void SnapSwapResult(bool requiresSwapBack)
        {
            for (int index = 0; index < swapViewItems.Count; index++)
            {
                SwapViewItem item = swapViewItems[index];
                item.View.SetAnchoredPosition(
                    requiresSwapBack ? item.Source : item.Target);
                item.View.SetLogicalPosition(
                    requiresSwapBack ? item.From : item.To);
            }
        }

        private void PrepareCascadeViews(
            BoardCascadeStepPresentationPlan plan)
        {
            removalViewItems.Clear();
            moveViewItems.Clear();
            spawnViewItems.Clear();

            for (int index = 0; index < plan.Removals.Count; index++)
            {
                BoardBlockRemoval removal = plan.Removals[index];
                BlockView view = GetRequiredView(
                    removal.RuntimeId,
                    "removal");
                removalViewItems.Add(new RemovalViewItem(view));
            }

            for (int index = 0; index < plan.Moves.Count; index++)
            {
                BoardBlockMove move = plan.Moves[index];
                BlockView view = GetRequiredView(move.RuntimeId, "collapse");
                moveViewItems.Add(new MoveViewItem(
                    view,
                    move.To,
                    BoardViewLayout.GetAnchoredPosition(move.From),
                    BoardViewLayout.GetAnchoredPosition(move.To)));
            }
        }

        private BlockView GetRequiredView(long runtimeId, string phase)
        {
            if (!viewsByRuntimeId.TryGetValue(runtimeId, out BlockView view))
            {
                throw new InvalidOperationException(
                    $"No active BlockView exists for {phase} RuntimeId " +
                    $"{runtimeId}.");
            }

            return view;
        }

        private IEnumerator AnimateCascadeStep(
            BoardCascadeStepPresentationPlan plan,
            float removalDuration,
            float collapseDuration,
            float refillDuration,
            int version,
            string contextPrefix)
        {
            bool completed = false;
            try
            {
                IEnumerator core = AnimateCascadeStepCore(
                    plan,
                    removalDuration,
                    collapseDuration,
                    refillDuration,
                    version,
                    ActiveAnimation.CascadeStep,
                    contextPrefix);
                while (core.MoveNext())
                {
                    yield return core.Current;
                }

                if (IsAnimationCurrent(
                    version,
                    ActiveAnimation.CascadeStep))
                {
                    ValidateCurrentViewLayout(
                        plan.Board,
                        $"{contextPrefix}.Complete");
                }

                completed = IsAnimationCurrent(
                    version,
                    ActiveAnimation.CascadeStep);
            }
            finally
            {
                if (IsAnimationCurrent(
                    version,
                    ActiveAnimation.CascadeStep))
                {
                    try
                    {
                        if (!completed)
                        {
                            StabilizeCascadeStep();
                        }
                    }
                    catch (Exception stabilizationException)
                    {
                        LogSecondaryStabilizationFailure(
                            "CascadeStep",
                            stabilizationException);
                    }
                    finally
                    {
                        CompleteCascadeStepSafely();
                    }
                }
            }
        }

        private IEnumerator AnimateCascadeStepCore(
            BoardCascadeStepPresentationPlan plan,
            float removalDuration,
            float collapseDuration,
            float refillDuration,
            int version,
            ActiveAnimation animation,
            string contextPrefix)
        {
            if (removalDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < removalDuration
                    && IsAnimationCurrent(version, animation))
                {
                    UpdateRemovalScales(elapsed / removalDuration);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }

            if (!IsAnimationCurrent(version, animation))
            {
                yield break;
            }

            ReleaseRemovedViews(plan);
            ValidateViewDictionaryIntegrity(
                plan.CollapseBoard,
                $"{contextPrefix}.RemovalComplete",
                true);
            if (collapseDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < collapseDuration
                    && IsAnimationCurrent(version, animation))
                {
                    UpdateMovePositions(elapsed / collapseDuration);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }

            if (!IsAnimationCurrent(version, animation))
            {
                yield break;
            }

            SnapMoveViews();
            ValidateCurrentViewLayout(
                plan.CollapseBoard,
                $"{contextPrefix}.CollapseComplete");
            AcquireSpawnViews(plan);
            ValidateViewDictionaryIntegrity(
                plan.Board,
                $"{contextPrefix}.SpawnRegistered",
                true);
            if (refillDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < refillDuration
                    && IsAnimationCurrent(version, animation))
                {
                    UpdateSpawnPositions(elapsed / refillDuration);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }

            if (!IsAnimationCurrent(version, animation))
            {
                yield break;
            }

            SnapSpawnViews();
            ValidateCurrentViewLayout(
                plan.Board,
                $"{contextPrefix}.RefillComplete");
        }

        private void UpdateRemovalScales(float progress)
        {
            float easedProgress = SmoothProgress(progress);
            Vector3 scale = Vector3.LerpUnclamped(
                Vector3.one,
                Vector3.zero,
                easedProgress);
            for (int index = 0; index < removalViewItems.Count; index++)
            {
                removalViewItems[index].View.SetLocalScale(scale);
            }
        }

        private void ReleaseRemovedViews(
            BoardCascadeStepPresentationPlan plan)
        {
            for (int index = 0; index < plan.Removals.Count; index++)
            {
                BoardBlockRemoval removal = plan.Removals[index];
                BlockView view = GetRequiredView(
                    removal.RuntimeId,
                    "removal");
                viewsByRuntimeId.Remove(removal.RuntimeId);
                RemoveInputState(view);
                pool.Release(view);
            }

            removalViewItems.Clear();
        }

        private void UpdateMovePositions(float progress)
        {
            float easedProgress = SmoothProgress(progress);
            for (int index = 0; index < moveViewItems.Count; index++)
            {
                MoveViewItem item = moveViewItems[index];
                item.View.SetAnchoredPosition(Vector2.LerpUnclamped(
                    item.Source,
                    item.Target,
                    easedProgress));
            }
        }

        private void SnapMoveViews()
        {
            for (int index = 0; index < moveViewItems.Count; index++)
            {
                MoveViewItem item = moveViewItems[index];
                item.View.SetAnchoredPosition(item.Target);
                item.View.SetLogicalPosition(item.To);
            }
        }

        private void AcquireSpawnViews(
            BoardCascadeStepPresentationPlan plan)
        {
            spawnViewItems.Clear();
            for (int index = 0; index < plan.Spawns.Count; index++)
            {
                BoardBlockSpawn spawn = plan.Spawns[index];
                if (viewsByRuntimeId.ContainsKey(spawn.RuntimeId))
                {
                    throw new InvalidOperationException(
                        $"Spawn RuntimeId {spawn.RuntimeId} already has a View.");
                }

                BlockView view = pool.Acquire();
                Sprite sprite = spriteSet.GetSprite(spawn.Block.Element.Value);
                view.Bind(spawn.Block, spawn.Target, sprite);
                view.SetAnchoredPosition(BoardViewLayout.GetAnchoredPosition(
                    spawn.SourceX,
                    spawn.SourceY));
                view.SetInputEnabled(false);
                viewsByRuntimeId.Add(spawn.RuntimeId, view);
                inputStateItems.Add(new InputStateItem(
                    view,
                    activeSpawnInputEnabled));
                spawnViewItems.Add(new SpawnViewItem(
                    view,
                    BoardViewLayout.GetAnchoredPosition(
                        spawn.SourceX,
                        spawn.SourceY),
                    BoardViewLayout.GetAnchoredPosition(spawn.Target)));
            }
        }

        private void UpdateSpawnPositions(float progress)
        {
            float easedProgress = SmoothProgress(progress);
            for (int index = 0; index < spawnViewItems.Count; index++)
            {
                SpawnViewItem item = spawnViewItems[index];
                item.View.SetAnchoredPosition(Vector2.LerpUnclamped(
                    item.Source,
                    item.Target,
                    easedProgress));
            }
        }

        private void SnapSpawnViews()
        {
            for (int index = 0; index < spawnViewItems.Count; index++)
            {
                SpawnViewItem item = spawnViewItems[index];
                item.View.SetAnchoredPosition(item.Target);
            }
        }

        private void PrepareShuffleViews(BoardShufflePresentationPlan plan)
        {
            shuffleViewItems.Clear();
            for (int index = 0; index < plan.Entries.Count; index++)
            {
                BoardShuffleEntry entry = plan.Entries[index];
                BlockView view = GetRequiredView(
                    entry.RuntimeId,
                    "shuffle");
                shuffleViewItems.Add(new ShuffleViewItem(view, entry));
            }
        }

        private IEnumerator AnimateShuffle(
            BoardShufflePresentationPlan plan,
            float duration,
            int version,
            string contextPrefix)
        {
            bool completed = false;
            try
            {
                IEnumerator core = AnimateShuffleCore(
                    plan,
                    duration,
                    version,
                    ActiveAnimation.Shuffle,
                    contextPrefix);
                while (core.MoveNext())
                {
                    yield return core.Current;
                }

                completed = IsAnimationCurrent(
                    version,
                    ActiveAnimation.Shuffle);
            }
            finally
            {
                if (IsAnimationCurrent(version, ActiveAnimation.Shuffle))
                {
                    try
                    {
                        if (!completed)
                        {
                            StabilizeShuffle();
                        }
                    }
                    catch (Exception stabilizationException)
                    {
                        LogSecondaryStabilizationFailure(
                            "Shuffle",
                            stabilizationException);
                    }
                    finally
                    {
                        CompleteShuffleSafely();
                    }
                }
            }
        }

        private IEnumerator AnimateShuffleCore(
            BoardShufflePresentationPlan plan,
            float duration,
            int version,
            ActiveAnimation animation,
            string contextPrefix)
        {
            float phaseDuration = duration * 0.5f;
            if (phaseDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < phaseDuration
                    && IsAnimationCurrent(version, animation))
                {
                    UpdateShuffleScales(
                        elapsed / phaseDuration,
                        true);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }

            if (!IsAnimationCurrent(version, animation))
            {
                yield break;
            }

            SetShuffleScale(Vector3.zero);
            ApplyShuffleSprites(plan);
            ValidateViewDictionaryIntegrity(
                plan.Board,
                $"{contextPrefix}.ShrinkComplete",
                true);
            if (phaseDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < phaseDuration
                    && IsAnimationCurrent(version, animation))
                {
                    UpdateShuffleScales(
                        elapsed / phaseDuration,
                        false);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }

            if (!IsAnimationCurrent(version, animation))
            {
                yield break;
            }

            SetShuffleScale(Vector3.one);
            ValidateCurrentViewLayout(
                plan.Board,
                $"{contextPrefix}.Complete");
        }

        private void UpdateShuffleScales(float progress, bool shrinking)
        {
            float easedProgress = SmoothProgress(progress);
            Vector3 from = shrinking ? Vector3.one : Vector3.zero;
            Vector3 to = shrinking ? Vector3.zero : Vector3.one;
            SetShuffleScale(Vector3.LerpUnclamped(
                from,
                to,
                easedProgress));
        }

        private void SetShuffleScale(Vector3 scale)
        {
            for (int index = 0; index < shuffleViewItems.Count; index++)
            {
                shuffleViewItems[index].View.SetLocalScale(scale);
            }
        }

        private void ApplyShuffleSprites(BoardShufflePresentationPlan plan)
        {
            if (shuffleViewItems.Count != plan.Entries.Count)
            {
                throw new InvalidOperationException(
                    "Shuffle plan and prepared Views are inconsistent.");
            }

            for (int index = 0; index < shuffleViewItems.Count; index++)
            {
                ShuffleViewItem item = shuffleViewItems[index];
                BoardShuffleEntry entry = plan.Entries[index];
                if (item.Entry != entry)
                {
                    throw new InvalidOperationException(
                        "Shuffle entry order changed during presentation.");
                }

                item.View.SetSprite(spriteSet.GetSprite(entry.NewElement));
            }
        }

        private IEnumerator AnimateActionSequence(
            BoardSwapActionResult result,
            BoardSwapPresentationPlan swapPlan,
            IReadOnlyList<BoardCascadeStepPresentationPlan> cascadePlans,
            BoardShufflePresentationPlan shufflePlan,
            BoardActionPresentationTimings timings,
            int version)
        {
            bool completed = false;
            try
            {
                IEnumerator swapCore = AnimateSwapCore(
                    swapPlan.RequiresSwapBack,
                    result.SwappedBoard,
                    "SwapForwardComplete",
                    timings.SwapDuration,
                    version,
                    ActiveAnimation.ActionSequence);
                while (swapCore.MoveNext())
                {
                    yield return swapCore.Current;
                }

                if (!IsAnimationCurrent(
                    version,
                    ActiveAnimation.ActionSequence))
                {
                    yield break;
                }

                SnapSwapResult(swapPlan.RequiresSwapBack);
                swapViewItems.Clear();
                BoardState afterSwap = result.Status
                    == BoardSwapActionStatus.NoMatch
                    ? result.Board
                    : result.SwappedBoard;
                ValidateCurrentViewLayout(
                    afterSwap,
                    swapPlan.RequiresSwapBack
                        ? "NoMatchSwapBackComplete"
                        : "SwapForwardSettled");

                if (result.Status == BoardSwapActionStatus.Resolved)
                {
                    for (int index = 0; index < cascadePlans.Count; index++)
                    {
                        BoardCascadeStepPresentationPlan cascadePlan =
                            cascadePlans[index];
                        PrepareCascadeViews(cascadePlan);
                        IEnumerator cascadeCore = AnimateCascadeStepCore(
                            cascadePlan,
                            timings.RemovalDuration,
                            timings.CollapseDuration,
                            timings.RefillDuration,
                            version,
                            ActiveAnimation.ActionSequence,
                            $"CascadeStep[{index}]");
                        while (cascadeCore.MoveNext())
                        {
                            yield return cascadeCore.Current;
                        }

                        if (!IsAnimationCurrent(
                            version,
                            ActiveAnimation.ActionSequence))
                        {
                            yield break;
                        }

                        ClearCascadeViewItems();
                        ValidateCurrentViewLayout(
                            cascadePlan.Board,
                            $"CascadeStep[{index}].Complete");
                    }

                    ValidateCurrentViewLayout(
                        result.Cascade.Board,
                        "CascadeComplete");
                    if (shufflePlan.HasAnimation)
                    {
                        PrepareShuffleViews(shufflePlan);
                        IEnumerator shuffleCore = AnimateShuffleCore(
                            shufflePlan,
                            timings.ShuffleDuration,
                            version,
                            ActiveAnimation.ActionSequence,
                            "Shuffle");
                        while (shuffleCore.MoveNext())
                        {
                            yield return shuffleCore.Current;
                        }

                        if (!IsAnimationCurrent(
                            version,
                            ActiveAnimation.ActionSequence))
                        {
                            yield break;
                        }

                        shuffleViewItems.Clear();
                        ValidateViewDictionaryIntegrity(
                            shufflePlan.Board,
                            "ShuffleComplete",
                            true);
                    }
                }

                ValidateCurrentViewLayout(
                    result.Board,
                    "ActionSequenceComplete");
                completed = true;
            }
            finally
            {
                if (IsAnimationCurrent(
                    version,
                    ActiveAnimation.ActionSequence))
                {
                    try
                    {
                        if (!completed)
                        {
                            StabilizeActionSequence();
                        }
                    }
                    catch (Exception stabilizationException)
                    {
                        GameLogger.Exception(
                            stabilizationException,
                            this);
                        GameLogger.Error(
                            "[BattleBoard] Secondary action stabilization " +
                            "failure. The original action exception, if any, " +
                            "remains authoritative.",
                            this);
                    }
                    finally
                    {
                        CompleteActionSequenceSafely();
                    }
                }
            }
        }

        private void StabilizeCascadeStep()
        {
            if (activeCascadePlan == null)
            {
                return;
            }

            SynchronizeViewsToBoard(
                activeCascadePlan.Board,
                "StabilizeCascadeStep");
        }

        private void StabilizeShuffle()
        {
            if (activeShufflePlan != null)
            {
                SynchronizeViewsToBoard(
                    activeShufflePlan.Board,
                    "StabilizeShuffle");
            }
        }

        private void StabilizeActionSequence()
        {
            if (activeActionFinalBoard != null)
            {
                SynchronizeViewsToBoard(
                    activeActionFinalBoard,
                    "StabilizeActionSequence");
            }
        }

        private void SynchronizeViewsToBoard(
            BoardState board,
            string context)
        {
            ValidateViewDictionaryIntegrity(
                null,
                $"{context}.Start",
                false);
            CollectRenderItems(board);
            ReleaseObsoleteViews();
            for (int index = 0; index < renderItems.Count; index++)
            {
                RenderItem item = renderItems[index];
                if (!viewsByRuntimeId.TryGetValue(
                    item.Block.RuntimeId,
                    out BlockView view))
                {
                    view = pool.Acquire();
                    viewsByRuntimeId.Add(item.Block.RuntimeId, view);
                    inputStateItems.Add(new InputStateItem(
                        view,
                        activeSpawnInputEnabled));
                }

                view.Bind(item.Block, item.Position, item.Sprite);
                view.SetInputEnabled(false);
            }

            ValidateViewDictionaryIntegrity(
                board,
                $"{context}.DictionaryComplete",
                true);
            ValidateCurrentViewLayout(board, context);
        }

        private void CompleteCascadeStep()
        {
            ClearCascadeViewItems();
            try
            {
                RestoreInput();
            }
            finally
            {
                activeCascadePlan = null;
                activeSpawnInputEnabled = false;
                activeAnimation = ActiveAnimation.None;
                IsAnimating = false;
            }
        }

        private void CompleteShuffle()
        {
            shuffleViewItems.Clear();
            try
            {
                RestoreInput();
            }
            finally
            {
                activeShufflePlan = null;
                activeAnimation = ActiveAnimation.None;
                IsAnimating = false;
            }
        }

        private void CompleteCascadeStepSafely()
        {
            try
            {
                CompleteCascadeStep();
            }
            catch (Exception cleanupException)
            {
                LogCleanupFailure("CascadeStep", cleanupException);
                ForceClearPresentationState();
            }
        }

        private void CompleteShuffleSafely()
        {
            try
            {
                CompleteShuffle();
            }
            catch (Exception cleanupException)
            {
                LogCleanupFailure("Shuffle", cleanupException);
                ForceClearPresentationState();
            }
        }

        private void CompleteActionSequence()
        {
            swapViewItems.Clear();
            ClearCascadeViewItems();
            shuffleViewItems.Clear();
            try
            {
                RestoreInput();
            }
            finally
            {
                activeActionFinalBoard = null;
                activeCascadePlan = null;
                activeShufflePlan = null;
                activeSwapRequiresBack = false;
                activeSpawnInputEnabled = false;
                activeAnimation = ActiveAnimation.None;
                IsAnimating = false;
            }
        }

        private void CompleteActionSequenceSafely()
        {
            try
            {
                CompleteActionSequence();
            }
            catch (Exception cleanupException)
            {
                LogCleanupFailure("ActionSequence", cleanupException);
                ForceClearPresentationState();
            }
        }

        private void LogSecondaryStabilizationFailure(
            string context,
            Exception exception)
        {
            GameLogger.Exception(exception, this);
            GameLogger.Error(
                $"[BattleBoard] Secondary {context} stabilization failure.",
                this);
        }

        private void LogCleanupFailure(
            string context,
            Exception exception)
        {
            GameLogger.Exception(exception, this);
            GameLogger.Error(
                $"[BattleBoard] {context} presentation cleanup failed.",
                this);
        }

        private void ForceClearPresentationState()
        {
            animationVersion++;
            swapViewItems.Clear();
            ClearCascadeViewItems();
            shuffleViewItems.Clear();
            inputStateItems.Clear();
            activeActionFinalBoard = null;
            activeCascadePlan = null;
            activeShufflePlan = null;
            activeSwapRequiresBack = false;
            activeSpawnInputEnabled = false;
            activeAnimation = ActiveAnimation.None;
            IsAnimating = false;
        }

        private void ClearCascadeViewItems()
        {
            removalViewItems.Clear();
            moveViewItems.Clear();
            spawnViewItems.Clear();
        }

        private static float SmoothProgress(float progress)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
        }

        private bool HasAnyInputEnabled()
        {
            foreach (BlockView view in viewsByRuntimeId.Values)
            {
                if (view.IsInputEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveInputState(BlockView view)
        {
            for (int index = inputStateItems.Count - 1; index >= 0; index--)
            {
                if (inputStateItems[index].View == view)
                {
                    inputStateItems.RemoveAt(index);
                }
            }
        }

        private void RestoreInput()
        {
            for (int index = 0; index < inputStateItems.Count; index++)
            {
                InputStateItem item = inputStateItems[index];
                if (item.View != null)
                {
                    item.View.SetInputEnabled(item.WasEnabled);
                }
            }

            inputStateItems.Clear();
        }

        private static IEnumerator EmptyTransition()
        {
            yield break;
        }

        private readonly struct RenderItem
        {
            public RenderItem(
                BoardBlock block,
                BoardPosition position,
                Sprite sprite)
            {
                Block = block;
                Position = position;
                Sprite = sprite;
            }

            public BoardBlock Block { get; }
            public BoardPosition Position { get; }
            public Sprite Sprite { get; }
        }

        private readonly struct InitialDropViewItem
        {
            public InitialDropViewItem(
                BlockView view,
                int column,
                Vector2 source,
                Vector2 target)
            {
                View = view;
                Column = column;
                Source = source;
                Target = target;
            }

            public BlockView View { get; }
            public int Column { get; }
            public Vector2 Source { get; }
            public Vector2 Target { get; }
        }

        private readonly struct SwapViewItem
        {
            public SwapViewItem(
                BlockView view,
                BoardPosition from,
                BoardPosition to,
                Vector2 source,
                Vector2 target)
            {
                View = view;
                From = from;
                To = to;
                Source = source;
                Target = target;
            }

            public BlockView View { get; }
            public BoardPosition From { get; }
            public BoardPosition To { get; }
            public Vector2 Source { get; }
            public Vector2 Target { get; }
        }

        private readonly struct RemovalViewItem
        {
            public RemovalViewItem(BlockView view)
            {
                View = view;
            }

            public BlockView View { get; }
        }

        private readonly struct MoveViewItem
        {
            public MoveViewItem(
                BlockView view,
                BoardPosition to,
                Vector2 source,
                Vector2 target)
            {
                View = view;
                To = to;
                Source = source;
                Target = target;
            }

            public BlockView View { get; }
            public BoardPosition To { get; }
            public Vector2 Source { get; }
            public Vector2 Target { get; }
        }

        private readonly struct SpawnViewItem
        {
            public SpawnViewItem(
                BlockView view,
                Vector2 source,
                Vector2 target)
            {
                View = view;
                Source = source;
                Target = target;
            }

            public BlockView View { get; }
            public Vector2 Source { get; }
            public Vector2 Target { get; }
        }

        private readonly struct ShuffleViewItem
        {
            public ShuffleViewItem(
                BlockView view,
                BoardShuffleEntry entry)
            {
                View = view;
                Entry = entry;
            }

            public BlockView View { get; }
            public BoardShuffleEntry Entry { get; }
        }

        private readonly struct InputStateItem
        {
            public InputStateItem(BlockView view, bool wasEnabled)
            {
                View = view;
                WasEnabled = wasEnabled;
            }

            public BlockView View { get; }
            public bool WasEnabled { get; }
        }

        private enum ActiveAnimation
        {
            None = 0,
            InitialDrop = 1,
            Swap = 2,
            CascadeStep = 3,
            Shuffle = 4,
            ActionSequence = 5
        }
    }
}
