using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
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
            Assert.That(combatBootstrap.HasInitializedCombat, Is.True);
            Assert.That(flow.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));
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
                Is.EqualTo(MareaBluefangRules.ActiveCooldownTurns));
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
            Assert.That(flow.Context.Phase, Is.EqualTo(BattlePhase.ActiveInput));
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

        private CombatActionExecutionResult ResolveWaterMatch(
            int blockCount,
            bool completeBossAction = true)
        {
            Assert.That(flow.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));
            Assert.That(flow.CompleteActiveInput(), Is.True);
            Assert.That(flow.Context.Phase,
                Is.EqualTo(BattlePhase.PuzzleInput));
            Assert.That(flow.Coordinator.NotifyBoardActionStarted(), Is.True);
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
                    Is.EqualTo(BattlePhase.ActiveInput));
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
