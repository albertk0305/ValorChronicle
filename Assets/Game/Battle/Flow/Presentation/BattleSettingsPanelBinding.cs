using UnityEngine;
using ValorChronicle.UI.Settings;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleSettingsPanelBinding : MonoBehaviour
    {
        [SerializeField]
        private SettingsPanelHost settingsPanelHost;

        [SerializeField]
        private BattleFlowController battleFlowController;

        private bool subscribed;

        public bool IsConfigured => settingsPanelHost != null
            && battleFlowController != null;

        private void OnEnable()
        {
            Subscribe();
            RefreshResultAvailability();
        }

        private void OnDisable()
        {
            Unsubscribe();
            settingsPanelHost?.ClosePanel();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            settingsPanelHost?.ClosePanel();
        }

        private void Subscribe()
        {
            if (subscribed || battleFlowController == null)
                return;

            battleFlowController.BattleResultReached +=
                HandleBattleResultReached;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || battleFlowController == null)
                return;

            battleFlowController.BattleResultReached -=
                HandleBattleResultReached;
            subscribed = false;
        }

        private void RefreshResultAvailability()
        {
            if (!IsConfigured)
                return;

            bool resultReached = battleFlowController.Context != null
                && battleFlowController.Context.Result
                    != BattleResultKind.None;
            settingsPanelHost.SetExternallyAvailable(!resultReached);
            if (resultReached)
                settingsPanelHost.ClosePanel();
        }

        private void HandleBattleResultReached(BattleResultKind result)
        {
            settingsPanelHost?.SetExternallyAvailable(false);
            settingsPanelHost?.ClosePanel();
        }
    }
}
