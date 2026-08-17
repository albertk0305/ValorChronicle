using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class CombatActionExecutionSessionTests
    {
        [Test]
        public void BeginExecution_EmptyActionsCompletesWithoutMutation()
        {
            BattleFixture battle = CreateBattle();

            CombatActionExecutionSession session =
                battle.Executor.BeginExecution(Array.Empty<CombatAction>());

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.NextAction, Is.Null);
            Assert.That(session.CompletedActionCount, Is.Zero);
            Assert.That(session.Result, Is.Not.Null);
            Assert.That(session.Result.CompletedActionCount, Is.Zero);
            Assert.That(session.Result.StoppedEarly, Is.False);
            Assert.That(session.TryExecuteNext(out _), Is.False);
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(10000L));
            Assert.That(battle.Party.CurrentHp, Is.EqualTo(1000L));
        }

        [Test]
        public void PeekAndExecuteNext_AppliesExactlyOneActionAtATime()
        {
            BattleFixture battle = CreateBattle();
            DamageAction damage = battle.Damage(1, 0.25d);
            var add = new AddResourceAction(
                2,
                ActionOrigin.System,
                battle.Boss,
                ResourceId,
                2);

            CombatActionExecutionSession session =
                battle.Executor.BeginExecution(
                    new CombatAction[] { damage, add });

            Assert.That(session.NextAction, Is.SameAs(damage));
            Assert.That(session.CompletedActionCount, Is.Zero);
            Assert.That(session.Result, Is.Null);
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(10000L));
            Assert.That(battle.Boss.Resources.GetAmount(ResourceId), Is.Zero);

            Assert.That(session.TryExecuteNext(out var first), Is.True);
            Assert.That(first.Action, Is.SameAs(damage));
            Assert.That(first.Result, Is.TypeOf<DamageActionResult>());
            Assert.That(first.WasTerminalAfterStep, Is.False);
            Assert.That(first.IsCompleted, Is.False);
            Assert.That(session.NextAction, Is.SameAs(add));
            Assert.That(session.CompletedActionCount, Is.EqualTo(1));
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(9750L));
            Assert.That(battle.Boss.Resources.GetAmount(ResourceId), Is.Zero);

            Assert.That(session.TryExecuteNext(out var second), Is.True);
            Assert.That(second.Action, Is.SameAs(add));
            Assert.That(second.IsCompleted, Is.True);
            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.NextAction, Is.Null);
            Assert.That(session.Result.CompletedActionCount, Is.EqualTo(2));
            Assert.That(battle.Boss.Resources.GetAmount(ResourceId),
                Is.EqualTo(2));
        }

        [Test]
        public void FullStepExecution_MatchesSynchronousMixedActionPipeline()
        {
            BattleFixture synchronous = CreateBattle();
            BattleFixture stepped = CreateBattle();
            PrepareMixedInitialState(synchronous);
            PrepareMixedInitialState(stepped);

            CombatActionExecutionResult expected = synchronous.Executor.Execute(
                new CombatActionQueue(CreateMixedActions(synchronous)));
            CombatActionExecutionSession session =
                stepped.Executor.BeginExecution(CreateMixedActions(stepped));
            var stepResults = new List<CombatActionExecutionStepResult>();
            while (session.TryExecuteNext(out var step))
            {
                stepResults.Add(step);
            }

            AssertExecutionParity(expected, session.Result);
            Assert.That(stepResults.Count,
                Is.EqualTo(expected.CompletedActionCount));
            Assert.That(stepped.Boss.CurrentHp,
                Is.EqualTo(synchronous.Boss.CurrentHp));
            Assert.That(stepped.Party.CurrentHp,
                Is.EqualTo(synchronous.Party.CurrentHp));
            Assert.That(stepped.Party.Shields.TotalShield,
                Is.EqualTo(synchronous.Party.Shields.TotalShield));
            Assert.That(stepped.Boss.Effects.Count,
                Is.EqualTo(synchronous.Boss.Effects.Count));
            Assert.That(stepped.Boss.Resources.GetAmount(ResourceId),
                Is.EqualTo(synchronous.Boss.Resources.GetAmount(ResourceId)));

            for (int index = 0; index < expected.ActionResults.Count; index++)
            {
                Assert.That(stepResults[index].Action.ActionId,
                    Is.EqualTo(expected.ActionResults[index].Action.ActionId));
                Assert.That(stepResults[index].Result.GetType(),
                    Is.EqualTo(expected.ActionResults[index].GetType()));
                Assert.That(stepResults[index].Result.ExecutionOrder,
                    Is.EqualTo(index + 1));
            }

            var expectedDamage =
                (DamageActionResult)expected.ActionResults[0];
            var actualDamage =
                (DamageActionResult)session.Result.ActionResults[0];
            Assert.That(actualDamage.DamageResult.FinalDamage,
                Is.EqualTo(expectedDamage.DamageResult.FinalDamage));
            Assert.That(actualDamage.Context.IsCritical,
                Is.EqualTo(expectedDamage.Context.IsCritical));
        }

        [Test]
        public void FullStepExecution_MatchesBossDamageShieldAbsorption()
        {
            BattleFixture synchronous = CreateBattle(bossAttack: 200d);
            BattleFixture stepped = CreateBattle(bossAttack: 200d);

            CombatActionExecutionResult expected = synchronous.Executor.Execute(
                new CombatActionQueue(CreateShieldThenBossDamage(synchronous)));
            CombatActionExecutionSession session =
                stepped.Executor.BeginExecution(
                    CreateShieldThenBossDamage(stepped));
            while (session.TryExecuteNext(out _))
            {
            }

            AssertExecutionParity(expected, session.Result);
            var expectedDamage =
                (BossDamageActionResult)expected.ActionResults[1];
            var actualDamage =
                (BossDamageActionResult)session.Result.ActionResults[1];
            Assert.That(actualDamage.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(expectedDamage.DamageResult.FinalDamageBeforeShield));
            Assert.That(actualDamage.ApplicationResult.ShieldAbsorbedDamage,
                Is.EqualTo(expectedDamage.ApplicationResult.ShieldAbsorbedDamage));
            Assert.That(actualDamage.ApplicationResult.HpDamage,
                Is.EqualTo(expectedDamage.ApplicationResult.HpDamage));
            Assert.That(stepped.Party.CurrentHp,
                Is.EqualTo(synchronous.Party.CurrentHp));
            Assert.That(stepped.Party.Shields.TotalShield,
                Is.EqualTo(synchronous.Party.Shields.TotalShield));
        }

        [Test]
        public void DerivedAndNestedActionsRemainAheadOfLaterRoots()
        {
            BattleFixture battle = CreateBattle();
            var firstRule = new DelegateCombatTriggerRule(context =>
                context.ActionId == 1
                    ? new CombatAction[]
                    {
                        new ApplyEffectAction(
                            10,
                            ActionOrigin.Additional,
                            battle.Boss,
                            Effect(10, "derived_effect"),
                            context.RootActionId,
                            context.ActionId)
                    }
                    : Array.Empty<CombatAction>());
            var nestedRule = new DelegateCombatTriggerRule(context =>
                context.ActionId == 10
                    ? new CombatAction[]
                    {
                        new AddResourceAction(
                            11,
                            ActionOrigin.Additional,
                            battle.Boss,
                            ResourceId,
                            1,
                            context.RootActionId,
                            context.ActionId)
                    }
                    : Array.Empty<CombatAction>());
            CombatActionExecutor executor = battle.CreateExecutor(
                firstRule,
                nestedRule);
            CombatActionExecutionSession session = executor.BeginExecution(
                new CombatAction[]
                {
                    battle.Damage(1, 0.10d, ActionOrigin.Match),
                    battle.Damage(2, 0.10d, ActionOrigin.Active)
                });
            var order = new List<long>();

            while (session.TryExecuteNext(out var step))
            {
                order.Add(step.Action.ActionId);
            }

            Assert.That(order, Is.EqualTo(new long[] { 1, 10, 11, 2 }));
            Assert.That(session.Result.ActionResults[1].Action.RootActionId,
                Is.EqualTo(1L));
            Assert.That(session.Result.ActionResults[2].Action.SourceActionId,
                Is.EqualTo(10L));
            Assert.That(battle.Boss.Effects.Count, Is.EqualTo(1));
            Assert.That(battle.Boss.Resources.GetAmount(ResourceId),
                Is.EqualTo(1));
        }

        [Test]
        public void TerminalStepClearsRemainderAndCannotMutateAgain()
        {
            BattleFixture battle = CreateBattle(bossHp: 100);
            CombatActionExecutionSession session =
                battle.Executor.BeginExecution(new CombatAction[]
                {
                    battle.Damage(1, 1d),
                    new AddResourceAction(
                        2,
                        ActionOrigin.System,
                        battle.Boss,
                        ResourceId,
                        3)
                });

            Assert.That(session.TryExecuteNext(out var terminal), Is.True);
            Assert.That(terminal.WasTerminalAfterStep, Is.True);
            Assert.That(terminal.IsCompleted, Is.True);
            Assert.That(session.Result.StoppedEarly, Is.True);
            Assert.That(session.Result.BossDefeated, Is.True);
            Assert.That(session.Result.ClearedRemainingActions, Is.True);
            Assert.That(session.Result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(session.NextAction, Is.Null);

            long hpAfterCompletion = battle.Boss.CurrentHp;
            Assert.That(session.TryExecuteNext(out var after), Is.False);
            Assert.That(after, Is.Null);
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(hpAfterCompletion));
            Assert.That(battle.Boss.Resources.GetAmount(ResourceId), Is.Zero);
        }

        [Test]
        public void BossDamageTerminalStepMatchesSynchronousIncapacitation()
        {
            BattleFixture synchronous = CreateBattle(
                partyHp: 100,
                bossAttack: 500d);
            BattleFixture stepped = CreateBattle(
                partyHp: 100,
                bossAttack: 500d);

            CombatActionExecutionResult expected = synchronous.Executor.Execute(
                new CombatActionQueue(
                    CreateTerminalBossDamageActions(synchronous)));
            CombatActionExecutionSession session =
                stepped.Executor.BeginExecution(
                    CreateTerminalBossDamageActions(stepped));

            Assert.That(session.TryExecuteNext(out var terminal), Is.True);

            AssertExecutionParity(expected, session.Result);
            Assert.That(terminal.Result, Is.TypeOf<BossDamageActionResult>());
            Assert.That(terminal.WasTerminalAfterStep, Is.True);
            Assert.That(session.Result.PartyIncapacitated, Is.True);
            Assert.That(session.Result.ClearedRemainingActions, Is.True);
            Assert.That(session.Result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(stepped.Party.CurrentHp,
                Is.EqualTo(synchronous.Party.CurrentHp));
            Assert.That(stepped.Party.Effects.Count, Is.Zero);
            Assert.That(session.TryExecuteNext(out _), Is.False);
        }

        [Test]
        public void BeginExecution_WhenBattleAlreadyEndedClearsWithoutStep()
        {
            BattleFixture battle = CreateBattle(bossHp: 100);
            battle.Executor.Execute(new CombatActionQueue(
                new CombatAction[] { battle.Damage(1, 1d) }));

            CombatActionExecutionSession session =
                battle.Executor.BeginExecution(new CombatAction[]
                {
                    new AddResourceAction(
                        2,
                        ActionOrigin.System,
                        battle.Boss,
                        ResourceId,
                        1)
                });

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.Result.StoppedEarly, Is.True);
            Assert.That(session.Result.ClearedRemainingActions, Is.True);
            Assert.That(session.Result.CompletedActionCount, Is.Zero);
            Assert.That(session.TryExecuteNext(out _), Is.False);
            Assert.That(battle.Boss.Resources.GetAmount(ResourceId), Is.Zero);
        }

        private const string ResourceId = "water";

        private static BattleFixture CreateBattle(
            long partyHp = 1000,
            long bossHp = 10000,
            double bossAttack = 100d)
        {
            return new BattleFixture(partyHp, bossHp, bossAttack);
        }

        private static void PrepareMixedInitialState(BattleFixture battle)
        {
            PartyDamageApplier.Apply(
                battle.Party,
                BossDamageCalculator.Calculate(new BossDamageContext(
                    200,
                    0d,
                    0d,
                    1d,
                    0d,
                    0d,
                    0d,
                    0d)));
            battle.Boss.Resources.Add(ResourceId, 2);
        }

        private static IReadOnlyList<CombatAction> CreateMixedActions(
            BattleFixture battle)
        {
            return new CombatAction[]
            {
                battle.Damage(1, 0.50d),
                new BossDamageAction(
                    2,
                    new BossDamageContextBuildRequest(
                        battle.Boss,
                        battle.Party,
                        0.50d,
                        AttackTag.None)),
                new HealAction(
                    3,
                    ActionOrigin.Active,
                    new HealingContextBuildRequest(
                        battle.Character,
                        battle.Party,
                        0.25d,
                        false,
                        0)),
                new ShieldAction(
                    4,
                    ActionOrigin.Active,
                    new ShieldGenerationContextBuildRequest(
                        battle.Character,
                        battle.Party,
                        0.20d,
                        false,
                        0),
                    new ShieldGrantRequest(
                        1,
                        battle.Character.CharacterId,
                        1,
                        2,
                        1)),
                new ApplyEffectAction(
                    5,
                    ActionOrigin.System,
                    battle.Boss,
                    Effect(50, "temporary_effect")),
                new RemoveEffectAction(
                    6,
                    ActionOrigin.System,
                    battle.Boss,
                    50),
                new AddResourceAction(
                    7,
                    ActionOrigin.System,
                    battle.Boss,
                    ResourceId,
                    3),
                new ConsumeResourceAction(
                    8,
                    ActionOrigin.System,
                    battle.Boss,
                    ResourceId,
                    battle.Character.CharacterId,
                    ResourceConsumptionMode.Amount,
                    4)
            };
        }

        private static IReadOnlyList<CombatAction>
            CreateShieldThenBossDamage(BattleFixture battle)
        {
            return new CombatAction[]
            {
                new ShieldAction(
                    1,
                    ActionOrigin.Active,
                    new ShieldGenerationContextBuildRequest(
                        battle.Character,
                        battle.Party,
                        0.30d,
                        false,
                        0),
                    new ShieldGrantRequest(
                        1,
                        battle.Character.CharacterId,
                        1,
                        2,
                        1)),
                new BossDamageAction(
                    2,
                    new BossDamageContextBuildRequest(
                        battle.Boss,
                        battle.Party,
                        1d,
                        AttackTag.None))
            };
        }

        private static IReadOnlyList<CombatAction>
            CreateTerminalBossDamageActions(BattleFixture battle)
        {
            return new CombatAction[]
            {
                new BossDamageAction(
                    1,
                    new BossDamageContextBuildRequest(
                        battle.Boss,
                        battle.Party,
                        1d,
                        AttackTag.None)),
                new ApplyEffectAction(
                    2,
                    ActionOrigin.System,
                    battle.Party,
                    Effect(20, "skipped_effect"))
            };
        }

        private static void AssertExecutionParity(
            CombatActionExecutionResult expected,
            CombatActionExecutionResult actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.CompletedActionCount,
                Is.EqualTo(expected.CompletedActionCount));
            Assert.That(actual.StoppedEarly,
                Is.EqualTo(expected.StoppedEarly));
            Assert.That(actual.BossDefeated,
                Is.EqualTo(expected.BossDefeated));
            Assert.That(actual.PartyIncapacitated,
                Is.EqualTo(expected.PartyIncapacitated));
            Assert.That(actual.ClearedRemainingActions,
                Is.EqualTo(expected.ClearedRemainingActions));
        }

        private static EffectInstance Effect(
            long runtimeId,
            string effectId)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "source",
                EffectCategory.Buff,
                EffectModifierType.HealingIncrease,
                0.10d,
                2,
                runtimeId);
        }

        private sealed class BattleFixture
        {
            public BattleFixture(
                long partyHp,
                long bossHp,
                double bossAttack)
            {
                Character = new CharacterBattleState(
                    "hero",
                    0,
                    ElementType.Fire,
                    partyHp,
                    1000d);
                Party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "boss",
                    ElementType.Fire,
                    bossHp,
                    bossAttack);
                Boss.Resources.Register(ResourceId, 10);
                Executor = CreateExecutor();
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionExecutor Executor { get; }

            public CombatActionExecutor CreateExecutor(
                params ICombatTriggerRule[] rules)
            {
                return new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(new SeededRandomSource(1)),
                    new CombatTriggerResolver(rules));
            }

            public DamageAction Damage(
                long actionId,
                double coefficient,
                ActionOrigin origin = ActionOrigin.Active)
            {
                return new DamageAction(
                    actionId,
                    origin,
                    new DamageContextBuildRequest(
                        Character,
                        Party,
                        Boss,
                        ElementType.Fire,
                        origin == ActionOrigin.Match
                            ? AttackType.Match
                            : AttackType.Active,
                        AttackTag.None,
                        coefficient,
                        false,
                        0,
                        false));
            }
        }
    }
}
