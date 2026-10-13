using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Gacha;
using ValorChronicle.Gacha.Persistence;
using ValorChronicle.Gacha.Presentation;
using ValorChronicle.Rewards;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaPresentationControllerTests
    {
        private const string ScenePath = "Assets/Scenes/Gacha.unity";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string CharacterId = "character_marea_bluefang";
        private const string RelicId = "relic_victors_broken_sword";

        [UnityTest]
        public IEnumerator SinglePull_AdvancesOncePerFrameAndFinishes()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Setup(out GachaSceneInstaller installer);
                GachaPresentationController controller =
                    installer.PresentationController;
                controller.Configure(
                    controller.View,
                    installer.ResultPresenter,
                    60f,
                    60f,
                    60f,
                    3f);
                GachaTransactionResult transaction = Transaction(
                    CurrencyPull(0, 1000));

                Assert.That(controller.Play(transaction), Is.True);
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.Opening));
                Assert.That(controller.View.IsSkipVisible, Is.False);
                Assert.That(controller.View.ItemText.text,
                    Is.EqualTo("+1,000"));

                controller.View.TouchButton.onClick.Invoke();
                controller.View.TouchButton.onClick.Invoke();
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.ShowingResult));
                Assert.That(controller.View.IsSkipVisible, Is.True);

                yield return null;
                controller.View.TouchButton.onClick.Invoke();
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.FinalSummary));

                yield return null;
                controller.View.TouchButton.onClick.Invoke();
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.Finished));
                Assert.That(controller.View.PresentationRoot.activeSelf,
                    Is.False);
            }
            finally
            {
                Restore(previous);
            }
        }

        [UnityTest]
        public IEnumerator TenPull_PreservesOrderAndSkipShowsSummary()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Setup(out GachaSceneInstaller installer);
                GachaPresentationController controller =
                    installer.PresentationController;
                controller.Configure(
                    controller.View,
                    installer.ResultPresenter,
                    60f,
                    60f,
                    0f,
                    3f);
                GachaTransactionPullResult[] pulls = Enumerable.Range(0, 10)
                    .Select(index => CurrencyPull(index, index + 1))
                    .ToArray();

                Assert.That(controller.Play(Transaction(pulls)), Is.True);
                Assert.That(controller.View.ItemText.text, Is.EqualTo("+1"));
                for (int index = 0; index < 3; index++)
                {
                    controller.View.TouchButton.onClick.Invoke();
                    yield return null;
                    controller.View.TouchButton.onClick.Invoke();
                    yield return null;
                    Assert.That(controller.CurrentIndex, Is.EqualTo(index + 1));
                    Assert.That(controller.View.ItemText.text,
                        Is.EqualTo("+" + (index + 2)));
                }

                controller.View.SkipButton.onClick.Invoke();
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.FinalSummary));
                Assert.That(installer.ResultPresenter.View.Slots
                    .Count(slot => slot.gameObject.activeSelf),
                    Is.EqualTo(10));
            }
            finally
            {
                Restore(previous);
            }
        }

        [Test]
        public void ClosedDelayHoldsCurtainAndUsesUnscaledTime()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            float previousTimeScale = Time.timeScale;
            try
            {
                const float closedDelay = 0.3f;
                Setup(out GachaSceneInstaller installer);
                GachaPresentationController controller =
                    installer.PresentationController;
                var timeSource = new ManualPresentationTimeSource();
                controller.Configure(
                    controller.View,
                    installer.ResultPresenter,
                    1f,
                    60f,
                    0.15f,
                    closedDelay,
                    3f,
                    timeSource);
                RectTransform leftCurtain = GetPrivateField<RectTransform>(
                    controller.View,
                    "leftCurtain");
                Vector2 closedPosition = leftCurtain.anchoredPosition;

                Time.timeScale = 0f;
                Assert.That(controller.Play(Transaction(
                    CurrencyPull(0, 1000))), Is.True);
                Assert.That(leftCurtain.anchoredPosition,
                    Is.EqualTo(closedPosition));

                InvokePrivate(controller, "StopFlow");
                int version = GetPrivateField<int>(
                    controller,
                    "presentationVersion");
                var closedDelayRoutine = (IEnumerator)InvokePrivate(
                    controller,
                    "WaitAtClosedCurtain",
                    version);

                Assert.That(closedDelayRoutine.MoveNext(), Is.True);

                timeSource.AdvanceTo(0.1);
                Assert.That(closedDelayRoutine.MoveNext(), Is.True);

                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.Opening));
                Assert.That(leftCurtain.anchoredPosition,
                    Is.EqualTo(closedPosition));
                Assert.That(Time.timeScale, Is.Zero);

                timeSource.AdvanceTo(0.29);
                Assert.That(closedDelayRoutine.MoveNext(), Is.True);

                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.Opening));
                Assert.That(leftCurtain.anchoredPosition,
                    Is.EqualTo(closedPosition));

                timeSource.AdvanceTo(closedDelay);
                Assert.That(closedDelayRoutine.MoveNext(), Is.False);

                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.Opening));
                Assert.That(leftCurtain.anchoredPosition,
                    Is.EqualTo(closedPosition));

                InvokePrivate(controller, "StopFlow");
                Action enterShowingResult = () => InvokePrivate(
                    controller,
                    "EnterShowingResult");
                var opening = (IEnumerator)InvokePrivate(
                    controller,
                    "AnimateCurtain",
                    0f,
                    1f,
                    1f,
                    version,
                    enterShowingResult);

                Assert.That(opening.MoveNext(), Is.True);
                Assert.That(leftCurtain.anchoredPosition,
                    Is.EqualTo(closedPosition));

                timeSource.AdvanceTo(0.4);
                Assert.That(opening.MoveNext(), Is.True);

                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.Opening));
                Assert.That(leftCurtain.anchoredPosition,
                    Is.Not.EqualTo(closedPosition));
                Assert.That(Time.timeScale, Is.Zero);

                timeSource.AdvanceTo(1.31);
                Assert.That(opening.MoveNext(), Is.True);
                Assert.That(opening.MoveNext(), Is.False);

                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.ShowingResult));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Restore(previous);
            }
        }

        [UnityTest]
        public IEnumerator UnityUnscaledTimeOpensCurtainWhenTimeScaleIsZero()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            float previousTimeScale = Time.timeScale;
            try
            {
                Setup(out GachaSceneInstaller installer);
                GachaPresentationController controller =
                    installer.PresentationController;
                controller.Configure(
                    controller.View,
                    installer.ResultPresenter,
                    0.05f,
                    60f,
                    0.15f,
                    0f,
                    3f);

                Time.timeScale = 0f;
                Assert.That(controller.Play(Transaction(
                    CurrencyPull(0, 1000))), Is.True);

                yield return null;
                float timeout = Time.realtimeSinceStartup + 2f;
                while (controller.State == GachaPresentationState.Opening
                    && Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.ShowingResult));
                Assert.That(Time.timeScale, Is.Zero);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Restore(previous);
            }
        }

        [UnityTest]
        public IEnumerator ClosedDelayAppliesToNextResultAndTouchRevealsOnlyIt()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Setup(out GachaSceneInstaller installer);
                GachaPresentationController controller =
                    installer.PresentationController;
                controller.Configure(
                    controller.View,
                    installer.ResultPresenter,
                    60f,
                    60f,
                    0f,
                    60f,
                    3f);
                RectTransform leftCurtain = GetPrivateField<RectTransform>(
                    controller.View,
                    "leftCurtain");
                GachaTransactionPullResult[] pulls = Enumerable.Range(0, 10)
                    .Select(index => CurrencyPull(index, index + 1))
                    .ToArray();

                Assert.That(controller.Play(Transaction(pulls)), Is.True);
                Vector2 closedPosition = leftCurtain.anchoredPosition;
                controller.View.TouchButton.onClick.Invoke();
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.ShowingResult));
                Assert.That(controller.CurrentIndex, Is.Zero);

                yield return null;
                controller.View.TouchButton.onClick.Invoke();

                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.Opening));
                Assert.That(controller.CurrentIndex, Is.EqualTo(1));
                Assert.That(controller.View.ItemText.text, Is.EqualTo("+2"));
                Assert.That(leftCurtain.anchoredPosition,
                    Is.EqualTo(closedPosition));

                yield return null;
                controller.View.TouchButton.onClick.Invoke();

                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.ShowingResult));
                Assert.That(controller.CurrentIndex, Is.EqualTo(1));
                Assert.That(controller.View.ItemText.text, Is.EqualTo("+2"));
            }
            finally
            {
                Restore(previous);
            }
        }

        [UnityTest]
        public IEnumerator ZeroDelayOpensImmediatelyAndSkipCancelsPendingDelay()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Setup(out GachaSceneInstaller installer);
                GachaPresentationController controller =
                    installer.PresentationController;
                controller.Configure(
                    controller.View,
                    installer.ResultPresenter,
                    0f,
                    60f,
                    0f,
                    0f,
                    3f);

                Assert.That(controller.Play(Transaction(
                    CurrencyPull(0, 1))), Is.True);
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.ShowingResult));

                controller.Configure(
                    controller.View,
                    installer.ResultPresenter,
                    0f,
                    60f,
                    0f,
                    60f,
                    3f);
                Assert.That(controller.Play(Transaction(
                    CurrencyPull(0, 3000))), Is.True);
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.Opening));

                controller.View.SkipButton.onClick.Invoke();
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.FinalSummary));
                Assert.That(installer.ResultPresenter.IsOpen, Is.True);

                yield return null;

                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.FinalSummary));
                Assert.That(controller.CurrentIndex, Is.Zero);
            }
            finally
            {
                Restore(previous);
            }
        }

        [Test]
        public void InstallerDefaultsDelayAndNegativeDelayIsRejected()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Setup(out GachaSceneInstaller installer);
                var serialized = new SerializedObject(installer);
                Assert.That(serialized.FindProperty("curtainClosedDelay")
                    .floatValue, Is.EqualTo(0.3f).Within(0.0001f));

                GachaPresentationController controller =
                    installer.PresentationController;
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    controller.Configure(
                        controller.View,
                        installer.ResultPresenter,
                        0.7f,
                        1f,
                        0.15f,
                        -0.01f,
                        3f));
            }
            finally
            {
                Restore(previous);
            }
        }

        [Test]
        public void IndividualEntriesUsePreviewAwakeningAndCurrencyRules()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Setup(out GachaSceneInstaller installer);
                GachaResultPresenter presenter = installer.ResultPresenter;

                AssertEntry(
                    presenter,
                    CharacterPull(
                        RewardGrantKind.NewCharacter,
                        null,
                        0,
                        null,
                        0),
                    GachaResultDisplayKind.Character,
                    string.Empty,
                    false);
                AssertEntry(
                    presenter,
                    CharacterPull(
                        RewardGrantKind.CharacterAwakeningIncreased,
                        0,
                        1,
                        null,
                        0),
                    GachaResultDisplayKind.Character,
                    "0 \u2192 1",
                    true);
                AssertEntry(
                    presenter,
                    CharacterPull(
                        RewardGrantKind.CharacterAwakeningIncreased,
                        5,
                        6,
                        null,
                        0),
                    GachaResultDisplayKind.Character,
                    "5 \u2192 6",
                    true);
                AssertEntry(
                    presenter,
                    CharacterPull(
                        RewardGrantKind
                            .MaxAwakeningCharacterConvertedToHeroTokens,
                        6,
                        6,
                        RewardCurrencyKind.HeroTokens,
                        60),
                    GachaResultDisplayKind.Character,
                    "+60",
                    true);
                AssertEntry(
                    presenter,
                    RelicPull(
                        RewardGrantKind.NewRelicInstanceCreated,
                        null,
                        0,
                        true),
                    GachaResultDisplayKind.Relic,
                    string.Empty,
                    false);
                AssertEntry(
                    presenter,
                    RelicPull(
                        RewardGrantKind.RelicAwakeningIncreased,
                        0,
                        1,
                        false),
                    GachaResultDisplayKind.Relic,
                    "0 \u2192 1",
                    true);
                AssertEntry(
                    presenter,
                    RelicPull(
                        RewardGrantKind.RelicAwakeningIncreased,
                        4,
                        5,
                        false),
                    GachaResultDisplayKind.Relic,
                    "4 \u2192 5",
                    true);
                AssertCurrencyEntry(
                    presenter,
                    GachaRewardKind.HeroTokens,
                    RewardCurrencyKind.HeroTokens,
                    3);
                AssertCurrencyEntry(
                    presenter,
                    GachaRewardKind.RelicTokens,
                    RewardCurrencyKind.RelicTokens,
                    1);
                AssertCurrencyEntry(
                    presenter,
                    GachaRewardKind.BattleRecords,
                    RewardCurrencyKind.BattleRecords,
                    12000);
            }
            finally
            {
                Restore(previous);
            }
        }

        [Test]
        public void NewPlayCancelsPreviousAndInvalidIndividualFallsBack()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Setup(out GachaSceneInstaller installer);
                GachaPresentationController controller =
                    installer.PresentationController;
                controller.Configure(
                    controller.View,
                    installer.ResultPresenter,
                    60f,
                    60f,
                    60f,
                    3f);

                Assert.That(controller.Play(Transaction(
                    CurrencyPull(0, 1))), Is.True);
                Assert.That(controller.Play(Transaction(
                    CurrencyPull(0, 3000))), Is.True);
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.Opening));
                Assert.That(controller.View.ItemText.text,
                    Is.EqualTo("+3,000"));

                Assert.That(controller.Play(Transaction(CharacterPull(
                    RewardGrantKind
                        .MaxAwakeningCharacterConvertedToHeroTokens,
                    6,
                    6,
                    RewardCurrencyKind.HeroTokens,
                    1))), Is.True);
                Assert.That(controller.State,
                    Is.EqualTo(GachaPresentationState.FinalSummary));
                Assert.That(installer.ResultPresenter.IsOpen, Is.True);
            }
            finally
            {
                Restore(previous);
            }
        }

        private static void AssertEntry(
            GachaResultPresenter presenter,
            GachaTransactionPullResult pull,
            GachaResultDisplayKind kind,
            string overlayText,
            bool hasOverlay)
        {
            bool success = presenter.TryCreateIndividualDisplayEntry(
                pull,
                out GachaIndividualDisplayEntry entry,
                out string error);
            Assert.That(success, Is.True, error);
            Assert.That(entry.Kind, Is.EqualTo(kind));
            Assert.That(entry.PrimarySprite, Is.Not.Null);
            Assert.That(entry.OverlayText, Is.EqualTo(overlayText));
            Assert.That(entry.HasOverlay, Is.EqualTo(hasOverlay));
        }

        private static void AssertCurrencyEntry(
            GachaResultPresenter presenter,
            GachaRewardKind rewardKind,
            RewardCurrencyKind currencyKind,
            long amount)
        {
            GachaTransactionPullResult pull = Pull(
                0,
                rewardKind,
                null,
                RewardGrantKind.CurrencyGranted,
                null,
                null,
                currencyKind,
                amount,
                false);
            bool success = presenter.TryCreateIndividualDisplayEntry(
                pull,
                out GachaIndividualDisplayEntry entry,
                out string error);
            Assert.That(success, Is.True, error);
            Assert.That(entry.Kind, Is.EqualTo(GachaResultDisplayKind.Item));
            Assert.That(entry.ItemAmount, Is.EqualTo(amount));
            Assert.That(entry.HasOverlay, Is.False);
        }

        private static void Setup(out GachaSceneInstaller installer)
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            Transform content = scene.GetRootGameObjects()
                .Single(root => root.name == "GachaCanvas")
                .transform.Find("SafeAreaRoot/GamaeViewport/GameContent");
            installer = content.GetComponent<GachaSceneInstaller>();
            installer.Install();
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabasePath);
            database.Initialize();
            installer.PresentationController.Initialize(database);
        }

        private static void Restore(SceneSetup[] previous)
        {
            if (previous.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
            else
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }

        private static GachaTransactionPullResult CurrencyPull(
            int index,
            long amount)
        {
            return Pull(
                index,
                GachaRewardKind.BattleRecords,
                null,
                RewardGrantKind.CurrencyGranted,
                null,
                null,
                RewardCurrencyKind.BattleRecords,
                amount,
                false);
        }

        private static GachaTransactionPullResult CharacterPull(
            RewardGrantKind grantKind,
            int? previous,
            int? next,
            RewardCurrencyKind? currencyKind,
            long currencyAmount)
        {
            return Pull(
                0,
                GachaRewardKind.RandomCharacter,
                CharacterId,
                grantKind,
                previous,
                next,
                currencyKind,
                currencyAmount,
                false);
        }

        private static GachaTransactionPullResult RelicPull(
            RewardGrantKind grantKind,
            int? previous,
            int? next,
            bool createdAfterMax)
        {
            return Pull(
                0,
                GachaRewardKind.RandomRelic,
                RelicId,
                grantKind,
                previous,
                next,
                null,
                0,
                createdAfterMax);
        }

        private static GachaTransactionPullResult Pull(
            int index,
            GachaRewardKind rewardKind,
            string contentId,
            RewardGrantKind grantKind,
            int? previous,
            int? next,
            RewardCurrencyKind? currencyKind,
            long currencyAmount,
            bool createdAfterMax)
        {
            long amount = currencyAmount > 0 ? currencyAmount : 1;
            var source = (GachaPullResult)CreateInternal(
                typeof(GachaPullResult),
                index,
                GachaScreenController.CharacterGachaId,
                rewardKind,
                contentId,
                amount,
                false,
                0,
                0);
            var request = new RewardGrantRequest(
                index,
                rewardKind,
                contentId,
                amount);
            CurrencyGrantResult currency = currencyKind.HasValue
                ? (CurrencyGrantResult)CreateInternal(
                    typeof(CurrencyGrantResult),
                    currencyKind.Value,
                    currencyAmount,
                    0L,
                    currencyAmount)
                : null;
            var grant = (RewardGrantResult)CreateInternal(
                typeof(RewardGrantResult),
                request,
                grantKind,
                contentId,
                rewardKind == GachaRewardKind.RandomRelic
                    ? "relic-instance-" + index
                    : null,
                previous,
                next,
                currency,
                createdAfterMax);
            return (GachaTransactionPullResult)CreateInternal(
                typeof(GachaTransactionPullResult),
                source,
                grant);
        }

        private static GachaTransactionResult Transaction(
            params GachaTransactionPullResult[] pulls)
        {
            var state = (GachaTransactionStateResult)CreateInternal(
                typeof(GachaTransactionStateResult),
                GachaScreenController.CharacterGachaId,
                0,
                (long)pulls.Length);
            return (GachaTransactionResult)CreateInternal(
                typeof(GachaTransactionResult),
                GachaScreenController.CharacterGachaId,
                GachaKind.Character,
                pulls.Length,
                100L * pulls.Length,
                10000L,
                10000L - 100L * pulls.Length,
                pulls,
                state,
                false);
        }

        private static object CreateInternal(
            Type type,
            params object[] arguments)
        {
            return Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                arguments,
                null);
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

            public double UnscaledTime => currentTime;

            public void AdvanceTo(double time)
            {
                Assert.That(time, Is.GreaterThanOrEqualTo(currentTime));
                currentTime = time;
            }
        }
    }
}
