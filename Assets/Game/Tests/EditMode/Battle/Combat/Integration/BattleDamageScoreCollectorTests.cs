using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Results;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class BattleDamageScoreCollectorTests
    {
        [Test]
        public void Collect_AddsAllAppliedDamageResults()
        {
            TestExecution execution = ExecuteDamage(
                bossHp: 1000L,
                1d,
                2d,
                3d);
            var collector = Collector();

            Assert.That(collector.Collect(execution.Result), Is.EqualTo(600L));
            Assert.That(collector.DamageScore, Is.EqualTo(600L));
        }

        [Test]
        public void Collect_IncludesAdditionalAndChaseDamageResults()
        {
            BossBattleState boss = Boss(1000L);
            CharacterBattleState character = Character();
            var party = new PartyBattleState(new[] { character });
            TestExecution execution = Execute(
                boss,
                new CombatAction[]
                {
                    Damage(
                        1L,
                        1d,
                        character,
                        party,
                        boss),
                    FollowUpDamage(
                        2L,
                        ActionOrigin.Additional,
                        AttackType.Additional,
                        1L,
                        1L,
                        character,
                        party,
                        boss),
                    FollowUpDamage(
                        3L,
                        ActionOrigin.Chase,
                        AttackType.Chase,
                        1L,
                        2L,
                        character,
                        party,
                        boss)
                },
                party);
            var collector = Collector();

            Assert.That(collector.Collect(execution.Result), Is.EqualTo(300L));
            Assert.That(collector.DamageScore, Is.EqualTo(300L));
        }

        [Test]
        public void Collect_UsesAppliedDamageInsteadOfRequestedOverkill()
        {
            TestExecution execution = ExecuteDamage(bossHp: 50L, 1d);
            var damage = (DamageActionResult)
                execution.Result.ActionResults[0];
            var collector = Collector();

            Assert.That(damage.DamageResult.FinalDamage, Is.EqualTo(100L));
            Assert.That(damage.AppliedDamage, Is.EqualTo(50L));
            Assert.That(collector.Collect(execution.Result), Is.EqualTo(50L));
            Assert.That(collector.DamageScore, Is.EqualTo(50L));
        }

        [Test]
        public void Collect_IgnoresNonDamageActionResults()
        {
            BossBattleState boss = Boss(1000L);
            boss.Resources.Register("test_resource", 10);
            TestExecution execution = Execute(
                boss,
                new CombatAction[]
                {
                    new AddResourceAction(
                        1L,
                        ActionOrigin.System,
                        boss,
                        "test_resource",
                        1)
                });
            var collector = Collector();

            Assert.That(collector.Collect(execution.Result), Is.Zero);
            Assert.That(collector.DamageScore, Is.Zero);
        }

        [Test]
        public void Collect_ExcludesActionsClearedAfterLethalDamage()
        {
            TestExecution execution = ExecuteDamage(
                bossHp: 100L,
                1d,
                3d);
            var collector = Collector();

            Assert.That(execution.Result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(execution.Result.ClearedRemainingActions, Is.True);
            Assert.That(collector.Collect(execution.Result), Is.EqualTo(100L));
        }

        [Test]
        public void Collect_SameExecutionIsIdempotent()
        {
            TestExecution execution = ExecuteDamage(bossHp: 1000L, 1d);
            var collector = Collector();

            Assert.That(collector.Collect(execution.Result), Is.EqualTo(100L));
            Assert.That(collector.Collect(execution.Result), Is.Zero);
            Assert.That(collector.DamageScore, Is.EqualTo(100L));
        }

        private static BattleDamageScoreCollector Collector()
        {
            return new BattleDamageScoreCollector(
                new BattleDamageScoreAccumulator());
        }

        private static TestExecution ExecuteDamage(
            long bossHp,
            params double[] coefficients)
        {
            BossBattleState boss = Boss(bossHp);
            CharacterBattleState character = Character();
            var party = new PartyBattleState(new[] { character });
            var actions = new CombatAction[coefficients.Length];
            for (int index = 0; index < coefficients.Length; index++)
            {
                actions[index] = Damage(
                    index + 1L,
                    coefficients[index],
                    character,
                    party,
                    boss);
            }

            return Execute(boss, actions, party);
        }

        private static TestExecution Execute(
            BossBattleState boss,
            CombatAction[] actions,
            PartyBattleState party = null)
        {
            party = party ?? new PartyBattleState(new[] { Character() });
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            CombatActionExecutionResult result = executor.Execute(
                new CombatActionQueue(actions));
            return new TestExecution(result);
        }

        private static DamageAction Damage(
            long actionId,
            double coefficient,
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss)
        {
            return new DamageAction(
                actionId,
                ActionOrigin.System,
                new DamageContextBuildRequest(
                    character,
                    party,
                    boss,
                    ElementType.Fire,
                    AttackType.Match,
                    AttackTag.None,
                    coefficient,
                    false,
                    0,
                    false));
        }

        private static DamageAction FollowUpDamage(
            long actionId,
            ActionOrigin origin,
            AttackType attackType,
            long rootActionId,
            long sourceActionId,
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss)
        {
            return new DamageAction(
                actionId,
                origin,
                new DamageContextBuildRequest(
                    character,
                    party,
                    boss,
                    ElementType.Fire,
                    attackType,
                    AttackTag.None,
                    1d,
                    false,
                    0,
                    false),
                rootActionId,
                sourceActionId);
        }

        private static CharacterBattleState Character()
        {
            return new CharacterBattleState(
                "character",
                0,
                ElementType.Fire,
                1000L,
                100d);
        }

        private static BossBattleState Boss(long hp)
        {
            return new BossBattleState(
                "boss",
                ElementType.Fire,
                hp,
                0d);
        }

        private sealed class TestExecution
        {
            public TestExecution(CombatActionExecutionResult result)
            {
                Result = result;
            }

            public CombatActionExecutionResult Result { get; }
        }
    }
}
