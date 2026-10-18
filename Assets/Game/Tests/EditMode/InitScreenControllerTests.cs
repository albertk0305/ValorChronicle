using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Audio.Core;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Localization;
using ValorChronicle.Save.Services;
using ValorChronicle.Settings.Services;
using ValorChronicle.Tests.EditMode.Audio;
using ValorChronicle.UI;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class InitScreenControllerTests
    {
        private GameObject root;
        private Button startButton;
        private TMP_Text initText;
        private InitScreenController controller;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(
                "Init Screen Test",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            root.SetActive(false);
            startButton = root.GetComponent<Button>();
            GameObject textObject = new GameObject(
                "Init Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root.transform, false);
            initText = textObject.GetComponent<TMP_Text>();
            controller = root.AddComponent<InitScreenController>();
            SetField(controller, "startButton", startButton);
            SetField(controller, "initText", initText);
            Invoke(controller, "Awake");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void PendingInitialization_HidesPromptAndBlocksInput()
        {
            Present(result: null);

            Assert.That(initText.gameObject.activeSelf, Is.False);
            Assert.That(startButton.interactable, Is.False);
        }

        [Test]
        public async Task SuccessfulInitialization_ShowsPromptAndAllowsInput()
        {
            BootstrapInitializationResult result =
                await CreateResult(validContent: true);

            Present(result);

            Assert.That(initText.gameObject.activeSelf, Is.True);
            Assert.That(initText.text,
                Is.EqualTo(InitScreenController.ReadyText));
            Assert.That(startButton.interactable, Is.True);
        }

        [Test]
        public async Task LocaleChange_RefreshesVisibleReadyPromptImmediately()
        {
            LocalizationService localization = CreateLocalization();
            Invoke(controller, "BindLocalization", localization);
            Present(await CreateResult(validContent: true));

            Assert.That(initText.text, Is.EqualTo("Press to Start"));

            localization.SetLocale("ko");

            Assert.That(initText.text, Is.EqualTo("시작하려면 누르세요"));
            Assert.That(startButton.interactable, Is.True);
        }

        [Test]
        public async Task FailedInitialization_ShowsFailureAndBlocksInput()
        {
            BootstrapInitializationResult result =
                await CreateResult(validContent: false);
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    "\\[InitScreenController\\] Game initialization failed"));

            Present(result);

            Assert.That(initText.gameObject.activeSelf, Is.True);
            Assert.That(initText.text,
                Is.EqualTo(InitScreenController.FailureText));
            Assert.That(startButton.interactable, Is.False);
        }

        [Test]
        public async Task FailedInitialization_LocalizesWhenServiceIsReady()
        {
            LocalizationService localization = CreateLocalization();
            localization.SetLocale("ko");
            Invoke(controller, "BindLocalization", localization);
            BootstrapInitializationResult result =
                await CreateResult(validContent: false);
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    "\\[InitScreenController\\] Game initialization failed"));

            Present(result);

            Assert.That(initText.text, Is.EqualTo(
                "게임 초기화에 실패했습니다. 게임을 다시 실행해 주세요."));
            Assert.That(startButton.interactable, Is.False);
        }

        [Test]
        public async Task LocalizationBinding_DoesNotDuplicateAndUnsubscribes()
        {
            LocalizationService localization = CreateLocalization();
            Invoke(controller, "BindLocalization", localization);
            Invoke(controller, "BindLocalization", localization);
            Present(await CreateResult(validContent: true));

            Assert.That(GetLocaleSubscriberCount(localization), Is.EqualTo(1));

            Invoke(controller, "OnDisable");

            Assert.That(GetLocaleSubscriberCount(localization), Is.Zero);
        }

        [Test]
        public async Task BootstrapperCompletion_StoresStateAndNotifiesSubscribers()
        {
            BootstrapInitializationResult result =
                await CreateResult(validContent: true);
            var bootstrapObject = new GameObject("Bootstrap Event Test");
            bootstrapObject.SetActive(false);
            GameBootstrapper bootstrapper =
                bootstrapObject.AddComponent<GameBootstrapper>();
            int notifications = 0;
            BootstrapInitializationResult notifiedResult = null;
            bootstrapper.InitializationCompleted += completed =>
            {
                notifications++;
                notifiedResult = completed;
            };

            try
            {
                Invoke(bootstrapper, "CompleteInitialization", result);

                Assert.That(bootstrapper.InitializationResult,
                    Is.SameAs(result));
                Assert.That(notifiedResult, Is.SameAs(result));
                Assert.That(notifications, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(bootstrapObject);
            }
        }

        [Test]
        public async Task AlreadyCompletedBootstrapper_ShowsPromptImmediately()
        {
            BootstrapInitializationResult result =
                await CreateResult(validContent: true);
            var bootstrapObject = new GameObject(
                "Completed Bootstrap Test");
            bootstrapObject.SetActive(false);
            GameBootstrapper bootstrapper =
                bootstrapObject.AddComponent<GameBootstrapper>();
            Invoke(bootstrapper, "CompleteInitialization", result);
            SetStaticProperty(
                typeof(GameBootstrapper),
                "Instance",
                bootstrapper);

            try
            {
                Invoke(controller, "TryBindBootstrapper", false);

                Assert.That(initText.gameObject.activeSelf, Is.True);
                Assert.That(initText.text,
                    Is.EqualTo(InitScreenController.ReadyText));
                Assert.That(startButton.interactable, Is.True);
            }
            finally
            {
                SetStaticProperty(
                    typeof(GameBootstrapper),
                    "Instance",
                    null);
                Object.DestroyImmediate(bootstrapObject);
            }
        }

        [Test]
        public async Task SuccessfulInitialization_StartsPendingInitBgmOnce()
        {
            BootstrapInitializationResult result =
                await CreateResult(validContent: true);
            var bootstrapObject = new GameObject("Init Audio Ready Test");
            bootstrapObject.SetActive(false);
            GameBootstrapper bootstrapper =
                bootstrapObject.AddComponent<GameBootstrapper>();
            var output = new RecordingAudioOutput();
            var audioService = new AudioService(
                SettingsService.CreateInMemoryDefaults(),
                output,
                output,
                output,
                new FakeSceneChangeSource("Init"));
            audioService.Initialize();
            SetProperty(bootstrapper, "AudioService", audioService);
            SetStaticProperty(
                typeof(GameBootstrapper),
                "Instance",
                bootstrapper);

            try
            {
                Invoke(controller, "TryBindBootstrapper", false);

                Assert.That(output.PlaySpecialCount, Is.Zero);

                Invoke(bootstrapper, "CompleteInitialization", result);
                Invoke(bootstrapper, "CompleteInitialization", result);

                Assert.That(initText.gameObject.activeSelf, Is.True);
                Assert.That(output.PlaySpecialCount, Is.EqualTo(1));
                Assert.That(output.LastSpecialTrack,
                    Is.EqualTo(BgmTrackKind.Init));
            }
            finally
            {
                audioService.Dispose();
                SetStaticProperty(
                    typeof(GameBootstrapper),
                    "Instance",
                    null);
                Object.DestroyImmediate(bootstrapObject);
            }
        }

        private void Present(BootstrapInitializationResult result)
        {
            Invoke(controller, "PresentInitializationResult", result);
        }

        private static async Task<BootstrapInitializationResult>
            CreateResult(bool validContent)
        {
            var coordinator = new GameInitializationCoordinator(
                () => { },
                () => validContent,
                () => SaveServiceTestFactory.Create(
                    new FakeSaveRepository()),
                new FixedProfileIdProvider());
            return await coordinator.InitializeAsync();
        }

        private static LocalizationService CreateLocalization()
        {
            LocalizationTable table = LocalizationTableParser.Parse(
                "init_screen_test",
                "key\ten\tko\n"
                    + $"{InitScreenController.ReadyTextKey}\t"
                    + "Press to Start\t시작하려면 누르세요\n"
                    + $"{InitScreenController.FailureTextKey}\t"
                    + "Game initialization failed. Please restart the game.\t"
                    + "게임 초기화에 실패했습니다. 게임을 다시 실행해 주세요.");
            return new LocalizationService(new[] { table });
        }

        private static int GetLocaleSubscriberCount(
            LocalizationService localization)
        {
            var field = typeof(LocalizationService).GetField(
                "LocaleChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var listeners = (System.Delegate)field?.GetValue(localization);
            return listeners?.GetInvocationList().Length ?? 0;
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
                    target,
                    value);
        }

        private static void Invoke(
            object target,
            string methodName,
            params object[] arguments)
        {
            target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(
                    target,
                    arguments);
        }

        private static void SetStaticProperty(
            System.Type type,
            string propertyName,
            object value)
        {
            type.GetProperty(
                propertyName,
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

        private sealed class FixedProfileIdProvider : IProfileIdProvider
        {
            public string CreateProfileId() => "init_screen_test_profile";
        }
    }
}
