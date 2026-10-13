using System;
using System.Linq;
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
                installer.InfoPresenter.Initialize(
                    LoadDatabase(), LoadLocalization());
                installer.InfoPresenter.SetEntryButtonsInteractable(true);

                GachaInfoPanelView view =
                    content.GetComponent<GachaInfoPanelView>();
                int requestCount = 0;
                view.CharacterInfoRequested += () => requestCount++;

                content.Find("HeroesPickup/InfoButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(requestCount, Is.EqualTo(1));
                Assert.That(view.IsOpen, Is.True);
                Assert.That(view.InfoText.text,
                    Does.Contain("Standard Character Summon"));
                Assert.That(view.PanelRoot.transform.GetSiblingIndex(),
                    Is.EqualTo(view.PanelRoot.transform.parent.childCount - 1));

                installer.InfoPresenter.SetEntryButtonsInteractable(true);
                content.Find("ArtifactPickup/InfoButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(view.InfoText.text,
                    Does.Contain("Standard Relic Summon"));

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
    }
}
