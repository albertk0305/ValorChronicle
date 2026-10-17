using System;
using UnityEngine;
using ValorChronicle.Core.Logging;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.Settings.Integration
{
    public sealed class SettingsPersistenceDriver : MonoBehaviour
    {
        private SettingsService settingsService;
        private SettingsSaveScheduler scheduler;

        public void Initialize(SettingsService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            if (settingsService != null)
                throw new InvalidOperationException(
                    "Settings persistence driver is already initialized.");

            settingsService = service;
            scheduler = new SettingsSaveScheduler();
            settingsService.SaveRequested += ScheduleSave;
            if (settingsService.IsDirty)
                ScheduleSave();
        }

        public SettingsWriteResult FlushNow()
        {
            if (settingsService == null)
                return new SettingsWriteResult(SettingsWriteStatus.NoChanges);

            scheduler.Clear();
            SettingsWriteResult result = settingsService.Flush();
            if (!result.IsSuccess)
            {
                if (result.Exception != null)
                    GameLogger.Exception(result.Exception, this);

                GameLogger.Error(
                    $"[Settings] Flush failed. Status={result.Status}. "
                        + result.Message,
                    this);
            }

            return result;
        }

        private void Update()
        {
            if (scheduler != null
                && scheduler.IsDue(Time.unscaledTimeAsDouble))
            {
                FlushNow();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                FlushNow();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                FlushNow();
        }

        private void OnApplicationQuit()
        {
            FlushNow();
        }

        private void OnDestroy()
        {
            if (settingsService != null)
            {
                settingsService.SaveRequested -= ScheduleSave;
                FlushNow();
            }
        }

        private void ScheduleSave()
        {
            scheduler?.NotifyChange(Time.unscaledTimeAsDouble);
        }
    }
}
