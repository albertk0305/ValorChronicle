using System;
using System.Collections;
using UnityEngine;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Random;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BattleBoardController : MonoBehaviour
    {
        [SerializeField]
        private BattleBoardView boardView = null;

        [SerializeField]
        [Min(0f)]
        private float initialDropDuration = 0.45f;

        [SerializeField]
        [Min(0f)]
        private float initialDropColumnStagger = 0.04f;

        [SerializeField]
        [Min(0f)]
        private float swapDuration = 0.15f;

        [SerializeField]
        [Min(0f)]
        private float removalDuration = 0.10f;

        [SerializeField]
        [Min(0f)]
        private float collapseDuration = 0.16f;

        [SerializeField]
        [Min(0f)]
        private float refillDuration = 0.18f;

        [SerializeField]
        [Min(0f)]
        private float shuffleDuration = 0.16f;

        private bool initialized;
        private bool isActionInProgress;
        private int actionVersion;
        private IRandomSource randomSource;
        private BoardBlockIdGenerator blockIdGenerator;
        private BoardGenerator boardGenerator;
        private BoardRefiller boardRefiller;
        private BoardShuffler boardShuffler;
        private BoardSwapActionResolver swapActionResolver;
        private BoardActionPresentationTimings actionTimings;
        private IEnumerator activePresentationSequence;

        public BoardState CurrentBoard { get; private set; }
        public BoardSwapActionResult LastSwapActionResult { get; private set; }
        public bool IsInitialized => initialized;
        public bool IsBoardReady { get; private set; }
        public bool CanAcceptBoardInput => initialized
            && IsBoardReady
            && !isActionInProgress
            && HasResolverGraph
            && boardView != null
            && !boardView.IsAnimating;

        private bool HasResolverGraph => randomSource != null
            && blockIdGenerator != null
            && boardGenerator != null
            && boardRefiller != null
            && boardShuffler != null
            && swapActionResolver != null
            && actionTimings != null;

        private void Start()
        {
            try
            {
                Initialize();
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Initial board setup failed.",
                    this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (!isActionInProgress)
            {
                return;
            }

            actionVersion++;
            IEnumerator interruptedSequence = activePresentationSequence;
            activePresentationSequence = null;
            try
            {
                if (interruptedSequence is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Failed to stop the board presentation.",
                    this);
            }

            isActionInProgress = false;
            UpdateReadyStateAfterPresentation(
                "The interrupted board presentation did not stabilize.");
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            ValidateInitializationSettings();
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
            {
                throw new InvalidOperationException(
                    "GameBootstrapper is not available.");
            }

            IRandomSource sharedRandomSource = bootstrapper.RandomSource;
            if (sharedRandomSource == null)
            {
                throw new InvalidOperationException(
                    "GameBootstrapper.RandomSource is not initialized.");
            }

            var sharedIdGenerator = new BoardBlockIdGenerator();
            var moveAnalyzer = new BoardMoveAnalyzer();
            var generator = new BoardGenerator(
                sharedRandomSource,
                BoardMatchFinder.FindMatches,
                moveAnalyzer,
                sharedIdGenerator);
            var refiller = new BoardRefiller(
                sharedRandomSource,
                sharedIdGenerator);
            var cascadeStepResolver = new BoardCascadeStepResolver(refiller);
            var cascadeResolver = new BoardCascadeResolver(
                cascadeStepResolver);
            var shuffler = new BoardShuffler(
                sharedRandomSource,
                moveAnalyzer);
            var actionResolver = new BoardSwapActionResolver(
                moveAnalyzer,
                cascadeResolver,
                shuffler);
            BoardActionPresentationTimings timings = CreateActionTimings();

            BoardState generatedBoard = generator.Generate();
            CurrentBoard = generatedBoard;
            IsBoardReady = false;

            IEnumerator initialDrop;
            try
            {
                initialDrop = boardView.RenderInitialDrop(
                    generatedBoard,
                    initialDropDuration,
                    initialDropColumnStagger);
            }
            catch
            {
                CurrentBoard = null;
                throw;
            }

            randomSource = sharedRandomSource;
            blockIdGenerator = sharedIdGenerator;
            boardGenerator = generator;
            boardRefiller = refiller;
            boardShuffler = shuffler;
            swapActionResolver = actionResolver;
            actionTimings = timings;
            initialized = true;
            StartCoroutine(RenderInitialBoard(initialDrop));
        }

        public bool TryExecuteSwap(BoardSwap swap)
        {
            if (!CanAcceptBoardInput)
            {
                return false;
            }

            BoardState beforeBoard = CurrentBoard;
            BoardSwapActionResult result;
            try
            {
                result = swapActionResolver.Resolve(beforeBoard, swap);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Board swap resolution failed.",
                    this);
                return false;
            }

            LastSwapActionResult = result;
            if (result.Status == BoardSwapActionStatus.NotSwappable)
            {
                UpdateReadyStateAfterPresentation(
                    "The not-swappable board no longer matches its View.");
                return true;
            }

            CurrentBoard = result.Board;
            IsBoardReady = false;
            isActionInProgress = true;
            int version = ++actionVersion;

            try
            {
                activePresentationSequence =
                    boardView.PlaySwapActionSequence(
                        beforeBoard,
                        result,
                        actionTimings);
                StartCoroutine(RunBoardAction(version));
                return true;
            }
            catch (Exception exception)
            {
                activePresentationSequence = null;
                isActionInProgress = false;
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Board presentation failed to start.",
                    this);
                UpdateReadyStateAfterPresentation(
                    "The failed board presentation did not stabilize.");
                return false;
            }
        }

        public bool TryExecuteSwap(
            BoardPosition first,
            BoardPosition second)
        {
            return TryExecuteSwap(new BoardSwap(first, second));
        }

        private IEnumerator RenderInitialBoard(IEnumerator initialDrop)
        {
            yield return initialDrop;
            UpdateReadyStateAfterInitialDrop();
        }

        private void UpdateReadyStateAfterInitialDrop()
        {
            string failureReason = null;
            bool matches = CurrentBoard != null
                && boardView != null
                && !boardView.IsAnimating;
            if (matches)
            {
                matches = boardView.TryValidateCurrentViewLayout(
                    CurrentBoard,
                    "InitialDropComplete",
                    out failureReason);
            }
            else if (CurrentBoard == null)
            {
                failureReason = "Context=InitialDropComplete; " +
                    "Field=CurrentBoard; Actual=<null>.";
            }
            else if (boardView == null)
            {
                failureReason = "Context=InitialDropComplete; " +
                    "Field=BattleBoardView; Actual=<null>.";
            }
            else
            {
                failureReason = "Context=InitialDropComplete; " +
                    "Field=IsAnimating; Expected=False; Actual=True.";
            }

            IsBoardReady = matches;
            if (!matches)
            {
                GameLogger.Error(
                    "[BattleBoard] Initial board View mismatch: " +
                    failureReason,
                    this);
            }
        }

        private IEnumerator RunBoardAction(int version)
        {
            Exception failure = null;
            while (actionVersion == version
                && activePresentationSequence != null)
            {
                bool hasNext;
                object current = null;
                try
                {
                    hasNext = activePresentationSequence.MoveNext();
                    if (hasNext)
                    {
                        current = activePresentationSequence.Current;
                    }
                }
                catch (Exception exception)
                {
                    failure = exception;
                    break;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return current;
            }

            if (actionVersion != version)
            {
                yield break;
            }

            activePresentationSequence = null;
            isActionInProgress = false;
            if (failure != null)
            {
                GameLogger.Exception(failure, this);
                GameLogger.Error(
                    "[BattleBoard] Board presentation failed.",
                    this);
            }

            UpdateReadyStateAfterPresentation(
                "The completed board View does not match CurrentBoard.");
        }

        private void UpdateReadyStateAfterPresentation(string failureMessage)
        {
            bool matches = CurrentBoard != null
                && boardView != null
                && !boardView.IsAnimating
                && boardView.MatchesBoard(CurrentBoard);
            IsBoardReady = matches;
            if (!matches)
            {
                GameLogger.Error($"[BattleBoard] {failureMessage}", this);
            }
        }

        private void ValidateInitializationSettings()
        {
            if (boardView == null)
            {
                throw new InvalidOperationException(
                    "A BattleBoardView must be assigned.");
            }

            ValidateNonNegative(initialDropDuration, "Initial drop duration");
            ValidateNonNegative(
                initialDropColumnStagger,
                "Initial drop column stagger");
            ValidateNonNegative(swapDuration, "Swap duration");
            ValidateNonNegative(removalDuration, "Removal duration");
            ValidateNonNegative(collapseDuration, "Collapse duration");
            ValidateNonNegative(refillDuration, "Refill duration");
            ValidateNonNegative(shuffleDuration, "Shuffle duration");
        }

        private BoardActionPresentationTimings CreateActionTimings()
        {
            return new BoardActionPresentationTimings(
                swapDuration,
                removalDuration,
                collapseDuration,
                refillDuration,
                shuffleDuration);
        }

        private static void ValidateNonNegative(float value, string name)
        {
            if (value < 0f)
            {
                throw new InvalidOperationException(
                    $"{name} cannot be negative.");
            }
        }
    }
}
