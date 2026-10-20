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
using ValorChronicle.Localization;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.Migration;
using ValorChronicle.Save.Processing;
using ValorChronicle.Save.Repository;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Services;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.PlayMode
{
    public sealed class BattleResultPresentationTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int index = createdObjects.Count - 1; index >= 0; index--)
                {
                    if (createdObjects[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(
                            createdObjects[index]);
                    }
                }
            }
            finally
            {
                createdObjects.Clear();
                Time.timeScale = previousTimeScale;
            }
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
        public void LocaleChanged_RerendersPersistedValuesWithoutChangingResult()
        {
            BattleResultView view = CreateView(out Sprite[] sprites);
            LocalizationService localization = CreateLocalizationService();
            view.InitializeLocalization(localization);
            SaveService saveService = LoadedService(Repository());
            BattlePersistedResult persisted = Persist(
                saveService,
                FinalResult(BattleResultKind.Victory, 100000L));

            view.Render(persisted);
            Sprite gradeBefore = view.ScoreGrade.sprite;
            Assert.That(view.CurrentResult, Is.SameAs(persisted));
            Assert.That(view.BestScoreText.text, Is.EqualTo("Best: 118,000"));
            Assert.That(view.ResultText.text, Is.EqualTo("Score: 118,000"));
            Assert.That(view.TurnText.text, Is.EqualTo("Turn: 10/25"));
            Assert.That(view.DamageScoreText.text,
                Is.EqualTo("Damage: 100,000"));
            Assert.That(view.TurnLeftScoreText.text,
                Is.EqualTo("Turn Bonus: 18,000"));

            Assert.That(localization.SetLocale("ko"), Is.True);

            Assert.That(view.CurrentResult, Is.SameAs(persisted));
            Assert.That(view.BestScoreText.text,
                Is.EqualTo("최고 기록: 118,000"));
            Assert.That(view.ResultText.text, Is.EqualTo("점수: 118,000"));
            Assert.That(view.TurnText.text, Is.EqualTo("턴: 10/25"));
            Assert.That(view.DamageScoreText.text,
                Is.EqualTo("피해 점수: 100,000"));
            Assert.That(view.TurnLeftScoreText.text,
                Is.EqualTo("잔여 턴 보너스: 18,000"));
            Assert.That(view.RewardText.text, Is.EqualTo("420"));
            Assert.That(view.InitialRewardText.text, Is.EqualTo("700"));
            Assert.That(view.ScoreGrade.sprite, Is.SameAs(gradeBefore));
            Assert.That(view.ScoreGrade.sprite, Is.SameAs(sprites[5]));
            Assert.That(view.BestScoreIcon.gameObject.activeSelf, Is.True);

            Assert.That(localization.SetLocale("en"), Is.True);
            Assert.That(view.ResultText.text, Is.EqualTo("Score: 118,000"));
        }

        [Test]
        public void DisabledView_ReopensWithLatestLocaleWithoutSubscriptionLeak()
        {
            BattleResultView view = CreateView(out _);
            LocalizationService localization = CreateLocalizationService();
            view.InitializeLocalization(localization);
            BattlePersistedResult persisted = Persist(
                LoadedService(Repository()),
                FinalResult(BattleResultKind.Victory, 100000L));
            view.Render(persisted);

            Assert.That(CountTargetSubscribers(
                    localization,
                    "LocaleChanged",
                    view),
                Is.EqualTo(1));
            view.gameObject.SetActive(false);
            Assert.That(CountTargetSubscribers(
                    localization,
                    "LocaleChanged",
                    view),
                Is.Zero);

            Assert.That(localization.SetLocale("ko"), Is.True);
            Assert.That(view.ResultText.text, Is.EqualTo("Score: 118,000"));
            view.gameObject.SetActive(true);

            Assert.That(view.ResultText.text, Is.EqualTo("점수: 118,000"));
            Assert.That(view.CurrentResult, Is.SameAs(persisted));
            Assert.That(CountTargetSubscribers(
                    localization,
                    "LocaleChanged",
                    view),
                Is.EqualTo(1));

            view.ResultText.text = "unchanged";
            Assert.That(localization.SetLocale("ko"), Is.False);
            Assert.That(view.ResultText.text, Is.EqualTo("unchanged"));
        }

        [Test]
        public void OpenResult_LocaleChangeDoesNotPersistOrRestartPresentation()
        {
            var repository = Repository();
            LocalizationService localization = CreateLocalizationService();
            PresentationFixture fixture = CreatePresentation(
                delay: 0f,
                repository,
                localization);
            fixture.Source.Publish(
                FinalResult(BattleResultKind.Victory, 100000L));
            BattlePersistedResult presented =
                fixture.Controller.LastPresentedResult;
            int writesBefore = repository.Count(
                nameof(FakeSaveRepository.WriteTemp));

            Assert.That(fixture.View.gameObject.activeSelf, Is.True);
            Assert.That(fixture.Controller.HasPendingPresentation, Is.False);
            Assert.That(localization.SetLocale("ko"), Is.True);

            Assert.That(fixture.Controller.LastPresentedResult,
                Is.SameAs(presented));
            Assert.That(fixture.View.CurrentResult, Is.SameAs(presented));
            Assert.That(fixture.Controller.HasPendingPresentation, Is.False);
            Assert.That(repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                Is.EqualTo(writesBefore));
            Assert.That(fixture.View.ResultText.text,
                Is.EqualTo("점수: 118,000"));
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

        [TestCase(BattleResultKind.Victory)]
        [TestCase(BattleResultKind.Defeat)]
        [TestCase(BattleResultKind.TurnLimitReached)]
        public void ResultPresented_FiresAfterPanelIsShownAndOnlyOnce(
            BattleResultKind resultKind)
        {
            PresentationFixture fixture = CreatePresentation(delay: 0f);
            int presentedCount = 0;
            BattleResultKind presentedKind = BattleResultKind.None;
            fixture.Controller.ResultPresented += kind =>
            {
                Assert.That(fixture.View.gameObject.activeSelf, Is.True);
                presentedCount++;
                presentedKind = kind;
            };
            BattleFinalResult result = FinalResult(resultKind, 100000L);

            fixture.Source.Publish(result);
            fixture.Source.Publish(result);

            Assert.That(presentedCount, Is.EqualTo(1));
            Assert.That(presentedKind, Is.EqualTo(resultKind));
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
            FakeSaveRepository repository = null,
            LocalizationService localization = null)
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
            if (localization != null)
            {
                view.InitializeLocalization(localization);
            }
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
            const int turnLimit = 25;
            int finishedTurn = resultKind == BattleResultKind.TurnLimitReached
                ? turnLimit
                : 10;
            return BattleResultFinalizer.Create(
                new BattleFinalizationInput(
                    resultKind,
                    "boss_test",
                    BattleDifficultyIds.Normal,
                    100000L,
                    turnLimit,
                    finishedTurn,
                    damageScore,
                    BattleResultBalanceDefaults.Create()));
        }

        private static FakeSaveRepository Repository()
        {
            return new FakeSaveRepository
            {
                MainText = new NewtonsoftJsonSaveSerializer().Serialize(
                    new NewProfileFactory().Create("profile_test", 10))
            };
        }

        private static SaveService LoadedService(FakeSaveRepository repository)
        {
            var cloner = new SaveDataCloner();
            var service = new SaveService(
                repository,
                new NewtonsoftJsonSaveSerializer(),
                new NewProfileFactory(),
                cloner,
                new SaveMigrationRunner(
                    cloner,
                    new ISaveMigrationStep[]
                    {
                        new V1ToV2PartyMigration(),
                        new V2ToV3RelicEquipMigration()
                    }),
                new SaveValidationProcessor(),
                new ExistingSaveContentCatalog(),
                new FixedUnixTimeProvider());
            SaveLoadResult load = service.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return service;
        }

        private static LocalizationService CreateLocalizationService()
        {
            const string tsv =
                "key\ten\tko\n"
                + "ui.battle.result.best_score_format"
                + "\tBest: {score}\t최고 기록: {score}\n"
                + "ui.battle.result.score_format"
                + "\tScore: {score}\t점수: {score}\n"
                + "ui.battle.result.turn_format"
                + "\tTurn: {current}/{limit}"
                + "\t턴: {current}/{limit}\n"
                + "ui.battle.result.damage_format"
                + "\tDamage: {score}\t피해 점수: {score}\n"
                + "ui.battle.result.turn_bonus_format"
                + "\tTurn Bonus: {score}"
                + "\t잔여 턴 보너스: {score}";
            LocalizationTable table = LocalizationTableParser.Parse(
                "battle_result.tsv",
                tsv);
            return new LocalizationService(new[] { table });
        }

        private static int CountTargetSubscribers(
            object source,
            string eventName,
            object target)
        {
            FieldInfo field = source.GetType().GetField(
                eventName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            var callback = field?.GetValue(source) as Delegate;
            if (callback == null)
            {
                return 0;
            }

            int count = 0;
            Delegate[] callbacks = callback.GetInvocationList();
            for (int index = 0; index < callbacks.Length; index++)
            {
                if (ReferenceEquals(callbacks[index].Target, target))
                {
                    count++;
                }
            }

            return count;
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

        private sealed class FakeSaveRepository : ISaveRepository
        {
            public string MainText { get; set; }
            public string BackupText { get; set; }
            public string TempText { get; set; }
            public bool FailWriteTemp { get; set; }
            public List<string> Calls { get; } = new List<string>();

            public bool MainExists => MainText != null;
            public bool BackupExists => BackupText != null;
            public bool TempExists => TempText != null;

            public int Count(string operation) =>
                Calls.FindAll(value => value == operation).Count;

            public void EnsureRootDirectory() =>
                Calls.Add(nameof(EnsureRootDirectory));

            public string ReadMain()
            {
                Calls.Add(nameof(ReadMain));
                return MainText;
            }

            public string ReadBackup()
            {
                Calls.Add(nameof(ReadBackup));
                return BackupText;
            }

            public string ReadTemp()
            {
                Calls.Add(nameof(ReadTemp));
                return TempText;
            }

            public void WriteTemp(string contents)
            {
                Calls.Add(nameof(WriteTemp));
                if (FailWriteTemp)
                {
                    throw new InvalidOperationException(
                        "Injected temp write failure.");
                }

                TempText = contents;
            }

            public void CopyMainToBackup()
            {
                Calls.Add(nameof(CopyMainToBackup));
                BackupText = MainText;
            }

            public void PromoteTempToMain()
            {
                Calls.Add(nameof(PromoteTempToMain));
                MainText = TempText;
                TempText = null;
            }

            public void DeleteTempIfExists()
            {
                Calls.Add(nameof(DeleteTempIfExists));
                TempText = null;
            }
        }

        private sealed class FixedUnixTimeProvider : IUnixTimeProvider
        {
            public long GetUtcUnixTimeSeconds() => 100;
        }

        private sealed class ExistingSaveContentCatalog : ISaveContentCatalog
        {
            public SaveContentLookupResult LookupCharacter(string characterId)
                => SaveContentLookupResult.Exists;

            public SaveContentLookupResult LookupRelic(string relicDefinitionId)
                => SaveContentLookupResult.Exists;

            public SaveContentLookupResult LookupBoss(string bossId)
                => SaveContentLookupResult.Exists;

            public SaveContentLookupResult LookupBossDifficulty(
                string bossId,
                string difficultyId) => SaveContentLookupResult.Exists;

            public SaveContentLookupResult LookupGacha(string gachaId)
                => SaveContentLookupResult.Exists;

            public SaveContentLookupResult LookupRewardGrade(
                string bossId,
                string difficultyId,
                string gradeId) => SaveContentLookupResult.Exists;
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
