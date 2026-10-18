using System;
using UnityEngine;
using ValorChronicle.Audio.Core;
using ValorChronicle.Settings.Services;
using GameAudioConfiguration =
    ValorChronicle.Audio.Configuration.AudioConfiguration;

namespace ValorChronicle.Audio.Runtime
{
    public sealed class AudioRuntime : MonoBehaviour
    {
        private UnitySceneChangeSource sceneChangeSource;

        public AudioService Service { get; private set; }

        public AudioService Initialize(
            SettingsService settingsService,
            GameAudioConfiguration configuration)
        {
            if (Service != null)
            {
                if (!Service.IsInitialized)
                    Service.Initialize();

                return Service;
            }
            if (settingsService == null)
                throw new ArgumentNullException(nameof(settingsService));

            var output = new UnityAudioOutput(gameObject, configuration);
            sceneChangeSource = new UnitySceneChangeSource();
            Service = new AudioService(
                settingsService,
                output,
                output,
                output,
                sceneChangeSource,
                bgmFadeSettings: configuration?.CreateFadeSettings());
            Service.Initialize();
            return Service;
        }

        private void Update()
        {
            Service?.Advance(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            Service?.Dispose();
            Service = null;
            sceneChangeSource?.Dispose();
            sceneChangeSource = null;
        }
    }
}
