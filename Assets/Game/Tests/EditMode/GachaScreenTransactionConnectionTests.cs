using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Gacha.Persistence;
using ValorChronicle.Gacha.Presentation;
using ValorChronicle.Localization;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaScreenTransactionConnectionTests
    {
        private const string ScenePath = "Assets/Scenes/Gacha.unity";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";

        [TestCase(99L, false, false)]
        [TestCase(100L, true, false)]
        [TestCase(999L, true, false)]
        [TestCase(1000L, true, true)]
        public void GoldControlsSingleAndTenPullButtons(
            long gold,
            bool expectedSingle,
            bool expectedTen)
        {
            WithContext(gold, context =>
            {
                Assert.That(
                    context.View.CharacterSingleButton.interactable,
                    Is.EqualTo(expectedSingle));
                Assert.That(
                    context.View.RelicSingleButton.interactable,
                    Is.EqualTo(expectedSingle));
                Assert.That(
                    context.View.CharacterTenButton.interactable,
                    Is.EqualTo(expectedTen));
                Assert.That(
                    context.View.RelicTenButton.interactable,
                    Is.EqualTo(expectedTen));
                Assert.That(context.View.SettingButton.interactable,
                    Is.False);
                Assert.That(context.View.ExchangeButton.interactable,
                    Is.False);
            });
        }

        [Test]
        public void FourButtonsExecuteOnePersistedTransactionWithCorrectBanner()
        {
            WithContext(10000L, context =>
            {
                int openingCount = 0;
                int finishedCount = 0;
                context.Presentation.StateChanged += state =>
                {
                    if (state == GachaPresentationState.Opening)
                    {
                        openingCount++;
                    }
                };
                context.Presentation.Finished += _ => finishedCount++;
                ClickAndAssert(
                    context,
                    context.View.CharacterSingleButton,
                    GachaScreenController.CharacterGachaId,
                    1,
                    9900L,
                    1);
                ClickAndAssert(
                    context,
                    context.View.CharacterTenButton,
                    GachaScreenController.CharacterGachaId,
                    10,
                    8900L,
                    2);
                ClickAndAssert(
                    context,
                    context.View.RelicSingleButton,
                    GachaScreenController.RelicGachaId,
                    1,
                    8800L,
                    3);
                ClickAndAssert(
                    context,
                    context.View.RelicTenButton,
                    GachaScreenController.RelicGachaId,
                    10,
                    7800L,
                    4);

                ProfileSaveData profile = context.SaveService
                    .GetCurrentProfileSnapshot();
                Assert.That(profile.GachaStates.Single(state =>
                    state.GachaId
                        == GachaScreenController.CharacterGachaId)
                    .TotalPullCount, Is.EqualTo(11));
                Assert.That(profile.GachaStates.Single(state =>
                    state.GachaId
                        == GachaScreenController.RelicGachaId)
                    .TotalPullCount, Is.EqualTo(11));
                Assert.That(openingCount, Is.EqualTo(4));
                Assert.That(finishedCount, Is.EqualTo(4));
            });
        }

        [Test]
        public void InfoPanelAndResultScreenBlockTransactionsUntilClosed()
        {
            WithContext(2000L, context =>
            {
                context.Content.Find("HeroesPickup/InfoButton")
                    .GetComponent<Button>().onClick.Invoke();
                AssertAllPullButtons(context.View, false);
                int writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));
                context.View.CharacterSingleButton.onClick.Invoke();
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));

                context.Content.Find("InfoPanel/ExitButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(
                    context.View.CharacterSingleButton.interactable,
                    Is.True);

                context.View.CharacterSingleButton.onClick.Invoke();
                Assert.That(context.ResultView.IsOpen, Is.True);
                Assert.That(context.Presentation.State,
                    Is.EqualTo(GachaPresentationState.Opening));
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.True);
                AssertAllPullButtons(context.View, false);
                writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));
                context.View.RelicSingleButton.onClick.Invoke();
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));

                CloseResult(context);
                Assert.That(context.ResultView.IsOpen, Is.False);
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.False);
                Assert.That(
                    context.View.CharacterSingleButton.interactable,
                    Is.True);
            });
        }

        [Test]
        public void UnavailableIndividualPresentationUsesDirectFinalSummary()
        {
            WithContext(100L, context =>
            {
                typeof(GachaScreenController).GetField(
                    "individualPresentationReady",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(context.Controller, false);

                context.View.CharacterSingleButton.onClick.Invoke();

                Assert.That(context.Presentation.State,
                    Is.EqualTo(GachaPresentationState.Idle));
                Assert.That(context.ResultView.IsOpen, Is.True);
                Assert.That(context.ResultView.Slots.Count(slot =>
                    slot.gameObject.activeSelf), Is.EqualTo(1));
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.True);
                CloseResult(context);
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.False);
            });
        }

        [Test]
        public void DisablingScreenCancelsPresentationAndIgnoresOldCompletion()
        {
            WithContext(200L, context =>
            {
                context.View.CharacterSingleButton.onClick.Invoke();
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.True);
                Assert.That(context.Presentation.IsPresenting, Is.True);

                context.Controller.enabled = false;
                InvokeLifecycle(context.Controller, "OnDisable");

                Assert.That(context.Presentation.State,
                    Is.EqualTo(GachaPresentationState.Idle));
                Assert.That(context.Presentation.IsPresenting, Is.False);
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.False);
                Assert.That(context.SaveService
                    .GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency, Is.EqualTo(100L));

                context.Controller.enabled = true;
                InvokeLifecycle(context.Controller, "OnEnable");
                Assert.That(context.View.CharacterSingleButton.interactable,
                    Is.True);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
            });
        }

        [Test]
        public void DisablingPresentationEndsLockWithoutRepeatingTransaction()
        {
            WithContext(200L, context =>
            {
                context.View.CharacterSingleButton.onClick.Invoke();
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.True);

                context.Presentation.enabled = false;
                InvokeLifecycle(context.Presentation, "OnDisable");

                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.False);
                Assert.That(context.Presentation.State,
                    Is.EqualTo(GachaPresentationState.Finished));
                Assert.That(context.Installer.InfoPresenter.IsOpen, Is.True);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
                Assert.That(context.SaveService
                    .GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency, Is.EqualTo(100L));
            });
        }

        [Test]
        public void PersistenceFailureShowsMessageWithoutOpeningResult()
        {
            WithContext(100L, context =>
            {
                string before = context.Repository.MainText;
                context.Repository.FailWriteTemp = true;
                LogAssert.Expect(
                    LogType.Exception,
                    new Regex("Injected temp write failure"));
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("Gacha transaction failed"));

                context.View.CharacterSingleButton.onClick.Invoke();

                Assert.That(context.ResultView.IsOpen, Is.False);
                Assert.That(context.Installer.InfoPresenter.IsOpen, Is.True);
                Assert.That(context.Installer.InfoPresenter
                    .GetComponent<GachaInfoPanelView>().InfoText.text,
                    Does.Contain("could not be completed"));
                Assert.That(context.Repository.MainText, Is.EqualTo(before));
                Assert.That(context.SaveService
                    .GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency, Is.EqualTo(100L));
            });
        }

        [Test]
        public void InsufficientGoldGuardDoesNotCallSaveService()
        {
            WithContext(99L, context =>
            {
                int writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));

                bool started = context.Controller.TryExecutePull(
                    GachaScreenController.CharacterGachaId,
                    1);

                Assert.That(started, Is.False);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));
                Assert.That(context.SaveService
                    .GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency, Is.EqualTo(99L));
            });
        }

        [Test]
        public void MissingRequiredResultSpriteKeepsTransactionsDisabled()
        {
            WithContext(1000L, context =>
            {
                ProfileSaveData before = context.SaveService
                    .GetCurrentProfileSnapshot();
                int writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));
                SetPrivateField(
                    context.Installer.ResultPresenter,
                    "heroTokenSprite",
                    null);
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("Result resource validation failed"));
                LogAssert.Expect(
                    LogType.Error,
                    new Regex(
                        "The Gacha screen view is not configured\\."));

                context.Controller.Initialize(
                    context.SaveService,
                    context.Database,
                    context.Localization,
                    context.TransactionService);

                bool started = context.Controller.TryExecutePull(
                    GachaScreenController.CharacterGachaId,
                    1);
                ProfileSaveData after = context.SaveService
                    .GetCurrentProfileSnapshot();

                AssertAllPullButtons(context.View, false);
                Assert.That(context.Installer.InfoPresenter.IsOpen, Is.True);
                Assert.That(started, Is.False);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));
                Assert.That(after.Currencies.GachaCurrency,
                    Is.EqualTo(before.Currencies.GachaCurrency));
                Assert.That(after.Currencies.HeroTokens,
                    Is.EqualTo(before.Currencies.HeroTokens));
                Assert.That(after.Currencies.RelicTokens,
                    Is.EqualTo(before.Currencies.RelicTokens));
                Assert.That(after.Currencies.BattleRecords,
                    Is.EqualTo(before.Currencies.BattleRecords));
                Assert.That(
                    new SaveDataValueComparer().Equals(before, after),
                    Is.True);
                Assert.That(context.ResultView.IsOpen, Is.False);
                Assert.That(context.Presentation.IsPresenting, Is.False);
            });
        }

        private static void ClickAndAssert(
            Context context,
            Button button,
            string gachaId,
            long pullCount,
            long expectedGold,
            int expectedWrites)
        {
            button.onClick.Invoke();
            Assert.That(context.ResultView.IsOpen, Is.True);
            Assert.That(context.Presentation.State,
                Is.EqualTo(GachaPresentationState.Opening));
            Assert.That(context.Presentation.ActiveTransaction, Is.Not.Null);
            Assert.That(context.Controller.IsTransactionInProgress, Is.True);
            Assert.That(context.ResultView.Slots.Count(slot =>
                slot.gameObject.activeSelf), Is.Zero);
            context.Presentation.View.SkipButton.onClick.Invoke();
            Assert.That(context.Presentation.State,
                Is.EqualTo(GachaPresentationState.FinalSummary));
            Assert.That(context.ResultView.Slots.Count(slot =>
                slot.gameObject.activeSelf), Is.EqualTo(pullCount));
            AssertAllPullButtons(context.View, false);
            Assert.That(context.Repository.Count(
                nameof(FakeSaveRepository.WriteTemp)),
                Is.EqualTo(expectedWrites));
            ProfileSaveData profile = context.SaveService
                .GetCurrentProfileSnapshot();
            Assert.That(profile.Currencies.GachaCurrency,
                Is.EqualTo(expectedGold));
            string expectedGoldText = expectedGold.ToString(
                CultureInfo.InvariantCulture);
            Assert.That(context.View.GachaCurrencyText.text,
                Is.EqualTo(expectedGoldText));
            Assert.That(profile.GachaStates.Any(state =>
                state.GachaId == gachaId), Is.True);
            CloseResult(context);
            Assert.That(context.View.GachaCurrencyText.text,
                Is.EqualTo(expectedGoldText));
        }

        private static void CloseResult(Context context)
        {
            if (context.Presentation.IsPresenting
                && context.Presentation.State
                    != GachaPresentationState.FinalSummary)
            {
                context.Presentation.View.SkipButton.onClick.Invoke();
            }

            context.Content.Find("GachaPresentation/TouchPanel")
                .GetComponent<Button>().onClick.Invoke();
        }

        private static void AssertAllPullButtons(
            GachaScreenView view,
            bool expected)
        {
            Assert.That(view.CharacterSingleButton.interactable,
                Is.EqualTo(expected));
            Assert.That(view.CharacterTenButton.interactable,
                Is.EqualTo(expected));
            Assert.That(view.RelicSingleButton.interactable,
                Is.EqualTo(expected));
            Assert.That(view.RelicTenButton.interactable,
                Is.EqualTo(expected));
        }

        private static void InvokeLifecycle(
            MonoBehaviour target,
            string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void WithContext(long gold, Action<Context> body)
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath, OpenSceneMode.Single);
                Transform content = scene.GetRootGameObjects()
                    .Single(root => root.name == "GachaCanvas")
                    .transform.Find(
                        "SafeAreaRoot/GamaeViewport/GameContent");
                GachaSceneInstaller installer =
                    content.GetComponent<GachaSceneInstaller>();
                installer.Install();

                DefinitionDatabase database =
                    AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                        DatabasePath);
                database.Initialize();
                LocalizationService localization = AssetDatabase
                    .LoadAssetAtPath<LocalizationCatalog>(LocalizationPath)
                    .CreateService();
                ProfileSaveData profile = SaveTestDataBuilder.Valid();
                profile.Currencies.GachaCurrency = gold;
                var repository = new FakeSaveRepository
                {
                    MainText = SaveTestDataBuilder.Json(profile)
                };
                SaveService saveService = SaveServiceTestFactory.Create(
                    repository,
                    new FixedUnixTimeProvider(100, 101, 102, 103, 104));
                SaveLoadResult load = saveService.LoadOrCreate("ignored");
                Assert.That(load.CanUseProfile, Is.True, load.Message);
                repository.Calls.Clear();
                var transactionService = new GachaTransactionService(
                    saveService,
                    database,
                    new MinimumRandomSource(),
                    new SequentialRelicIdProvider());
                installer.Controller.Initialize(
                    saveService,
                    database,
                    localization,
                    transactionService);

                body(new Context(
                    content,
                    installer,
                    database,
                    localization,
                    repository,
                    saveService,
                    transactionService));
            }
            finally
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
        }

        private sealed class Context
        {
            public Context(
                Transform content,
                GachaSceneInstaller installer,
                DefinitionDatabase database,
                LocalizationService localization,
                FakeSaveRepository repository,
                SaveService saveService,
                GachaTransactionService transactionService)
            {
                Content = content;
                Installer = installer;
                Database = database;
                Localization = localization;
                Repository = repository;
                SaveService = saveService;
                TransactionService = transactionService;
            }

            public Transform Content { get; }
            public GachaSceneInstaller Installer { get; }
            public GachaScreenController Controller =>
                Installer.Controller;
            public GachaScreenView View => Installer.View;
            public GachaPresentationController Presentation =>
                Installer.PresentationController;
            public GachaResultView ResultView =>
                Installer.ResultPresenter.View;
            public DefinitionDatabase Database { get; }
            public LocalizationService Localization { get; }
            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }
            public GachaTransactionService TransactionService { get; }
        }

        private sealed class MinimumRandomSource : IRandomSource
        {
            public int Next(int minInclusive, int maxExclusive) =>
                minInclusive;

            public float NextFloat() => 0f;
        }

        private sealed class SequentialRelicIdProvider
            : IRelicInstanceIdProvider
        {
            private int next;

            public string CreateRelicInstanceId() =>
                "gacha_test_relic_" + next++;
        }
    }
}
