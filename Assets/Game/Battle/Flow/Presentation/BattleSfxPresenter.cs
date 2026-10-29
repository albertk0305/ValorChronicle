using System;
using System.Collections.Generic;
using ValorChronicle.Audio.Core;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleSfxPresenter : IDisposable
    {
        private readonly Action<SfxCue> requestSfx;
        private readonly HashSet<CombatActionResult> handledDamageResults =
            new HashSet<CombatActionResult>();
        private BattleBoardController boardController;
        private BattleFlowCombatBridge combatBridge;

        public BattleSfxPresenter(AudioService audioService)
        {
            requestSfx = cue =>
            {
                if (audioService?.IsInitialized == true)
                {
                    audioService.PlaySfx(cue);
                }
            };
        }

        internal BattleSfxPresenter(Action<SfxCue> requestSfx)
        {
            this.requestSfx = requestSfx
                ?? throw new ArgumentNullException(nameof(requestSfx));
        }

        public bool IsInitialized { get; private set; }

        public bool Initialize(
            BattleBoardController targetBoardController,
            BattleFlowCombatBridge targetCombatBridge)
        {
            if (IsInitialized)
            {
                return false;
            }

            boardController = targetBoardController
                ?? throw new ArgumentNullException(
                    nameof(targetBoardController));
            combatBridge = targetCombatBridge
                ?? throw new ArgumentNullException(nameof(targetCombatBridge));

            boardController.MatchGroupsRemovalStarted +=
                HandleMatchGroupsRemovalStarted;
            combatBridge.CombatActionStepApplied +=
                HandleCombatActionStepApplied;
            combatBridge.PreMatchQueueCombatActionStepApplied +=
                HandlePreMatchQueueCombatActionStepApplied;
            combatBridge.ActiveCombatActionStepApplied +=
                HandleActiveCombatActionStepApplied;
            combatBridge.BossCombatActionStepApplied +=
                HandleBossCombatActionStepApplied;
            IsInitialized = true;
            return true;
        }

        public void Dispose()
        {
            if (!IsInitialized)
            {
                return;
            }

            boardController.MatchGroupsRemovalStarted -=
                HandleMatchGroupsRemovalStarted;
            combatBridge.CombatActionStepApplied -=
                HandleCombatActionStepApplied;
            combatBridge.PreMatchQueueCombatActionStepApplied -=
                HandlePreMatchQueueCombatActionStepApplied;
            combatBridge.ActiveCombatActionStepApplied -=
                HandleActiveCombatActionStepApplied;
            combatBridge.BossCombatActionStepApplied -=
                HandleBossCombatActionStepApplied;
            boardController = null;
            combatBridge = null;
            handledDamageResults.Clear();
            IsInitialized = false;
        }

        private void HandleMatchGroupsRemovalStarted(int matchGroupCount)
        {
            for (int index = 0; index < matchGroupCount; index++)
            {
                RequestSfx(SfxCue.BlockPop);
            }
        }

        private void HandleCombatActionStepApplied(
            MatchEventCharacterCombatExecutionStepResult step)
        {
            HandleCombatActionResult(step?.ActionStep?.Result);
        }

        private void HandlePreMatchQueueCombatActionStepApplied(
            PreMatchQueueCombatExecutionStepResult step)
        {
            HandleCombatActionResult(step?.ActionStep?.Result);
        }

        private void HandleActiveCombatActionStepApplied(
            ActiveCombatActionExecutionStepResult step)
        {
            HandleCombatActionResult(step?.ActionStep?.Result);
        }

        private void HandleBossCombatActionStepApplied(
            CombatActionExecutionStepResult step)
        {
            HandleCombatActionResult(step?.Result);
        }

        private void HandleCombatActionResult(CombatActionResult result)
        {
            if (result is DamageActionResult bossDamage
                && bossDamage.ApplicationResult != null
                && bossDamage.AppliedDamage > 0)
            {
                if (!handledDamageResults.Add(result))
                {
                    return;
                }

                RequestSfx(SfxCue.Hit);
                return;
            }

            if (result is BossDamageActionResult partyDamage
                && partyDamage.ApplicationResult != null
                && (partyDamage.ApplicationResult.HpDamage > 0
                    || partyDamage.ApplicationResult
                        .ShieldAbsorbedDamage > 0))
            {
                if (!handledDamageResults.Add(result))
                {
                    return;
                }

                RequestSfx(SfxCue.Hit);
            }
        }

        private void RequestSfx(SfxCue cue)
        {
            requestSfx(cue);
        }
    }
}
