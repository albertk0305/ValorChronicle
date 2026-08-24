using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Battle.Results;
using ValorChronicle.Battle.Results.Persistence;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleResultPresentationTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [UnitySetUp]
        public IEnumerator EnterRuntimeMode()
        {
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ExitRuntimeMode()
        {
            yield return new ExitPlayMode();
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void Render_UsesPersistedValuesAndClearsPriorBestState()
        {
            BattleResultView view = CreateView(out _);
            SaveService saveService = LoadedService(Repository());
            BattlePersistedResult first = Persist(
                saveService,
                FinalResult(BattleResultKind.Victory, 100000L));

            view.Render(first);

            Assert.That(view.BestScoreText.text, Is.EqualTo("Best: 118,000"));
            Assert.That(view.ResultText.text, Is.EqualTo("Score: 118,000"));
            Assert.That(view.TurnText.text, Is.EqualTo("Turn: 10/25"));
            Assert.That(view.DamageScoreText.text,
                Is.EqualTo("Damage: 100,000"));
            Assert.That(view.TurnLeftScoreText.text,
                Is.EqualTo("Turn Bonus: 18,000"));
            Assert.That(view.RewardText.text, Is.EqualTo("420"));
            Assert.That(view.InitialRewardText.text, Is.EqualTo("700"));
            Assert.That(view.BestScoreIcon.gameObject.activeSelf, Is.True);

            BattlePersistedResult equalScore = Persist(
                saveService,
                FinalResult(BattleResultKind.Victory, 100000L));
            view.Render(equalScore);

            Assert.That(equalScore.IsNewHighScore, Is.False);
            Assert.That(view.BestScoreIcon.gameObject.activeSelf, Is.False);
            Assert.That(view.InitialRewardText.text, Is.EqualTo("0"));
        }

        [Test]
        public void GradeSprites_MapAllRewardGrades()
        {
            BattleResultView view = CreateView(out Sprite[] sprites);
            BattleGrade[] grades =
            {
                BattleGrade.C,
                BattleGrade.B,
                BattleGrade.A,
                BattleGrade.S,
                BattleGrade.SS,
                BattleGrade.SSS
            };

            for (int index = 0; index < grades.Length; index++)
            {
                Assert.That(view.GetGradeSprite(grades[index]),
                    Is.SameAs(sprites[index]));
            }
        }

        [Test]
        public void BelowC_ClearsPriorGradeInsteadOfUsingCSprite()
        {
            BattleResultView view = CreateView(out Sprite[] sprites);
            SaveService saveService = LoadedService(Repository());
            view.Render(Persist(
                saveService,
                FinalResult(BattleResultKind.Victory, 100000L)));
            Assert.That(view.ScoreGrade.sprite, Is.SameAs(sprites[5]));
            Assert.That(view.ScoreGrade.gameObject.activeSelf, Is.True);

            view.Render(Persist(
                saveService,
                FinalResult(BattleResultKind.Defeat, 10000L)));

            Assert.That(view.ScoreGrade.sprite, Is.Null);
            Assert.That(view.ScoreGrade.gameObject.activeSelf, Is.False);
            Assert.That(view.GetGradeSprite(BattleGrade.BelowC), Is.Null);
        }

        [UnityTest]
        public IEnumerator ResultPersisted_RendersThenShowsAfterConfiguredDelay()
        {
            PresentationFixture fixture = CreatePresentation(delay: 0.04f);
            Assert.That(fixture.View.gameObject.activeSelf, Is.False);

            fixture.Source.Publish(
                FinalResult(BattleResultKind.Victory, 100000L));

            Assert.That(fixture.View.ResultText.text,
                Is.EqualTo("Score: 118,000"));
            Assert.That(fixture.View.gameObject.activeSelf, Is.False);
            Assert.That(fixture.Controller.HasPendingPresentation, Is.True);

            yield return new WaitForSeconds(0.08f);

            Assert.That(fixture.View.gameObject.activeSelf, Is.True);
            Assert.That(fixture.Controller.HasPendingPresentation, Is.False);
            Assert.That(fixture.Controller.LastPresentedResult, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SaveFailure_StaysHiddenAndRetryCanPresent()
        {
            var repository = Repository();
            PresentationFixture fixture = CreatePresentation(
                delay: 0f,
                repository);
            repository.FailWriteTemp = true;
            LogAssert.Expect(
                LogType.Error,
                new Regex("BattleResultPersistence.*Save failed"));

            fixture.Source.Publish(
                FinalResult(BattleResultKind.Victory, 100000L));

            Assert.That(fixture.View.gameObject.activeSelf, Is.False);
            Assert.That(fixture.Coordinator.HasPendingPersistence, Is.True);
            Assert.That(fixture.Controller.LastPresentedResult, Is.Null);

            repository.FailWriteTemp = false;
            fixture.Coordinator.RetryPendingPersistence();
            yield return null;

            Assert.That(fixture.View.gameObject.activeSelf, Is.True);
            Assert.That(fixture.Controller.LastPresentedResult, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator DuplicateResult_DoesNotRestartPendingPresentation()
        {
            PresentationFixture fixture = CreatePresentation(delay: 0.04f);
            BattleFinalResult finalResult =
                FinalResult(BattleResultKind.Victory, 100000L);

            fixture.Source.Publish(finalResult);
            fixture.Source.Publish(finalResult);

            Assert.That(fixture.Controller.HasPendingPresentation, Is.True);
            yield return new WaitForSeconds(0.08f);

            Assert.That(fixture.View.gameObject.activeSelf, Is.True);
            Assert.That(fixture.Controller.HasPendingPresentation, Is.False);
        }

        [UnityTest]
        public IEnumerator Disable_CancelsPendingDelayAndKeepsOverlayHidden()
        {
            PresentationFixture fixture = CreatePresentation(delay: 0.04f);
            fixture.Source.Publish(
                FinalResult(BattleResultKind.Victory, 100000L));
            Assert.That(fixture.Controller.HasPendingPresentation, Is.True);

            fixture.Controller.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.08f);

            Assert.That(fixture.Controller.HasPendingPresentation, Is.False);
            Assert.That(fixture.View.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator Reenable_ReplaysStoredPersistedResult()
        {
            PresentationFixture fixture = CreatePresentation(delay: 0f);
            fixture.Source.Publish(
                FinalResult(BattleResultKind.Victory, 100000L));
            Assert.That(fixture.View.gameObject.activeSelf, Is.True);

            fixture.Controller.gameObject.SetActive(false);
            Assert.That(fixture.View.gameObject.activeSelf, Is.False);
            fixture.Controller.gameObject.SetActive(true);
            yield return null;

            Assert.That(fixture.View.gameObject.activeSelf, Is.True);
            Assert.That(fixture.Controller.LastPresentedResult,
                Is.SameAs(fixture.Coordinator.LastPersistedResult));
        }

        private PresentationFixture CreatePresentation(
            float delay,
            FakeSaveRepository repository = null)
        {
            var root = new GameObject("BattleResultPresentationTests");
            root.SetActive(false);
            createdObjects.Add(root);
            BattleHudController hud = root.AddComponent<BattleHudController>();
            SetField(hud, "resultOverlayDelaySeconds", delay);
            var overlay = new GameObject(
                "BattleResultOverlay",
                typeof(RectTransform));
            overlay.transform.SetParent(root.transform, false);
            BattleResultView view = CreateViewOn(overlay, out _);
            BattleResultPresentationController controller =
                root.AddComponent<BattleResultPresentationController>();
            SetField(controller, "resultView", view);
            SetField(controller, "battleHudController", hud);

            repository = repository ?? Repository();
            SaveService saveService = LoadedService(repository);
            var source = new FakeFinalResultSource();
            var coordinator = new BattleResultPersistenceCoordinator(
                source,
                new BattleResultPersistenceService(saveService));
            root.SetActive(true);
            controller.Initialize(coordinator);
            return new PresentationFixture(
                view,
                controller,
                coordinator,
                source);
        }

        private BattleResultView CreateView(out Sprite[] sprites)
        {
            var overlay = new GameObject(
                "BattleResultOverlay",
                typeof(RectTransform));
            createdObjects.Add(overlay);
            return CreateViewOn(overlay, out sprites);
        }

        private BattleResultView CreateViewOn(
            GameObject overlay,
            out Sprite[] sprites)
        {
            BattleResultView view = overlay.AddComponent<BattleResultView>();
            SetField(view, "bestScoreText", CreateText(overlay, "BestScoreText"));
            SetField(view, "bestScoreIcon", CreateImage(overlay, "BestScoreIcon"));
            SetField(view, "resultText", CreateText(overlay, "ResultText"));
            SetField(view, "turnText", CreateText(overlay, "TurnText"));
            SetField(view, "damageScoreText", CreateText(overlay, "DamageScoreText"));
            SetField(view, "turnLeftScoreText", CreateText(overlay, "TurnLeftScoreText"));
            SetField(view, "scoreGrade", CreateImage(overlay, "ScoreGrade"));
            SetField(view, "rewardText", CreateText(overlay, "RewardText"));
            SetField(view, "initialRewardText", CreateText(overlay, "InitialRewardText"));
            sprites = new Sprite[6];
            string[] fields =
                { "gradeC", "gradeB", "gradeA", "gradeS", "gradeSS", "gradeSSS" };
            for (int index = 0; index < sprites.Length; index++)
            {
                var texture = new Texture2D(1, 1);
                createdObjects.Add(texture);
                sprites[index] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    Vector2.zero);
                createdObjects.Add(sprites[index]);
                SetField(view, fields[index], sprites[index]);
            }

            return view;
        }

        private static TMP_Text CreateText(GameObject parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<TextMeshProUGUI>();
        }

        private static Image CreateImage(GameObject parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<Image>();
        }

        private static BattlePersistedResult Persist(
            SaveService saveService,
            BattleFinalResult finalResult)
        {
            BattleResultPersistenceOperationResult operation =
                new BattleResultPersistenceService(saveService).Persist(
                    finalResult);
            Assert.That(operation.IsSuccess, Is.True);
            return operation.PersistedResult;
        }

        private static BattleFinalResult FinalResult(
            BattleResultKind resultKind,
            long damageScore)
        {
            return BattleResultFinalizer.Create(
                new BattleFinalizationInput(
                    resultKind,
                    "boss_test",
                    BattleDifficultyIds.Normal,
                    100000L,
                    25,
                    10,
                    damageScore,
                    BattleResultBalanceDefaults.Create()));
        }

        private static FakeSaveRepository Repository()
        {
            return new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson()
            };
        }

        private static SaveService LoadedService(FakeSaveRepository repository)
        {
            SaveService service = SaveServiceTestFactory.Create(repository);
            SaveLoadResult load = service.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return service;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private sealed class FakeFinalResultSource : IBattleFinalResultSource
        {
            public event Action<BattleFinalResult> ResultFinalized;

            public void Publish(BattleFinalResult result)
            {
                ResultFinalized?.Invoke(result);
            }
        }

        private sealed class PresentationFixture
        {
            public PresentationFixture(
                BattleResultView view,
                BattleResultPresentationController controller,
                BattleResultPersistenceCoordinator coordinator,
                FakeFinalResultSource source)
            {
                View = view;
                Controller = controller;
                Coordinator = coordinator;
                Source = source;
            }

            public BattleResultView View { get; }
            public BattleResultPresentationController Controller { get; }
            public BattleResultPersistenceCoordinator Coordinator { get; }
            public FakeFinalResultSource Source { get; }
        }
    }
}
