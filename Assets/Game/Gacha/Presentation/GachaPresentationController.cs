using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Gacha.Persistence;

namespace ValorChronicle.Gacha.Presentation
{
    public interface IGachaPresentationTimeSource
    {
        double UnscaledTime { get; }
    }

    public enum GachaPresentationState
    {
        Idle = 0,
        Opening = 1,
        ShowingResult = 2,
        Transitioning = 3,
        FinalSummary = 4,
        Finished = 5
    }

    public sealed class GachaPresentationController : MonoBehaviour
    {
        private sealed class UnityUnscaledTimeSource
            : IGachaPresentationTimeSource
        {
            public double UnscaledTime => Time.unscaledTimeAsDouble;
        }

        private static readonly IGachaPresentationTimeSource
            DefaultTimeSource = new UnityUnscaledTimeSource();

        public event Action<GachaPresentationState> StateChanged;
        public event Action<GachaTransactionResult> Finished;
        public event Action<GachaTransactionResult, string> Failed;
        public event Action<GachaTransactionResult, string> Cancelled;

        private GachaPresentationView view;
        private GachaResultPresenter resultPresenter;
        private GachaIndividualDisplayEntry[] entries =
            Array.Empty<GachaIndividualDisplayEntry>();
        private GachaTransactionResult activeTransaction;
        private Coroutine flowCoroutine;
        private Coroutine skipHideCoroutine;
        private float openingDuration;
        private float resultDisplayDuration;
        private float transitionDuration;
        private float curtainClosedDelay;
        private float skipHideDelay;
        private int currentIndex;
        private int presentationVersion;
        private int skipVisibilityVersion;
        private int lastInputFrame = -1;
        private bool inputOwned;
        private bool resultSubscribed;
        private IGachaPresentationTimeSource timeSource =
            DefaultTimeSource;

        public GachaPresentationState State { get; private set; } =
            GachaPresentationState.Idle;
        public bool IsConfigured =>
            view != null && view.IsConfigured && resultPresenter != null;
        public bool IsInitialized =>
            IsConfigured && resultPresenter.IsInitialized;
        public bool IsPresenting =>
            activeTransaction != null
            && State != GachaPresentationState.Idle
            && State != GachaPresentationState.Finished;
        public int CurrentIndex => currentIndex;
        public GachaPresentationView View => view;
        public GachaTransactionResult ActiveTransaction => activeTransaction;

        private void OnEnable()
        {
            SubscribeResult();
        }

        private void OnDisable()
        {
            bool wasPresenting = IsPresenting;
            GachaTransactionResult interrupted = activeTransaction;
            CancelPresentation(resetView: true);
            ReleaseInput();
            UnsubscribeResult();
            resultPresenter?.SetCloseInputEnabled(true);
            SetState(wasPresenting
                ? GachaPresentationState.Finished
                : GachaPresentationState.Idle);
            if (wasPresenting)
            {
                const string message =
                    "Gacha presentation was disabled before completion.";
                GameLogger.Warning(
                    "[GachaPresentationController] " + message,
                    this);
                Cancelled?.Invoke(interrupted, message);
            }
        }

        private void OnDestroy()
        {
            CancelPresentation(resetView: true);
            ReleaseInput();
            UnsubscribeResult();
        }

        private void Update()
        {
            if (IsPresenting
                && view?.PresentationRoot != null
                && !view.PresentationRoot.activeInHierarchy)
            {
                ShowFallbackSummary(
                    activeTransaction,
                    "Gacha presentation root became inactive.");
            }
        }

        public void Configure(
            GachaPresentationView configuredView,
            GachaResultPresenter configuredResultPresenter,
            float configuredOpeningDuration,
            float configuredResultDisplayDuration,
            float configuredTransitionDuration,
            float configuredSkipHideDelay)
        {
            Configure(
                configuredView,
                configuredResultPresenter,
                configuredOpeningDuration,
                configuredResultDisplayDuration,
                configuredTransitionDuration,
                0f,
                configuredSkipHideDelay);
        }

        public void Configure(
            GachaPresentationView configuredView,
            GachaResultPresenter configuredResultPresenter,
            float configuredOpeningDuration,
            float configuredResultDisplayDuration,
            float configuredTransitionDuration,
            float configuredCurtainClosedDelay,
            float configuredSkipHideDelay)
        {
            Configure(
                configuredView,
                configuredResultPresenter,
                configuredOpeningDuration,
                configuredResultDisplayDuration,
                configuredTransitionDuration,
                configuredCurtainClosedDelay,
                configuredSkipHideDelay,
                DefaultTimeSource);
        }

        public void Configure(
            GachaPresentationView configuredView,
            GachaResultPresenter configuredResultPresenter,
            float configuredOpeningDuration,
            float configuredResultDisplayDuration,
            float configuredTransitionDuration,
            float configuredCurtainClosedDelay,
            float configuredSkipHideDelay,
            IGachaPresentationTimeSource configuredTimeSource)
        {
            if (configuredOpeningDuration < 0f
                || configuredResultDisplayDuration < 0f
                || configuredTransitionDuration < 0f
                || configuredCurtainClosedDelay < 0f
                || configuredSkipHideDelay < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredOpeningDuration),
                    "Presentation timings cannot be negative.");
            }
            if (configuredTimeSource == null)
            {
                throw new ArgumentNullException(
                    nameof(configuredTimeSource));
            }

            CancelPresentation(resetView: true);
            ReleaseInput();
            UnsubscribeResult();
            view = configuredView
                ?? throw new ArgumentNullException(nameof(configuredView));
            resultPresenter = configuredResultPresenter
                ?? throw new ArgumentNullException(
                    nameof(configuredResultPresenter));
            openingDuration = configuredOpeningDuration;
            resultDisplayDuration = configuredResultDisplayDuration;
            transitionDuration = configuredTransitionDuration;
            curtainClosedDelay = configuredCurtainClosedDelay;
            skipHideDelay = configuredSkipHideDelay;
            timeSource = configuredTimeSource;
            SubscribeResult();
            SetState(GachaPresentationState.Idle);
        }

        public void Initialize(DefinitionDatabase database)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Gacha presentation must be configured first.");
            }

            resultPresenter.Initialize(database);
        }

        public bool TryValidateResources(
            IReadOnlyList<GachaDefinition> definitions,
            out string error)
        {
            if (!IsConfigured || !IsInitialized)
            {
                error = "Gacha presentation is not initialized.";
                return false;
            }

            return resultPresenter.TryValidateIndividualResources(
                definitions,
                out error);
        }

        public void Cancel()
        {
            CancelPresentation(resetView: true);
            resultPresenter?.Close(notify: false);
            resultPresenter?.SetCloseInputEnabled(true);
            SetState(GachaPresentationState.Idle);
        }

        public bool Play(GachaTransactionResult transaction)
        {
            CancelPresentation(resetView: true);
            resultPresenter?.Close(notify: false);
            if (!TryPrepareEntries(transaction, out string error))
            {
                return ShowFallbackSummary(transaction, error);
            }

            activeTransaction = transaction;
            currentIndex = 0;
            resultPresenter.SetCloseInputEnabled(false);
            AcquireInput();
            try
            {
                view.Prepare(entries[0]);
                BeginOpening(presentationVersion);
                return true;
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                return ShowFallbackSummary(
                    transaction,
                    "Individual result presentation could not start.");
            }
        }

        private bool TryPrepareEntries(
            GachaTransactionResult transaction,
            out string error)
        {
            error = string.Empty;
            if (!IsInitialized)
            {
                error = "Gacha presentation is not initialized.";
                return false;
            }

            if (transaction == null
                || transaction.Pulls == null
                || transaction.PullCount != transaction.Pulls.Count
                || (transaction.PullCount != 1
                    && transaction.PullCount
                        != GachaResultView.MaximumResultCount))
            {
                error = "Transaction result must contain one or ten pulls.";
                return false;
            }

            var prepared = new GachaIndividualDisplayEntry[
                transaction.Pulls.Count];
            for (int index = 0; index < prepared.Length; index++)
            {
                GachaTransactionPullResult pull = transaction.Pulls[index];
                if (pull == null || pull.Index != index)
                {
                    error = "Transaction pull order is invalid.";
                    return false;
                }

                if (!resultPresenter.TryCreateIndividualDisplayEntry(
                    pull,
                    out prepared[index],
                    out error))
                {
                    return false;
                }
            }

            entries = prepared;
            return true;
        }

        private void BeginOpening(int version)
        {
            SetState(GachaPresentationState.Opening);
            if (!TrySetCurtainProgress(0f))
            {
                return;
            }

            if (curtainClosedDelay > 0f)
            {
                StartFlow(WaitAtClosedCurtain(version));
                return;
            }

            StartOpeningAnimation(version);
        }

        private IEnumerator WaitAtClosedCurtain(int version)
        {
            double endTime = timeSource.UnscaledTime
                + curtainClosedDelay;
            while (timeSource.UnscaledTime < endTime)
            {
                if (version != presentationVersion)
                {
                    yield break;
                }

                yield return null;
            }

            if (version != presentationVersion)
            {
                yield break;
            }

            flowCoroutine = null;
            StartOpeningAnimation(version);
        }

        private void StartOpeningAnimation(int version)
        {
            if (openingDuration <= 0f)
            {
                if (TrySetCurtainProgress(1f)
                    && version == presentationVersion)
                {
                    EnterShowingResult();
                }

                return;
            }

            StartFlow(AnimateCurtain(
                0f,
                1f,
                openingDuration,
                version,
                EnterShowingResult));
        }

        private void EnterShowingResult()
        {
            flowCoroutine = null;
            SetState(GachaPresentationState.ShowingResult);
            StartFlow(WaitForResult(presentationVersion));
        }

        private IEnumerator WaitForResult(int version)
        {
            float elapsed = 0f;
            while (elapsed < resultDisplayDuration)
            {
                if (version != presentationVersion)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (version == presentationVersion)
            {
                AdvanceFromResult();
            }
        }

        private void AdvanceFromResult()
        {
            StopFlow();
            if (currentIndex >= entries.Length - 1)
            {
                EnterFinalSummary();
                return;
            }

            SetState(GachaPresentationState.Transitioning);
            StartFlow(AnimateCurtain(
                1f,
                0f,
                transitionDuration,
                presentationVersion,
                BindNextAndOpen));
        }

        private void BindNextAndOpen()
        {
            flowCoroutine = null;
            currentIndex++;
            try
            {
                view.Bind(entries[currentIndex]);
                BeginOpening(presentationVersion);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                ShowFallbackSummary(
                    activeTransaction,
                    "An individual result could not be displayed.");
            }
        }

        private IEnumerator AnimateCurtain(
            float from,
            float to,
            float duration,
            int version,
            Action completed)
        {
            if (duration <= 0f)
            {
                if (!TrySetCurtainProgress(to))
                {
                    yield break;
                }

                if (version == presentationVersion)
                {
                    completed?.Invoke();
                }

                yield break;
            }

            double startTime = timeSource.UnscaledTime;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (version != presentationVersion)
                {
                    yield break;
                }

                elapsed = (float)(timeSource.UnscaledTime - startTime);
                if (!TrySetCurtainProgress(Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(elapsed / duration))))
                {
                    yield break;
                }

                yield return null;
            }

            if (version == presentationVersion)
            {
                if (!TrySetCurtainProgress(to))
                {
                    yield break;
                }

                completed?.Invoke();
            }
        }

        private void HandleTouch()
        {
            if (!TryAcceptInput())
            {
                return;
            }

            ShowSkipTemporarily();
            switch (State)
            {
                case GachaPresentationState.Opening:
                    StopFlow();
                    if (!TrySetCurtainProgress(1f))
                    {
                        return;
                    }

                    EnterShowingResult();
                    break;
                case GachaPresentationState.ShowingResult:
                    AdvanceFromResult();
                    break;
            }
        }

        private void HandleSkip()
        {
            if (!TryAcceptInput()
                || State == GachaPresentationState.Idle
                || State == GachaPresentationState.FinalSummary
                || State == GachaPresentationState.Finished)
            {
                return;
            }

            EnterFinalSummary();
        }

        private bool TryAcceptInput()
        {
            if (!inputOwned || Time.frameCount == lastInputFrame)
            {
                return false;
            }

            lastInputFrame = Time.frameCount;
            return true;
        }

        private void ShowSkipTemporarily()
        {
            view.SetSkipVisible(true);
            skipVisibilityVersion++;
            if (skipHideCoroutine != null)
            {
                StopCoroutine(skipHideCoroutine);
            }

            skipHideCoroutine = StartCoroutine(HideSkipAfterDelay(
                presentationVersion,
                skipVisibilityVersion));
        }

        private IEnumerator HideSkipAfterDelay(int version, int visibility)
        {
            float elapsed = 0f;
            while (elapsed < skipHideDelay)
            {
                if (version != presentationVersion
                    || visibility != skipVisibilityVersion)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (version == presentationVersion
                && visibility == skipVisibilityVersion)
            {
                view.SetSkipVisible(false);
                skipHideCoroutine = null;
            }
        }

        private void EnterFinalSummary()
        {
            GachaTransactionResult transaction = activeTransaction;
            CancelCoroutinesAndAdvanceVersion();
            view.PrepareFinalSummary();
            ReleaseInput();
            resultPresenter.SetCloseInputEnabled(true);
            SetState(GachaPresentationState.FinalSummary);
            if (!resultPresenter.ShowResult(transaction))
            {
                FailAndReset("Final Gacha summary could not be displayed.");
            }
        }

        private bool ShowFallbackSummary(
            GachaTransactionResult transaction,
            string reason)
        {
            CancelPresentation(resetView: true);
            activeTransaction = transaction;
            ReleaseInput();
            resultPresenter?.SetCloseInputEnabled(true);
            if (resultPresenter != null
                && transaction != null
                && resultPresenter.ShowResult(transaction))
            {
                SetState(GachaPresentationState.FinalSummary);
                GameLogger.Warning(
                    "[GachaPresentationController] " + reason
                        + " Showing the final summary instead.",
                    this);
                return true;
            }

            FailAndReset(reason);
            return false;
        }

        private bool TrySetCurtainProgress(float progress)
        {
            try
            {
                view.SetCurtainProgress(progress);
                return true;
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                ShowFallbackSummary(
                    activeTransaction,
                    "Curtain presentation failed.");
                return false;
            }
        }

        private void HandleFinalSummaryClosed()
        {
            if (State != GachaPresentationState.FinalSummary)
            {
                return;
            }

            GachaTransactionResult completed = activeTransaction;
            CancelPresentation(resetView: true);
            SetState(GachaPresentationState.Finished);
            Finished?.Invoke(completed);
        }

        private void FailAndReset(string message)
        {
            GachaTransactionResult failedTransaction = activeTransaction;
            CancelPresentation(resetView: true);
            resultPresenter?.SetCloseInputEnabled(true);
            SetState(GachaPresentationState.Finished);
            GameLogger.Error(
                "[GachaPresentationController] " + message,
                this);
            Failed?.Invoke(failedTransaction, message);
        }

        private void StartFlow(IEnumerator routine)
        {
            StopFlow();
            flowCoroutine = StartCoroutine(routine);
        }

        private void StopFlow()
        {
            if (flowCoroutine == null)
            {
                return;
            }

            StopCoroutine(flowCoroutine);
            flowCoroutine = null;
        }

        private void CancelCoroutinesAndAdvanceVersion()
        {
            presentationVersion++;
            StopFlow();
            if (skipHideCoroutine != null)
            {
                StopCoroutine(skipHideCoroutine);
                skipHideCoroutine = null;
            }

            skipVisibilityVersion++;
        }

        private void CancelPresentation(bool resetView)
        {
            CancelCoroutinesAndAdvanceVersion();
            ReleaseInput();
            entries = Array.Empty<GachaIndividualDisplayEntry>();
            activeTransaction = null;
            currentIndex = 0;
            if (resetView && view != null && view.IsConfigured)
            {
                view.ResetToIdle();
            }
        }

        private void AcquireInput()
        {
            if (inputOwned)
            {
                return;
            }

            view.TouchButton.onClick.AddListener(HandleTouch);
            view.SkipButton.onClick.AddListener(HandleSkip);
            inputOwned = true;
            lastInputFrame = -1;
        }

        private void ReleaseInput()
        {
            if (!inputOwned || view == null)
            {
                return;
            }

            view.TouchButton.onClick.RemoveListener(HandleTouch);
            view.SkipButton.onClick.RemoveListener(HandleSkip);
            inputOwned = false;
        }

        private void SubscribeResult()
        {
            if (resultSubscribed || resultPresenter == null)
            {
                return;
            }

            resultPresenter.Closed += HandleFinalSummaryClosed;
            resultSubscribed = true;
        }

        private void UnsubscribeResult()
        {
            if (!resultSubscribed || resultPresenter == null)
            {
                return;
            }

            resultPresenter.Closed -= HandleFinalSummaryClosed;
            resultSubscribed = false;
        }

        private void SetState(GachaPresentationState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
