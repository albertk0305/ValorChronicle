using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);
                BattleFlowController[] flowControllers =
                    Object.FindObjectsByType<BattleFlowController>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);
                BattleFlowDebugPanel[] debugPanels =
                    Object.FindObjectsByType<BattleFlowDebugPanel>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                BattleHudController[] hudControllers =
                    Object.FindObjectsByType<BattleHudController>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);
                BattleSceneCombatBootstrap[] combatBootstraps =
                    Object.FindObjectsByType<BattleSceneCombatBootstrap>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);
                BattleCombatPresentationController[]
                    combatPresentationControllers =
                        Object.FindObjectsByType<
                            BattleCombatPresentationController>(
                            FindObjectsInactive.Exclude,
                            FindObjectsSortMode.None);
                AttackProjectilePool[] projectilePools =
                    Object.FindObjectsByType<AttackProjectilePool>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);
                DamageNumberPool[] damageNumberPools =
                    Object.FindObjectsByType<DamageNumberPool>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);

                Assert.That(boardControllers, Has.Length.EqualTo(1));
                Assert.That(flowControllers, Has.Length.EqualTo(1));
                Assert.That(debugPanels, Is.Empty);
                Assert.That(hudControllers, Has.Length.EqualTo(1));
                Assert.That(combatBootstraps, Has.Length.EqualTo(1));
                Assert.That(combatPresentationControllers,
                    Has.Length.EqualTo(1));
                Assert.That(projectilePools, Has.Length.EqualTo(1));
                Assert.That(damageNumberPools, Has.Length.EqualTo(1));

                BattleBoardController boardController = boardControllers[0];
                BattleFlowController flowController = flowControllers[0];
                BattleHudController hudController = hudControllers[0];
                BattleSceneCombatBootstrap combatBootstrap =
                    combatBootstraps[0];
                BattleCombatPresentationController combatPresentation =
                    combatPresentationControllers[0];
                AttackProjectilePool projectilePool = projectilePools[0];
                DamageNumberPool damageNumberPool = damageNumberPools[0];
                Assert.That(flowController.gameObject.name,
                    Is.EqualTo("GameContent"));
                Assert.That(hudController.gameObject,
                    Is.SameAs(flowController.gameObject));
                Assert.That(combatBootstrap.gameObject,
                    Is.SameAs(flowController.gameObject));
                Assert.That(IsBelowSafeArea(hudController.transform), Is.True);

                var flowObject = new SerializedObject(flowController);
                Assert.That(
                    flowObject.FindProperty("boardController")
                        .objectReferenceValue,
                    Is.SameAs(boardController));
                Assert.That(
                    flowObject.FindProperty("requireCombatBridge").boolValue,
                    Is.True);
                Assert.That(
                    flowObject.FindProperty("preBossActionDelaySeconds")
                        .floatValue,
                    Is.EqualTo(0.65f).Within(0.0001f));
                Assert.That(
                    flowObject.FindProperty("combatPresentationController")
                        .objectReferenceValue,
                    Is.SameAs(combatPresentation));
                AssertBoardInputBlocker(
                    flowController,
                    boardController,
                    flowObject);
                Assert.That(combatPresentation.gameObject.name,
                    Is.EqualTo("BattleEffectLayer"));
                Assert.That(combatPresentation.HudController,
                    Is.SameAs(hudController));
                Assert.That(combatPresentation.EffectLayer,
                    Is.SameAs(combatPresentation.transform));
                Assert.That(combatPresentation.ProjectilePool,
                    Is.SameAs(projectilePool));
                Assert.That(combatPresentation.DamageNumberPool,
                    Is.SameAs(damageNumberPool));
                Assert.That(combatPresentation.ProjectileSprite,
                    Is.Not.Null);
                Assert.That(combatPresentation.ProjectileSprite.name,
                    Does.StartWith("AttackProjectile"));
                Assert.That(combatPresentation.ProjectileDuration,
                    Is.EqualTo(0.18f).Within(0.000001f));
                Assert.That(combatPresentation.GetComponent<Graphic>(),
                    Is.Null);
                var effectRect = (RectTransform)combatPresentation.transform;
                Assert.That(effectRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(effectRect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(effectRect.sizeDelta, Is.EqualTo(Vector2.zero));
                Assert.That(
                    combatPresentation.transform.GetSiblingIndex(),
                    Is.EqualTo(hudController.ResultOverlay.transform
                        .GetSiblingIndex() - 1));

                var poolObject = new SerializedObject(projectilePool);
                var projectilePrefab = poolObject.FindProperty(
                    "projectilePrefab").objectReferenceValue
                    as AttackProjectileView;
                Assert.That(projectilePrefab, Is.Not.Null);
                Assert.That(projectilePrefab.Image.raycastTarget, Is.False);
                Assert.That(
                    poolObject.FindProperty("prewarmCount").intValue,
                    Is.EqualTo(1));

                var damagePoolObject = new SerializedObject(
                    damageNumberPool);
                var damageNumberPrefab = damagePoolObject.FindProperty(
                    "damageNumberPrefab").objectReferenceValue
                    as DamageNumberView;
                Assert.That(damageNumberPrefab, Is.Not.Null);
                Assert.That(damageNumberPrefab.Text.raycastTarget, Is.False);
                Assert.That(
                    damagePoolObject.FindProperty("prewarmCount").intValue,
                    Is.EqualTo(DamageNumberPool.DefaultPrewarmCount));

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
                if (CanRestoreSceneManagerSetup(previousSetup))
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
                else
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
            }
        }

        private static bool CanRestoreSceneManagerSetup(SceneSetup[] setup)
        {
            if (setup == null || setup.Length == 0)
            {
                return false;
            }

            bool hasLoadedScene = false;
            bool hasActiveScene = false;
            foreach (SceneSetup sceneSetup in setup)
            {
                hasLoadedScene |= sceneSetup.isLoaded;
                hasActiveScene |= sceneSetup.isActive;
            }

            return hasLoadedScene && hasActiveScene;
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
            Assert.That(hudController.ResultOverlay, Is.Not.Null);
            Assert.That(hudController.ResultOverlay.name,
                Is.EqualTo("BattleResultOverlay"));
            Assert.That(hudController.ResultOverlay.activeSelf, Is.False);
            Assert.That(hudController.ResultText, Is.Not.Null);
            Assert.That(hudController.ResultText.name,
                Is.EqualTo("ResultText"));
            Assert.That(
                serializedHud.FindProperty("resultOverlayDelaySeconds")
                    .floatValue,
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(hudController.BossImage, Is.Not.Null);
            Assert.That(hudController.ResourceFallbackIcon, Is.Not.Null);
            Assert.That(hudController.BossHpSlider, Is.Not.Null);
            AssertHpFillHasNoHorizontalInset(hudController.BossHpSlider);
            Assert.That(hudController.BossHpText, Is.Not.Null);
            Assert.That(hudController.BossShieldImage, Is.Not.Null);
            Assert.That(hudController.ComboText, Is.Not.Null);
            Assert.That(hudController.PartyHpSlider, Is.Not.Null);
            AssertHpFillHasNoHorizontalInset(hudController.PartyHpSlider);
            Assert.That(hudController.PartyHpText, Is.Not.Null);
            Assert.That(hudController.PartyShieldImage, Is.Not.Null);
            Assert.That(hudController.PartyShieldImage.sprite, Is.Not.Null);
            Assert.That(hudController.PartyShieldImage.sprite,
                Is.SameAs(hudController.BossShieldImage.sprite));
            Assert.That(hudController.PartyShieldImage.sprite.name,
                Does.StartWith("Shield"));
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
                Assert.That(slot.CooldownOverlay, Is.Not.Null);
                Assert.That(slot.CooldownOverlay.raycastTarget, Is.False);
                Assert.That(slot.CooldownOverlay.transform.parent,
                    Is.SameAs(slot.CharacterImage.transform));
                Assert.That(slot.CooldownOverlay.gameObject.activeSelf,
                    Is.False);
                Assert.That(slot.ActiveButton.transition,
                    Is.EqualTo(Selectable.Transition.None));
                RectTransform cooldownOverlayRect =
                    slot.CooldownOverlay.rectTransform;
                Assert.That(cooldownOverlayRect.anchorMin,
                    Is.EqualTo(Vector2.zero));
                Assert.That(cooldownOverlayRect.anchorMax,
                    Is.EqualTo(Vector2.one));
                Assert.That(cooldownOverlayRect.sizeDelta,
                    Is.EqualTo(Vector2.zero));
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

        private static void AssertHpFillHasNoHorizontalInset(Slider slider)
        {
            Assert.That(slider.fillRect, Is.Not.Null);
            var fillArea = slider.fillRect.parent as RectTransform;
            Assert.That(fillArea, Is.Not.Null);
            Assert.That(slider.direction,
                Is.EqualTo(Slider.Direction.LeftToRight));
            Assert.That(fillArea.anchorMin.x, Is.Zero);
            Assert.That(fillArea.anchorMax.x, Is.EqualTo(1f));
            Assert.That(fillArea.anchoredPosition.x, Is.Zero);
            Assert.That(fillArea.sizeDelta.x, Is.Zero);
            Assert.That(slider.fillRect.anchoredPosition.x, Is.Zero);
            Assert.That(slider.fillRect.sizeDelta.x, Is.Zero);

            Image fillImage = slider.fillRect.GetComponent<Image>();
            Assert.That(fillImage, Is.Not.Null);
            Assert.That(fillImage.type, Is.EqualTo(Image.Type.Simple));
            Assert.That(fillImage.sprite, Is.Not.Null);
            Assert.That(fillImage.sprite.border, Is.EqualTo(Vector4.zero));

            float originalValue = slider.value;
            try
            {
                slider.SetValueWithoutNotify(1f);
                Canvas.ForceUpdateCanvases();
                Assert.That(slider.fillRect.rect.width,
                    Is.EqualTo(fillArea.rect.width).Within(0.001f));

                slider.SetValueWithoutNotify(0f);
                Canvas.ForceUpdateCanvases();
                Assert.That(slider.fillRect.rect.width,
                    Is.Zero.Within(0.001f));
            }
            finally
            {
                slider.SetValueWithoutNotify(originalValue);
                Canvas.ForceUpdateCanvases();
            }
        }

        private static void AssertBoardInputBlocker(
            BattleFlowController flowController,
            BattleBoardController boardController,
            SerializedObject flowObject)
        {
            GameObject panel = flowObject.FindProperty("notUsersTurnPanel")
                .objectReferenceValue as GameObject;
            Assert.That(panel, Is.SameAs(flowController.NotUsersTurnPanel));
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.name, Is.EqualTo("NotUsersTurnPanel"));
            Assert.That(panel.transform.parent,
                Is.SameAs(boardController.transform));

            Transform blocks = boardController.transform.Find("Blocks");
            Transform background = boardController.transform.Find(
                "BlockFieldBackground");
            Assert.That(blocks, Is.Not.Null);
            Assert.That(background, Is.Not.Null);
            Assert.That(panel.transform.GetSiblingIndex(),
                Is.GreaterThan(blocks.GetSiblingIndex()));

            Image panelImage = panel.GetComponent<Image>();
            Assert.That(panelImage, Is.Not.Null);
            Assert.That(panelImage.raycastTarget, Is.True);

            var panelRect = (RectTransform)panel.transform;
            var backgroundRect = (RectTransform)background;
            Assert.That(panelRect.anchorMin,
                Is.EqualTo(backgroundRect.anchorMin));
            Assert.That(panelRect.anchorMax,
                Is.EqualTo(backgroundRect.anchorMax));
            Assert.That(panelRect.anchoredPosition,
                Is.EqualTo(backgroundRect.anchoredPosition));
            Assert.That(panelRect.sizeDelta,
                Is.EqualTo(backgroundRect.sizeDelta));
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
