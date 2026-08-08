using System;
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
    public sealed class CombatActionExecutorPipelineTests
    {
        [Test]
        public void Execute_RunsDamageHealAndShieldPipelinesInOrder()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            PartyDamageApplier.Apply(party, BossDamage(400));
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new DamageAction(
                    1,
                    ActionOrigin.Active,
                    DamageRequest(character, party, boss, 0.50d)),
                new HealAction(
                    2,
                    ActionOrigin.Active,
                    new HealingContextBuildRequest(
                        character,
                        party,
                        0.25d,
                        false,
                        0)),
                new ShieldAction(
                    3,
                    ActionOrigin.Active,
                    new ShieldGenerationContextBuildRequest(
                        character,
                        party,
                        0.20d,
                        false,
                        0),
                    new ShieldGrantRequest(
                        10,
                        character.CharacterId,
                        1,
                        2,
                        10))
            });

            CombatActionExecutionResult result = executor.Execute(queue);

            Assert.That(result.CompletedActionCount, Is.EqualTo(3));
            var damage = (DamageActionResult)result.ActionResults[0];
            var healing = (HealActionResult)result.ActionResults[1];
            var shield = (ShieldActionResult)result.ActionResults[2];
            Assert.That(damage.DamageResult.FinalDamage, Is.EqualTo(500L));
            Assert.That(damage.ApplicationResult.AppliedDamage,
                Is.EqualTo(500L));
            Assert.That(healing.FinalHealing, Is.EqualTo(250L));
            Assert.That(healing.AppliedHealing, Is.EqualTo(250L));
            Assert.That(healing.OverhealAmount, Is.Zero);
            Assert.That(shield.GenerationResult.FinalShieldAmount,
                Is.EqualTo(200L));
            Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                Is.EqualTo(200L));
            Assert.That(party.Shields.TotalShield, Is.EqualTo(200L));
            Assert.That(result.ActionResults[0].ExecutionOrder, Is.EqualTo(1));
            Assert.That(result.ActionResults[2].ExecutionOrder, Is.EqualTo(3));
        }

        [Test]
        public void Execute_ApplyEffectSupportsCharacterPartyAndBossTargets()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new ApplyEffectAction(
                    1,
                    ActionOrigin.System,
                    character,
                    Effect(1, "character_effect",
                        EffectModifierType.HealingIncrease, 0.10d)),
                new ApplyEffectAction(
                    2,
                    ActionOrigin.System,
                    party,
                    Effect(2, "party_effect",
                        EffectModifierType.ShieldAmountIncrease, 0.20d)),
                new ApplyEffectAction(
                    3,
                    ActionOrigin.System,
                    boss,
                    Effect(3, "boss_effect",
                        EffectModifierType.TargetTakenDamageIncrease, 0.30d))
            });

            CombatActionExecutionResult result = executor.Execute(queue);

            Assert.That(result.CompletedActionCount, Is.EqualTo(3));
            Assert.That(character.Effects.Count, Is.EqualTo(1));
            Assert.That(party.Effects.Count, Is.EqualTo(1));
            Assert.That(boss.Effects.Count, Is.EqualTo(1));
            Assert.That(result.ActionResults,
                Has.All.TypeOf<ApplyEffectActionResult>());
        }

        [Test]
        public void Execute_DamageContextSeesEffectAppliedEarlierInQueue()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            DamageAction damageAction = new DamageAction(
                2,
                ActionOrigin.Active,
                DamageRequest(character, party, boss, 1d));
            Assert.That(boss.Effects.Count, Is.Zero);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new ApplyEffectAction(
                    1,
                    ActionOrigin.System,
                    boss,
                    Effect(
                        1,
                        "taken_damage_up",
                        EffectModifierType.TargetTakenDamageIncrease,
                        0.20d)),
                damageAction
            });

            CombatActionExecutionResult result = executor.Execute(queue);
            var damage = (DamageActionResult)result.ActionResults[1];

            Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.20d));
            Assert.That(damage.DamageResult.FinalDamage, Is.EqualTo(1200L));
        }

        [Test]
        public void Execute_AddAndConsumeResourcesPreservesFifoAndOneRecord()
        {
            CreateBattle(
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
                    DamageRequest(character, party, boss, 0.10d)),
                new AddResourceAction(
                    2,
                    ActionOrigin.System,
                    boss,
                    "water",
                    2),
                new ConsumeResourceAction(
                    3,
                    ActionOrigin.System,
                    boss,
                    "water",
                    character.CharacterId,
                    ResourceConsumptionMode.All)
            });

            CombatActionExecutionResult result = executor.Execute(queue);
            var add = (AddResourceActionResult)result.ActionResults[1];
            var consume =
                (ConsumeResourceActionResult)result.ActionResults[2];

            Assert.That(result.ActionResults[0], Is.TypeOf<DamageActionResult>());
            Assert.That(add.AddResult.AmountBefore, Is.EqualTo(3));
            Assert.That(add.AddResult.AmountAfter, Is.EqualTo(5));
            Assert.That(consume.ConsumeResult.AmountBefore, Is.EqualTo(5));
            Assert.That(consume.ConsumeResult.AmountAfter, Is.Zero);
            Assert.That(consume.ConsumptionRecord, Is.Not.Null);
            Assert.That(consume.ConsumptionRecord.ConsumedAmount,
                Is.EqualTo(5));
            Assert.That(boss.Resources.GetAmount("water"), Is.Zero);
        }

        [Test]
        public void Execute_AmountConsumptionCanProduceNoRecord()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            boss.Resources.Register("water", 10);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new ConsumeResourceAction(
                    1,
                    ActionOrigin.System,
                    boss,
                    "water",
                    character.CharacterId,
                    ResourceConsumptionMode.Amount,
                    2)
            });

            CombatActionExecutionResult result = executor.Execute(queue);
            var consume =
                (ConsumeResourceActionResult)result.ActionResults[0];

            Assert.That(consume.ConsumeResult.ConsumedAmount, Is.Zero);
            Assert.That(consume.ConsumptionRecord, Is.Null);
        }

        [Test]
        public void Execute_AmountConsumptionCreatesOneAggregateRecord()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            boss.Resources.Register("water", 10);
            boss.Resources.Add("water", 3);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new ConsumeResourceAction(
                    1,
                    ActionOrigin.System,
                    boss,
                    "water",
                    character.CharacterId,
                    ResourceConsumptionMode.Amount,
                    2)
            });

            CombatActionExecutionResult result = executor.Execute(queue);
            var consume =
                (ConsumeResourceActionResult)result.ActionResults[0];

            Assert.That(consume.ConsumeResult.AmountBefore, Is.EqualTo(3));
            Assert.That(consume.ConsumeResult.AmountAfter, Is.EqualTo(1));
            Assert.That(consume.ConsumptionRecord, Is.Not.Null);
            Assert.That(consume.ConsumptionRecord.ConsumedAmount,
                Is.EqualTo(2));
        }

        [Test]
        public void Execute_RejectsActionFromAnotherBattleBeforeMutation()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            BossBattleState otherBoss = new BossBattleState(
                "other",
                ElementType.Fire,
                1000,
                100d);
            otherBoss.Resources.Register("water", 10);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new AddResourceAction(
                    1,
                    ActionOrigin.System,
                    otherBoss,
                    "water",
                    1)
            });

            Assert.Throws<InvalidOperationException>(() =>
                executor.Execute(queue));
            Assert.That(otherBoss.Resources.GetAmount("water"), Is.Zero);
        }

        private static void CreateBattle(
            out CharacterBattleState character,
            out PartyBattleState party,
            out BossBattleState boss,
            out CombatActionExecutor executor)
        {
            character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Fire,
                1000,
                1000d);
            party = new PartyBattleState(new[] { character });
            boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                10000,
                500d);
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

        private static EffectInstance Effect(
            long runtimeId,
            string effectId,
            EffectModifierType modifierType,
            double magnitude)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "source",
                EffectCategory.Buff,
                modifierType,
                magnitude,
                2,
                runtimeId);
        }

        private static BossDamageResult BossDamage(long damage)
        {
            return BossDamageCalculator.Calculate(new BossDamageContext(
                damage,
                0d,
                0d,
                1d,
                0d,
                0d,
                0d,
                0d));
        }
    }
}
