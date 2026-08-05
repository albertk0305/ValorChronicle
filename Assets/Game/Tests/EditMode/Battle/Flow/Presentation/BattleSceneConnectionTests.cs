using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleSceneConnectionTests
    {
        private const string BattleScenePath = "Assets/Scenes/Battle.unity";

        [Test]
        public void BattleSceneHasConnectedFlowPanelAndNoMissingScripts()
        {
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = default;

            try
            {
                scene = EditorSceneManager.OpenScene(
                    BattleScenePath,
                    OpenSceneMode.Single);
                Assert.That(scene.IsValid(), Is.True);
                Assert.That(scene.isLoaded, Is.True);

                BattleBoardController[] boardControllers =
                    Object.FindObjectsByType<BattleBoardController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                BattleFlowController[] flowControllers =
                    Object.FindObjectsByType<BattleFlowController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                BattleFlowDebugPanel[] debugPanels =
                    Object.FindObjectsByType<BattleFlowDebugPanel>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                Assert.That(boardControllers, Has.Length.EqualTo(1));
                Assert.That(flowControllers, Has.Length.EqualTo(1));
                Assert.That(debugPanels, Has.Length.EqualTo(1));

                BattleBoardController boardController = boardControllers[0];
                BattleFlowController flowController = flowControllers[0];
                BattleFlowDebugPanel debugPanel = debugPanels[0];
                Assert.That(flowController.gameObject.name,
                    Is.EqualTo("GameContent"));
                Assert.That(debugPanel.gameObject,
                    Is.SameAs(flowController.gameObject));
                Assert.That(IsBelowSafeArea(debugPanel.transform), Is.True);

                var flowObject = new SerializedObject(flowController);
                Assert.That(
                    flowObject.FindProperty("boardController")
                        .objectReferenceValue,
                    Is.SameAs(boardController));

                var panelObject = new SerializedObject(debugPanel);
                Assert.That(
                    panelObject.FindProperty("battleFlowController")
                        .objectReferenceValue,
                    Is.SameAs(flowController));
                BossDefinition fallbackBoss = panelObject.FindProperty(
                    "fallbackBossDefinition").objectReferenceValue
                    as BossDefinition;
                Assert.That(fallbackBoss, Is.Not.Null);
                Assert.That(fallbackBoss.name, Is.EqualTo("boss_kragmor"));
                Assert.That(fallbackBoss.TurnLimit, Is.EqualTo(25));

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
                            $"Missing Script: {GetPath(child)}");
                    }
                }
            }
            finally
            {
                try
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
                catch (System.ArgumentException) when (
                    !HasLoadedScene(previousSetup))
                {
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
        }

        private static bool HasLoadedScene(SceneSetup[] setup)
        {
            foreach (SceneSetup sceneSetup in setup)
            {
                if (sceneSetup.isLoaded)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBelowSafeArea(Transform transform)
        {
            for (Transform current = transform;
                 current != null;
                 current = current.parent)
            {
                if (current.name == "SafeAreaRoot")
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            for (Transform parent = transform.parent;
                 parent != null;
                 parent = parent.parent)
            {
                path = $"{parent.name}/{path}";
            }

            return path;
        }
    }
}
