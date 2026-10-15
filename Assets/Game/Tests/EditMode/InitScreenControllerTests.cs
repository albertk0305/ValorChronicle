using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Save.Services;
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

        private sealed class FixedProfileIdProvider : IProfileIdProvider
        {
            public string CreateProfileId() => "init_screen_test_profile";
        }
    }
}
