using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValorChronicle.Data.Database;
using ValorChronicle.Gacha.Presentation;
using ValorChronicle.Localization;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaInfoPanelTests
    {
        private const string ScenePath = "Assets/Scenes/Gacha.unity";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";

        [Test]
        public void CanonicalInfoTextShowsExactRatesCostsAndPityRules()
        {
            DefinitionDatabase database = LoadDatabase();
            LocalizationService localization = LoadLocalization();

            Assert.That(database.TryGetGacha(
                GachaScreenController.CharacterGachaId,
                out var character), Is.True);
            Assert.That(GachaInfoPanelPresenter.TryBuildText(
                character, localization, out string characterText,
                out string characterError), Is.True, characterError);
            Assert.That(characterText, Does.Contain(
                "Standard Character Summon"));
            Assert.That(characterText, Does.Contain(
                "Random Character x1: 2%"));
            Assert.That(characterText, Does.Contain(
                "Battle Records x12,000: 1%"));
            Assert.That(characterText, Does.Contain(
                "1 pull 100 Gold / 10 pulls 1,000 Gold"));
            Assert.That(characterText, Does.Contain("Pity: 50 pulls"));
            Assert.That(characterText, Does.Contain(
                "no additional guaranteed reward"));

            localization.SetLocale("ko");
            Assert.That(GachaInfoPanelPresenter.TryBuildText(
                character, localization, out string koreanCharacterText,
                out string koreanCharacterError),
                Is.True, koreanCharacterError);
            Assert.That(koreanCharacterText,
                Does.Contain("일반 캐릭터 소환"));
            Assert.That(koreanCharacterText,
                Does.Contain("무작위 캐릭터 x1: 2%"));
            Assert.That(koreanCharacterText,
                Does.Contain("천장: 50회"));
            Assert.That(koreanCharacterText,
                Does.Contain("1회 100 골드 / 10회 1,000 골드"));

            localization.SetLocale("en");

            Assert.That(database.TryGetGacha(
                GachaScreenController.RelicGachaId,
                out var relic), Is.True);
            Assert.That(GachaInfoPanelPresenter.TryBuildText(
                relic, localization, out string relicText,
                out string relicError), Is.True, relicError);
            Assert.That(relicText, Does.Contain("Standard Relic Summon"));
            Assert.That(relicText, Does.Contain(
                "Random Character x1: 0.5%"));
            Assert.That(relicText, Does.Contain("Pity: 20 pulls"));
        }

        [Test]
        public void SharedInfoPanelSwitchesBannerClosesAndDoesNotDuplicateEvents()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath, OpenSceneMode.Single);
                Transform content = FindContent(scene);
                GachaSceneInstaller installer =
                    content.GetComponent<GachaSceneInstaller>();
                installer.Install();
                installer.Install();
                LocalizationService localization = LoadLocalization();
                DefinitionDatabase database = LoadDatabase();
                installer.InfoPresenter.Initialize(database, localization);
                installer.InfoPresenter.Initialize(database, localization);
                Assert.That(SubscriberCount(
                    localization,
                    installer.InfoPresenter), Is.EqualTo(1));
                installer.InfoPresenter.SetEntryButtonsInteractable(true);

                GachaInfoPanelView view =
                    content.GetComponent<GachaInfoPanelView>();
                int requestCount = 0;
                int openedCount = 0;
                view.CharacterInfoRequested += () => requestCount++;
                installer.InfoPresenter.Opened += () => openedCount++;

                content.Find("HeroesPickup/InfoButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(requestCount, Is.EqualTo(1));
                Assert.That(view.IsOpen, Is.True);
                Assert.That(view.InfoText.text,
                    Does.Contain("Standard Character Summon"));
                string englishCharacterText = view.InfoText.text;
                Assert.That(openedCount, Is.EqualTo(1));
                Assert.That(view.PanelRoot.transform.GetSiblingIndex(),
                    Is.EqualTo(view.PanelRoot.transform.parent.childCount - 1));

                localization.SetLocale("ko");

                Assert.That(view.IsOpen, Is.True);
                Assert.That(view.InfoText.text,
                    Does.Contain("일반 캐릭터 소환"));
                Assert.That(view.InfoText.text,
                    Does.Contain("무작위 캐릭터 x1: 2%"));
                Assert.That(requestCount, Is.EqualTo(1));
                Assert.That(openedCount, Is.EqualTo(1));

                localization.SetLocale("en");

                Assert.That(view.InfoText.text,
                    Is.EqualTo(englishCharacterText));
                Assert.That(openedCount, Is.EqualTo(1));

                content.Find("InfoPanel/ExitButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(view.IsOpen, Is.False);
                localization.SetLocale("ko");

                installer.InfoPresenter.SetEntryButtonsInteractable(true);
                content.Find("ArtifactPickup/InfoButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(view.InfoText.text,
                    Does.Contain("일반 유물 소환"));
                Assert.That(openedCount, Is.EqualTo(2));

                content.gameObject.SetActive(false);
                InvokeLifecycle(installer.InfoPresenter, "OnDisable");
                Assert.That(SubscriberCount(
                    localization,
                    installer.InfoPresenter), Is.Zero);
                localization.SetLocale("en");
                Assert.That(view.InfoText.text,
                    Does.Contain("일반 유물 소환"));
                content.gameObject.SetActive(true);
                InvokeLifecycle(installer.InfoPresenter, "OnEnable");
                Assert.That(SubscriberCount(
                    localization,
                    installer.InfoPresenter), Is.EqualTo(1));
                Assert.That(view.InfoText.text,
                    Does.Contain("Standard Relic Summon"));
                Assert.That(openedCount, Is.EqualTo(2));

                content.Find("InfoPanel/ExitButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(view.IsOpen, Is.False);
                Assert.That(view.InfoText.text, Is.Empty);
            }
            finally
            {
                Restore(previous);
            }
        }

        [Test]
        public void OpenLocalizedMessageRefreshesButRawMessageIsPreserved()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath, OpenSceneMode.Single);
                Transform content = FindContent(scene);
                GachaSceneInstaller installer =
                    content.GetComponent<GachaSceneInstaller>();
                installer.Install();
                LocalizationService localization = LoadLocalization();
                installer.InfoPresenter.Initialize(
                    LoadDatabase(), localization);
                GachaInfoPanelView view =
                    content.GetComponent<GachaInfoPanelView>();
                int openedCount = 0;
                installer.InfoPresenter.Opened += () => openedCount++;

                Assert.That(installer.InfoPresenter.ShowLocalizedMessage(
                    "gacha.error.insufficient_currency"), Is.True);
                Assert.That(installer.InfoPresenter.IsOpen, Is.True);
                Assert.That(view.InfoText.text,
                    Is.EqualTo(
                        "There is not enough Gold for this summon."));

                localization.SetLocale("ko");

                Assert.That(view.InfoText.text,
                    Is.EqualTo("소환에 필요한 골드가 부족합니다."));
                Assert.That(openedCount, Is.EqualTo(1));

                Assert.That(installer.InfoPresenter.ShowMessage(
                    "Raw exchange feedback"), Is.True);
                localization.SetLocale("en");
                Assert.That(view.InfoText.text,
                    Is.EqualTo("Raw exchange feedback"));
                Assert.That(openedCount, Is.EqualTo(2));
            }
            finally
            {
                Restore(previous);
            }
        }

        private static DefinitionDatabase LoadDatabase()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }

        private static LocalizationService LoadLocalization()
        {
            LocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(
                    LocalizationPath);
            Assert.That(catalog, Is.Not.Null);
            return catalog.CreateService();
        }

        private static Transform FindContent(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Single(root => root.name == "GachaCanvas")
                .transform.Find("SafeAreaRoot/GamaeViewport/GameContent");
        }

        private static void Restore(SceneSetup[] setup)
        {
            if (setup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            else
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static int SubscriberCount(
            LocalizationService service,
            object target)
        {
            FieldInfo field = typeof(LocalizationService).GetField(
                "LocaleChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            var subscribers = field.GetValue(service) as Delegate;
            return subscribers?.GetInvocationList().Count(callback =>
                ReferenceEquals(callback.Target, target)) ?? 0;
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
    }
}
