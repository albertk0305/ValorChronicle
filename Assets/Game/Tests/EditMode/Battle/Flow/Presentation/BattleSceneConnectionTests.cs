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
        public void BattleSceneHasConnectedCombatBootstrapAndNoMissingScripts()
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
                BattleHudController[] hudControllers =
                    Object.FindObjectsByType<BattleHudController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                BattleSceneCombatBootstrap[] combatBootstraps =
                    Object.FindObjectsByType<BattleSceneCombatBootstrap>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                Assert.That(boardControllers, Has.Length.EqualTo(1));
                Assert.That(flowControllers, Has.Length.EqualTo(1));
                Assert.That(debugPanels, Has.Length.EqualTo(1));
                Assert.That(hudControllers, Has.Length.EqualTo(1));
                Assert.That(combatBootstraps, Has.Length.EqualTo(1));

                BattleBoardController boardController = boardControllers[0];
                BattleFlowController flowController = flowControllers[0];
                BattleFlowDebugPanel debugPanel = debugPanels[0];
                BattleHudController hudController = hudControllers[0];
                BattleSceneCombatBootstrap combatBootstrap =
                    combatBootstraps[0];
                Assert.That(flowController.gameObject.name,
                    Is.EqualTo("GameContent"));
                Assert.That(debugPanel.gameObject,
                    Is.SameAs(flowController.gameObject));
                Assert.That(hudController.gameObject,
                    Is.SameAs(flowController.gameObject));
                Assert.That(combatBootstrap.gameObject,
                    Is.SameAs(flowController.gameObject));
                Assert.That(IsBelowSafeArea(debugPanel.transform), Is.True);
                Assert.That(IsBelowSafeArea(hudController.transform), Is.True);

                var flowObject = new SerializedObject(flowController);
                Assert.That(
                    flowObject.FindProperty("boardController")
                        .objectReferenceValue,
                    Is.SameAs(boardController));
                Assert.That(
                    flowObject.FindProperty("requireCombatBridge").boolValue,
                    Is.True);

                var panelObject = new SerializedObject(debugPanel);
                Assert.That(
                    panelObject.FindProperty("battleFlowController")
                        .objectReferenceValue,
                    Is.SameAs(flowController));
                Assert.That(
                    panelObject.FindProperty("fallbackBossDefinition"),
                    Is.Null);

                AssertHudReferences(
                    hudController,
                    flowController,
                    boardController,
                    combatBootstrap);

                var bootstrapObject =
                    new SerializedObject(combatBootstrap);
                Assert.That(
                    bootstrapObject.FindProperty("battleFlowController")
                        .objectReferenceValue,
                    Is.SameAs(flowController));
                BossDefinition fallbackBoss = bootstrapObject.FindProperty(
                    "fallbackBossDefinition").objectReferenceValue
                    as BossDefinition;
                Assert.That(fallbackBoss, Is.Not.Null);
                Assert.That(fallbackBoss.name, Is.EqualTo("boss_kragmor"));
                Assert.That(fallbackBoss.TurnLimit, Is.EqualTo(25));
                CharacterDefinition fallbackMarea =
                    bootstrapObject.FindProperty(
                    "fallbackMareaDefinition").objectReferenceValue
                    as CharacterDefinition;
                Assert.That(fallbackMarea, Is.Not.Null);
                Assert.That(fallbackMarea.Id,
                    Is.EqualTo("character_marea_bluefang"));
                Assert.That(fallbackMarea.Element,
                    Is.EqualTo(ElementType.Water));
                Assert.That(
                    bootstrapObject.FindProperty("developmentMareaLevel")
                        .intValue,
                    Is.EqualTo(1));
                Assert.That(
                    bootstrapObject.FindProperty("developmentDifficultyId")
                        .stringValue,
                    Is.EqualTo("difficulty_normal"));
                Assert.That(
                    bootstrapObject.FindProperty("developmentBossMaxHp"),
                    Is.Null);
                Assert.That(
                    bootstrapObject.FindProperty("developmentBossAttack"),
                    Is.Null);

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

        private static void AssertHudReferences(
            BattleHudController hudController,
            BattleFlowController flowController,
            BattleBoardController boardController,
            BattleSceneCombatBootstrap combatBootstrap)
        {
            var serializedHud = new SerializedObject(hudController);
            Assert.That(
                serializedHud.FindProperty("battleFlowController")
                    .objectReferenceValue,
                Is.SameAs(flowController));
            Assert.That(hudController.BattleSceneCombatBootstrap,
                Is.SameAs(combatBootstrap));
            Assert.That(hudController.TurnText, Is.Not.Null);
            Assert.That(hudController.TurnText.name, Is.EqualTo("TurnText"));
            Assert.That(hudController.ScoreText, Is.Not.Null);
            Assert.That(hudController.ScoreText.name, Is.EqualTo("ScoreText"));
            Assert.That(hudController.BossImage, Is.Not.Null);
            Assert.That(hudController.CoreCompressionBossSprite, Is.Null);
            Assert.That(hudController.CoreExposureBossSprite, Is.Not.Null);
            Assert.That(hudController.CoreExposureBossSprite.name,
                Is.EqualTo("KragmorExposed_0"));
            Assert.That(hudController.BossHpSlider, Is.Not.Null);
            Assert.That(hudController.BossHpText, Is.Not.Null);
            Assert.That(hudController.BossShieldImage, Is.Not.Null);
            Assert.That(hudController.ComboText, Is.Not.Null);
            Assert.That(hudController.PartyHpSlider, Is.Not.Null);
            Assert.That(hudController.PartyHpText, Is.Not.Null);
            Assert.That(hudController.PartyShieldImage, Is.Not.Null);
            Assert.That(hudController.PartyShieldImage.sprite, Is.Null);
            Assert.That(hudController.PartyShieldImage.type,
                Is.EqualTo(UnityEngine.UI.Image.Type.Simple));
            Assert.That(hudController.ElementSpriteSet, Is.Not.Null);
            var serializedBoardController =
                new SerializedObject(boardController);
            var boardView = serializedBoardController.FindProperty(
                "boardView").objectReferenceValue as BattleBoardView;
            Assert.That(boardView, Is.Not.Null);
            var serializedBoardView = new SerializedObject(boardView);
            Assert.That(
                hudController.ElementSpriteSet,
                Is.SameAs(serializedBoardView.FindProperty("spriteSet")
                    .objectReferenceValue));

            Assert.That(hudController.BossIntentSlotCount, Is.EqualTo(10));
            Assert.That(hudController.ColossusIronFistIcon, Is.Null);
            Assert.That(hudController.RockshardEruptionIcon, Is.Null);
            Assert.That(hudController.CoreCompressionIntentIcon, Is.Null);
            Assert.That(hudController.EarthCollapseIcon, Is.Null);
            for (int index = 0;
                index < hudController.BossIntentSlotCount;
                index++)
            {
                Assert.That(
                    hudController.GetBossIntentSlot(index).IsConfigured,
                    Is.True,
                    $"Boss intent slot {index} is incomplete.");
                Assert.That(
                    hudController.GetBossIntentSlot(index).ActionImage.sprite,
                    Is.Not.Null,
                    $"Boss intent slot {index} needs a generic fallback.");
            }

            Assert.That(hudController.BossStatusSlotCount, Is.EqualTo(16));
            for (int index = 0;
                index < hudController.BossStatusSlotCount;
                index++)
            {
                Assert.That(
                    hudController.GetBossStatusSlot(index).IsConfigured,
                    Is.True,
                    $"Boss status slot {index} is incomplete.");
            }

            Assert.That(hudController.MatchEventSlotCount, Is.EqualTo(10));
            for (int index = 0;
                index < hudController.MatchEventSlotCount;
                index++)
            {
                Assert.That(
                    hudController.GetMatchEventSlot(index).IsConfigured,
                    Is.True,
                    $"MatchEvent slot {index} is incomplete.");
            }

            Assert.That(hudController.CharacterSlotCount, Is.EqualTo(5));
            for (int index = 0;
                index < hudController.CharacterSlotCount;
                index++)
            {
                BattleCharacterSlotView slot =
                    hudController.GetCharacterSlot(index);
                Assert.That(slot.IsConfigured, Is.True,
                    $"Character slot {index} is incomplete.");
                Assert.That(slot.StatusSlotCount, Is.EqualTo(4));
                for (int statusIndex = 0;
                    statusIndex < slot.StatusSlotCount;
                    statusIndex++)
                {
                    Assert.That(
                        slot.GetStatusSlot(statusIndex).IsConfigured,
                        Is.True,
                        $"Character {index} status {statusIndex} "
                            + "is incomplete.");
                }
            }

            Assert.That(hudController.PartyStatusSlotCount, Is.EqualTo(21));
            for (int index = 0;
                index < hudController.PartyStatusSlotCount;
                index++)
            {
                Assert.That(
                    hudController.GetPartyStatusSlot(index).IsConfigured,
                    Is.True,
                    $"Party status slot {index} is incomplete.");
            }
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
