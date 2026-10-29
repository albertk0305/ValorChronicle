using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Relics.Stats;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornMatchActionProviderTests
    {
        [TestCase(0, 70L)]
        [TestCase(4, 80L)]
        public void ThreeMatchCreatesOnlyExpectedShield(
            int awakening,
            long expectedShield)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                awakening))
            {
                Assert.That(battle.Actions.Count, Is.EqualTo(1));
                Assert.That(battle.Actions[0], Is.TypeOf<ShieldAction>());
                Assert.That(battle.Actions.OfType<DamageAction>(), Is.Empty);

                CombatActionExecutionResult result = battle.Execute();
                ShieldActionResult shield = ShieldResult(result);

                Assert.That(shield.Context.ShieldCoefficient,
                    Is.EqualTo(awakening >= 4 ? 0.08d : 0.07d));
                Assert.That(shield.GenerationResult.FinalShieldAmount,
                    Is.EqualTo(expectedShield));
                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.EqualTo(expectedShield));
                Assert.That(battle.Party.Shields.ActiveShields.Count,
                    Is.EqualTo(1));
                Assert.That(battle.Party.Shields.ActiveShields[0]
                        .RemainingTurns,
                    Is.EqualTo(3));
                Assert.That(battle.Party.Shields.ActiveShields[0]
                        .CreatedTurn,
                    Is.EqualTo(battle.PlayerTurnNumber));
                Assert.That(battle.Party.Shields.ActiveShields[0].SourceId,
                    Is.EqualTo(NairaWindhornRules.Match3AbilityId));
            }
        }

        [Test]
        public void ThreeMatchAppliesComboOnceAndFloorsFinalShield()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                finalComboCount: 3))
            {
                CombatActionExecutionResult result = battle.Execute();
                ShieldActionResult shield = ShieldResult(result);

                Assert.That(shield.Context.AppliesCombo, Is.True);
                Assert.That(shield.Context.FinalComboCount, Is.EqualTo(3));
                Assert.That(shield.GenerationResult.ComboMultiplier,
                    Is.EqualTo(1.12d));
                Assert.That(shield.GenerationResult.RawShieldAmount,
                    Is.EqualTo(78.4d).Within(0.000000001d));
                Assert.That(shield.GenerationResult.FinalShieldAmount,
                    Is.EqualTo(78L));
            }
        }

        [TestCase(0, 120L)]
        [TestCase(4, 130L)]
        public void FourMatchCreatesShieldThenDeferredMatchDamage(
            int awakening,
            long expectedShield)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                awakening))
            {
                AssertShieldThenDamage(
                    battle,
                    NairaWindhornRules.Match4AbilityId,
                    AttackTag.Match4,
                    baseCoefficient: 1.20d);
                DamageAction damageAction = (DamageAction)battle.Actions[1];
                Assert.That(damageAction.ContextRequest.SkillCoefficient,
                    Is.EqualTo(1.20d));

                CombatActionExecutionResult result = battle.Execute();
                ShieldActionResult shield = ShieldResult(result);
                DamageActionResult damage = DamageResult(result);
                double expectedCharge = expectedShield / 1000d;
                double expectedCoefficient =
                    1.20d * (1d + (expectedCharge * 0.50d));

                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.EqualTo(expectedShield));
                Assert.That(damage.Context.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient)
                        .Within(0.000000001d));
                Assert.That(result.ActionResults[0],
                    Is.TypeOf<ShieldActionResult>());
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(expectedShield));
            }
        }

        [Test]
        public void FourMatchAtZeroChargeUsesBaseCoefficient()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                sourceMaxHp: 1L,
                sourceAttack: 1000d))
            {
                CombatActionExecutionResult result = battle.Execute();

                Assert.That(ShieldResult(result).GenerationResult
                        .FinalShieldAmount,
                    Is.Zero);
                Assert.That(battle.Party.Shields.ActiveShields, Is.Empty);
                Assert.That(DamageResult(result).Context.SkillCoefficient,
                    Is.EqualTo(1.20d));
                Assert.That(DamageResult(result).DamageResult.FinalDamage,
                    Is.EqualTo(1200L));
            }
        }

        [Test]
        public void FourMatchUsesPostCapTotalShieldForMaximumCoefficient()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                initialShield: 950L))
            {
                CombatActionExecutionResult result = battle.Execute();
                ShieldActionResult shield = ShieldResult(result);
                DamageActionResult damage = DamageResult(result);

                Assert.That(shield.ApplicationResult.RequestedShieldAmount,
                    Is.EqualTo(120L));
                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.EqualTo(50L));
                Assert.That(shield.ApplicationResult.TotalShieldAfter,
                    Is.EqualTo(1000L));
                Assert.That(damage.Context.SkillCoefficient,
                    Is.EqualTo(1.80d).Within(0.000000001d));
                Assert.That(damage.DamageResult.FinalDamage,
                    Is.EqualTo(1800L));
            }
        }

        [Test]
        public void FourMatchAtShieldCapCreatesNoInstanceButUsesCurrentShield()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                initialShield: 1000L))
            {
                CombatActionExecutionResult result = battle.Execute();
                ShieldActionResult shield = ShieldResult(result);

                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.Zero);
                Assert.That(shield.ApplicationResult.CreatedShieldRuntimeId,
                    Is.Null);
                Assert.That(battle.Party.Shields.ActiveShields.Count,
                    Is.EqualTo(1));
                Assert.That(DamageResult(result).Context.SkillCoefficient,
                    Is.EqualTo(1.80d).Within(0.000000001d));
            }
        }

        [Test]
        public void FourMatchAddsToExistingShieldWithoutConsumingIt()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                initialShield: 200L))
            {
                CombatActionExecutionResult result = battle.Execute();

                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(320L));
                Assert.That(battle.Party.Shields.ActiveShields.Count,
                    Is.EqualTo(2));
                Assert.That(DamageResult(result).Context.SkillCoefficient,
                    Is.EqualTo(1.392d).Within(0.000000001d));
            }
        }

        [Test]
        public void FourMatchDamageAndShieldBothApplyFinalCombo()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                finalComboCount: 3))
            {
                CombatActionExecutionResult result = battle.Execute();
                ShieldActionResult shield = ShieldResult(result);
                DamageActionResult damage = DamageResult(result);

                Assert.That(shield.GenerationResult.ComboMultiplier,
                    Is.EqualTo(1.12d));
                Assert.That(shield.GenerationResult.FinalShieldAmount,
                    Is.EqualTo(134L));
                Assert.That(damage.Context.AppliesCombo, Is.True);
                Assert.That(damage.Context.FinalComboCount, Is.EqualTo(3));
                Assert.That(damage.DamageResult.ComboMultiplier,
                    Is.EqualTo(1.12d));
                Assert.That(damage.DamageResult.FinalDamage,
                    Is.EqualTo(1434L));
            }
        }

        [TestCase(0)]
        [TestCase(4)]
        public void FiveOrMoreAlwaysUsesTwentyPercentShield(int awakening)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                awakening))
            {
                AssertShieldThenDamage(
                    battle,
                    NairaWindhornRules.Match5AbilityId,
                    AttackTag.Match5Plus,
                    baseCoefficient: 2.20d);

                CombatActionExecutionResult result = battle.Execute();

                Assert.That(ShieldResult(result).Context.ShieldCoefficient,
                    Is.EqualTo(0.20d));
                Assert.That(ShieldResult(result).ApplicationResult
                        .GrantedShieldAmount,
                    Is.EqualTo(200L));
                Assert.That(DamageResult(result).Context.SkillCoefficient,
                    Is.EqualTo(2.64d).Within(0.000000001d));
            }
        }

        [Test]
        public void FiveOrMoreAtFullChargeUsesMaximumCoefficient()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                initialShield: 800L))
            {
                CombatActionExecutionResult result = battle.Execute();

                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(1000L));
                Assert.That(DamageResult(result).Context.SkillCoefficient,
                    Is.EqualTo(4.40d).Within(0.000000001d));
                Assert.That(DamageResult(result).DamageResult.FinalDamage,
                    Is.EqualTo(4400L));
            }
        }

        [Test]
        public void ChargeAboveAttackerMaxHpIsClampedToOne()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                initialShield: 1500L,
                additionalPartyMaxHp: 1000L))
            {
                CombatActionExecutionResult result = battle.Execute();

                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(1700L));
                Assert.That(DamageResult(result).Context.SkillCoefficient,
                    Is.EqualTo(4.40d).Within(0.000000001d));
            }
        }

        [Test]
        public void ProviderCreationDoesNotResolveDeferredCoefficient()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                initialShield: 100L))
            {
                DamageAction damage = (DamageAction)battle.Actions[1];

                Assert.That(damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(1.20d));
                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(100L));

                CombatActionExecutionResult result = battle.Execute();

                Assert.That(damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(1.20d));
                Assert.That(DamageResult(result).Context.SkillCoefficient,
                    Is.EqualTo(1.332d).Within(0.000000001d));
            }
        }

        [Test]
        public void FinalMaxHpFromAwakeningAndRelicDrivesShield()
        {
            CharacterDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                    "Assets/Data/Characters/naira_windhorn.asset");
            var resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new NairaWindhornBuildModifier()
                });
            ResolvedCharacterBuild build = resolver.Resolve(
                definition,
                level: 100,
                awakening: 2,
                new RelicStatBonusSummary(0.10d, 0d));

            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                awakening: 2,
                sourceMaxHp: build.MaxHp,
                sourceAttack: build.Attack))
            {
                CombatActionExecutionResult result = battle.Execute();

                Assert.That(build.MaxHp, Is.EqualTo(7161L));
                Assert.That(ShieldResult(result).GenerationResult
                        .SourceMaxHp,
                    Is.EqualTo(7161L));
                Assert.That(ShieldResult(result).GenerationResult
                        .FinalShieldAmount,
                    Is.EqualTo(501L));
            }
        }

        [Test]
        public void RepeatedMatchesCreateIndependentShieldInstances()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                battle.Execute();
                battle.RecreateActions(BoardMatchTier.Three);
                battle.Execute();

                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(140L));
                Assert.That(battle.Party.Shields.ActiveShields.Count,
                    Is.EqualTo(2));
                Assert.That(battle.Party.Shields.ActiveShields[0].RuntimeId,
                    Is.Not.EqualTo(
                        battle.Party.Shields.ActiveShields[1].RuntimeId));
                Assert.That(
                    battle.Party.Shields.ActiveShields[0].CreationOrder,
                    Is.LessThan(
                        battle.Party.Shields.ActiveShields[1]
                            .CreationOrder));
            }
        }

        [Test]
        public void FactoryPreservesActionIdsRootsAndSixBlockTier()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                createActions: false))
            {
                var factory = new MatchEventCombatActionFactory(
                    battle.Party,
                    battle.Boss,
                    battle.Provider,
                    battle.ActionIds);

                IReadOnlyList<CombatAction> actions =
                    factory.CreateCharacterRootActions(
                        CreateExecution(
                            BoardMatchTier.FiveOrMore,
                            removedBlockCount: 6),
                        battle.Character,
                        playerTurnNumber: 7);

                Assert.That(actions.Count, Is.EqualTo(2));
                Assert.That(actions[0], Is.TypeOf<ShieldAction>());
                Assert.That(actions[1], Is.TypeOf<DamageAction>());
                Assert.That(actions[0].ActionId, Is.EqualTo(1L));
                Assert.That(actions[1].ActionId, Is.EqualTo(2L));
                Assert.That(actions, Has.All.Matches<CombatAction>(action =>
                    action.RootActionId == action.ActionId
                        && !action.SourceActionId.HasValue));
                Assert.That(((ShieldAction)actions[0]).GrantRequest
                        .CreatedTurn,
                    Is.EqualTo(7));
                Assert.That(((ShieldAction)actions[0]).GrantRequest.SourceId,
                    Is.EqualTo(NairaWindhornRules.Match5AbilityId));
                Assert.That(((DamageAction)actions[1]).ContextRequest
                        .AttackTags,
                    Is.EqualTo(AttackTag.Match5Plus));
            }
        }

        [Test]
        public void TerminalBattleSkipsNairaActionsWithoutCreatingShield()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                bossHp: 1L))
            {
                var lethal = new DamageAction(
                    100L,
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        battle.Character,
                        battle.Party,
                        battle.Boss,
                        ElementType.Grass,
                        AttackType.Match,
                        AttackTag.None,
                        1d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
                CombatAction[] queue = new CombatAction[] { lethal }
                    .Concat(battle.Actions)
                    .ToArray();

                CombatActionExecutionResult result = battle.Execute(queue);

                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(result.BossDefeated, Is.True);
                Assert.That(result.ClearedRemainingActions, Is.True);
                Assert.That(battle.Party.Shields.TotalShield, Is.Zero);
            }
        }

        [Test]
        public void ConstructorRejectsNullAndInvalidConfig()
        {
            Assert.That(
                () => new NairaWindhornMatchActionProvider(null),
                Throws.TypeOf<ArgumentNullException>());

            NairaWindhornCombatConfig config =
                NairaWindhornTestConfig.Create();
            try
            {
                NairaWindhornTestConfig.SetDouble(
                    config,
                    "match3ShieldMaxHpRate",
                    double.NaN);

                Assert.That(
                    () => new NairaWindhornMatchActionProvider(config),
                    Throws.TypeOf<ArgumentException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static void AssertShieldThenDamage(
            TestBattle battle,
            string expectedSourceId,
            AttackTag expectedTag,
            double baseCoefficient)
        {
            Assert.That(battle.Actions.Count, Is.EqualTo(2));
            var shield = battle.Actions[0] as ShieldAction;
            var damage = battle.Actions[1] as DamageAction;
            Assert.That(shield, Is.Not.Null);
            Assert.That(damage, Is.Not.Null);
            Assert.That(shield.Origin, Is.EqualTo(ActionOrigin.Match));
            Assert.That(shield.GrantRequest.SourceId,
                Is.EqualTo(expectedSourceId));
            Assert.That(shield.GrantRequest.RemainingTurns,
                Is.EqualTo(3));
            Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.Match));
            Assert.That(damage.ContextRequest.Attacker,
                Is.SameAs(battle.Character));
            Assert.That(damage.ContextRequest.Party,
                Is.SameAs(battle.Party));
            Assert.That(damage.ContextRequest.TargetBoss,
                Is.SameAs(battle.Boss));
            Assert.That(damage.ContextRequest.AttackElement,
                Is.EqualTo(ElementType.Grass));
            Assert.That(damage.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Match));
            Assert.That(damage.ContextRequest.AttackTags,
                Is.EqualTo(expectedTag));
            Assert.That(damage.ContextRequest.SkillCoefficient,
                Is.EqualTo(baseCoefficient));
            Assert.That(damage.ContextRequest.AppliesCombo, Is.True);
            Assert.That(damage.ContextRequest.FinalComboCount,
                Is.EqualTo(battle.FinalComboCount));
            Assert.That(damage.ContextRequest.CanCritical, Is.True);
        }

        private static ShieldActionResult ShieldResult(
            CombatActionExecutionResult result)
        {
            return result.ActionResults
                .OfType<ShieldActionResult>()
                .Single();
        }

        private static DamageActionResult DamageResult(
            CombatActionExecutionResult result)
        {
            return result.ActionResults
                .OfType<DamageActionResult>()
                .Single();
        }

        private static MatchEventExecution CreateExecution(
            BoardMatchTier tier,
            int removedBlockCount)
        {
            var positions = new List<BoardPosition>(removedBlockCount);
            for (int index = 0; index < removedBlockCount; index++)
            {
                positions.Add(new BoardPosition(index, 0));
            }

            MatchEvent matchEvent = CreateInternal<MatchEvent>(
                0,
                0,
                0,
                ElementType.Grass,
                tier,
                positions[0],
                positions,
                removedBlockCount);
            return CreateInternal<MatchEventExecution>(
                1L,
                matchEvent,
                1);
        }

        private static T CreateInternal<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic,
                null,
                arguments,
                null);
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly NairaWindhornCombatConfig config;

            public TestBattle(
                BoardMatchTier matchTier,
                int awakening = 0,
                int finalComboCount = 1,
                long sourceMaxHp = 1000L,
                double sourceAttack = 1000d,
                long initialShield = 0L,
                long additionalPartyMaxHp = 0L,
                long bossHp = 1000000L,
                int playerTurnNumber = 4,
                bool createActions = true)
            {
                config = NairaWindhornTestConfig.Create();
                Character = new CharacterBattleState(
                    NairaWindhornRules.CharacterId,
                    0,
                    ElementType.Grass,
                    sourceMaxHp,
                    sourceAttack);
                var characters = new List<CharacterBattleState>
                {
                    Character
                };
                if (additionalPartyMaxHp > 0)
                {
                    characters.Add(new CharacterBattleState(
                        "ally",
                        1,
                        ElementType.Fire,
                        additionalPartyMaxHp,
                        1d));
                }

                Party = new PartyBattleState(characters);
                if (initialShield > 0)
                {
                    Party.Shields.Add(new ShieldInstance(
                        runtimeId: 9000L,
                        sourceId: "existing_shield",
                        initialAmount: initialShield,
                        createdTurn: 1,
                        remainingTurns: 5,
                        creationOrder: 9000L));
                }

                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Grass,
                    bossHp,
                    100d);
                Provider = new NairaWindhornMatchActionProvider(
                    config,
                    awakening);
                ActionIds = new CombatActionIdSequence();
                FinalComboCount = finalComboCount;
                PlayerTurnNumber = playerTurnNumber;
                Actions = createActions
                    ? Provider.CreateActions(
                        Character,
                        Party,
                        Boss,
                        matchTier,
                        finalComboCount,
                        playerTurnNumber,
                        ActionIds)
                    : Array.Empty<CombatAction>();
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public NairaWindhornMatchActionProvider Provider { get; }
            public CombatActionIdSequence ActionIds { get; }
            public int FinalComboCount { get; }
            public int PlayerTurnNumber { get; }
            public IReadOnlyList<CombatAction> Actions { get; private set; }

            public void RecreateActions(BoardMatchTier matchTier)
            {
                Actions = Provider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    matchTier,
                    FinalComboCount,
                    PlayerTurnNumber,
                    ActionIds);
            }

            public CombatActionExecutionResult Execute()
            {
                return Execute(Actions);
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions)
            {
                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)));
                return executor.Execute(new CombatActionQueue(actions));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}

