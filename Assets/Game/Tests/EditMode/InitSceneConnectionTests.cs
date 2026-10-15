using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.UI;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class InitSceneConnectionTests
    {
        private const string InitScenePath = "Assets/Scenes/Init.unity";

        [Test]
        public void InitSceneHasConnectedStartScreenAndNoMissingScripts()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    InitScenePath,
                    OpenSceneMode.Single);
                Assert.That(scene.IsValid(), Is.True);
                Assert.That(scene.isLoaded, Is.True);

                InitScreenController[] controllers =
                    Object.FindObjectsByType<InitScreenController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                GameBootstrapper[] bootstrappers =
                    Object.FindObjectsByType<GameBootstrapper>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                Assert.That(controllers, Has.Length.EqualTo(1));
                Assert.That(bootstrappers, Has.Length.EqualTo(1));
                Assert.That(
                    Object.FindObjectsByType<EventSystem>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                    Has.Length.EqualTo(1));
                Assert.That(
                    Object.FindObjectsByType<GraphicRaycaster>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                    Has.Length.EqualTo(1));

                InitScreenController controller = controllers[0];
                Assert.That(controller.IsConfigured, Is.True);
                Assert.That(controller.gameObject.name,
                    Is.EqualTo("InitBackgroundImage"));
                Assert.That(controller.StartButton,
                    Is.SameAs(controller.GetComponent<Button>()));
                Assert.That(controller.StartButton.interactable, Is.False);
                Assert.That(controller.StartButton.transition,
                    Is.EqualTo(Selectable.Transition.None));
                Assert.That(
                    controller.GetComponent<Image>().raycastTarget,
                    Is.True);
                Assert.That(controller.InitText.gameObject.activeSelf,
                    Is.False);
                Assert.That(controller.InitText.raycastTarget, Is.False);

                GameObject title = GameObject.Find("TitleImage");
                Assert.That(title, Is.Not.Null);
                Assert.That(title.GetComponent<Image>().raycastTarget,
                    Is.False);

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform child in
                        root.GetComponentsInChildren<Transform>(true))
                    {
                        Assert.That(
                            GameObjectUtility
                                .GetMonoBehavioursWithMissingScriptCount(
                                    child.gameObject),
                            Is.Zero,
                            $"Missing Script: {child.name}");
                    }
                }
            }
            finally
            {
                if (previousSetup != null && previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(
                        previousSetup);
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
}
