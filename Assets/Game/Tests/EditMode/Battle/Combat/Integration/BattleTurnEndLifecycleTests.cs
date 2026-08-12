using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class BattleTurnEndLifecycleTests
    {
        [Test]
        public void ProcessorTicksCharacterPartyBossAndShieldForTwoTurns()
        {
            CharacterBattleState character = Character("hero", 2);
            PartyBattleState party = new PartyBattleState(
                new[] { character });
            BossBattleState boss = Boss();
            EffectInstance characterEffect = Effect(
                1,
                "character_attack",
                EffectModifierType.AttackIncrease,
                2);
            EffectInstance partyEffect = Effect(
                2,
                "party_damage",
                EffectModifierType.DealtDamageIncrease,
                2);
            EffectInstance bossEffect = Effect(
                3,
                "boss_taken",
                EffectModifierType.TargetTakenDamageIncrease,
                2);
            var shield = new ShieldInstance(1, "hero", 50, 1, 2, 1);
            character.Effects.ApplyEffect(characterEffect);
            party.Effects.ApplyEffect(partyEffect);
            boss.Effects.ApplyEffect(bossEffect);
            party.Shields.Add(shield);
            var processor = new BattleTurnEndProcessor(party, boss);

            processor.ProcessTurnEnd();

            Assert.That(characterEffect.RemainingTurns, Is.EqualTo(1));
            Assert.That(partyEffect.RemainingTurns, Is.EqualTo(1));
            Assert.That(bossEffect.RemainingTurns, Is.EqualTo(1));
            Assert.That(shield.RemainingTurns, Is.EqualTo(1));
            AllyDamageModifierSnapshot modifiers =
                CombatModifierCollector.CollectAllyDamage(
                    character,
                    party,
                    boss,
                    ElementType.Fire);
            Assert.That(modifiers.AttackIncreaseRateSum, Is.EqualTo(0.50d));
            Assert.That(modifiers.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.50d));
            Assert.That(modifiers.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.50d));

            processor.ProcessTurnEnd();

            Assert.That(character.Effects.Count, Is.Zero);
            Assert.That(party.Effects.Count, Is.Zero);
            Assert.That(boss.Effects.Count, Is.Zero);
            Assert.That(party.Shields.ActiveShields, Is.Empty);
            Assert.That(processor.ProcessedTurnCount, Is.EqualTo(2));
            modifiers = CombatModifierCollector.CollectAllyDamage(
                character,
                party,
                boss,
                ElementType.Fire);
            Assert.That(modifiers.AttackIncreaseRateSum, Is.Zero);
            Assert.That(modifiers.DealtDamageIncreaseRateSum, Is.Zero);
            Assert.That(modifiers.TargetTakenDamageIncreaseRateSum, Is.Zero);
        }

        [Test]
        public void OneTurnShieldAbsorbsBossDamageBeforeExpiring()
        {
            LifecycleBattle battle = CreateBattle(
                EmptyMatchProvider(),
                BossDamageProvider(0.30d));
            var shield = new ShieldInstance(1, "hero", 50, 1, 1, 1);
            battle.Party.Shields.Add(shield);
            EnterBossActing(battle.Coordinator);

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            var result = (BossDamageActionResult)
                battle.Bridge.LastBossExecutionResult.ActionResults[0];
            Assert.That(result.ApplicationResult.ShieldAbsorbedDamage,
                Is.EqualTo(30));
            Assert.That(result.ApplicationResult.HpDamage, Is.Zero);
            Assert.That(battle.Party.CurrentHp,
                Is.EqualTo(battle.Party.MaxHp));
            Assert.That(battle.Party.Shields.ActiveShields, Is.Empty);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
        }

        [Test]
        public void TwoTurnShieldAbsorbsBothBossActionsThenExpires()
        {
            LifecycleBattle battle = CreateBattle(
                EmptyMatchProvider(),
                BossDamageProvider(0.10d),
                2);
            var shield = new ShieldInstance(1, "hero", 25, 1, 2, 1);
            battle.Party.Shields.Add(shield);
            battle.Coordinator.StartBattle();

            EnterBossActingFromPlayerInput(battle.Coordinator);
            battle.Bridge.ResolveBossAction(1);
            Assert.That(battle.Party.Shields.TotalShield, Is.EqualTo(15));
            Assert.That(shield.RemainingTurns, Is.EqualTo(1));

            EnterBossActingFromPlayerInput(battle.Coordinator);
            battle.Bridge.ResolveBossAction(2);
            Assert.That(battle.Party.CurrentHp,
                Is.EqualTo(battle.Party.MaxHp));
            Assert.That(battle.Party.Shields.ActiveShields, Is.Empty);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(2));
        }

        [Test]
        public void EffectCreatedDuringMatchAppliesImmediatelyAndTicksAfterBoss()
        {
            EffectInstance appliedEffect = null;
            var matchProvider = new DelegateMatchEventActionProvider(context =>
            {
                appliedEffect = Effect(
                    1,
                    "same_turn_attack",
                    EffectModifierType.AttackIncrease,
                    2);
                return new CombatAction[]
                {
                    new ApplyEffectAction(
                        context.ActionIds.Next(),
                        ActionOrigin.System,
                        context.Character,
                        appliedEffect),
                    CombatIntegrationTestSupport.MatchDamage(context, 1d)
                };
            });
            LifecycleBattle battle = CreateBattle(
                matchProvider,
                EmptyBossProvider(),
                2);
            battle.Coordinator.StartBattle();
            EnterMatchResolving(battle.Coordinator, SingleMatchCascade());
            battle.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.True);

            var damage = (DamageActionResult)
                battle.Bridge.LastMatchExecutionResult.ActionResults[1];
            Assert.That(damage.DamageResult.FinalAttack, Is.EqualTo(150d));
            Assert.That(appliedEffect.RemainingTurns, Is.EqualTo(2));
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));

            battle.Bridge.ResolveBossAction(1);

            Assert.That(appliedEffect.RemainingTurns, Is.EqualTo(1));
            Assert.That(battle.Character.Effects.Count, Is.EqualTo(1));
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
        }

        [Test]
        public void ZeroPlayerAndBossActionsStillProcessTurnEnd()
        {
            int matchProviderCalls = 0;
            var matchProvider = new DelegateMatchEventActionProvider(context =>
            {
                matchProviderCalls++;
                return Array.Empty<CombatAction>();
            });
            LifecycleBattle battle = CreateBattle(
                matchProvider,
                EmptyBossProvider(),
                2,
                ElementType.Water);
            EffectInstance effect = Effect(
                1,
                "party_effect",
                EffectModifierType.PartyDamageReduction,
                2);
            battle.Party.Effects.ApplyEffect(effect);
            battle.Coordinator.StartBattle();
            EnterMatchResolving(battle.Coordinator, SingleMatchCascade());
            battle.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.True);
            Assert.That(matchProviderCalls, Is.Zero);
            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(effect.RemainingTurns, Is.EqualTo(1));
            Assert.That(battle.Bridge.LastMatchExecutionResult, Is.Null);
            Assert.That(battle.Bridge.LastBossExecutionResult, Is.Null);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
        }

        [Test]
        public void VictoryDoesNotProcessTurnEndAndRejectsStaleExecution()
        {
            LifecycleBattle battle = CreateBattle(
                MatchDamageProvider(10d),
                EmptyBossProvider(),
                25,
                ElementType.Fire,
                50);
            EffectInstance effect = Effect(
                1,
                "victory_effect",
                EffectModifierType.AttackIncrease,
                2);
            battle.Character.Effects.ApplyEffect(effect);
            battle.Coordinator.StartBattle();
            EnterMatchResolving(battle.Coordinator, SingleMatchCascade());
            battle.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(effect.RemainingTurns, Is.EqualTo(2));
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.False);
            Assert.That(battle.Bridge.ResolveBossAction(1), Is.False);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
        }

        [Test]
        public void DefeatAndAbortDoNotProcessTurnEnd()
        {
            LifecycleBattle defeatBattle = CreateBattle(
                EmptyMatchProvider(),
                BossDamageProvider(10d),
                25,
                ElementType.Fire,
                10000,
                50);
            EffectInstance defeatEffect = Effect(
                1,
                "defeat_effect",
                EffectModifierType.PartyDamageReduction,
                2);
            defeatBattle.Party.Effects.ApplyEffect(defeatEffect);
            defeatBattle.Coordinator.StartBattle();
            EnterBossActingFromPlayerInput(defeatBattle.Coordinator);

            Assert.That(defeatBattle.Bridge.ResolveBossAction(1), Is.True);
            Assert.That(defeatBattle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(defeatEffect.RemainingTurns, Is.EqualTo(2));
            Assert.That(defeatBattle.Bridge.ProcessedTurnEndCount, Is.Zero);

            LifecycleBattle abortBattle = CreateBattle(
                EmptyMatchProvider(),
                EmptyBossProvider());
            EffectInstance abortEffect = Effect(
                2,
                "abort_effect",
                EffectModifierType.AttackIncrease,
                2);
            abortBattle.Character.Effects.ApplyEffect(abortEffect);
            abortBattle.Coordinator.StartBattle();
            EnterBossActingFromPlayerInput(abortBattle.Coordinator);
            Assert.That(abortBattle.Coordinator.AbortBattle(), Is.True);

            Assert.That(abortBattle.Bridge.ResolveBossAction(1), Is.False);
            Assert.That(abortEffect.RemainingTurns, Is.EqualTo(2));
            Assert.That(abortBattle.Bridge.ProcessedTurnEndCount, Is.Zero);
        }

        [Test]
        public void TwentyFifthTurnProcessesBeforeTurnLimitResult()
        {
            LifecycleBattle battle = CreateBattle(
                EmptyMatchProvider(),
                EmptyBossProvider(),
                25);
            EffectInstance effect = Effect(
                1,
                "long_effect",
                EffectModifierType.AttackIncrease,
                25);
            battle.Character.Effects.ApplyEffect(effect);
            battle.Coordinator.StartBattle();

            for (int turn = 1; turn <= 25; turn++)
            {
                EnterBossActingFromPlayerInput(battle.Coordinator);
                Assert.That(
                    battle.Bridge.ResolveBossAction(turn),
                    Is.True);
            }

            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(25));
            Assert.That(battle.Character.Effects.Count, Is.Zero);
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.Result));
        }

        [Test]
        public void SeparateBattlesDoNotShareRuntimeStateOrIds()
        {
            LifecycleBattle battleA = CreateBattle(
                MatchDamageProvider(1d),
                BossDamageProvider(1d),
                2);
            battleA.Boss.Resources.Register("resource", 10);
            battleA.Boss.Resources.Add("resource", 3);
            battleA.Boss.Marks.Register("mark", 5);
            battleA.Boss.Marks.Add("mark", 2);
            battleA.Boss.Effects.ApplyEffect(Effect(
                1,
                "boss_effect",
                EffectModifierType.TargetTakenDamageIncrease,
                5));
            battleA.Party.Shields.Add(
                new ShieldInstance(1, "hero", 10, 1, 5, 1));
            battleA.Coordinator.StartBattle();
            EnterMatchResolving(battleA.Coordinator, SingleMatchCascade());
            battleA.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);
            battleA.Bridge.ResolveMatchEvent(execution);
            battleA.Bridge.ResolveBossAction(1);
            Assert.That(battleA.ActionIds.LastIssuedId, Is.GreaterThan(1));

            LifecycleBattle battleB = CreateBattle(
                EmptyMatchProvider(),
                EmptyBossProvider());

            Assert.That(battleB.Boss.CurrentHp, Is.EqualTo(battleB.Boss.MaxHp));
            Assert.That(battleB.Party.CurrentHp,
                Is.EqualTo(battleB.Party.MaxHp));
            Assert.That(battleB.Boss.Resources.Count, Is.Zero);
            Assert.That(battleB.Boss.Marks.Count, Is.Zero);
            Assert.That(battleB.Boss.Effects.Count, Is.Zero);
            Assert.That(battleB.Party.Effects.Count, Is.Zero);
            Assert.That(battleB.Character.Effects.Count, Is.Zero);
            Assert.That(battleB.Party.Shields.ActiveShields, Is.Empty);
            Assert.That(battleB.ActionIds.Next(), Is.EqualTo(1));
            Assert.That(battleB.Coordinator.HasMatchEventInFlight, Is.False);
            Assert.That(battleB.Bridge.LastMatchExecutionResult, Is.Null);
            Assert.That(battleB.Bridge.LastBossExecutionResult, Is.Null);
            Assert.That(battleB.Bridge.ProcessedTurnEndCount, Is.Zero);
        }

        private static LifecycleBattle CreateBattle(
            IMatchEventActionProvider matchProvider,
            IBossCombatActionProvider bossProvider,
            int turnLimit = 25,
            ElementType characterElement = ElementType.Fire,
            long bossHp = 10000,
            long partyHp = 1000)
        {
            var character = new CharacterBattleState(
                "hero",
                0,
                characterElement,
                partyHp,
                100d);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                bossHp,
                100d);
            var coordinator = new BattleFlowCoordinator(turnLimit);
            var actionIds = new CombatActionIdSequence();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            var bridge = new BattleFlowCombatBridge(
                coordinator,
                party,
                boss,
                executor,
                matchProvider,
                bossProvider,
                actionIds);
            return new LifecycleBattle(
                character,
                party,
                boss,
                coordinator,
                bridge,
                actionIds);
        }

        private static IMatchEventActionProvider MatchDamageProvider(
            double coefficient)
        {
            return new DelegateMatchEventActionProvider(context =>
                new CombatAction[]
                {
                    CombatIntegrationTestSupport.MatchDamage(
                        context,
                        coefficient)
                });
        }

        private static IMatchEventActionProvider EmptyMatchProvider()
        {
            return new DelegateMatchEventActionProvider(
                context => Array.Empty<CombatAction>());
        }

        private static IBossCombatActionProvider BossDamageProvider(
            double coefficient)
        {
            return new DelegateBossCombatActionProvider(context =>
                new CombatAction[]
                {
                    CombatIntegrationTestSupport.BossDamage(
                        context,
                        coefficient)
                });
        }

        private static IBossCombatActionProvider EmptyBossProvider()
        {
            return new DelegateBossCombatActionProvider(
                context => Array.Empty<CombatAction>());
        }

        private static EffectInstance Effect(
            long runtimeId,
            string effectId,
            EffectModifierType modifierType,
            int remainingTurns)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "test",
                EffectCategory.Buff,
                modifierType,
                0.50d,
                remainingTurns,
                runtimeId);
        }

        private static CharacterBattleState Character(string id, int slot)
        {
            return new CharacterBattleState(
                id,
                slot,
                ElementType.Fire,
                1000,
                100d);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "boss",
                ElementType.Fire,
                10000,
                100d);
        }

        private static void EnterMatchResolving(
            BattleFlowCoordinator coordinator,
            BoardCascadeResult cascade)
        {
            coordinator.TryBeginBoardResolution();
            coordinator.NotifyBoardActionResolved(cascade, true);
        }

        private static void EnterBossActing(
            BattleFlowCoordinator coordinator)
        {
            coordinator.StartBattle();
            EnterBossActingFromPlayerInput(coordinator);
        }

        private static void EnterBossActingFromPlayerInput(
            BattleFlowCoordinator coordinator)
        {
            EnterMatchResolving(
                coordinator,
                BattleFlowTestSupport.CreateCascade());
            coordinator.TryBeginNextMatchEvent(out _);
        }

        private static BoardCascadeResult SingleMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(new[]
            {
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0),
                    new BoardPosition(2, 0))
            });
        }

        private sealed class LifecycleBattle
        {
            public LifecycleBattle(
                CharacterBattleState character,
                PartyBattleState party,
                BossBattleState boss,
                BattleFlowCoordinator coordinator,
                BattleFlowCombatBridge bridge,
                CombatActionIdSequence actionIds)
            {
                Character = character;
                Party = party;
                Boss = boss;
                Coordinator = coordinator;
                Bridge = bridge;
                ActionIds = actionIds;
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public BattleFlowCoordinator Coordinator { get; }
            public BattleFlowCombatBridge Bridge { get; }
            public CombatActionIdSequence ActionIds { get; }
        }
    }
}
