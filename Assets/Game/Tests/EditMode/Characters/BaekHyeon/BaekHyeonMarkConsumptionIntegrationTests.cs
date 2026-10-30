using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.BaekHyeon;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonMarkConsumptionIntegrationTests
    {
        [TestCase(AttackType.Additional, AttackType.Additional)]
        [TestCase(AttackType.Chase, AttackType.Chase)]
        [TestCase(AttackType.Additional, AttackType.Chase)]
        public void ConsecutiveAttacksOnlyFirstUsesAndConsumesMarks(
            AttackType firstType,
            AttackType secondType)
        {
            using (var battle = new BaekHyeonMarkConsumptionRuleTests
                .MarkConsumptionBattle())
            {
                battle.AddMarks(5);
                var actions = new CombatAction[]
                {
                    battle.Damage(firstType, 1d),
                    battle.Damage(secondType, 1d)
                };

                CombatActionExecutionResult execution = battle.Execute(
                    actions,
                    battle.Rule);
                DamageActionResult[] damages = execution.ActionResults
                    .OfType<DamageActionResult>().ToArray();

                Assert.That(damages.Length, Is.EqualTo(2));
                Assert.That(damages[0].Context
                    .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.15d).Within(0.000000001d));
                Assert.That(damages[1].Context
                    .TargetTakenDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(execution.ActionResults[1],
                    Is.TypeOf<RemoveEffectActionResult>());
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TriggerRegistrationOrderStillRemovesBeforeQueuedAttack(
            bool consumptionRuleFirst)
        {
            using (var battle = new BaekHyeonMarkConsumptionRuleTests
                .MarkConsumptionBattle())
            {
                battle.AddMarks(5);
                battle.Boss.Resources.Register("test_marker", 10);
                var markerRule = new MarkerTriggerRule(battle.ActionIds);
                ICombatTriggerRule[] rules = consumptionRuleFirst
                    ? new ICombatTriggerRule[] { battle.Rule, markerRule }
                    : new ICombatTriggerRule[] { markerRule, battle.Rule };

                CombatActionExecutionResult execution = battle.Execute(
                    new CombatAction[]
                    {
                        battle.Damage(AttackType.Additional, 1d),
                        battle.Damage(AttackType.Chase, 1d)
                    },
                    rules);
                DamageActionResult[] damages = execution.ActionResults
                    .OfType<DamageActionResult>().ToArray();

                Assert.That(damages[0].Context
                    .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.15d).Within(0.000000001d));
                Assert.That(damages[1].Context
                    .TargetTakenDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(execution.ActionResults
                    .OfType<RemoveEffectActionResult>().Count(), Is.EqualTo(1));
                Assert.That(battle.Boss.Resources.GetAmount("test_marker"),
                    Is.EqualTo(2));
            }
        }

        [Test]
        public void FollowUpAttackCannotChainAnotherFollowUpAttack()
        {
            using (var battle = new BaekHyeonMarkConsumptionRuleTests
                .MarkConsumptionBattle())
            {
                var illegalRule = new IllegalFollowUpTriggerRule(
                    battle.ActionIds);

                Assert.That(
                    () => battle.Execute(
                        new[]
                        {
                            battle.Damage(AttackType.Additional, 1d)
                        },
                        illegalRule),
                    Throws.TypeOf<InvalidOperationException>());
            }
        }

        [Test]
        public void LethalFollowUpUsesBonusButPreservesTerminalMarks()
        {
            using (var battle = new BaekHyeonMarkConsumptionRuleTests
                .MarkConsumptionBattle(bossHp: 100L))
            {
                battle.AddMarks(5);

                CombatActionExecutionResult execution = battle.Execute(
                    new[]
                    {
                        battle.Damage(AttackType.Additional, 1d)
                    },
                    battle.Rule);

                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>().Single();
                Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.15d).Within(0.000000001d));
                Assert.That(execution.BossDefeated, Is.True);
                Assert.That(execution.ActionResults.Count, Is.EqualTo(1));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(5));
            }
        }

        [Test]
        public void BrinaPassiveAdditionalUsesAndConsumesMarks()
        {
            var baekConfig = BaekHyeonTestConfig.Create();
            var brinaConfig = ScriptableObject.CreateInstance<
                BrinaAshwoodCombatConfig>();
            try
            {
                var actionIds = new CombatActionIdSequence();
                var brina = new CharacterBattleState(
                    BrinaAshwoodRules.CharacterId,
                    0,
                    ElementType.Grass,
                    1000L,
                    100d);
                var baek = new CharacterBattleState(
                    BaekHyeonRules.CharacterId,
                    1,
                    ElementType.Grass,
                    1000L,
                    100d);
                var party = new PartyBattleState(new[] { brina, baek });
                var boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    100000L,
                    100d);
                AddMarks(boss, baekConfig, 5);
                BrinaAshwoodResolvedCombatRules brinaRules =
                    BrinaAshwoodCombatRuleResolver.Resolve(brinaConfig, 0);
                var brinaState = new BrinaAshwoodBattleState(
                    brina,
                    brinaRules.WoodcutTriggerThreshold);
                brinaState.GainWoodcut(
                    brinaRules.WoodcutTriggerThreshold - 1);
                var brinaRule = new BrinaAshwoodMatchCompletionRule(
                    brinaState,
                    brinaConfig,
                    0,
                    actionIds);
                var consumptionRule = new BaekHyeonMarkConsumptionRule(
                    baekConfig,
                    0,
                    actionIds);
                IReadOnlyList<CombatAction> roots =
                    new BrinaAshwoodMatchActionProvider(brinaConfig)
                        .CreateActions(
                            brina,
                            party,
                            boss,
                            BoardMatchTier.Three,
                            1,
                            actionIds);

                CombatActionExecutionResult execution = Execute(
                    party,
                    boss,
                    roots,
                    brinaRule,
                    consumptionRule);
                DamageActionResult additional = execution.ActionResults
                    .OfType<DamageActionResult>()
                    .Single(result => ((DamageAction)result.Action)
                        .ContextRequest.AttackType
                        == AttackType.Additional);

                Assert.That(additional.Context
                    .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.15d).Within(0.000000001d));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(boss),
                    Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(brinaConfig);
                UnityEngine.Object.DestroyImmediate(baekConfig);
            }
        }

        [Test]
        public void SerenaPassiveChaseUsesAndConsumesMarks()
        {
            var baekConfig = BaekHyeonTestConfig.Create();
            var serenaConfig = ScriptableObject.CreateInstance<
                SerenaCoralspearCombatConfig>();
            try
            {
                var actionIds = new CombatActionIdSequence();
                var source = new CharacterBattleState(
                    "water_consumer",
                    0,
                    ElementType.Fire,
                    1000L,
                    100d);
                var serena = new CharacterBattleState(
                    SerenaCoralspearRules.CharacterId,
                    1,
                    ElementType.Water,
                    1000L,
                    100d);
                var baek = new CharacterBattleState(
                    BaekHyeonRules.CharacterId,
                    2,
                    ElementType.Grass,
                    1000L,
                    100d);
                var party = new PartyBattleState(
                    new[] { source, serena, baek });
                var boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    100000L,
                    100d);
                WaterElementResource.Register(boss.Resources);
                boss.Resources.Add(WaterElementResource.Id, 3);
                AddMarks(boss, baekConfig, 5);
                SerenaCoralspearResolvedCombatRules serenaRules =
                    SerenaCoralspearCombatRuleResolver.Resolve(
                        serenaConfig,
                        0);
                var serenaState = new SerenaCoralspearBattleState(
                    serena,
                    serenaRules.EmpoweredChaseMaxCharges);
                var serenaRule = new SerenaCoralspearPassiveChaseRule(
                    serenaState,
                    serenaConfig,
                    0,
                    actionIds);
                var consumptionRule = new BaekHyeonMarkConsumptionRule(
                    baekConfig,
                    0,
                    actionIds);
                long rootId = actionIds.Next();
                var root = new DamageAction(
                    rootId,
                    ActionOrigin.Match,
                    new DamageContextBuildRequest(
                        source,
                        party,
                        boss,
                        ElementType.Fire,
                        AttackType.Match,
                        AttackTag.Match3,
                        1d,
                        appliesCombo: true,
                        finalComboCount: 1,
                        canCritical: false));
                var consume = new ConsumeResourceAction(
                    actionIds.Next(),
                    ActionOrigin.System,
                    boss,
                    WaterElementResource.Id,
                    source.CharacterId,
                    ResourceConsumptionMode.All,
                    rootActionId: rootId,
                    sourceActionId: rootId);

                CombatActionExecutionResult execution = Execute(
                    party,
                    boss,
                    new CombatAction[] { root, consume },
                    serenaRule,
                    consumptionRule);
                DamageActionResult chase = execution.ActionResults
                    .OfType<DamageActionResult>()
                    .Single(result => ((DamageAction)result.Action)
                        .ContextRequest.AttackType
                        == AttackType.Chase);

                Assert.That(chase.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.15d).Within(0.000000001d));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(boss),
                    Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(serenaConfig);
                UnityEngine.Object.DestroyImmediate(baekConfig);
            }
        }

        [TestCase(true, 0)]
        [TestCase(false, 1)]
        public void PartyOrderControlsWhetherBrinaConsumesNewMatchMark(
            bool baekActsFirst,
            int expectedFinalMarks)
        {
            var baekConfig = BaekHyeonTestConfig.Create();
            var brinaConfig = ScriptableObject.CreateInstance<
                BrinaAshwoodCombatConfig>();
            try
            {
                var actionIds = new CombatActionIdSequence();
                var baek = new CharacterBattleState(
                    BaekHyeonRules.CharacterId,
                    baekActsFirst ? 0 : 1,
                    ElementType.Grass,
                    1000L,
                    100d);
                var brina = new CharacterBattleState(
                    BrinaAshwoodRules.CharacterId,
                    baekActsFirst ? 1 : 0,
                    ElementType.Grass,
                    1000L,
                    100d);
                CharacterBattleState[] ordered = baekActsFirst
                    ? new[] { baek, brina }
                    : new[] { brina, baek };
                var party = new PartyBattleState(ordered);
                var boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    100000L,
                    100d);
                BrinaAshwoodResolvedCombatRules brinaRules =
                    BrinaAshwoodCombatRuleResolver.Resolve(brinaConfig, 0);
                var brinaState = new BrinaAshwoodBattleState(
                    brina,
                    brinaRules.WoodcutTriggerThreshold);
                brinaState.GainWoodcut(
                    brinaRules.WoodcutTriggerThreshold - 1);
                var brinaRule = new BrinaAshwoodMatchCompletionRule(
                    brinaState,
                    brinaConfig,
                    0,
                    actionIds);
                var consumptionRule = new BaekHyeonMarkConsumptionRule(
                    baekConfig,
                    0,
                    actionIds);
                IReadOnlyList<CombatAction> baekActions =
                    new BaekHyeonMatchActionProvider(baekConfig)
                        .CreateActions(
                            baek,
                            party,
                            boss,
                            BoardMatchTier.Three,
                            1,
                            actionIds);
                IReadOnlyList<CombatAction> brinaActions =
                    new BrinaAshwoodMatchActionProvider(brinaConfig)
                        .CreateActions(
                            brina,
                            party,
                            boss,
                            BoardMatchTier.Three,
                            1,
                            actionIds);
                CombatAction[] actions = baekActsFirst
                    ? baekActions.Concat(brinaActions).ToArray()
                    : brinaActions.Concat(baekActions).ToArray();

                CombatActionExecutionResult execution = Execute(
                    party,
                    boss,
                    actions,
                    brinaRule,
                    consumptionRule);
                DamageActionResult additional = execution.ActionResults
                    .OfType<DamageActionResult>()
                    .Single(result => ((DamageAction)result.Action)
                        .ContextRequest.AttackType
                        == AttackType.Additional);

                Assert.That(additional.Context
                    .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(baekActsFirst ? 0.03d : 0d)
                        .Within(0.000000001d));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(boss),
                    Is.EqualTo(expectedFinalMarks));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(brinaConfig);
                UnityEngine.Object.DestroyImmediate(baekConfig);
            }
        }

        private static void AddMarks(
            BossBattleState boss,
            BaekHyeonCombatConfig config,
            int count)
        {
            BaekHyeonResolvedCombatRules rules =
                BaekHyeonCombatRuleResolver.Resolve(config, 0);
            for (int index = 0; index < count; index++)
            {
                long id = 1000L + index;
                boss.Effects.ApplyEffect(
                    BaekHyeonMarkEffectFactory.Create(id, rules));
            }
        }

        private static CombatActionExecutionResult Execute(
            PartyBattleState party,
            BossBattleState boss,
            IReadOnlyList<CombatAction> actions,
            params ICombatTriggerRule[] rules)
        {
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(123)),
                new CombatTriggerResolver(rules));
            return executor.Execute(new CombatActionQueue(actions));
        }

        private sealed class MarkerTriggerRule : ICombatTriggerRule
        {
            private readonly CombatActionIdSequence actionIds;

            public MarkerTriggerRule(CombatActionIdSequence actionIds)
            {
                this.actionIds = actionIds;
            }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (!(context.CompletedResult is DamageActionResult result)
                    || (((DamageAction)result.Action).ContextRequest.AttackType
                            != AttackType.Additional
                        && ((DamageAction)result.Action).ContextRequest
                            .AttackType != AttackType.Chase))
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    new AddResourceAction(
                        actionIds.Next(),
                        ActionOrigin.System,
                        context.Boss,
                        "test_marker",
                        1,
                        context.RootActionId,
                        context.ActionId)
                };
            }
        }

        private sealed class IllegalFollowUpTriggerRule : ICombatTriggerRule
        {
            private readonly CombatActionIdSequence actionIds;

            public IllegalFollowUpTriggerRule(
                CombatActionIdSequence actionIds)
            {
                this.actionIds = actionIds;
            }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (!(context.CompletedResult is DamageActionResult result)
                    || ((DamageAction)result.Action).ContextRequest.AttackType
                        != AttackType.Additional)
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    new DamageAction(
                        actionIds.Next(),
                        ActionOrigin.Additional,
                        new DamageContextBuildRequest(
                            ((DamageAction)result.Action).ContextRequest
                                .Attacker,
                            context.Party,
                            context.Boss,
                            ElementType.Grass,
                            AttackType.Additional,
                            AttackTag.None,
                            1d,
                            appliesCombo: false,
                            finalComboCount: 0,
                            canCritical: false),
                        context.RootActionId,
                        context.ActionId)
                };
            }
        }
    }
}
