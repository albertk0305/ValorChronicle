using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class CombatActionExecutorTerminationTests
    {
        [Test]
        public void Execute_BossDefeatCompletesCurrentActionAndClearsRemainder()
        {
            CreateBattle(
                1000,
                100,
                500d,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            boss.Resources.Register("water", 10);
            boss.Resources.Add("water", 3);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new DamageAction(
                    1,
                    ActionOrigin.Active,
                    DamageRequest(character, party, boss, 1d)),
                new ApplyEffectAction(
                    2,
                    ActionOrigin.System,
                    boss,
                    Effect(1)),
                new ConsumeResourceAction(
                    3,
                    ActionOrigin.System,
                    boss,
                    "water",
                    character.CharacterId,
                    ResourceConsumptionMode.All)
            });

            CombatActionExecutionResult result = executor.Execute(queue);

            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.BossDefeated, Is.True);
            Assert.That(result.PartyIncapacitated, Is.False);
            Assert.That(result.ClearedRemainingActions, Is.True);
            Assert.That(queue.Count, Is.Zero);
            Assert.That(boss.Effects.Count, Is.Zero);
            Assert.That(boss.Resources.GetAmount("water"), Is.EqualTo(3));
            Assert.That(
                ((DamageActionResult)result.ActionResults[0]).BecameDefeated,
                Is.True);
        }

        [Test]
        public void Execute_PartyIncapacitationClearsFollowingActions()
        {
            CreateBattle(
                100,
                10000,
                1000d,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new BossDamageAction(
                    1,
                    new BossDamageContextBuildRequest(
                        boss,
                        party,
                        1d,
                        AttackTag.None)),
                new ApplyEffectAction(
                    2,
                    ActionOrigin.System,
                    party,
                    Effect(1))
            });

            CombatActionExecutionResult result = executor.Execute(queue);

            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.BossDefeated, Is.False);
            Assert.That(result.PartyIncapacitated, Is.True);
            Assert.That(result.ClearedRemainingActions, Is.True);
            Assert.That(queue.Count, Is.Zero);
            Assert.That(party.Effects.Count, Is.Zero);
            Assert.That(
                ((BossDamageActionResult)result.ActionResults[0])
                    .BecameIncapacitated,
                Is.True);
        }

        [Test]
        public void Execute_WhenBattleAlreadyEndedRunsNothingAndClearsQueue()
        {
            CreateBattle(
                1000,
                100,
                500d,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            executor.Execute(new CombatActionQueue(new CombatAction[]
            {
                new DamageAction(
                    1,
                    ActionOrigin.Active,
                    DamageRequest(character, party, boss, 1d))
            }));
            boss.Resources.Register("water", 10);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new AddResourceAction(
                    2,
                    ActionOrigin.System,
                    boss,
                    "water",
                    1)
            });

            CombatActionExecutionResult result = executor.Execute(queue);

            Assert.That(result.CompletedActionCount, Is.Zero);
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.BossDefeated, Is.True);
            Assert.That(result.ClearedRemainingActions, Is.True);
            Assert.That(queue.Count, Is.Zero);
            Assert.That(boss.Resources.GetAmount("water"), Is.Zero);
        }

        [Test]
        public void Execute_RejectsNullQueue()
        {
            CreateBattle(
                1000,
                1000,
                500d,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);

            Assert.Throws<ArgumentNullException>(() => executor.Execute(null));
        }

        private static void CreateBattle(
            long partyHp,
            long bossHp,
            double bossAttack,
            out CharacterBattleState character,
            out PartyBattleState party,
            out BossBattleState boss,
            out CombatActionExecutor executor)
        {
            character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Fire,
                partyHp,
                1000d);
            party = new PartyBattleState(new[] { character });
            boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                bossHp,
                bossAttack);
            executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
        }

        private static DamageContextBuildRequest DamageRequest(
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss,
            double coefficient)
        {
            return new DamageContextBuildRequest(
                character,
                party,
                boss,
                ElementType.Fire,
                AttackType.Active,
                AttackTag.None,
                coefficient,
                false,
                0,
                false);
        }

        private static EffectInstance Effect(long runtimeId)
        {
            return new EffectInstance(
                runtimeId,
                $"effect_{runtimeId}",
                "source",
                EffectCategory.Buff,
                EffectModifierType.HealingIncrease,
                0.10d,
                2,
                runtimeId);
        }
    }
}
