using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Gacha.Presentation;

namespace ValorChronicle.Tests.PlayMode
{
    public sealed class GachaPresentationCoroutinePlayModeTests
    {
        private readonly List<UnityEngine.Object> created =
            new List<UnityEngine.Object>();
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = previousTimeScale;
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    UnityEngine.Object.Destroy(created[index]);
                }
            }

            created.Clear();
            yield return null;
        }

        [UnityTest]
        [Timeout(5000)]
        public IEnumerator ClosedDelayThenOpeningResumesAtTimeScaleZero()
        {
            const float closedDelay = 0.3f;
            var timeSource = new ManualPresentationTimeSource();
            Fixture fixture = CreateFixture(timeSource, closedDelay);
            GachaPresentationController controller = fixture.Controller;
            Vector2 closedPosition = fixture.LeftCurtain.anchoredPosition;

            Time.timeScale = 0f;
            int version = GetPrivateField<int>(
                controller,
                "presentationVersion");
            InvokePrivate(controller, "BeginOpening", version);

            Assert.That(controller.State,
                Is.EqualTo(GachaPresentationState.Opening));
            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.gameObject.activeInHierarchy, Is.True);
            Assert.That(fixture.LeftCurtain.anchoredPosition,
                Is.EqualTo(closedPosition));

            timeSource.AdvanceTo(0.1);
            yield return WaitUntil(
                () => timeSource.LastObservedTime >= 0.1,
                "Closed delay did not observe 0.1 seconds.");
            Assert.That(fixture.LeftCurtain.anchoredPosition,
                Is.EqualTo(closedPosition));

            timeSource.AdvanceTo(0.29);
            yield return WaitUntil(
                () => timeSource.LastObservedTime >= 0.29,
                "Closed delay did not observe 0.29 seconds.");
            Assert.That(fixture.LeftCurtain.anchoredPosition,
                Is.EqualTo(closedPosition));

            timeSource.AdvanceTo(closedDelay);
            yield return WaitUntil(
                () => timeSource.LastObservedTime >= closedDelay,
                "Closed delay did not reach its boundary.");
            Assert.That(fixture.LeftCurtain.anchoredPosition,
                Is.EqualTo(closedPosition));

            timeSource.AdvanceTo(0.4);
            yield return WaitUntil(
                () => fixture.LeftCurtain.anchoredPosition
                    != closedPosition,
                "Opening coroutine did not resume after its first yield.");
            Assert.That(controller.State,
                Is.EqualTo(GachaPresentationState.Opening));
            Assert.That(Time.timeScale, Is.Zero);

            timeSource.AdvanceTo(1.31);
            yield return WaitUntil(
                () => controller.State
                    == GachaPresentationState.ShowingResult,
                "Opening did not complete into ShowingResult.");
            Assert.That(Time.timeScale, Is.Zero);
        }

        private Fixture CreateFixture(
            IGachaPresentationTimeSource timeSource,
            float closedDelay)
        {
            var root = new GameObject(
                "GachaPresentationCoroutinePlayModeTests",
                typeof(RectTransform),
                typeof(Canvas));
            created.Add(root);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1080f, 1920f);

            var view = root.AddComponent<GachaPresentationView>();
            var presenter = root.AddComponent<GachaResultPresenter>();
            var controller = root.AddComponent<GachaPresentationController>();

            RectTransform presentationRoot = Rect(
                "PresentationRoot",
                root.transform,
                new Vector2(1080f, 1920f));
            GameObject characterRoot = Rect(
                "CharacterRoot",
                presentationRoot).gameObject;
            Image characterImage = Image("CharacterImage", characterRoot.transform);
            GameObject characterOverlayRoot = Rect(
                "CharacterOverlayRoot",
                presentationRoot).gameObject;
            Image characterOverlayImage = Image(
                "CharacterOverlayImage",
                characterOverlayRoot.transform);
            TMP_Text characterOverlayText = Text(
                "CharacterOverlayText",
                characterOverlayRoot.transform);
            GameObject artifactOrItemRoot = Rect(
                "ArtifactOrItemRoot",
                presentationRoot).gameObject;
            GameObject artifactRoot = Rect(
                "ArtifactRoot",
                artifactOrItemRoot.transform).gameObject;
            Image artifactImage = Image(
                "ArtifactImage",
                artifactRoot.transform);
            GameObject itemRoot = Rect(
                "ItemRoot",
                artifactOrItemRoot.transform).gameObject;
            Image itemImage = Image("ItemImage", itemRoot.transform);
            TMP_Text itemText = Text("ItemText", itemRoot.transform);
            GameObject artifactOverlayRoot = Rect(
                "ArtifactOverlayRoot",
                presentationRoot).gameObject;
            Image artifactOverlayImage = Image(
                "ArtifactOverlayImage",
                artifactOverlayRoot.transform);
            TMP_Text artifactOverlayText = Text(
                "ArtifactOverlayText",
                artifactOverlayRoot.transform);
            GameObject resultRoot = Rect(
                "ResultRoot",
                presentationRoot).gameObject;
            GameObject curtainRoot = Rect(
                "CurtainRoot",
                presentationRoot).gameObject;
            RectTransform leftCurtain = Rect(
                "LeftCurtain",
                curtainRoot.transform,
                new Vector2(540f, 1920f));
            leftCurtain.anchoredPosition = new Vector2(-430f, 0f);
            RectTransform rightCurtain = Rect(
                "RightCurtain",
                curtainRoot.transform,
                new Vector2(540f, 1920f));
            rightCurtain.anchoredPosition = new Vector2(430f, 0f);
            Button touchButton = Button("TouchButton", presentationRoot);
            Button skipButton = Button("SkipButton", presentationRoot);

            view.Configure(
                presentationRoot,
                characterRoot,
                characterImage,
                characterOverlayRoot,
                characterOverlayImage,
                characterOverlayText,
                artifactOrItemRoot,
                artifactRoot,
                artifactImage,
                itemRoot,
                itemImage,
                itemText,
                artifactOverlayRoot,
                artifactOverlayImage,
                artifactOverlayText,
                resultRoot,
                curtainRoot,
                leftCurtain,
                rightCurtain,
                touchButton,
                skipButton);
            controller.Configure(
                view,
                presenter,
                1f,
                60f,
                0.15f,
                closedDelay,
                3f,
                timeSource);
            presentationRoot.gameObject.SetActive(true);
            curtainRoot.SetActive(true);

            return new Fixture(controller, leftCurtain);
        }

        private static IEnumerator WaitUntil(
            Func<bool> condition,
            string failureMessage)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 1d;
            while (!condition()
                && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, failureMessage);
        }

        private static RectTransform Rect(
            string name,
            Transform parent,
            Vector2? size = null)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size ?? new Vector2(100f, 100f);
            return rect;
        }

        private static Image Image(string name, Transform parent)
        {
            return Rect(name, parent).gameObject.AddComponent<Image>();
        }

        private static TMP_Text Text(string name, Transform parent)
        {
            return Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        }

        private static Button Button(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent);
            rect.gameObject.AddComponent<Image>();
            return rect.gameObject.AddComponent<Button>();
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static object InvokePrivate(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(target, arguments);
        }

        private sealed class ManualPresentationTimeSource
            : IGachaPresentationTimeSource
        {
            private double currentTime;

            public double UnscaledTime
            {
                get
                {
                    LastObservedTime = currentTime;
                    return currentTime;
                }
            }

            public double LastObservedTime { get; private set; } =
                double.NegativeInfinity;

            public void AdvanceTo(double time)
            {
                Assert.That(time, Is.GreaterThanOrEqualTo(currentTime));
                currentTime = time;
            }
        }

        private sealed class Fixture
        {
            public Fixture(
                GachaPresentationController controller,
                RectTransform leftCurtain)
            {
                Controller = controller;
                LeftCurtain = leftCurtain;
            }

            public GachaPresentationController Controller { get; }
            public RectTransform LeftCurtain { get; }
        }
    }
}
