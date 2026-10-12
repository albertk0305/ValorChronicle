using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValorChronicle.Core.Scene;
using ValorChronicle.Gacha.Presentation;
using ValorChronicle.UI.Navigation;
using Object = UnityEngine.Object;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaSceneConnectionTests
    {
        private const string ScenePath = "Assets/Scenes/Gacha.unity";

        [Test]
        public void GachaSceneIsMappedWithoutChangingExistingEnumValues()
        {
            Assert.That((int)GameScene.Init, Is.Zero);
            Assert.That((int)GameScene.Main, Is.EqualTo(1));
            Assert.That((int)GameScene.Battle, Is.EqualTo(2));
            Assert.That((int)GameScene.Party, Is.EqualTo(3));
            Assert.That((int)GameScene.Character, Is.EqualTo(4));
            Assert.That((int)GameScene.Gacha, Is.EqualTo(5));
            Assert.That(GameSceneNames.GetName(GameScene.Gacha),
                Is.EqualTo("Gacha"));

            EditorBuildSettingsScene[] matches = EditorBuildSettings.scenes
                .Where(scene => string.Equals(
                    scene.path,
                    ScenePath,
                    StringComparison.Ordinal))
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1));
            Assert.That(matches[0].enabled, Is.True);
        }

        [Test]
        public void GachaSceneConnectsExistingUiAndKeepsFutureActionsSafe()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
                Transform content = FindContent(scene);
                GachaSceneInstaller installer =
                    content.GetComponent<GachaSceneInstaller>();
                Assert.That(installer, Is.Not.Null);

                installer.Install();

                Assert.That(installer.IsConfigured, Is.True);
                Assert.That(installer.InfoPresenter, Is.Not.Null);
                Assert.That(installer.InfoPresenter.IsConfigured, Is.True);
                Assert.That(installer.ResultPresenter, Is.Not.Null);
                Assert.That(installer.ResultPresenter.IsConfigured, Is.True);
                GachaScreenView view = installer.View;
                Assert.That(view.HeroTokensText, Is.SameAs(content.Find(
                    "Medal/MedalText").GetComponent<TMP_Text>()));
                Assert.That(view.RelicTokensText, Is.SameAs(content.Find(
                    "Artifact/ArtifactText").GetComponent<TMP_Text>()));
                Assert.That(view.GachaCurrencyText, Is.SameAs(content.Find(
                    "Gold/GoldText").GetComponent<TMP_Text>()));
                Assert.That(content.Find("CurrencyDisplay"), Is.Null);
                Assert.That(view.CharacterPityText,
                    Is.SameAs(content.Find(
                        "HeroesPickup/UpperBound/UpperBoundText")
                        .GetComponent<TMP_Text>()));
                Assert.That(view.RelicPityText,
                    Is.SameAs(content.Find(
                        "ArtifactPickup/UpperBound/UpperBoundText")
                        .GetComponent<TMP_Text>()));

                view.Bind(new GachaScreenDisplayData(
                    17,
                    29,
                    1234567890123,
                    new GachaBannerDisplayData(
                        GachaScreenController.CharacterGachaId,
                        11,
                        50,
                        100,
                        1000,
                        1234567890123),
                    new GachaBannerDisplayData(
                        GachaScreenController.RelicGachaId,
                        6,
                        20,
                        100,
                        1000,
                        1234567890123)));
                Assert.That(view.HeroTokensText.text,
                    Is.EqualTo("17"));
                Assert.That(view.RelicTokensText.text,
                    Is.EqualTo("29"));
                Assert.That(view.GachaCurrencyText.text,
                    Is.EqualTo("1234567890123"));
                Assert.That(view.CharacterPityText.text,
                    Is.EqualTo("11 / 50"));
                Assert.That(view.RelicPityText.text,
                    Is.EqualTo("6 / 20"));
                Assert.That(view.CharacterSingleCostText.text,
                    Is.EqualTo("X100"));
                Assert.That(view.CharacterTenCostText.text,
                    Is.EqualTo("X1000"));
                Assert.That(view.RelicSingleCostText.text,
                    Is.EqualTo("X100"));
                Assert.That(view.RelicTenCostText.text,
                    Is.EqualTo("X1000"));

                Assert.That(view.CharacterSingleButton.interactable,
                    Is.False);
                Assert.That(view.CharacterTenButton.interactable,
                    Is.False);
                Assert.That(view.RelicSingleButton.interactable, Is.False);
                Assert.That(view.RelicTenButton.interactable, Is.False);
                Assert.That(view.SettingButton.interactable, Is.False);
                Assert.That(view.ExchangeButton.interactable, Is.False);
                Assert.That(view.CharacterSingleButton.onClick
                    .GetPersistentEventCount(), Is.Zero);
                Assert.That(view.CharacterTenButton.onClick
                    .GetPersistentEventCount(), Is.Zero);
                Assert.That(view.RelicSingleButton.onClick
                    .GetPersistentEventCount(), Is.Zero);
                Assert.That(view.RelicTenButton.onClick
                    .GetPersistentEventCount(), Is.Zero);

                Assert.That(view.InfoPanel.activeSelf, Is.False);
                Assert.That(view.GachaPresentation.activeSelf, Is.False);
                Assert.That(content.Find("HeroesPickup/InfoButton")
                    .GetComponent<Button>(), Is.Not.Null);
                Assert.That(content.Find("ArtifactPickup/InfoButton")
                    .GetComponent<Button>(), Is.Not.Null);
                Assert.That(content.Find("InfoPanel/ExitButton")
                    .GetComponent<Button>(), Is.Not.Null);
                Assert.That(content.Find("InfoPanel/Panel")
                    .GetComponent<Image>().raycastTarget, Is.True);
                GachaResultView resultView = content.GetComponent<
                    GachaResultView>();
                Assert.That(resultView, Is.Not.Null);
                Assert.That(resultView.Slots.Count, Is.EqualTo(10));
                Assert.That(content.Find("GachaPresentation/TouchPanel")
                    .GetComponent<Image>().raycastTarget, Is.True);
                Assert.That(content.Find("GachaPresentation/TouchPanel")
                    .GetComponent<Button>(), Is.Not.Null);

                installer.Install();
                Assert.That(content.Find("CurrencyDisplay"), Is.Null);
                Assert.That(installer.View.HeroTokensText,
                    Is.SameAs(view.HeroTokensText));
                Assert.That(installer.View.RelicTokensText,
                    Is.SameAs(view.RelicTokensText));
                Assert.That(installer.View.GachaCurrencyText,
                    Is.SameAs(view.GachaCurrencyText));
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        [Test]
        public void GachaReturnTargetsMainAndNoNavigationTargetsBattle()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
                Transform content = FindContent(scene);
                SceneNavigationButton returnNavigation = content.Find(
                    "ReturnButton").GetComponent<SceneNavigationButton>();
                Assert.That(returnNavigation, Is.Not.Null);

                var serializedReturn = new SerializedObject(
                    returnNavigation);
                Assert.That(serializedReturn.FindProperty("destination")
                    .enumValueIndex, Is.EqualTo((int)GameScene.Main));

                SceneNavigationButton[] allNavigation =
                    Object.FindObjectsByType<SceneNavigationButton>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                Assert.That(allNavigation, Has.Length.EqualTo(1));
                foreach (SceneNavigationButton navigation in allNavigation)
                {
                    var serialized = new SerializedObject(navigation);
                    Assert.That(serialized.FindProperty("destination")
                        .enumValueIndex, Is.Not.EqualTo(
                            (int)GameScene.Battle));
                }
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        private static Transform FindContent(Scene scene)
        {
            Transform canvas = scene.GetRootGameObjects()
                .Single(root => root.name == "GachaCanvas")
                .transform;
            Transform content = canvas.Find(
                "SafeAreaRoot/GamaeViewport/GameContent");
            Assert.That(content, Is.Not.Null);
            return content;
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
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }
    }
}
