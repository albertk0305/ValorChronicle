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
            Assert.That(damage.AppliedDamage, Is.EqualTo(500L));
            Assert.That(damage.WasCritical, Is.False);
            Assert.That(damage.WasWeakness, Is.False);
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
        public void DamageResultExposesResolvedCriticalAndWeaknessWithoutRecalculation()
        {
            var character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Fire,
                1000,
                1000d);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "boss",
                ElementType.Grass,
                10000,
                500d);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            var action = new DamageAction(
                1,
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    character,
                    party,
                    boss,
                    ElementType.Fire,
                    AttackType.Match,
                    AttackTag.None,
                    1d,
                    false,
                    0,
                    true,
                    baseCriticalChance: 1d));

            var result = (DamageActionResult)executor.Execute(
                new CombatActionQueue(new CombatAction[] { action }))
                .ActionResults[0];

            Assert.That(result.WasCritical, Is.True);
            Assert.That(result.WasWeakness, Is.True);
            Assert.That(result.Context.IsCritical, Is.True);
            Assert.That(result.DamageResult.ElementAffinityMultiplier,
                Is.GreaterThan(1d));
            Assert.That(result.AppliedDamage,
                Is.EqualTo(result.ApplicationResult.AppliedDamage));
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
        public void Execute_DeferredCoefficientSeesShieldGrantedEarlierInQueue()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            int resolveCount = 0;
            var shield = new ShieldAction(
                10,
                ActionOrigin.Match,
                new ShieldGenerationContextBuildRequest(
                    character,
                    party,
                    0.50d,
                    false,
                    0),
                new ShieldGrantRequest(
                    100,
                    character.CharacterId,
                    1,
                    3,
                    100));
            var damage = new DamageAction(
                11,
                ActionOrigin.Match,
                DeferredDamageRequest(
                    character,
                    party,
                    boss,
                    request =>
                    {
                        resolveCount++;
                        return Match4Coefficient(request);
                    }));

            Assert.That(resolveCount, Is.Zero);
            CombatActionExecutionResult result = executor.Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    shield,
                    damage
                }));
            var shieldResult = (ShieldActionResult)result.ActionResults[0];
            var damageResult = (DamageActionResult)result.ActionResults[1];

            Assert.That(shieldResult.ApplicationResult.GrantedShieldAmount,
                Is.EqualTo(500L));
            Assert.That(damageResult.Context.SkillCoefficient,
                Is.EqualTo(1.50d).Within(0.000000001d));
            Assert.That(damageResult.DamageResult.FinalDamage,
                Is.EqualTo(1500L));
            Assert.That(resolveCount, Is.EqualTo(1));
            Assert.That(shield.ActionId, Is.EqualTo(10L));
            Assert.That(shield.RootActionId, Is.EqualTo(10L));
            Assert.That(shield.SourceActionId, Is.Null);
            Assert.That(damage.ActionId, Is.EqualTo(11L));
            Assert.That(damage.RootActionId, Is.EqualTo(11L));
            Assert.That(damage.SourceActionId, Is.Null);
        }

        [Test]
        public void Execute_DeferredCoefficientUsesCurrentShieldWhenGrantIsZero()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            party.Shields.Add(new ShieldInstance(
                90,
                "existing",
                400,
                1,
                2,
                90));
            var queue = new CombatActionQueue(new CombatAction[]
            {
                Shield(character, party, coefficient: 0d),
                new DamageAction(
                    2,
                    ActionOrigin.Match,
                    DeferredDamageRequest(
                        character,
                        party,
                        boss,
                        Match4Coefficient))
            });

            CombatActionExecutionResult result = executor.Execute(queue);
            var shieldResult = (ShieldActionResult)result.ActionResults[0];
            var damageResult = (DamageActionResult)result.ActionResults[1];

            Assert.That(shieldResult.ApplicationResult.GrantedShieldAmount,
                Is.Zero);
            Assert.That(shieldResult.ApplicationResult.CreatedShieldRuntimeId,
                Is.Null);
            Assert.That(damageResult.Context.SkillCoefficient,
                Is.EqualTo(1.44d).Within(0.000000001d));
            Assert.That(damageResult.DamageResult.FinalDamage,
                Is.EqualTo(1440L));
        }

        [Test]
        public void Execute_DeferredCoefficientUsesPostCapShieldTotal()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            party.Shields.Add(new ShieldInstance(
                90,
                "existing",
                900,
                1,
                2,
                90));
            var queue = new CombatActionQueue(new CombatAction[]
            {
                Shield(character, party, coefficient: 0.50d),
                new DamageAction(
                    2,
                    ActionOrigin.Match,
                    DeferredDamageRequest(
                        character,
                        party,
                        boss,
                        Match4Coefficient))
            });

            CombatActionExecutionResult result = executor.Execute(queue);
            var shieldResult = (ShieldActionResult)result.ActionResults[0];
            var damageResult = (DamageActionResult)result.ActionResults[1];

            Assert.That(shieldResult.GenerationResult.FinalShieldAmount,
                Is.EqualTo(500L));
            Assert.That(shieldResult.ApplicationResult.GrantedShieldAmount,
                Is.EqualTo(100L));
            Assert.That(party.Shields.TotalShield, Is.EqualTo(1000L));
            Assert.That(damageResult.Context.SkillCoefficient,
                Is.EqualTo(1.80d).Within(0.000000001d));
        }

        [Test]
        public void Execute_DeferredCoefficientUsesExistingAndNewShieldTotal()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            party.Shields.Add(new ShieldInstance(
                90,
                "existing",
                200,
                1,
                2,
                90));
            var queue = new CombatActionQueue(new CombatAction[]
            {
                Shield(character, party, coefficient: 0.30d),
                new DamageAction(
                    2,
                    ActionOrigin.Match,
                    DeferredDamageRequest(
                        character,
                        party,
                        boss,
                        Match4Coefficient))
            });

            CombatActionExecutionResult result = executor.Execute(queue);
            var damageResult = (DamageActionResult)result.ActionResults[1];

            Assert.That(party.Shields.TotalShield, Is.EqualTo(500L));
            Assert.That(damageResult.Context.SkillCoefficient,
                Is.EqualTo(1.50d).Within(0.000000001d));
        }

        [Test]
        public void Execute_DeferredCoefficientClampsChargeAboveOne()
        {
            var character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Fire,
                1000,
                1000d);
            var ally = new CharacterBattleState(
                "ally",
                1,
                ElementType.Water,
                1000,
                100d);
            var party = new PartyBattleState(new[] { character, ally });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                10000,
                500d);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            party.Shields.Add(new ShieldInstance(
                90,
                "existing",
                1200,
                1,
                2,
                90));
            var queue = new CombatActionQueue(new CombatAction[]
            {
                Shield(character, party, coefficient: 0.50d),
                new DamageAction(
                    2,
                    ActionOrigin.Match,
                    DeferredDamageRequest(
                        character,
                        party,
                        boss,
                        Match5Coefficient,
                        baseCoefficient: 2.20d,
                        attackTag: AttackTag.Match5Plus))
            });

            CombatActionExecutionResult result = executor.Execute(queue);
            var damageResult = (DamageActionResult)result.ActionResults[1];

            Assert.That(party.Shields.TotalShield, Is.EqualTo(1700L));
            Assert.That(damageResult.Context.SkillCoefficient,
                Is.EqualTo(4.40d).Within(0.000000001d));
            Assert.That(damageResult.DamageResult.FinalDamage,
                Is.EqualTo(4400L));
        }

        [Test]
        public void Execute_DeferredCoefficientCanUseFinalAttackerMaximumHp()
        {
            var character = new CharacterBattleState(
                "adjusted_hero",
                0,
                ElementType.Fire,
                1650,
                1000d);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                10000,
                500d);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            var queue = new CombatActionQueue(new CombatAction[]
            {
                Shield(character, party, coefficient: 0.50d),
                new DamageAction(
                    2,
                    ActionOrigin.Match,
                    DeferredDamageRequest(
                        character,
                        party,
                        boss,
                        Match4Coefficient))
            });

            CombatActionExecutionResult result = executor.Execute(queue);
            var damageResult = (DamageActionResult)result.ActionResults[1];

            Assert.That(character.MaxHp, Is.EqualTo(1650L));
            Assert.That(party.Shields.TotalShield, Is.EqualTo(825L));
            Assert.That(damageResult.Context.SkillCoefficient,
                Is.EqualTo(1.50d).Within(0.000000001d));
        }

        [Test]
        public void Execute_TerminalBossSkipsDeferredCoefficientEvaluation()
        {
            CreateBattle(
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            int resolveCount = 0;
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new DamageAction(
                    1,
                    ActionOrigin.Active,
                    DamageRequest(character, party, boss, 10d)),
                new DamageAction(
                    2,
                    ActionOrigin.Active,
                    new DamageContextBuildRequest(
                        character,
                        party,
                        boss,
                        ElementType.Fire,
                        AttackType.Active,
                        AttackTag.None,
                        1d,
                        false,
                        0,
                        false,
                        skillCoefficientResolver: request =>
                        {
                            resolveCount++;
                            return 1d;
                        }))
            });

            CombatActionExecutionResult result = executor.Execute(queue);

            Assert.That(boss.IsDefeated, Is.True);
            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.ClearedRemainingActions, Is.True);
            Assert.That(resolveCount, Is.Zero);
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

        private static DamageContextBuildRequest DeferredDamageRequest(
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss,
            DamageSkillCoefficientResolver resolver,
            double baseCoefficient = 1.20d,
            AttackTag attackTag = AttackTag.Match4)
        {
            return new DamageContextBuildRequest(
                character,
                party,
                boss,
                ElementType.Fire,
                AttackType.Match,
                attackTag,
                baseCoefficient,
                false,
                0,
                false,
                skillCoefficientResolver: resolver);
        }

        private static ShieldAction Shield(
            CharacterBattleState character,
            PartyBattleState party,
            double coefficient)
        {
            return new ShieldAction(
                1,
                ActionOrigin.Match,
                new ShieldGenerationContextBuildRequest(
                    character,
                    party,
                    coefficient,
                    false,
                    0),
                new ShieldGrantRequest(
                    100,
                    character.CharacterId,
                    1,
                    3,
                    100));
        }

        private static double Match4Coefficient(
            DamageContextBuildRequest request)
        {
            double charge = Math.Min(
                (double)request.Party.Shields.TotalShield
                    / request.Attacker.MaxHp,
                1d);
            return request.SkillCoefficient * (1d + (charge * 0.50d));
        }

        private static double Match5Coefficient(
            DamageContextBuildRequest request)
        {
            double charge = Math.Min(
                (double)request.Party.Shields.TotalShield
                    / request.Attacker.MaxHp,
                1d);
            return request.SkillCoefficient * (1d + charge);
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
