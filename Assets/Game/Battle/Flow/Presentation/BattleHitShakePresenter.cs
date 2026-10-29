using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Pause;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleHitShakePresenter : IDisposable
    {
        private readonly BattleScreenShake screenShake;
        private readonly HashSet<CombatActionResult> handledDamageResults =
            new HashSet<CombatActionResult>();
        private BattleFlowCombatBridge combatBridge;
        private BattleFlowController flowController;
        private GamePauseService pauseService;
        private bool isPaused;
        private bool battleEnded;

        public BattleHitShakePresenter(BattleScreenShake screenShake)
        {
            this.screenShake = screenShake
                ?? throw new ArgumentNullException(nameof(screenShake));
        }

        public bool IsInitialized { get; private set; }
        public bool IsPaused => isPaused;
        public bool HasBattleEnded => battleEnded;

        public bool Initialize(
            BattleFlowCombatBridge targetCombatBridge,
            BattleFlowController targetFlowController,
            GamePauseService targetPauseService)
        {
            if (IsInitialized)
            {
                return false;
            }

            combatBridge = targetCombatBridge
                ?? throw new ArgumentNullException(
                    nameof(targetCombatBridge));
            flowController = targetFlowController
                ?? throw new ArgumentNullException(
                    nameof(targetFlowController));
            pauseService = targetPauseService
                ?? throw new ArgumentNullException(nameof(targetPauseService));
            isPaused = pauseService.IsPaused;
            battleEnded = flowController.Context != null
                && flowController.Context.Result != BattleResultKind.None;

            combatBridge.BossCombatActionStepApplied +=
                HandleBossCombatActionStepApplied;
            pauseService.PauseStateChanged += HandlePauseStateChanged;
            flowController.BattleResultReached += HandleBattleResultReached;
            IsInitialized = true;

            if (isPaused || battleEnded)
            {
                StopAndRestoreScreen();
            }

            return true;
        }

        public void Dispose()
        {
            StopAndRestoreScreen();
            if (!IsInitialized)
            {
                return;
            }

            combatBridge.BossCombatActionStepApplied -=
                HandleBossCombatActionStepApplied;
            pauseService.PauseStateChanged -= HandlePauseStateChanged;
            flowController.BattleResultReached -= HandleBattleResultReached;
            combatBridge = null;
            flowController = null;
            pauseService = null;
            handledDamageResults.Clear();
            isPaused = false;
            battleEnded = false;
            IsInitialized = false;
        }

        private void HandleBossCombatActionStepApplied(
            CombatActionExecutionStepResult step)
        {
            HandleCombatActionResult(step?.Result);
        }

        private void HandleCombatActionResult(CombatActionResult result)
        {
            if (!IsInitialized
                || isPaused
                || battleEnded
                || !(result is BossDamageActionResult partyDamage)
                || partyDamage.ApplicationResult == null
                || (partyDamage.ApplicationResult.HpDamage <= 0
                    && partyDamage.ApplicationResult
                        .ShieldAbsorbedDamage <= 0)
                || !handledDamageResults.Add(result))
            {
                return;
            }

            if (screenShake != null)
            {
                screenShake.Shake();
            }
        }

        private void HandlePauseStateChanged(bool paused)
        {
            if (!IsInitialized)
            {
                return;
            }

            isPaused = paused;
            if (paused)
            {
                StopAndRestoreScreen();
            }
        }

        private void HandleBattleResultReached(BattleResultKind result)
        {
            if (!IsInitialized || result == BattleResultKind.None)
            {
                return;
            }

            battleEnded = true;
            StopAndRestoreScreen();
        }

        private void StopAndRestoreScreen()
        {
            if (screenShake != null)
            {
                screenShake.StopAndRestore();
            }
        }
    }
}
