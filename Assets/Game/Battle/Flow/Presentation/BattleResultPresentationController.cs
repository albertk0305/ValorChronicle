using System;
using System.Collections;
using UnityEngine;
using ValorChronicle.Battle.Results.Persistence;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleResultPresentationController : MonoBehaviour
    {
        [SerializeField]
        private BattleResultView resultView = null;

        [SerializeField]
        private BattleHudController battleHudController = null;

        private BattleResultPersistenceCoordinator coordinator;
        private BattleResultPersistenceCoordinator subscribedCoordinator;
        private BattlePersistedResult pendingResult;
        private BattlePersistedResult lastPresentedResult;
        private Coroutine presentationCoroutine;
        private int presentationVersion;

        public BattleResultView ResultView => resultView;
        public BattleHudController BattleHudController =>
            battleHudController;
        public BattleResultPersistenceCoordinator Coordinator => coordinator;
        public BattlePersistedResult LastPresentedResult =>
            lastPresentedResult;
        public bool HasPendingPresentation => presentationCoroutine != null;

        private void Awake()
        {
            resultView?.ResetPresentation();
        }

        private void OnEnable()
        {
            SubscribeIfReady();
        }

        private void OnDisable()
        {
            Unsubscribe();
            CancelPresentation();
            lastPresentedResult = null;
            resultView?.Hide();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            CancelPresentation();
        }

        public void Initialize(
            BattleResultPersistenceCoordinator persistenceCoordinator)
        {
            if (persistenceCoordinator == null)
            {
                throw new ArgumentNullException(
                    nameof(persistenceCoordinator));
            }

            if (ReferenceEquals(coordinator, persistenceCoordinator))
            {
                SubscribeIfReady();
                return;
            }

            Unsubscribe();
            CancelPresentation();
            coordinator = persistenceCoordinator;
            lastPresentedResult = null;
            resultView?.ResetPresentation();
            SubscribeIfReady();
        }

        private void SubscribeIfReady()
        {
            if (!isActiveAndEnabled
                || coordinator == null
                || ReferenceEquals(subscribedCoordinator, coordinator))
            {
                return;
            }

            Unsubscribe();
            coordinator.ResultPersisted += HandleResultPersisted;
            subscribedCoordinator = coordinator;

            if (coordinator.LastPersistedResult != null)
            {
                HandleResultPersisted(coordinator.LastPersistedResult);
            }
        }

        private void Unsubscribe()
        {
            if (subscribedCoordinator == null)
            {
                return;
            }

            subscribedCoordinator.ResultPersisted -= HandleResultPersisted;
            subscribedCoordinator = null;
        }

        private void HandleResultPersisted(
            BattlePersistedResult persistedResult)
        {
            if (persistedResult == null
                || ReferenceEquals(pendingResult, persistedResult)
                || ReferenceEquals(lastPresentedResult, persistedResult))
            {
                return;
            }

            CancelPresentation();
            pendingResult = persistedResult;
            resultView.Render(persistedResult);

            float delay = battleHudController == null
                ? 0f
                : battleHudController.ResultOverlayDelaySeconds;
            int version = ++presentationVersion;
            if (delay <= 0f)
            {
                CompletePresentation(persistedResult, version);
                return;
            }

            presentationCoroutine = StartCoroutine(
                ShowAfterDelay(persistedResult, delay, version));
        }

        private IEnumerator ShowAfterDelay(
            BattlePersistedResult persistedResult,
            float delay,
            int version)
        {
            yield return new WaitForSeconds(delay);
            CompletePresentation(persistedResult, version);
        }

        private void CompletePresentation(
            BattlePersistedResult persistedResult,
            int version)
        {
            if (version != presentationVersion
                || !ReferenceEquals(pendingResult, persistedResult))
            {
                return;
            }

            presentationCoroutine = null;
            pendingResult = null;
            lastPresentedResult = persistedResult;
            resultView.Show();
        }

        private void CancelPresentation()
        {
            presentationVersion++;
            if (presentationCoroutine != null)
            {
                StopCoroutine(presentationCoroutine);
                presentationCoroutine = null;
            }

            pendingResult = null;
        }
    }
}
