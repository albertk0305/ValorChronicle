using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Pause;
using ValorChronicle.Core.Random;
using ValorChronicle.Core.Scene;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.Services;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.Tests.PlayMode
{
    public sealed class BattleSceneCombatPlayModeTests
    {
        private BattleSceneCombatBootstrap combatBootstrap;
        private BattleFlowController flow;
        private BattleSceneCombatComposition combat;
        private BattleBoardController boardController;
        private BattleBoardView boardView;
        private BattleBoardInput boardInput;
        private GameObject bootstrapRoot;
        private DefinitionDatabase testDefinitionDatabase;
        private CharacterDefinition testMareaDefinition;
        private CharacterDefinition testSerenaDefinition;
        private EffectDefinition testSerenaActiveEffectDefinition;
        private SkillDefinition testSerenaPassiveDefinition;
        private BossDefinition testKragmorDefinition;
        private BattleResultBalanceDefinition testResultBalanceDefinition;
        private MareaBluefangCombatConfig testMareaConfig;
        private SerenaCoralspearCombatConfig testSerenaConfig;
        private KragmorCombatConfig testKragmorConfig;
        private Texture2D testSerenaEffectTexture;
        private Sprite testSerenaEffectIcon;
        private Texture2D testSerenaChaseTexture;
        private Sprite testSerenaChaseIcon;
        private string saveRoot;
        private float previousTimeScale;
        private GamePauseService gamePauseService;
        private SaveService saveService;
        private SettingsService settingsService;
        private BattlePausePanelBinding pausePanelBinding;

        [UnitySetUp]
        public IEnumerator LoadBattleScene()
        {
            previousTimeScale = Time.timeScale;
            Assert.That(previousTimeScale, Is.EqualTo(1f).Within(0.0001f),
                "Battle startup requires the project Time Scale to be 1. "
                    + "A zero value freezes the initial board drop before "
                    + "InitialBoardReady can start the battle.");
            Time.timeScale = 1f;
            InstallTestBootstrapper();
            yield return SceneManager.LoadSceneAsync(
                "Battle",
                LoadSceneMode.Additive);

            Assert.That(UnityEngine.Object.FindObjectsByType<
                BattleFlowDebugPanel>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None), Is.Empty);
            combatBootstrap = UnityEngine.Object.FindFirstObjectByType<
                BattleSceneCombatBootstrap>();
            Assert.That(combatBootstrap, Is.Not.Null);

            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!combatBootstrap.HasInitializedCombat
                    || combatBootstrap.CombatComposition == null
                    || combatBootstrap.GetComponent<BattleFlowController>()
                        .Context?.Phase == BattlePhase.NotStarted)
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            flow = combatBootstrap.GetComponent<BattleFlowController>();
            combat = combatBootstrap.CombatComposition;
            boardController = UnityEngine.Object.FindFirstObjectByType<
                BattleBoardController>();
            boardView = UnityEngine.Object.FindFirstObjectByType<
                BattleBoardView>();
            boardInput = UnityEngine.Object.FindFirstObjectByType<
                BattleBoardInput>();
            pausePanelBinding = UnityEngine.Object.FindFirstObjectByType<
                BattlePausePanelBinding>();
            Assert.That(combatBootstrap.HasInitializedCombat, Is.True);
            Assert.That(boardController, Is.Not.Null);
            Assert.That(boardView, Is.Not.Null);
            Assert.That(boardInput, Is.Not.Null);
            Assert.That(pausePanelBinding, Is.Not.Null);
            Assert.That(pausePanelBinding.IsConfigured, Is.True);
            Assert.That(pausePanelBinding.PauseButton.interactable, Is.True);
            Assert.That(flow.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(Time.timeScale, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator PausePanelPausesAndResumesLoadedBattleInPlace()
        {
            BattlePhase phase = flow.Context.Phase;
            int turn = flow.Context.CurrentTurn;
            long bossHp = combat.Boss.CurrentHp;
            long partyHp = combat.Party.CurrentHp;
            int patternIndex = combat.KragmorRuntimeState.PatternIndex;
            int resultCount = 0;
            int sceneLoadStartCount = 0;
            flow.BattleResultReached += _ => resultCount++;
            GameBootstrapper.Instance.SceneService.LoadingStateChanged +=
                isLoading =>
                {
                    if (isLoading)
                    {
                        sceneLoadStartCount++;
                    }
                };
            SetField(boardInput, "isTrackingPointer", true);

            ClickThroughEventSystem(pausePanelBinding.PauseButton);
            yield return null;

            Assert.That(pausePanelBinding.IsOpen, Is.True);
            Assert.That(pausePanelBinding.IsGiveUpConfirmationOpen, Is.False);
            Assert.That(
                pausePanelBinding.ResumeButton.gameObject.activeSelf,
                Is.True);
            Assert.That(
                pausePanelBinding.GiveUpButton.gameObject.activeSelf,
                Is.True);
            Assert.That(pausePanelBinding.PauseText.LocalizationKey,
                Is.EqualTo("ui.battle.pause.paused"));
            Assert.That(flow.Context.Result,
                Is.EqualTo(BattleResultKind.None));
            Assert.That(resultCount, Is.Zero);
            Assert.That(sceneLoadStartCount, Is.Zero);
            Assert.That(SceneManager.GetSceneByName("Battle").isLoaded,
                Is.True);
            Assert.That(gamePauseService.ActiveRequestCount, Is.EqualTo(1));
            Assert.That(gamePauseService.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(boardInput.IsTrackingPointer, Is.False);
            Assert.That(flow.CanUseActive(0), Is.False);

            ClickThroughEventSystem(pausePanelBinding.GiveUpButton);
            Assert.That(pausePanelBinding.IsGiveUpConfirmationOpen, Is.True);
            Assert.That(
                pausePanelBinding.ResumeButton.gameObject.activeSelf,
                Is.False);
            Assert.That(
                pausePanelBinding.GiveUpButton.gameObject.activeSelf,
                Is.False);
            Assert.That(gamePauseService.ActiveRequestCount, Is.EqualTo(1));
            Assert.That(flow.Context.Result,
                Is.EqualTo(BattleResultKind.None));

            ClickThroughEventSystem(pausePanelBinding.CancelGiveUpButton);
            Assert.That(pausePanelBinding.IsGiveUpConfirmationOpen, Is.False);
            Assert.That(
                pausePanelBinding.ResumeButton.gameObject.activeSelf,
                Is.True);
            Assert.That(
                pausePanelBinding.GiveUpButton.gameObject.activeSelf,
                Is.True);
            Assert.That(pausePanelBinding.PauseText.LocalizationKey,
                Is.EqualTo("ui.battle.pause.paused"));
            Assert.That(gamePauseService.ActiveRequestCount, Is.EqualTo(1));
            Assert.That(gamePauseService.IsPaused, Is.True);

            ClickThroughEventSystem(pausePanelBinding.ResumeButton);
            yield return null;

            Assert.That(pausePanelBinding.IsOpen, Is.False);
            Assert.That(gamePauseService.ActiveRequestCount, Is.Zero);
            Assert.That(gamePauseService.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(flow.Context.Phase, Is.EqualTo(phase));
            Assert.That(flow.Context.CurrentTurn, Is.EqualTo(turn));
            Assert.That(combat.Boss.CurrentHp, Is.EqualTo(bossHp));
            Assert.That(combat.Party.CurrentHp, Is.EqualTo(partyHp));
            Assert.That(combat.KragmorRuntimeState.PatternIndex,
                Is.EqualTo(patternIndex));
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
            Assert.That(flow.Context.Result,
                Is.EqualTo(BattleResultKind.None));
            Assert.That(resultCount, Is.Zero);
            Assert.That(sceneLoadStartCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator GiveUpAbortsWithoutFinalizationOrProfileMutation()
        {
            ProfileSaveData before = saveService.GetCurrentProfileSnapshot();
            int navigationCount = 0;
            SetField(
                pausePanelBinding,
                "loadMainSceneAsync",
                (Func<System.Threading.Tasks.Task<bool>>)(() =>
                {
                    navigationCount++;
                    Assert.That(flow.Context.Result,
                        Is.EqualTo(BattleResultKind.Aborted));
                    return System.Threading.Tasks.Task.FromResult(true);
                }));

            ClickThroughEventSystem(pausePanelBinding.PauseButton);
            ClickThroughEventSystem(pausePanelBinding.GiveUpButton);

            Assert.That(pausePanelBinding.IsGiveUpConfirmationOpen, Is.True);
            Assert.That(
                pausePanelBinding.ResumeButton.gameObject.activeSelf,
                Is.False);
            Assert.That(
                pausePanelBinding.GiveUpButton.gameObject.activeSelf,
                Is.False);
            Assert.That(pausePanelBinding.PauseText.LocalizationKey,
                Is.EqualTo("ui.battle.pause.give_up_confirm"));
            Assert.That(flow.Context.Result,
                Is.EqualTo(BattleResultKind.None));
            Assert.That(navigationCount, Is.Zero);

            ClickThroughEventSystem(pausePanelBinding.ConfirmGiveUpButton);
            yield return null;

            Assert.That(flow.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(navigationCount, Is.EqualTo(1));
            Assert.That(gamePauseService.ActiveRequestCount, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                combat.ResultPersistenceCoordinator.IsPersistenceCompleted,
                Is.False);
            BattleHudController hud = UnityEngine.Object
                .FindFirstObjectByType<BattleHudController>();
            Assert.That(hud.ResultOverlay.activeSelf, Is.False);
            Assert.That(
                new SaveDataValueComparer().Equals(
                    before,
                    saveService.GetCurrentProfileSnapshot()),
                Is.True);
        }

        [UnityTest]
        public IEnumerator BattleSpeedChangesPreserveStateAndPauseRestoresLatest()
        {
            BattlePhase phase = flow.Context.Phase;
            int turn = flow.Context.CurrentTurn;
            long bossHp = combat.Boss.CurrentHp;
            long partyHp = combat.Party.CurrentHp;
            int patternIndex = combat.KragmorRuntimeState.PatternIndex;

            settingsService.SetBattleSpeed(2);
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.0001f));

            IDisposable first = gamePauseService.RequestPause();
            IDisposable second = gamePauseService.RequestPause();
            settingsService.SetBattleSpeed(3);
            Assert.That(Time.timeScale, Is.Zero);

            first.Dispose();
            Assert.That(Time.timeScale, Is.Zero);
            second.Dispose();
            Assert.That(Time.timeScale, Is.EqualTo(1.5f).Within(0.0001f));

            settingsService.SetBattleSpeed(1);
            Assert.That(Time.timeScale, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(flow.Context.Phase, Is.EqualTo(phase));
            Assert.That(flow.Context.CurrentTurn, Is.EqualTo(turn));
            Assert.That(combat.Boss.CurrentHp, Is.EqualTo(bossHp));
            Assert.That(combat.Party.CurrentHp, Is.EqualTo(partyHp));
            Assert.That(combat.KragmorRuntimeState.PatternIndex,
                Is.EqualTo(patternIndex));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoardFallDurationIsSpeedIndependentAndPausesInPlace()
        {
            const float dropDuration = 0.12f;
            var measuredDurations = new List<float>();
            int[] speeds = { 1, 2, 3 };
            for (int index = 0; index < speeds.Length; index++)
            {
                settingsService.SetBattleSpeed(speeds[index]);
                yield return null;
                float startedAt = Time.realtimeSinceStartup;
                IEnumerator drop = boardView.RenderInitialDrop(
                    boardController.CurrentBoard,
                    dropDuration,
                    0f);
                while (drop.MoveNext())
                    yield return drop.Current;
                measuredDurations.Add(
                    Time.realtimeSinceStartup - startedAt);
            }

            Assert.That(measuredDurations.Max() - measuredDurations.Min(),
                Is.LessThan(0.08f));
            Assert.That(measuredDurations[1],
                Is.EqualTo(dropDuration).Within(0.08f));

            settingsService.SetBattleSpeed(3);
            IEnumerator pausedDrop = boardView.RenderInitialDrop(
                boardController.CurrentBoard,
                0.3f,
                0f);
            IDisposable pause = null;
            try
            {
                Assert.That(pausedDrop.MoveNext(), Is.True);
                yield return pausedDrop.Current;
                BlockView trackedView = boardView.ActiveViews[
                    boardController.CurrentBoard.Get(
                        new BoardPosition(0, 0)).RuntimeId];
                pause = gamePauseService.RequestPause();
                Vector2 pausedPosition =
                    trackedView.RectTransform.anchoredPosition;
                for (int frame = 0; frame < 3; frame++)
                {
                    Assert.That(pausedDrop.MoveNext(), Is.True);
                    yield return pausedDrop.Current;
                    Assert.That(trackedView.RectTransform.anchoredPosition,
                        Is.EqualTo(pausedPosition));
                }

                pause.Dispose();
                pause = null;
                while (pausedDrop.MoveNext())
                    yield return pausedDrop.Current;
            }
            finally
            {
                pause?.Dispose();
                if (pausedDrop is IDisposable disposable)
                    disposable.Dispose();
            }

            Assert.That(boardView.MatchesBoard(boardController.CurrentBoard),
                Is.True);
            Assert.That(boardView.IsAnimating, Is.False);
            settingsService.SetBattleSpeed(1);
        }

        private static void ClickThroughEventSystem(Button button)
        {
            Assert.That(EventSystem.current, Is.Not.Null);
            var eventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                clickCount = 1,
                pointerId = -1,
                position = RectTransformUtility.WorldToScreenPoint(
                    null,
                    button.transform.position)
            };

            ExecuteEvents.Execute(
                button.gameObject,
                eventData,
                ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(
                button.gameObject,
                eventData,
                ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(
                button.gameObject,
                eventData,
                ExecuteEvents.pointerClickHandler);
        }

        [UnityTearDown]
        public IEnumerator UnloadBattleScene()
        {
            Time.timeScale = previousTimeScale;
            Scene battleScene = SceneManager.GetSceneByName("Battle");
            if (battleScene.IsValid() && battleScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(battleScene);
            }

            SetAutoProperty(
                typeof(GameBootstrapper),
                "Instance",
                null,
                null);
            gamePauseService?.Dispose();
            gamePauseService = null;
            if (bootstrapRoot != null)
            {
                UnityEngine.Object.Destroy(bootstrapRoot);
            }

            if (testDefinitionDatabase != null)
            {
                UnityEngine.Object.Destroy(testDefinitionDatabase);
            }

            if (testMareaDefinition != null)
            {
                UnityEngine.Object.Destroy(testMareaDefinition);
            }

            if (testSerenaDefinition != null)
            {
                UnityEngine.Object.Destroy(testSerenaDefinition);
            }

            if (testSerenaActiveEffectDefinition != null)
            {
                UnityEngine.Object.Destroy(testSerenaActiveEffectDefinition);
            }

            if (testSerenaPassiveDefinition != null)
            {
                UnityEngine.Object.Destroy(testSerenaPassiveDefinition);
            }

            if (testKragmorDefinition != null)
            {
                UnityEngine.Object.Destroy(testKragmorDefinition);
            }

            if (testResultBalanceDefinition != null)
            {
                UnityEngine.Object.Destroy(testResultBalanceDefinition);
            }

            if (testKragmorConfig != null)
            {
                UnityEngine.Object.Destroy(testKragmorConfig);
            }

            if (testMareaConfig != null)
            {
                UnityEngine.Object.Destroy(testMareaConfig);
            }

            if (testSerenaConfig != null)
            {
                UnityEngine.Object.Destroy(testSerenaConfig);
            }

            if (testSerenaEffectIcon != null)
            {
                UnityEngine.Object.Destroy(testSerenaEffectIcon);
            }

            if (testSerenaEffectTexture != null)
            {
                UnityEngine.Object.Destroy(testSerenaEffectTexture);
            }

            if (testSerenaChaseIcon != null)
            {
                UnityEngine.Object.Destroy(testSerenaChaseIcon);
            }

            if (testSerenaChaseTexture != null)
            {
                UnityEngine.Object.Destroy(testSerenaChaseTexture);
            }

            if (!string.IsNullOrEmpty(saveRoot)
                && Directory.Exists(saveRoot))
            {
                Directory.Delete(saveRoot, recursive: true);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneComposesMareaAndActiveAffectsSameTurnMatch()
        {
            Assert.That(flow.RequiresCombatBridge, Is.True);
            Assert.That(flow.CombatBridge, Is.SameAs(combat.Bridge));
            Assert.That(combat.Party.Characters, Has.Count.EqualTo(1));
            Assert.That(combat.Party.Characters[0].CharacterId,
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(combat.Party.Characters[0].PartySlotIndex, Is.Zero);
            Assert.That(combat.Party.Characters[0].Element,
                Is.EqualTo(ElementType.Water));
            Assert.That(combat.Party.Characters[0].MaxHp, Is.EqualTo(900));
            Assert.That(combat.Party.Characters[0].Attack, Is.EqualTo(180d));
            BattleHudController hud = UnityEngine.Object
                .FindFirstObjectByType<BattleHudController>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.CharacterPresentationCatalog, Is.Not.Null);
            Assert.That(
                hud.CharacterPresentationCatalog.IsInitialized,
                Is.True);
            Assert.That(
                hud.CharacterPresentationCatalog.TryGet(
                    MareaBluefangRules.CharacterId,
                    out CharacterPresentationDefinition mareaPresentation),
                Is.True);
            BattleCharacterSlotView mareaSlot = hud.GetCharacterSlot(0);
            Assert.That(mareaSlot.CharacterId,
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(mareaSlot.CharacterImage.sprite,
                Is.SameAs(mareaPresentation.FaceSprite));
            Assert.That(mareaSlot.CharacterImage.enabled, Is.True);
            Assert.That(combat.Boss.BossId, Is.EqualTo("kragmor"));
            Assert.That(combat.Boss.MaxHp, Is.EqualTo(66000));
            Assert.That(combat.Boss.CurrentHp, Is.EqualTo(66000));
            Assert.That(combat.Boss.Attack, Is.EqualTo(850d));
            Assert.That(combatBootstrap.BossIntentSource,
                Is.SameAs(combat.BossIntentSource));
            Assert.That(combat.BossIntentSource.NextIntent.ActionKind,
                Is.EqualTo(KragmorActionKind.ColossusIronFist));
            Assert.That(combat.Boss.Resources.Get(
                    WaterElementResource.Id).MaxAmount,
                Is.EqualTo(5));
            Assert.That(combat.Boss.Resources.GetAmount(
                    WaterElementResource.Id),
                Is.Zero);
            Assert.That(combat.MatchProviders.TryResolve(
                MareaBluefangRules.CharacterId,
                out _), Is.True);
            Assert.That(combat.ActiveProviders.TryResolve(
                MareaBluefangRules.CharacterId,
                MareaBluefangRules.ActiveAbilityId,
                out _), Is.True);
            Assert.That(combat.ActiveBindings, Has.Count.EqualTo(1));
            Assert.That(combat.ActiveBindings[0].ActiveAbilityIndex, Is.Zero);
            Assert.That(combat.ActiveBindings[0].PartySlotIndex, Is.Zero);
            Assert.That(combat.ResultBalance.RemainingTurnBonusRate,
                Is.EqualTo(0.30m));
            BattleGradeRewardResult challengeSss =
                BattleGradeRewardCalculator.Calculate(
                    combat.ResultBalance,
                    BattleDifficultyIds.Challenge,
                    100000L,
                    115000L);
            Assert.That(challengeSss.Grade, Is.EqualTo(BattleGrade.SSS));
            Assert.That(challengeSss.RepeatRewardAmount,
                Is.EqualTo(1000L));

            ActiveAbilityRuntimeState active =
                flow.Context.ActiveAbilities[0];
            Assert.That(active.CanUse, Is.True);
            Assert.That(active.RemainingCooldown, Is.Zero);

            Assert.That(flow.TryUseActive(0), Is.True);
            Assert.That(active.RemainingCooldown,
                Is.EqualTo(combat.FlowSetup.ActiveAbilityCooldowns[0]));
            Assert.That(combat.Party.Characters[0].Effects.FindByEffectId(
                MareaBluefangRules.ActiveEffectId), Has.Count.EqualTo(1));
            Assert.That(combat.Bridge.LastActiveExecutionResult
                .ActionResults.Single(), Is.TypeOf<ApplyEffectActionResult>());

            CombatActionExecutionResult matchResult =
                ResolveWaterMatch(blockCount: 3);
            DamageActionResult damage = matchResult.ActionResults
                .OfType<DamageActionResult>()
                .Single();
            Assert.That(damage.Context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.25d).Within(0.000000001d));
            Assert.That(combat.Boss.Resources.GetAmount(
                    WaterElementResource.Id),
                Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneComposesStandaloneSerenaFromOwnedPartyProfile()
        {
            Assert.That(combat.Party.Characters, Has.Count.EqualTo(1));
            CharacterBattleState serena = combat.Party.Characters[0];
            Assert.That(serena.CharacterId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(serena.PartySlotIndex, Is.Zero);
            Assert.That(serena.Element, Is.EqualTo(ElementType.Water));
            Assert.That(serena.MaxHp, Is.EqualTo(870));
            Assert.That(serena.Attack, Is.EqualTo(170d));
            Assert.That(combat.MatchProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                out _), Is.True);
            Assert.That(combat.ActiveProviders.TryResolve(
                SerenaCoralspearRules.CharacterId,
                SerenaCoralspearRules.ActiveAbilityId,
                out _), Is.True);
            Assert.That(combat.ActiveBindings, Has.Count.EqualTo(1));
            Assert.That(combat.ActiveBindings[0].PartySlotIndex, Is.Zero);
            Assert.That(combat.Boss.Resources.GetAll().Count(resource =>
                    resource.ResourceId == WaterElementResource.Id),
                Is.EqualTo(1));

            BattleHudController hud = UnityEngine.Object
                .FindFirstObjectByType<BattleHudController>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.CharacterPresentationCatalog.TryGet(
                    SerenaCoralspearRules.CharacterId,
                    out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation.FaceSprite, Is.Not.Null);
            BattleCharacterSlotView slot = hud.GetCharacterSlot(0);
            Assert.That(slot.CharacterId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(slot.CharacterImage.sprite,
                Is.SameAs(presentation.FaceSprite));
            Assert.That(slot.ActiveButton.interactable, Is.True);
            Assert.That(hud.TryResolvePlayerProjectileAnchors(
                    serena.PartySlotIndex,
                    serena.CharacterId,
                    out RectTransform serenaSource,
                    out RectTransform serenaTarget),
                Is.True);
            Assert.That(serenaSource,
                Is.SameAs(slot.CharacterImage.rectTransform));
            Assert.That(serenaTarget,
                Is.SameAs(hud.BossImage.rectTransform));
            Assert.That(combat.CombatStatusSources, Has.Count.EqualTo(1));
            Assert.That(combat.CombatStatusSources[0].PartySlotIndex,
                Is.Zero);
            Assert.That(slot.GetStatusSlot(0).Root.activeSelf, Is.False);

            CombatActionExecutionResult threeMatch =
                ResolveWaterMatch(blockCount: 3);
            DamageActionResult threeDamage = FindDamage(
                threeMatch,
                SerenaCoralspearRules.CharacterId,
                ActionOrigin.Match);
            Assert.That(threeDamage.Context.SkillCoefficient,
                Is.EqualTo(0.75d).Within(0.000000001d));
            Assert.That(combat.Boss.Resources.GetAmount(
                    WaterElementResource.Id),
                Is.EqualTo(1));
            RestorePartyHpForSnapshotTest();

            CombatActionExecutionResult fourMatch =
                ResolveWaterMatch(blockCount: 4);
            DamageActionResult additional = FindDamage(
                fourMatch,
                SerenaCoralspearRules.CharacterId,
                ActionOrigin.Additional);
            Assert.That(additional.Context.SkillCoefficient,
                Is.EqualTo(0.90d).Within(0.000000001d));
            Assert.That(additional.Action.RootActionId,
                Is.EqualTo(additional.Action.SourceActionId));
            RestorePartyHpForSnapshotTest();

            CombatActionExecutionResult fiveMatch =
                ResolveWaterMatch(blockCount: 5);
            Assert.That(FindDamage(
                    fiveMatch,
                    SerenaCoralspearRules.CharacterId,
                    ActionOrigin.Match).Context.SkillCoefficient,
                Is.EqualTo(2.20d).Within(0.000000001d));
            Assert.That(fiveMatch.ActionResults
                    .OfType<DamageActionResult>()
                    .Any(result => result.Action.Origin
                        == ActionOrigin.Chase),
                Is.False);
            Assert.That(combat.CombatStatusSources[0].CurrentCount,
                Is.EqualTo(1));
            Assert.That(slot.GetStatusSlot(0).Root.activeSelf, Is.True);
            Assert.That(slot.GetStatusSlot(0).IconImage.sprite,
                Is.SameAs(testSerenaChaseIcon));
            Assert.That(slot.GetStatusSlot(0).ValueText.text,
                Is.EqualTo("1"));
            RestorePartyHpForSnapshotTest();

            ResolveWaterMatch(blockCount: 5);
            Assert.That(slot.GetStatusSlot(0).ValueText.text,
                Is.EqualTo("2"));
            RestorePartyHpForSnapshotTest();
            ResolveWaterMatch(blockCount: 5);
            Assert.That(slot.GetStatusSlot(0).ValueText.text,
                Is.EqualTo("3"));
            RestorePartyHpForSnapshotTest();
            ResolveWaterMatch(blockCount: 5);
            Assert.That(combat.CombatStatusSources[0].CurrentCount,
                Is.EqualTo(3));
            Assert.That(slot.GetStatusSlot(0).ValueText.text,
                Is.EqualTo("3"));
            RestorePartyHpForSnapshotTest();

            SetBossHpForTerminalTest(1);
            CombatActionExecutionResult lethal = ResolveWaterMatch(
                blockCount: 3,
                completeBossAction: false);
            Assert.That(lethal.StoppedEarly, Is.True);
            Assert.That(lethal.BossDefeated, Is.True);
            Assert.That(lethal.ActionResults
                    .OfType<AddResourceActionResult>(),
                Is.Empty);
            Assert.That(flow.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(slot.GetStatusSlot(0).Root.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneComposesMareaAndSerenaWithSharedCombatState()
        {
            Assert.That(combat.Party.Characters.Select(character =>
                    character.CharacterId),
                Is.EqualTo(new[]
                {
                    MareaBluefangRules.CharacterId,
                    SerenaCoralspearRules.CharacterId
                }));
            Assert.That(combat.ActiveBindings.Select(binding =>
                    binding.PartySlotIndex),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(flow.Context.ActiveAbilities[1].RemainingCooldown,
                Is.Zero);
            Assert.That(combat.Party.Characters[1].Effects.FindByEffectId(
                    SerenaCoralspearRules.ActiveEffectId),
                Is.Empty);
            Assert.That(combat.Boss.Resources.GetAll().Count(resource =>
                    resource.ResourceId == WaterElementResource.Id),
                Is.EqualTo(1));

            BattleHudController hud = UnityEngine.Object
                .FindFirstObjectByType<BattleHudController>();
            BattleCharacterSlotView serenaSlot = hud.GetCharacterSlot(1);
            Assert.That(serenaSlot.CharacterId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(serenaSlot.ActiveButton.interactable, Is.True);
            CharacterBattleState serena = combat.Party.Characters[1];
            Assert.That(hud.TryResolvePlayerProjectileAnchors(
                    serena.PartySlotIndex,
                    serena.CharacterId,
                    out RectTransform serenaSource,
                    out RectTransform serenaTarget),
                Is.True);
            Assert.That(serenaSource,
                Is.SameAs(serenaSlot.CharacterImage.rectTransform));
            Assert.That(serenaTarget,
                Is.SameAs(hud.BossImage.rectTransform));
            Assert.That(combat.CombatStatusSources, Has.Count.EqualTo(1));
            Assert.That(combat.CombatStatusSources[0].PartySlotIndex,
                Is.EqualTo(1));
            Assert.That(serenaSlot.GetStatusSlot(0).Root.activeSelf,
                Is.False);

            CombatActionExecutionResult threeMatch =
                ResolveWaterMatch(blockCount: 3);
            Assert.That(threeMatch.ActionResults
                    .OfType<DamageActionResult>()
                    .Count(result => result.Action.Origin
                        == ActionOrigin.Match),
                Is.EqualTo(2));
            Assert.That(combat.Boss.Resources.GetAmount(
                    WaterElementResource.Id),
                Is.EqualTo(2));
            RestorePartyHpForSnapshotTest();

            CombatActionExecutionResult fourMatch =
                ResolveWaterMatch(blockCount: 4);
            Assert.That(FindDamage(
                    fourMatch,
                    SerenaCoralspearRules.CharacterId,
                    ActionOrigin.Additional).Context.SkillCoefficient,
                Is.EqualTo(0.90d).Within(0.000000001d));
            RestorePartyHpForSnapshotTest();

            CombatActionExecutionResult firstFiveMatch =
                ResolveWaterMatch(blockCount: 5);
            DamageActionResult firstChase = firstFiveMatch.ActionResults
                .OfType<DamageActionResult>()
                .Single(result => result.Action.Origin
                    == ActionOrigin.Chase);
            Assert.That(firstChase.Context.AttackTypeDamageIncreaseRateSum,
                Is.Zero.Within(0.000000001d));
            Assert.That(combat.Boss.Resources.GetAmount(
                    WaterElementResource.Id),
                Is.Zero);
            Assert.That(combat.CombatStatusSources[0].CurrentCount,
                Is.EqualTo(1));
            Assert.That(serenaSlot.GetStatusSlot(0).IconImage.sprite,
                Is.SameAs(testSerenaChaseIcon));
            Assert.That(serenaSlot.GetStatusSlot(0).ValueText.text,
                Is.EqualTo("1"));
            RestorePartyHpForSnapshotTest();

            ClickThroughEventSystem(serenaSlot.ActiveButton);
            float activeDeadline = Time.realtimeSinceStartup + 5f;
            while (combat.Party.Characters[1].Effects.FindByEffectId(
                       SerenaCoralspearRules.ActiveEffectId).Count == 0
                && Time.realtimeSinceStartup < activeDeadline)
            {
                yield return null;
            }

            ActiveAbilityRuntimeState serenaActive =
                flow.Context.ActiveAbilities[1];
            Assert.That(serenaActive.RemainingCooldown, Is.EqualTo(8));
            Assert.That(combat.Party.Characters[1].Effects.FindByEffectId(
                    SerenaCoralspearRules.ActiveEffectId),
                Has.Count.EqualTo(1));
            BattleStatusIconView activeStatus =
                serenaSlot.GetStatusSlot(0);
            Assert.That(activeStatus.Root.activeSelf, Is.True);
            Assert.That(activeStatus.IconImage.sprite,
                Is.SameAs(testSerenaEffectIcon));
            Assert.That(activeStatus.ValueText.text, Is.EqualTo("3"));
            BattleStatusIconView chaseStatus =
                serenaSlot.GetStatusSlot(1);
            Assert.That(chaseStatus.Root.activeSelf, Is.True);
            Assert.That(chaseStatus.IconImage.sprite,
                Is.SameAs(testSerenaChaseIcon));
            Assert.That(chaseStatus.ValueText.text, Is.EqualTo("1"));

            CombatActionExecutionResult secondThreeMatch =
                ResolveWaterMatch(blockCount: 3);
            Assert.That(FindDamage(
                    secondThreeMatch,
                    SerenaCoralspearRules.CharacterId,
                    ActionOrigin.Match).Context
                    .AttackTypeDamageIncreaseRateSum,
                Is.Zero.Within(0.000000001d));
            Assert.That(serenaActive.RemainingCooldown, Is.EqualTo(7));
            Assert.That(activeStatus.ValueText.text, Is.EqualTo("2"));
            RestorePartyHpForSnapshotTest();

            bool observedConsumedCharge = false;
            CombatActionExecutionResult empoweredFiveMatch =
                ResolveWaterMatchWithStepObserver(
                    blockCount: 5,
                    action =>
                    {
                        if (action.Origin != ActionOrigin.Chase)
                        {
                            return;
                        }

                        observedConsumedCharge = true;
                        Assert.That(combat.CombatStatusSources[0]
                                .CurrentCount,
                            Is.Zero);
                        Assert.That(chaseStatus.Root.activeSelf,
                            Is.False);
                    });
            DamageActionResult empoweredChase = empoweredFiveMatch
                .ActionResults
                .OfType<DamageActionResult>()
                .Single(result => result.Action.Origin
                    == ActionOrigin.Chase);
            Assert.That(empoweredChase.Context
                    .AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(1.15d).Within(0.000000001d));
            Assert.That(observedConsumedCharge, Is.True);
            Assert.That(combat.CombatStatusSources[0].CurrentCount,
                Is.EqualTo(1));
            Assert.That(chaseStatus.Root.activeSelf, Is.True);
            Assert.That(chaseStatus.ValueText.text, Is.EqualTo("1"));
            Assert.That(serenaActive.RemainingCooldown, Is.EqualTo(6));
            Assert.That(activeStatus.ValueText.text, Is.EqualTo("1"));
            RestorePartyHpForSnapshotTest();

            combat.Boss.Resources.Get(WaterElementResource.Id).Add(1);
            CombatActionExecutionResult activeFourMatch =
                ResolveWaterMatch(blockCount: 4);
            DamageActionResult activeAdditional = FindDamage(
                activeFourMatch,
                SerenaCoralspearRules.CharacterId,
                ActionOrigin.Additional);
            Assert.That(activeAdditional.Context
                    .AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.35d).Within(0.000000001d));
            Assert.That(combat.Party.Characters[1].Effects.FindByEffectId(
                    SerenaCoralspearRules.ActiveEffectId),
                Is.Empty);
            Assert.That(combat.CombatStatusSources[0].CurrentCount,
                Is.EqualTo(1));
            Assert.That(activeStatus.Root.activeSelf, Is.True);
            Assert.That(activeStatus.IconImage.sprite,
                Is.SameAs(testSerenaChaseIcon));
            Assert.That(activeStatus.ValueText.text, Is.EqualTo("1"));
            Assert.That(chaseStatus.Root.activeSelf, Is.False);
            Assert.That(serenaActive.RemainingCooldown, Is.EqualTo(5));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerProjectileUsesScenePoolAndCompletes()
        {
            BattleCombatPresentationController presentation =
                flow.CombatPresentationController;
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.ProjectilePool.ActiveCount, Is.Zero);
            bool completed = false;
            MatchDamageProjectileCompletion completion =
                MatchDamageProjectileCompletion.Cancelled;

            bool started = presentation.TryPresent(
                new MatchDamageProjectileRequest(
                    combat.Party.Characters[0].PartySlotIndex,
                    combat.Party.Characters[0].CharacterId,
                    ElementType.Water),
                result =>
                {
                    completion = result;
                    completed = true;
                });

            Assert.That(started, Is.True);
            Assert.That(presentation.IsPresenting, Is.True);
            Assert.That(presentation.ProjectilePool.ActiveCount,
                Is.EqualTo(1));
            float deadline = Time.realtimeSinceStartup + 2f;
            while (!completed && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(completed, Is.True);
            Assert.That(completion,
                Is.EqualTo(MatchDamageProjectileCompletion.Arrived));
            Assert.That(presentation.IsPresenting, Is.False);
            Assert.That(presentation.ProjectilePool.ActiveCount, Is.Zero);
            Assert.That(presentation.ProjectilePool.AvailableCount,
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BossProjectileUsesScenePoolAndCompletes()
        {
            BattleCombatPresentationController presentation =
                flow.CombatPresentationController;
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.BossProjectileColor,
                Is.EqualTo(Color.white));
            Assert.That(presentation.ProjectilePool.ActiveCount, Is.Zero);
            bool completed = false;
            BossDamageProjectileCompletion completion =
                BossDamageProjectileCompletion.Cancelled;

            bool started = presentation.TryPresent(
                new BossDamageProjectileRequest(1, 1),
                result =>
                {
                    completion = result;
                    completed = true;
                });

            Assert.That(started, Is.True);
            Assert.That(presentation.IsPresenting, Is.True);
            Assert.That(presentation.ProjectilePool.ActiveCount,
                Is.EqualTo(1));
            float deadline = Time.realtimeSinceStartup + 2f;
            while (!completed && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(completed, Is.True);
            Assert.That(completion,
                Is.EqualTo(BossDamageProjectileCompletion.Arrived));
            Assert.That(presentation.IsPresenting, Is.False);
            Assert.That(presentation.ProjectilePool.ActiveCount, Is.Zero);
            Assert.That(presentation.ProjectilePool.AvailableCount,
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AppliedStepsSpawnNonBlockingFloatingNumbersAndRelease()
        {
            BattleCombatPresentationController presentation =
                flow.CombatPresentationController;
            presentation.SetDamageNumberRandomSource(
                new SystemPresentationRandomSource(7357));
            Assert.That(presentation.DamageNumberPool, Is.Not.Null);
            Assert.That(presentation.DamageNumberPool.ActiveCount, Is.Zero);

            CombatActionExecutionResult match = ResolveWaterMatch(
                blockCount: 3);
            DamageActionResult playerDamage = match.ActionResults
                .OfType<DamageActionResult>()
                .Single();
            BossDamageActionResult bossDamage = combat.Bridge
                .LastBossExecutionResult.ActionResults
                .OfType<BossDamageActionResult>()
                .Single();

            Assert.That(flow.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(presentation.IsPresenting, Is.False,
                "Floating numbers must not hold the projectile gate.");
            Assert.That(presentation.DamageNumberPool.ActiveCount,
                Is.EqualTo(2));
            DamageNumberView[] activeNumbers =
                UnityEngine.Object.FindObjectsByType<DamageNumberView>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Where(view => view.gameObject.activeInHierarchy)
                    .ToArray();
            Assert.That(activeNumbers, Has.Length.EqualTo(2));
            Assert.That(activeNumbers.Select(view => view.DisplayText),
                Is.EquivalentTo(new[]
                {
                    playerDamage.AppliedDamage.ToString(),
                    bossDamage.AppliedHpDamage.ToString()
                }));
            Assert.That(activeNumbers,
                Has.All.Matches<DamageNumberView>(view =>
                    !view.BlocksRaycasts));

            float deadline = Time.realtimeSinceStartup + 2f;
            while (presentation.DamageNumberPool.ActiveCount > 0
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(presentation.DamageNumberPool.ActiveCount, Is.Zero);
            Assert.That(presentation.DamageNumberPool.AvailableCount,
                Is.EqualTo(DamageNumberPool.DefaultPrewarmCount));
        }

        [UnityTest]
        public IEnumerator ProductionCombatRunsWithoutDebugPanel()
        {
            BattleFlowCombatBridge bridge = flow.CombatBridge;
            BattleContext context = flow.Context;
            yield return null;

            Assert.That(UnityEngine.Object.FindObjectsByType<
                BattleFlowDebugPanel>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None), Is.Empty);
            Assert.That(combatBootstrap, Is.Not.Null);
            Assert.That(combatBootstrap.HasInitializedCombat, Is.True);
            Assert.That(flow.Context, Is.SameAs(context));
            Assert.That(flow.CombatBridge, Is.SameAs(bridge));
            Assert.That(flow.Context.Phase, Is.EqualTo(BattlePhase.PlayerInput));
        }

        [UnityTest]
        public IEnumerator SceneRuntimeResolvesWaterSnapshotsAndTerminal()
        {
            CombatActionExecutionResult threeMatch =
                ResolveWaterMatch(blockCount: 3);
            Assert.That(threeMatch.ActionResults[0],
                Is.TypeOf<DamageActionResult>());
            Assert.That(threeMatch.ActionResults[1],
                Is.TypeOf<AddResourceActionResult>());
            Assert.That(combat.Boss.Resources.GetAmount(
                    WaterElementResource.Id),
                Is.EqualTo(1));
            BossDamageActionResult firstBossAction = combat.Bridge
                .LastBossExecutionResult.ActionResults
                .OfType<BossDamageActionResult>()
                .Single();
            Assert.That(firstBossAction.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(637));
            Assert.That(combat.KragmorRuntimeState.PatternIndex,
                Is.EqualTo(1));
            Assert.That(combat.KragmorRuntimeState.NextActionKind,
                Is.EqualTo(KragmorActionKind.RockshardEruption));
            Assert.That(CountRocks(boardController.CurrentBoard), Is.Zero);
            RestorePartyHpForSnapshotTest();

            CombatActionExecutionResult fourMatch =
                ResolveWaterMatch(blockCount: 4);
            DamageAction fourDamage = (DamageAction)
                fourMatch.ActionResults[0].Action;
            Assert.That(fourDamage.ContextRequest.SkillCoefficient,
                Is.EqualTo(1.90d));
            Assert.That(
                fourDamage.ContextRequest.ActionLocalDealtDamageIncreaseRate,
                Is.EqualTo(0.15d));
            Assert.That(combat.Boss.Resources.GetAmount(
                    WaterElementResource.Id),
                Is.EqualTo(2));
            BossDamageActionResult secondBossAction = combat.Bridge
                .LastBossExecutionResult.ActionResults
                .OfType<BossDamageActionResult>()
                .Single();
            Assert.That(secondBossAction.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(552));
            Assert.That(combat.KragmorRuntimeState.PatternIndex,
                Is.EqualTo(2));
            Assert.That(combat.KragmorRuntimeState.NextActionKind,
                Is.EqualTo(KragmorActionKind.CoreCompression));
            AssertRockshardPresentation(expectedRockCount: 3);
            RestorePartyHpForSnapshotTest();

            CombatActionExecutionResult fiveMatch =
                ResolveWaterMatch(blockCount: 5);
            Assert.That(fiveMatch.ActionResults
                .OfType<DamageActionResult>().Count(), Is.EqualTo(1));
            ConsumeResourceActionResult consumption = fiveMatch.ActionResults
                .OfType<ConsumeResourceActionResult>()
                .Single();
            Assert.That(consumption.ConsumeResult.ConsumedAmount,
                Is.EqualTo(2));
            Assert.That(consumption.ConsumptionRecord, Is.Not.Null);
            Assert.That(consumption.ConsumptionRecord.ConsumerId,
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(combat.Boss.Resources.GetAmount(
                    WaterElementResource.Id),
                Is.Zero);
            Assert.That(combat.Bridge.LastBossExecutionResult.ActionResults
                    .Select(result => result.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(RemoveEffectActionResult),
                    typeof(ApplyEffectActionResult)
                }));
            Assert.That(combat.KragmorRuntimeState.PatternIndex,
                Is.EqualTo(3));
            AssertDefense(KragmorDefenseState.CoreCompression);
            Assert.That(combat.KragmorRuntimeState.NextActionKind,
                Is.EqualTo(KragmorActionKind.EarthCollapse));

            SetBossHpForTerminalTest(1);
            int turnBeforeLethal = flow.Context.CurrentTurn;
            CombatActionExecutionResult lethal =
                ResolveWaterMatch(blockCount: 3, completeBossAction: false);
            Assert.That(lethal.StoppedEarly, Is.True);
            Assert.That(lethal.BossDefeated, Is.True);
            Assert.That(combat.Boss.IsDefeated, Is.True);
            Assert.That(flow.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(flow.Context.Phase, Is.EqualTo(BattlePhase.Result));
            Assert.That(flow.Context.CurrentTurn, Is.EqualTo(turnBeforeLethal));
            yield return null;

            Assert.That(
                combat.ResultPersistenceCoordinator.IsPersistenceCompleted,
                Is.True);
            Assert.That(
                combat.ResultPersistenceCoordinator.LastPersistenceOperation
                    .IsSuccess,
                Is.True);
            ProfileSaveData savedProfile = GameBootstrapper.Instance
                .SaveService.GetCurrentProfileSnapshot();
            BossRecordSaveData savedRecord = savedProfile.BossRecords.Single(
                record => record.BossId == "kragmor"
                    && record.DifficultyId
                        == BattleDifficultyIds.Normal);
            Assert.That(savedRecord.IsCleared, Is.True);
        }

        [UnityTest]
        public IEnumerator SceneCascadeRemovesCollateralRocksAndMovesRockView()
        {
            BoardState fixture = CreateRockCascadeFixture(
                out BoardSwap swap,
                out long firstCollateralRockId,
                out long secondCollateralRockId,
                out long fallingRockId);
            SetAutoProperty(
                typeof(BattleBoardController),
                "CurrentBoard",
                boardController,
                fixture);
            boardView.Render(fixture);
            BlockView fallingView = boardView.ActiveViews[fallingRockId];

            Assert.That(boardController.TryExecuteSwap(swap), Is.True);

            float deadline = Time.realtimeSinceStartup + 10f;
            while (!boardController.IsBoardReady
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            BoardSwapActionResult result =
                boardController.LastSwapActionResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status,
                Is.EqualTo(BoardSwapActionStatus.Resolved));
            BoardCascadeStep firstStep = result.Cascade.Steps[0];
            Assert.That(firstStep.Matches, Has.Count.EqualTo(1));
            Assert.That(firstStep.Matches[0].Positions,
                Has.Count.EqualTo(3));
            Assert.That(firstStep.Collapse.Removals,
                Has.Count.EqualTo(5));
            Assert.That(firstStep.Collapse.Removals.Count(removal =>
                    removal.Block.BlockType == BoardBlockType.Rock),
                Is.EqualTo(2));
            Assert.That(boardView.TryGetView(
                firstCollateralRockId,
                out _), Is.False);
            Assert.That(boardView.TryGetView(
                secondCollateralRockId,
                out _), Is.False);
            Assert.That(boardView.TryGetView(
                fallingRockId,
                out BlockView movedRockView), Is.True);
            Assert.That(movedRockView, Is.SameAs(fallingView));
            Assert.That(movedRockView.Position,
                Is.EqualTo(new BoardPosition(0, 0)));
            Assert.That(boardController.CurrentBoard.Get(
                    new BoardPosition(0, 0)).RuntimeId,
                Is.EqualTo(fallingRockId));
            Assert.That(boardView.TryValidateCurrentViewLayout(
                boardController.CurrentBoard,
                "Rock cascade PlayMode result",
                out string failure), Is.True, failure);
        }

        [UnityTest]
        public IEnumerator SceneRuntimeTransitionsKragmorDefenseAcrossCycle()
        {
            AssertDefense(KragmorDefenseState.VolcanicCarapace);
            combat.Party.Shields.Add(new ShieldInstance(
                runtimeId: 900001,
                sourceId: "test_kragmor_cycle_survival",
                initialAmount: 10000,
                createdTurn: 1,
                remainingTurns: null,
                creationOrder: 900001));

            ResolveWaterMatch(blockCount: 3);
            AssertDefense(KragmorDefenseState.VolcanicCarapace);

            ResolveWaterMatch(blockCount: 3);
            AssertDefense(KragmorDefenseState.VolcanicCarapace);

            ResolveWaterMatch(blockCount: 3);
            AssertDefense(KragmorDefenseState.CoreCompression);
            Assert.That(combat.Bridge.LastBossExecutionResult.ActionResults
                    .Select(result => result.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(RemoveEffectActionResult),
                    typeof(ApplyEffectActionResult)
                }));

            ResolveWaterMatch(blockCount: 3);
            AssertDefense(KragmorDefenseState.CoreExposure);
            Assert.That(combat.Bridge.LastBossExecutionResult.ActionResults
                    .Select(result => result.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(BossDamageActionResult),
                    typeof(RemoveEffectActionResult),
                    typeof(ApplyEffectActionResult)
                }));

            CombatActionExecutionResult exposedPlayerAction =
                ResolveWaterMatch(blockCount: 3);
            DamageActionResult exposedDamage = exposedPlayerAction
                .ActionResults.OfType<DamageActionResult>().Single();
            Assert.That(exposedDamage.DamageResult
                    .TargetTakenDamageMultiplier,
                Is.EqualTo(1.30d).Within(0.000000001d));
            AssertDefense(KragmorDefenseState.VolcanicCarapace);
            Assert.That(combat.KragmorRuntimeState.PatternIndex,
                Is.EqualTo(1));

            ResolveWaterMatch(blockCount: 3);
            Assert.That(CountRocks(boardController.CurrentBoard),
                Is.EqualTo(6));
            Assert.That(boardController.LastRockMutationResult.CreatedCount,
                Is.EqualTo(3));
            ResolveWaterMatch(blockCount: 3);
            ResolveWaterMatch(blockCount: 3);
            ResolveWaterMatch(blockCount: 3);
            ResolveWaterMatch(blockCount: 3);
            Assert.That(CountRocks(boardController.CurrentBoard),
                Is.EqualTo(6));
            Assert.That(boardController.LastRockMutationResult.CreatedCount,
                Is.Zero);
            Assert.That(combat.KragmorRuntimeState.NextActionKind,
                Is.EqualTo(KragmorActionKind.CoreCompression));
            yield return null;
        }

        private CombatActionExecutionResult ResolveWaterMatch(
            int blockCount,
            bool completeBossAction = true)
        {
            Assert.That(flow.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(flow.Coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(flow.Coordinator.NotifyBoardActionResolved(
                CreateWaterCascade(blockCount),
                consumesTurn: true), Is.True);
            Assert.That(flow.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);
            Assert.That(combat.Bridge.ResolveMatchEvent(execution), Is.True);

            CombatActionExecutionResult result =
                combat.Bridge.LastMatchExecutionResult;
            Assert.That(result, Is.Not.Null);
            if (completeBossAction
                && flow.Context.Result == BattleResultKind.None)
            {
                Assert.That(flow.Context.Phase,
                    Is.EqualTo(BattlePhase.BossActing));
                Assert.That(combat.Bridge.ResolveBossAction(
                    flow.Context.CurrentTurn), Is.True);
                Assert.That(flow.Context.Phase,
                    Is.EqualTo(BattlePhase.PlayerInput));
            }

            return result;
        }

        private CombatActionExecutionResult
            ResolveWaterMatchWithStepObserver(
                int blockCount,
                Action<CombatAction> stepApplied)
        {
            Assert.That(stepApplied, Is.Not.Null);
            Assert.That(flow.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(flow.Coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(flow.Coordinator.NotifyBoardActionResolved(
                CreateWaterCascade(blockCount),
                consumesTurn: true), Is.True);
            Assert.That(flow.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);
            Assert.That(combat.Bridge.TryBeginMatchEventCombat(
                execution,
                out bool hasMatchingCharacter), Is.True);
            Assert.That(hasMatchingCharacter, Is.True);

            while (combat.Bridge.TryGetNextMatchCombatAction(
                out _,
                out CombatAction action))
            {
                Assert.That(combat.Bridge.TryApplyNextMatchCombatAction(
                    out _), Is.True);
                stepApplied(action);
            }

            Assert.That(combat.Bridge.TryCompleteMatchEventCombat(execution),
                Is.True);
            CombatActionExecutionResult result =
                combat.Bridge.LastMatchExecutionResult;
            Assert.That(result, Is.Not.Null);
            if (flow.Context.Result == BattleResultKind.None)
            {
                Assert.That(combat.Bridge.ResolveBossAction(
                    flow.Context.CurrentTurn), Is.True);
                Assert.That(flow.Context.Phase,
                    Is.EqualTo(BattlePhase.PlayerInput));
            }

            return result;
        }

        private static BoardCascadeResult CreateWaterCascade(int blockCount)
        {
            var board = new BoardState();
            var positions = new List<BoardPosition>(blockCount);
            var removals = new List<BoardBlockRemoval>(blockCount);
            for (int index = 0; index < blockCount; index++)
            {
                var position = new BoardPosition(index, 0);
                var block = new BoardBlock(
                    index + 1,
                    BoardBlockType.Normal,
                    ElementType.Water);
                positions.Add(position);
                removals.Add(CreateNonPublic<BoardBlockRemoval>(
                    block,
                    position));
            }

            BoardMatch match = CreateNonPublic<BoardMatch>(
                ElementType.Water,
                ResolveTier(blockCount),
                positions[0],
                positions);
            BoardCollapseResult collapse =
                CreateNonPublic<BoardCollapseResult>(
                    board,
                    removals,
                    new List<BoardBlockMove>());
            BoardRefillResult refill =
                CreateNonPublic<BoardRefillResult>(
                    board,
                    new List<BoardBlockSpawn>());
            BoardCascadeStep step = CreateNonPublic<BoardCascadeStep>(
                new List<BoardMatch> { match },
                collapse,
                refill);
            return CreateNonPublic<BoardCascadeResult>(
                board,
                new List<BoardCascadeStep> { step });
        }

        private static BoardMatchTier ResolveTier(int blockCount)
        {
            if (blockCount == 3)
            {
                return BoardMatchTier.Three;
            }

            if (blockCount == 4)
            {
                return BoardMatchTier.Four;
            }

            if (blockCount >= 5)
            {
                return BoardMatchTier.FiveOrMore;
            }

            throw new ArgumentOutOfRangeException(nameof(blockCount));
        }

        private void SetBossHpForTerminalTest(long currentHp)
        {
            long damage = combat.Boss.CurrentHp - currentHp;
            MethodInfo applyDamage = typeof(BossBattleState).GetMethod(
                "ApplyDamage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(applyDamage, Is.Not.Null);
            applyDamage.Invoke(combat.Boss, new object[] { damage });
            Assert.That(combat.Boss.CurrentHp, Is.EqualTo(currentHp));
        }

        private void RestorePartyHpForSnapshotTest()
        {
            MethodInfo applyHealing = typeof(PartyBattleState).GetMethod(
                "ApplyHpHealing",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(applyHealing, Is.Not.Null);
            applyHealing.Invoke(
                combat.Party,
                new object[] { combat.Party.MaxHp });
            Assert.That(combat.Party.CurrentHp,
                Is.EqualTo(combat.Party.MaxHp));
        }

        private static DamageActionResult FindDamage(
            CombatActionExecutionResult result,
            string attackerId,
            ActionOrigin origin)
        {
            return result.ActionResults
                .OfType<DamageActionResult>()
                .Single(item => item.Action.Origin == origin
                    && item.Action is DamageAction damage
                    && string.Equals(
                        damage.ContextRequest.Attacker.CharacterId,
                        attackerId,
                        StringComparison.Ordinal));
        }

        private void AssertDefense(KragmorDefenseState expectedState)
        {
            Assert.That(combat.KragmorRuntimeState.CurrentDefenseState,
                Is.EqualTo(expectedState));
            Assert.That(
                KragmorDefenseEffectFactory.CountActiveDefenseEffects(
                    combat.Boss),
                Is.EqualTo(1));
            Assert.That(combat.Boss.Effects.FindByEffectId(
                    KragmorDefenseEffectFactory.GetEffectId(expectedState)),
                Has.Count.EqualTo(1));
        }

        private void AssertRockshardPresentation(int expectedRockCount)
        {
            Assert.That(boardController.LastRockMutationResult, Is.Not.Null);
            Assert.That(boardController.LastRockMutationResult.Succeeded,
                Is.True);
            Assert.That(boardController.LastRockMutationResult.CreatedCount,
                Is.EqualTo(expectedRockCount));
            Assert.That(CountRocks(boardController.CurrentBoard),
                Is.EqualTo(expectedRockCount));
            Assert.That(boardView.TryValidateCurrentViewLayout(
                boardController.CurrentBoard,
                "Rockshard PlayMode result",
                out string failure), Is.True, failure);

            BlockView firstRockView = null;
            foreach (BoardRockPlacement placement
                in boardController.LastRockMutationResult.Placements)
            {
                Assert.That(boardView.TryGetView(
                    placement.RockBlock.RuntimeId,
                    out BlockView rockView), Is.True);
                Assert.That(rockView.Position,
                    Is.EqualTo(placement.Position));
                Assert.That(rockView.Image.sprite, Is.Not.Null);
                Assert.That(rockView.Image.sprite.name,
                    Is.EqualTo("RockBlock"));
                firstRockView ??= rockView;
            }

            var pointer = new PointerEventData(EventSystem.current)
            {
                pointerId = 701,
                pointerPress = firstRockView.gameObject,
                position = RectTransformUtility.WorldToScreenPoint(
                    null,
                    firstRockView.RectTransform.position)
            };
            boardInput.OnBeginDrag(pointer);
            Assert.That(boardInput.IsTrackingPointer, Is.False);
            Assert.That(flow.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
        }

        private static BoardState CreateRockCascadeFixture(
            out BoardSwap swap,
            out long firstCollateralRockId,
            out long secondCollateralRockId,
            out long fallingRockId)
        {
            var board = new BoardState();
            long runtimeId = 1;
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    board.Set(
                        new BoardPosition(x, y),
                        new BoardBlock(
                            runtimeId++,
                            BoardBlockType.Normal,
                            (ElementType)((x + y) % 5)));
                }
            }

            SetNormal(board, 0, 0, ElementType.Grass);
            SetNormal(board, 1, 0, ElementType.Grass);
            SetNormal(board, 2, 0, ElementType.Dark);
            SetNormal(board, 3, 0, ElementType.Grass);
            firstCollateralRockId = 101;
            secondCollateralRockId = 102;
            fallingRockId = 103;
            board.Set(
                new BoardPosition(0, 1),
                new BoardBlock(
                    firstCollateralRockId,
                    BoardBlockType.Rock,
                    null));
            board.Set(
                new BoardPosition(1, 1),
                new BoardBlock(
                    secondCollateralRockId,
                    BoardBlockType.Rock,
                    null));
            board.Set(
                new BoardPosition(0, 2),
                new BoardBlock(
                    fallingRockId,
                    BoardBlockType.Rock,
                    null));
            swap = new BoardSwap(
                new BoardPosition(2, 0),
                new BoardPosition(3, 0));
            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            Assert.That(new BoardMoveAnalyzer().IsValidSwap(
                board,
                swap.First,
                swap.Second), Is.True);
            return board;
        }

        private static void SetNormal(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            var position = new BoardPosition(x, y);
            board.Set(
                position,
                new BoardBlock(
                    board.Get(position).RuntimeId,
                    BoardBlockType.Normal,
                    element));
        }

        private static int CountRocks(BoardState board)
        {
            int count = 0;
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                if (board.Get(BoardPosition.FromIndex(index))?.BlockType
                    == BoardBlockType.Rock)
                {
                    count++;
                }
            }

            return count;
        }

        private void InstallTestBootstrapper()
        {
            string testName = TestContext.CurrentContext.Test.Name;
            bool standaloneSerena = string.Equals(
                testName,
                nameof(SceneComposesStandaloneSerenaFromOwnedPartyProfile),
                StringComparison.Ordinal);
            bool mareaAndSerena = string.Equals(
                testName,
                nameof(SceneComposesMareaAndSerenaWithSharedCombatState),
                StringComparison.Ordinal);
            bool includeSerena = standaloneSerena || mareaAndSerena;

            testMareaConfig = ScriptableObject.CreateInstance<
                MareaBluefangCombatConfig>();
            SetField(testMareaConfig, "match3Coefficient", 0.90d);
            SetField(testMareaConfig, "match4BaseCoefficient", 1.50d);
            SetField(
                testMareaConfig,
                "match4WaterElementBonusCoefficient",
                0.40d);
            SetField(testMareaConfig, "match5BaseCoefficient", 2.40d);
            SetField(
                testMareaConfig,
                "match5CoefficientPerWaterElement",
                1.40d);
            SetField(
                testMareaConfig,
                "passiveDealtDamageIncreaseRate",
                0.15d);
            SetField(
                testMareaConfig,
                "activeWaterDamageIncreaseRate",
                0.25d);
            SetField(testMareaConfig, "activeDurationTurns", 3);
            SetField(testMareaConfig, "activeCooldownTurns", 8);
            SetField(testMareaConfig, "waterElementMaxAmount", 5);

            testMareaDefinition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            SetField(
                testMareaDefinition,
                "id",
                MareaBluefangRules.CharacterId);
            SetField(testMareaDefinition, "element", ElementType.Water);
            SetField(testMareaDefinition, "level1Hp", 900);
            SetField(testMareaDefinition, "level1Attack", 180);
            SetField(testMareaDefinition, "level100Hp", 3400);
            SetField(testMareaDefinition, "level100Attack", 1050);
            SetField(
                testMareaDefinition,
                "combatConfig",
                testMareaConfig);

            if (includeSerena)
            {
                testSerenaConfig = ScriptableObject.CreateInstance<
                    SerenaCoralspearCombatConfig>();
                testSerenaDefinition = ScriptableObject.CreateInstance<
                    CharacterDefinition>();
                SetField(
                    testSerenaDefinition,
                    "id",
                    SerenaCoralspearRules.CharacterId);
                SetField(
                    testSerenaDefinition,
                    "element",
                    ElementType.Water);
                SetField(testSerenaDefinition, "level1Hp", 870);
                SetField(testSerenaDefinition, "level1Attack", 170);
                SetField(testSerenaDefinition, "level100Hp", 3200);
                SetField(testSerenaDefinition, "level100Attack", 980);
                SetField(
                    testSerenaDefinition,
                    "combatConfig",
                    testSerenaConfig);

                testSerenaEffectTexture = new Texture2D(1, 1);
                testSerenaEffectTexture.SetPixel(0, 0, Color.cyan);
                testSerenaEffectTexture.Apply();
                testSerenaEffectIcon = Sprite.Create(
                    testSerenaEffectTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f));
                testSerenaChaseTexture = new Texture2D(1, 1);
                testSerenaChaseTexture.SetPixel(0, 0, Color.blue);
                testSerenaChaseTexture.Apply();
                testSerenaChaseIcon = Sprite.Create(
                    testSerenaChaseTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f));
                testSerenaActiveEffectDefinition =
                    ScriptableObject.CreateInstance<EffectDefinition>();
                SetField(
                    testSerenaActiveEffectDefinition,
                    "id",
                    SerenaCoralspearRules.ActiveEffectId);
                SetField(
                    testSerenaActiveEffectDefinition,
                    "icon",
                    testSerenaEffectIcon);
                testSerenaPassiveDefinition =
                    ScriptableObject.CreateInstance<SkillDefinition>();
                SetField(
                    testSerenaPassiveDefinition,
                    "id",
                    SerenaCoralspearRules.EmpoweredChaseStatusId);
                SetField(
                    testSerenaPassiveDefinition,
                    "skillKind",
                    SkillKind.Passive);
                SetField(
                    testSerenaPassiveDefinition,
                    "icon",
                    testSerenaChaseIcon);
            }

            testKragmorDefinition = ScriptableObject.CreateInstance<
                BossDefinition>();
            testKragmorConfig = ScriptableObject.CreateInstance<
                KragmorCombatConfig>();
            SetField(
                testKragmorConfig,
                "colossusIronFistCoefficient",
                0.75d);
            SetField(
                testKragmorConfig,
                "rockshardEruptionCoefficient",
                0.65d);
            SetField(testKragmorConfig, "rockshardRockCreationCount", 3);
            SetField(testKragmorConfig, "maximumRockCount", 6);
            SetField(
                testKragmorConfig,
                "earthCollapseCoefficient",
                2.40d);
            SetField(
                testKragmorConfig,
                "volcanicCarapaceReductionRate",
                0.10d);
            SetField(
                testKragmorConfig,
                "coreCompressionReductionRate",
                0.20d);
            SetField(
                testKragmorConfig,
                "coreExposureIncreaseRate",
                0.30d);
            SetField(testKragmorDefinition, "id", "kragmor");
            SetField(testKragmorDefinition, "element", ElementType.Fire);
            SetField(testKragmorDefinition, "turnLimit", 25);
            SetField(
                testKragmorDefinition,
                "combatConfig",
                testKragmorConfig);
            SetField(
                testKragmorDefinition,
                "difficultyStats",
                new[]
                {
                    new BossDifficultyStats(
                        BattleDifficultyIds.Normal,
                        maxHp: 66000,
                        attack: includeSerena ? 100d : 850d),
                    new BossDifficultyStats(
                        BattleDifficultyIds.Challenge,
                        maxHp: 66000,
                        attack: includeSerena ? 100d : 850d)
                });

            testDefinitionDatabase = ScriptableObject.CreateInstance<
                DefinitionDatabase>();
            SetField(
                testDefinitionDatabase,
                "characters",
                includeSerena
                    ? new[]
                    {
                        testMareaDefinition,
                        testSerenaDefinition
                    }
                    : new[] { testMareaDefinition });
            SetField(
                testDefinitionDatabase,
                "bosses",
                new[] { testKragmorDefinition });
            if (includeSerena)
            {
                SetField(
                    testDefinitionDatabase,
                    "skills",
                    new[] { testSerenaPassiveDefinition });
                SetField(
                    testDefinitionDatabase,
                    "effects",
                    new[] { testSerenaActiveEffectDefinition });
            }
            testResultBalanceDefinition = CreateResultBalanceDefinition();
            SetField(
                testDefinitionDatabase,
                "battleResultBalance",
                testResultBalanceDefinition);
            testDefinitionDatabase.Initialize();

            saveRoot = Path.Combine(
                Application.temporaryCachePath,
                "ValorChronicleBattleSceneTests",
                Guid.NewGuid().ToString("N"));
            saveService = SaveSystemFactory.Create(
                saveRoot,
                testDefinitionDatabase);
            SaveLoadResult load = saveService.LoadOrCreate(
                "battle_scene_test");
            Assert.That(load.IsSuccess, Is.True, load.Message);
            SaveTransactionResult partySetup =
                saveService.ExecuteTransaction(profile =>
                {
                    if (!standaloneSerena)
                    {
                        profile.Characters.Add(new CharacterSaveData
                        {
                            CharacterId = MareaBluefangRules.CharacterId,
                            Level = 1,
                            Awakening = 0
                        });
                        profile.Party.Presets[0].CharacterSlotIds[0] =
                            MareaBluefangRules.CharacterId;
                    }

                    if (includeSerena)
                    {
                        profile.Characters.Add(new CharacterSaveData
                        {
                            CharacterId =
                                SerenaCoralspearRules.CharacterId,
                            Level = 1,
                            Awakening = 0
                        });
                        profile.Party.Presets[0].CharacterSlotIds[
                            mareaAndSerena ? 1 : 0] =
                            SerenaCoralspearRules.CharacterId;
                    }
                });
            Assert.That(partySetup.IsSuccess, Is.True, partySetup.Message);

            bootstrapRoot = new GameObject("Test GameBootstrapper");
            bootstrapRoot.SetActive(false);
            GameBootstrapper bootstrapper =
                bootstrapRoot.AddComponent<GameBootstrapper>();
            SetAutoProperty(
                typeof(GameBootstrapper),
                "Instance",
                null,
                bootstrapper);
            SetAutoProperty(
                typeof(GameBootstrapper),
                "RandomSource",
                bootstrapper,
                new SeededRandomSource(54321));
            gamePauseService = new GamePauseService();
            SetAutoProperty(
                typeof(GameBootstrapper),
                "GamePauseService",
                bootstrapper,
                gamePauseService);
            settingsService = SettingsService.CreateInMemoryDefaults();
            settingsService.LoadOrCreate();
            SetAutoProperty(
                typeof(GameBootstrapper),
                "SettingsService",
                bootstrapper,
                settingsService);
            SetAutoProperty(
                typeof(GameBootstrapper),
                "SceneService",
                bootstrapper,
                new SceneService());
            var battleEntryService = new BattleEntryService();
            Assert.That(
                battleEntryService.TryRegister(
                    new BattleEntryRequest(
                        KragmorRules.BossId,
                        BattleDifficultyIds.Normal)),
                Is.True);
            SetAutoProperty(
                typeof(GameBootstrapper),
                "BattleEntryService",
                bootstrapper,
                battleEntryService);
            SetField(
                bootstrapper,
                "definitionDatabase",
                testDefinitionDatabase);
            SetAutoProperty(
                typeof(GameBootstrapper),
                "SaveService",
                bootstrapper,
                saveService);
            SetAutoProperty(
                typeof(GameBootstrapper),
                "InitializationResult",
                bootstrapper,
                CreateSuccessfulInitializationResult(load));
        }

        private static BootstrapInitializationResult
            CreateSuccessfulInitializationResult(SaveLoadResult saveLoad)
        {
            return (BootstrapInitializationResult)Activator.CreateInstance(
                typeof(BootstrapInitializationResult),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[]
                {
                    BootstrapInitializationStatus.Success,
                    saveLoad,
                    null,
                    string.Empty
                },
                culture: null);
        }

        private static BattleResultBalanceDefinition
            CreateResultBalanceDefinition()
        {
            BattleResultBalanceDefinition definition =
                ScriptableObject.CreateInstance<
                    BattleResultBalanceDefinition>();
            SetField(
                definition,
                "remainingTurnBonusPercent",
                BattleResultBalanceDefaults.RemainingTurnBonusPercent);
            SetField(
                definition,
                "gradeThresholds",
                BattleResultBalanceDefaults.CreateGradeThresholds());
            SetField(
                definition,
                "firstGradeRewards",
                BattleResultBalanceDefaults.CreateFirstGradeRewards());
            SetField(
                definition,
                "difficultyRepeatRewards",
                BattleResultBalanceDefaults
                    .CreateDifficultyRepeatRewards());
            return definition;
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(
                target.GetType().FullName,
                fieldName);
        }

        private static void SetAutoProperty(
            Type declaringType,
            string propertyName,
            object target,
            object value)
        {
            FieldInfo backingField = declaringType.GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.NonPublic
                    | (target == null
                        ? BindingFlags.Static
                        : BindingFlags.Instance));
            Assert.That(backingField, Is.Not.Null);
            backingField.SetValue(target, value);
        }

        private static T CreateNonPublic<T>(params object[] arguments)
        {
            ConstructorInfo constructor = typeof(T).GetConstructors(
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Single();
            return (T)constructor.Invoke(arguments);
        }
    }
}
