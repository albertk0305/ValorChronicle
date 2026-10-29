using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Audio.Core;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Settings.Services;
using ValorChronicle.Tests.EditMode.Audio;
using ValorChronicle.UI.Audio;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class ButtonPressSfxTests
    {
        private readonly List<GameObject> createdObjects =
            new List<GameObject>();
        private readonly List<AudioClip> createdClips =
            new List<AudioClip>();
        private readonly List<AudioService> createdServices =
            new List<AudioService>();

        private GameBootstrapper previousBootstrapper;
        private GameObject bootstrapObject;
        private GameBootstrapper bootstrapper;
        private RecordingAudioOutput output;
        private FakeUnscaledTimeSource clock;

        [SetUp]
        public void SetUp()
        {
            previousBootstrapper = GameBootstrapper.Instance;
            SetBootstrapperInstance(null);

            bootstrapObject = new GameObject("Button Press Sfx Bootstrap");
            bootstrapObject.SetActive(false);
            bootstrapper = bootstrapObject.AddComponent<GameBootstrapper>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = createdServices.Count - 1; index >= 0; index--)
                createdServices[index]?.Dispose();

            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                    Object.DestroyImmediate(createdObjects[index]);
            }

            for (int index = createdClips.Count - 1; index >= 0; index--)
            {
                if (createdClips[index] != null)
                    Object.DestroyImmediate(createdClips[index]);
            }

            SetBootstrapperInstance(null);
            if (bootstrapObject != null)
                Object.DestroyImmediate(bootstrapObject);
            SetBootstrapperInstance(previousBootstrapper);
        }

        [Test]
        public void Click_PlaysOnceAndPreservesExistingButtonListener()
        {
            CreateAudioService(initialize: true);
            Button button = CreateButton();
            int existingListenerCalls = 0;
            button.onClick.AddListener(() => existingListenerCalls++);

            button.onClick.Invoke();

            Assert.That(output.UiSfxCount, Is.EqualTo(1));
            Assert.That(existingListenerCalls, Is.EqualTo(1));
        }

        [Test]
        public void NonInteractableOrInactiveButton_DoesNotPlay()
        {
            CreateAudioService(initialize: true);
            Button nonInteractable = CreateButton();
            nonInteractable.interactable = false;
            Button inactive = CreateButton();
            inactive.gameObject.SetActive(false);

            nonInteractable.onClick.Invoke();
            inactive.onClick.Invoke();

            Assert.That(output.UiSfxCount, Is.Zero);
        }

        [Test]
        public void DisabledComponent_UnsubscribesFromButtonClick()
        {
            CreateAudioService(initialize: true);
            Button button = CreateButton();
            ButtonPressSfx component = button.GetComponent<ButtonPressSfx>();
            component.enabled = false;
            InvokeLifecycle(component, "OnDisable");

            button.onClick.Invoke();

            Assert.That(output.UiSfxCount, Is.Zero);
        }

        [Test]
        public void RepeatedEnable_DoesNotCreateDuplicatePlayback()
        {
            CreateAudioService(initialize: true);
            Button button = CreateButton();
            ButtonPressSfx component = button.GetComponent<ButtonPressSfx>();

            component.enabled = false;
            InvokeLifecycle(component, "OnDisable");
            component.enabled = true;
            InvokeLifecycle(component, "OnEnable");
            component.enabled = false;
            InvokeLifecycle(component, "OnDisable");
            component.enabled = true;
            InvokeLifecycle(component, "OnEnable");
            button.onClick.Invoke();

            Assert.That(output.UiSfxCount, Is.EqualTo(1));
        }

        [Test]
        public void MissingOrUninitializedAudioService_DoesNotThrow()
        {
            SetBootstrapperInstance(null);
            Button missingServiceButton = CreateButton();
            Assert.That(
                () => missingServiceButton.onClick.Invoke(),
                Throws.Nothing);

            SetBootstrapperInstance(bootstrapper);
            CreateAudioService(initialize: false);
            Button uninitializedServiceButton = CreateButton();
            Assert.That(
                () => uninitializedServiceButton.onClick.Invoke(),
                Throws.Nothing);
        }

        [Test]
        public void MultipleButtons_ShareAudioServiceCueCooldown()
        {
            CreateAudioService(initialize: true);
            Button first = CreateButton();
            Button second = CreateButton();

            first.onClick.Invoke();
            second.onClick.Invoke();
            Assert.That(output.UiSfxCount, Is.EqualTo(1));

            clock.Advance(
                SfxCuePlaybackPolicy.ButtonPressMinimumIntervalSeconds);
            second.onClick.Invoke();
            Assert.That(output.UiSfxCount, Is.EqualTo(2));
        }

        private AudioService CreateAudioService(bool initialize)
        {
            output = new RecordingAudioOutput();
            clock = new FakeUnscaledTimeSource();
            var clips = new FakeSfxClipProvider();
            AudioClip clip = AudioClip.Create(
                "button_press_test",
                64,
                1,
                44100,
                false);
            createdClips.Add(clip);
            clips.SetClip(SfxCue.ButtonPress, clip);

            var service = new AudioService(
                SettingsService.CreateInMemoryDefaults(),
                output,
                output,
                output,
                new FakeSceneChangeSource("Main"),
                sfxClipProvider: clips,
                unscaledTimeSource: clock);
            createdServices.Add(service);
            if (initialize)
                service.Initialize();

            SetProperty(bootstrapper, "AudioService", service);
            SetBootstrapperInstance(bootstrapper);
            return service;
        }

        private Button CreateButton()
        {
            var gameObject = new GameObject(
                "Test Button",
                typeof(RectTransform),
                typeof(Button));
            createdObjects.Add(gameObject);
            Button button = gameObject.GetComponent<Button>();
            ButtonPressSfx component =
                gameObject.AddComponent<ButtonPressSfx>();
            InvokeLifecycle(component, "OnEnable");
            return button;
        }

        private static void InvokeLifecycle(
            ButtonPressSfx component,
            string methodName)
        {
            MethodInfo method = component.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(component, null);
        }

        private static void SetBootstrapperInstance(
            GameBootstrapper value)
        {
            typeof(GameBootstrapper).GetProperty(
                nameof(GameBootstrapper.Instance),
                BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic)?.SetValue(null, value);
        }

        private static void SetProperty(
            object target,
            string propertyName,
            object value)
        {
            target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic)?.SetValue(target, value);
        }
    }
}
