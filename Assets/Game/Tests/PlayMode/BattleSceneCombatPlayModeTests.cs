using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.PlayMode
{
    public sealed class BattleSceneCombatPlayModeTests
    {
        private BattleFlowDebugPanel panel;
        private BattleSceneCombatBootstrap combatBootstrap;
        private BattleFlowController flow;
        private BattleSceneCombatComposition combat;
        private BattleBoardController boardController;
        private BattleBoardView boardView;
        private BattleBoardInput boardInput;
        private GameObject bootstrapRoot;

        [UnitySetUp]
        public IEnumerator LoadBattleScene()
        {
            InstallTestBootstrapper();
            yield return SceneManager.LoadSceneAsync(
                "Battle",
                LoadSceneMode.Additive);

            panel = UnityEngine.Object.FindFirstObjectByType<
                BattleFlowDebugPanel>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.enabled, Is.False);
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
            Assert.That(combatBootstrap.HasInitializedCombat, Is.True);
            Assert.That(boardController, Is.Not.Null);
            Assert.That(boardView, Is.Not.Null);
            Assert.That(boardInput, Is.Not.Null);
            Assert.That(flow.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
        }

        [UnityTearDown]
        public IEnumerator UnloadBattleScene()
        {
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
            if (bootstrapRoot != null)
            {
                UnityEngine.Object.Destroy(bootstrapRoot);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneComposesMareaAndActiveAffectsSameTurnMatch()
        {
            Assert.That(flow.RequiresCombatBridge, Is.True);
            Assert.That(flow.CombatBridge, Is.SameAs(combat.Bridge));
            Assert.That(combat.Party.Characters, Has.Count.EqualTo(1));
            Assert.That(combat.Marea.CharacterId,
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(combat.Marea.PartySlotIndex, Is.Zero);
            Assert.That(combat.Marea.Element, Is.EqualTo(ElementType.Water));
            Assert.That(combat.Marea.MaxHp, Is.EqualTo(900));
            Assert.That(combat.Marea.Attack, Is.EqualTo(180d));
            Assert.That(combat.Boss.BossId, Is.EqualTo("kragmor"));
            Assert.That(combat.Boss.MaxHp, Is.EqualTo(66000));
            Assert.That(combat.Boss.CurrentHp, Is.EqualTo(66000));
            Assert.That(combat.Boss.Attack, Is.EqualTo(850d));
            Assert.That(combatBootstrap.BossIntentSource,
                Is.SameAs(combat.BossIntentSource));
            Assert.That(combat.BossIntentSource.NextIntent.ActionKind,
                Is.EqualTo(KragmorActionKind.ColossusIronFist));
            Assert.That(combat.WaterElement.MaxAmount, Is.EqualTo(5));
            Assert.That(combat.WaterElement.CurrentAmount, Is.Zero);
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

            ActiveAbilityRuntimeState active =
                flow.Context.ActiveAbilities[0];
            Assert.That(active.CanUse, Is.True);
            Assert.That(active.RemainingCooldown, Is.Zero);

            Assert.That(flow.TryUseActive(0), Is.True);
            Assert.That(active.RemainingCooldown,
                Is.EqualTo(combat.MareaConfig.ActiveCooldownTurns));
            Assert.That(combat.Marea.Effects.FindByEffectId(
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
            Assert.That(combat.WaterElement.CurrentAmount, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemovingDebugPanelDoesNotAffectProductionCombat()
        {
            BattleFlowCombatBridge bridge = flow.CombatBridge;
            BattleContext context = flow.Context;
            UnityEngine.Object.Destroy(panel);
            yield return null;

            Assert.That(UnityEngine.Object.FindFirstObjectByType<
                BattleFlowDebugPanel>(), Is.Null);
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
            Assert.That(combat.WaterElement.CurrentAmount, Is.EqualTo(1));
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
            Assert.That(combat.WaterElement.CurrentAmount, Is.EqualTo(2));
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
            Assert.That(combat.WaterElement.CurrentAmount, Is.Zero);
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
