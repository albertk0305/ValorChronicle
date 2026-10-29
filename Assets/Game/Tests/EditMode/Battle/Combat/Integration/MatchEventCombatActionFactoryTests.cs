using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class MatchEventCombatActionFactoryTests
    {
        [Test]
        public void FactoryFiltersElementAndPreservesSlotActionAndIdOrder()
        {
            PartyBattleState party = Party(
                Character("fire_4", 4, ElementType.Fire),
                Character("water_1", 1, ElementType.Water),
                Character("fire_2", 2, ElementType.Fire),
                Character("fire_0", 0, ElementType.Fire));
            BossBattleState boss = Boss();
            var actionIds = new CombatActionIdSequence();
            var calls = new List<MatchEventActionContext>();
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                calls.Add(context);
                return new CombatAction[]
                {
                    CombatIntegrationTestSupport.MatchDamage(context, 1d),
                    CombatIntegrationTestSupport.MatchDamage(context, 2d)
                };
            });
            var factory = new MatchEventCombatActionFactory(
                party,
                boss,
                provider,
                actionIds);
            MatchEventExecution execution = BeginSingleMatch(
                ElementType.Fire,
                4);

            IReadOnlyList<CombatAction> actions =
                factory.CreateRootActions(execution);

            Assert.That(
                calls.ConvertAll(call => call.Character.PartySlotIndex),
                Is.EqualTo(new[] { 0, 2, 4 }));
            Assert.That(actions, Has.Count.EqualTo(6));
            Assert.That(
                Array.ConvertAll(
                    new List<CombatAction>(actions).ToArray(),
                    action => action.ActionId),
                Is.EqualTo(new long[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(calls, Has.All.Matches<MatchEventActionContext>(
                call => call.FinalComboCount == 1
                    && call.MatchAttackTag == AttackTag.Match4));
            Assert.That(actions, Has.All.Matches<CombatAction>(
                action => action.RootActionId == action.ActionId
                    && !action.SourceActionId.HasValue));
        }

        [Test]
        public void FactoryAllowsZeroActionsAndNoMatchingCharacter()
        {
            int providerCallCount = 0;
            PartyBattleState party = Party(
                Character("water", 1, ElementType.Water),
                Character("fire", 3, ElementType.Fire));
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                providerCallCount++;
                return Array.Empty<CombatAction>();
            });
            var factory = new MatchEventCombatActionFactory(
                party,
                Boss(),
                provider,
                new CombatActionIdSequence());

            Assert.That(
                factory.CreateRootActions(
                    BeginSingleMatch(ElementType.Fire, 3)),
                Is.Empty);
            Assert.That(providerCallCount, Is.EqualTo(1));
            Assert.That(
                factory.CreateRootActions(
                    BeginSingleMatch(ElementType.Dark, 5)),
                Is.Empty);
            Assert.That(providerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void FactoryPreservesShieldThenDeferredMatchDamageOrder()
        {
            CharacterBattleState character =
                Character("fire", 0, ElementType.Fire);
            PartyBattleState party = Party(character);
            BossBattleState boss = Boss();
            int resolveCount = 0;
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                long shieldActionId = context.ActionIds.Next();
                long damageActionId = context.ActionIds.Next();
                return new CombatAction[]
                {
                    new ShieldAction(
                        shieldActionId,
                        ActionOrigin.Match,
                        new ShieldGenerationContextBuildRequest(
                            context.Character,
                            context.Party,
                            0.50d,
                            true,
                            context.FinalComboCount),
                        new ShieldGrantRequest(
                            shieldActionId,
                            context.Character.CharacterId,
                            context.PlayerTurnNumber,
                            3,
                            shieldActionId)),
                    new DamageAction(
                        damageActionId,
                        ActionOrigin.Match,
                        new DamageContextBuildRequest(
                            context.Character,
                            context.Party,
                            context.Boss,
                            context.Character.Element,
                            AttackType.Match,
                            context.MatchAttackTag,
                            1.20d,
                            true,
                            context.FinalComboCount,
                            false,
                            skillCoefficientResolver: request =>
                            {
                                resolveCount++;
                                double charge = Math.Min(
                                    (double)request.Party.Shields.TotalShield
                                        / request.Attacker.MaxHp,
                                    1d);
                                return request.SkillCoefficient
                                    * (1d + (charge * 0.50d));
                            }))
                };
            });
            var factory = new MatchEventCombatActionFactory(
                party,
                boss,
                provider,
                new CombatActionIdSequence());

            IReadOnlyList<CombatAction> actions =
                factory.CreateRootActions(BeginSingleMatch(
                    ElementType.Fire,
                    4),
                    playerTurnNumber: 7);

            Assert.That(resolveCount, Is.Zero);
            Assert.That(actions, Has.Count.EqualTo(2));
            Assert.That(actions[0], Is.TypeOf<ShieldAction>());
            Assert.That(actions[1], Is.TypeOf<DamageAction>());
            Assert.That(actions[0].ActionId, Is.EqualTo(1L));
            Assert.That(actions[1].ActionId, Is.EqualTo(2L));
            Assert.That(actions, Has.All.Matches<CombatAction>(action =>
                action.RootActionId == action.ActionId
                    && !action.SourceActionId.HasValue));

            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            CombatActionExecutionResult result = executor.Execute(
                new CombatActionQueue(actions));
            var shieldResult = (ShieldActionResult)result.ActionResults[0];
            var damageResult = (DamageActionResult)result.ActionResults[1];

            Assert.That(shieldResult.ApplicationResult.GrantedShieldAmount,
                Is.EqualTo(500L));
            Assert.That(shieldResult.ApplicationResult.CreatedShieldRuntimeId,
                Is.EqualTo(1L));
            Assert.That(party.Shields.ActiveShields[0].CreatedTurn,
                Is.EqualTo(7));
            Assert.That(damageResult.Context.SkillCoefficient,
                Is.EqualTo(1.50d).Within(0.000000001d));
            Assert.That(resolveCount, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedElementEventsRemainSeparateAndMapEveryTier()
        {
            PartyBattleState party = Party(
                Character("fire_2", 2, ElementType.Fire),
                Character("fire_0", 0, ElementType.Fire));
            var contexts = new List<MatchEventActionContext>();
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                contexts.Add(context);
                return Array.Empty<CombatAction>();
            });
            var factory = new MatchEventCombatActionFactory(
                party,
                Boss(),
                provider,
                new CombatActionIdSequence());
            BattleFlowCoordinator coordinator = StartBoardResolution(25);
            coordinator.NotifyBoardActionResolved(
                ThreeTierCascade(),
                true);

            for (int eventIndex = 0; eventIndex < 3; eventIndex++)
            {
                coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution execution);
                Assert.That(factory.CreateRootActions(execution), Is.Empty);
                coordinator.CompleteCurrentMatchEvent(execution.ExecutionId);
            }

            Assert.That(
                contexts.ConvertAll(context =>
                    $"E{context.MatchEvent.SequenceIndex}-"
                        + $"S{context.Character.PartySlotIndex}"),
                Is.EqualTo(new[]
                {
                    "E0-S0", "E0-S2",
                    "E1-S0", "E1-S2",
                    "E2-S0", "E2-S2"
                }));
            Assert.That(
                contexts.ConvertAll(context => context.MatchAttackTag),
                Is.EqualTo(new[]
                {
                    AttackTag.Match3, AttackTag.Match3,
                    AttackTag.Match4, AttackTag.Match4,
                    AttackTag.Match5Plus, AttackTag.Match5Plus
                }));
            Assert.That(
                contexts.ConvertAll(context => context.FinalComboCount),
                Has.All.EqualTo(3));
        }

        [Test]
        public void FourEventBatchSharesFinalComboAndResetsNextTurn()
        {
            BattleFlowCoordinator coordinator = StartBoardResolution(2);
            coordinator.NotifyBoardActionResolved(
                FourMatchCascade(),
                true);

            var observedCounts = new List<int>();
            while (coordinator.Context.Phase
                == BattlePhase.MatchEventResolving)
            {
                if (!coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution execution))
                {
                    break;
                }

                observedCounts.Add(execution.FinalComboCount);
                coordinator.CompleteCurrentMatchEvent(
                    execution.ExecutionId);
            }

            Assert.That(observedCounts, Is.EqualTo(new[] { 4, 4, 4, 4 }));
            Assert.That(coordinator.CompleteBossAction(), Is.True);
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(
                coordinator.NotifyBoardActionResolved(
                    SingleMatchCascade(ElementType.Fire, 3),
                    true),
                Is.True);
            Assert.That(
                coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution nextTurnExecution),
                Is.True);
            Assert.That(nextTurnExecution.FinalComboCount, Is.EqualTo(1));
        }

        [Test]
        public void FactoryAllowsMatchFollowedByDerivedAdditionalDamage()
        {
            CharacterBattleState character =
                Character("fire", 0, ElementType.Fire);
            PartyBattleState party = Party(character);
            BossBattleState boss = Boss();
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                DamageAction match =
                    CombatIntegrationTestSupport.MatchDamage(context, 1d);
                return new CombatAction[]
                {
                    match,
                    CombatIntegrationTestSupport.AdditionalMatchDamage(
                        context,
                        match,
                        0.5d)
                };
            });
            var actionIds = new CombatActionIdSequence();
            var factory = new MatchEventCombatActionFactory(
                party,
                boss,
                provider,
                actionIds);

            IReadOnlyList<CombatAction> actions =
                factory.CreateRootActions(BeginSingleMatch(
                    ElementType.Fire,
                    4));

            Assert.That(actions, Has.Count.EqualTo(2));
            var match = (DamageAction)actions[0];
            var additional = (DamageAction)actions[1];
            Assert.That(match.Origin, Is.EqualTo(ActionOrigin.Match));
            Assert.That(match.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Match));
            Assert.That(match.RootActionId, Is.EqualTo(match.ActionId));
            Assert.That(match.SourceActionId, Is.Null);
            Assert.That(additional.Origin,
                Is.EqualTo(ActionOrigin.Additional));
            Assert.That(additional.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Additional));
            Assert.That(additional.RootActionId,
                Is.EqualTo(match.ActionId));
            Assert.That(additional.SourceActionId,
                Is.EqualTo(match.ActionId));
            Assert.That(additional.ContextRequest.AppliesCombo, Is.True);
            Assert.That(additional.ContextRequest.FinalComboCount,
                Is.EqualTo(1));
            Assert.That(additional.CanTriggerFollowUp, Is.False);
            Assert.That(actionIds.LastIssuedId, Is.EqualTo(2));
        }

        [TestCase(0, 1)]
        [TestCase(1, 2)]
        public void ProviderSnapshotsAdditionalConditionWhenActionsAreCreated(
            int resourceAmount,
            int expectedActionCount)
        {
            CharacterBattleState character =
                Character("fire", 0, ElementType.Fire);
            PartyBattleState party = Party(character);
            BossBattleState boss = Boss();
            boss.Resources.Register("condition", 1);
            if (resourceAmount > 0)
            {
                boss.Resources.Add("condition", resourceAmount);
            }

            var provider = new DelegateMatchEventActionProvider(context =>
            {
                DamageAction match =
                    CombatIntegrationTestSupport.MatchDamage(context, 1d);
                if (context.Boss.Resources.GetAmount("condition") == 0)
                {
                    return new CombatAction[] { match };
                }

                return new CombatAction[]
                {
                    match,
                    CombatIntegrationTestSupport.AdditionalMatchDamage(
                        context,
                        match,
                        0.5d)
                };
            });
            var factory = new MatchEventCombatActionFactory(
                party,
                boss,
                provider,
                new CombatActionIdSequence());
            IReadOnlyList<CombatAction> actions =
                factory.CreateRootActions(BeginSingleMatch(
                    ElementType.Fire,
                    4));

            boss.Resources.ConsumeAll("condition");
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            CombatActionExecutionResult result = executor.Execute(
                new CombatActionQueue(actions));

            Assert.That(actions, Has.Count.EqualTo(expectedActionCount));
            Assert.That(result.CompletedActionCount,
                Is.EqualTo(expectedActionCount));
        }

        [TestCase("additional_only")]
        [TestCase("wrong_root")]
        [TestCase("wrong_source")]
        [TestCase("missing_source")]
        public void FactoryRejectsInvalidAdditionalLineage(string scenario)
        {
            CharacterBattleState character =
                Character("fire", 0, ElementType.Fire);
            PartyBattleState party = Party(character);
            BossBattleState boss = Boss();
            var actionIds = new CombatActionIdSequence();
            long outsideSequenceId = scenario == "additional_only"
                ? actionIds.Next()
                : 0;
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                if (scenario == "additional_only")
                {
                    return new CombatAction[]
                    {
                        Additional(
                            context,
                            outsideSequenceId,
                            outsideSequenceId)
                    };
                }

                var marker = new AddResourceAction(
                    context.ActionIds.Next(),
                    ActionOrigin.System,
                    context.Boss,
                    "marker",
                    1);
                DamageAction match =
                    CombatIntegrationTestSupport.MatchDamage(context, 1d);
                if (scenario == "wrong_root")
                {
                    return new CombatAction[]
                    {
                        marker,
                        match,
                        Additional(
                            context,
                            marker.ActionId,
                            match.ActionId)
                    };
                }

                if (scenario == "wrong_source")
                {
                    return new CombatAction[]
                    {
                        marker,
                        match,
                        Additional(
                            context,
                            match.ActionId,
                            marker.ActionId)
                    };
                }

                return new CombatAction[]
                {
                    marker,
                    match,
                    Additional(context, 99, 99)
                };
            });
            var factory = new MatchEventCombatActionFactory(
                party,
                boss,
                provider,
                actionIds);

            Assert.Throws<InvalidOperationException>(() =>
                factory.CreateRootActions(
                    BeginSingleMatch(ElementType.Fire, 4)));
        }

        [TestCase("attacker")]
        [TestCase("party")]
        [TestCase("boss")]
        [TestCase("element")]
        [TestCase("tag")]
        [TestCase("combo")]
        [TestCase("combo_disabled")]
        public void FactoryRejectsAdditionalWithWrongMatchMetadata(
            string scenario)
        {
            CharacterBattleState character =
                Character("fire", 0, ElementType.Fire);
            PartyBattleState party = Party(character);
            BossBattleState boss = Boss();
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                DamageAction match =
                    CombatIntegrationTestSupport.MatchDamage(context, 1d);
                return new CombatAction[]
                {
                    match,
                    Additional(
                        context,
                        match.ActionId,
                        match.ActionId,
                        attacker: scenario == "attacker"
                            ? Character("other", 1, ElementType.Fire)
                            : null,
                        party: scenario == "party"
                            ? Party(Character(
                                "other",
                                1,
                                ElementType.Fire))
                            : null,
                        boss: scenario == "boss" ? Boss() : null,
                        element: scenario == "element"
                            ? ElementType.Water
                            : (ElementType?)null,
                        attackTag: scenario == "tag"
                            ? AttackTag.Match3
                            : (AttackTag?)null,
                        appliesCombo: scenario != "combo_disabled",
                        finalComboCount: scenario == "combo" ? 99 : 1)
                };
            });
            var factory = new MatchEventCombatActionFactory(
                party,
                boss,
                provider,
                new CombatActionIdSequence());

            Assert.Throws<InvalidOperationException>(() =>
                factory.CreateRootActions(
                    BeginSingleMatch(ElementType.Fire, 4)));
        }

        [Test]
        public void FactoryRejectsDuplicateActionId()
        {
            CharacterBattleState character =
                Character("fire", 0, ElementType.Fire);
            PartyBattleState party = Party(character);
            BossBattleState boss = Boss();
            var duplicateProvider = new DelegateMatchEventActionProvider(
                context =>
                {
                    DamageAction first =
                        CombatIntegrationTestSupport.MatchDamage(context, 1d);
                    context.ActionIds.Next();
                    return new CombatAction[]
                    {
                        first,
                        MatchWithId(context, first.ActionId)
                    };
                });
            Assert.Throws<InvalidOperationException>(() =>
                new MatchEventCombatActionFactory(
                    party,
                    boss,
                    duplicateProvider,
                    new CombatActionIdSequence()).CreateRootActions(
                        BeginSingleMatch(ElementType.Fire, 4)));
        }

        [TestCase(ActionOrigin.Active, AttackType.Active)]
        [TestCase(
            ActionOrigin.DamageOverTime,
            AttackType.DamageOverTime)]
        [TestCase(ActionOrigin.Chase, AttackType.Chase)]
        public void FactoryRejectsUnsupportedDamageOrigins(
            ActionOrigin origin,
            AttackType attackType)
        {
            CharacterBattleState character =
                Character("fire", 0, ElementType.Fire);
            PartyBattleState party = Party(character);
            BossBattleState boss = Boss();
            var provider = new DelegateMatchEventActionProvider(context =>
            {
                DamageAction match =
                    CombatIntegrationTestSupport.MatchDamage(context, 1d);
                var unsupported = new DamageAction(
                    context.ActionIds.Next(),
                    origin,
                    Request(
                        context,
                        attackType,
                        context.Character,
                        context.Party,
                        context.Boss,
                        context.Character.Element,
                        context.MatchAttackTag,
                        true,
                        context.FinalComboCount),
                    rootActionId: origin == ActionOrigin.Chase
                        ? match.ActionId
                        : (long?)null,
                    sourceActionId: origin == ActionOrigin.Chase
                        ? match.ActionId
                        : (long?)null);
                return new CombatAction[] { match, unsupported };
            });

            Assert.Throws<InvalidOperationException>(() =>
                new MatchEventCombatActionFactory(
                    party,
                    boss,
                    provider,
                    new CombatActionIdSequence()).CreateRootActions(
                        BeginSingleMatch(ElementType.Fire, 4)));
        }

        [Test]
        public void FactoryRejectsDamageWithWrongFinalComboMetadata()
        {
            CharacterBattleState character =
                Character("fire", 0, ElementType.Fire);
            PartyBattleState party = Party(character);
            BossBattleState boss = Boss();
            var provider = new DelegateMatchEventActionProvider(context =>
                new CombatAction[]
                {
                    new DamageAction(
                        context.ActionIds.Next(),
                        ActionOrigin.Match,
                        new DamageContextBuildRequest(
                            character,
                            party,
                            boss,
                            ElementType.Fire,
                            AttackType.Match,
                            AttackTag.Match3,
                            1d,
                            true,
                            99,
                            false))
                });
            var factory = new MatchEventCombatActionFactory(
                party,
                boss,
                provider,
                new CombatActionIdSequence());

            Assert.Throws<InvalidOperationException>(() =>
                factory.CreateRootActions(
                    BeginSingleMatch(ElementType.Fire, 3)));
        }

        private static MatchEventExecution BeginSingleMatch(
            ElementType element,
            int blockCount)
        {
            BattleFlowCoordinator coordinator = StartBoardResolution(25);
            coordinator.NotifyBoardActionResolved(
                SingleMatchCascade(element, blockCount),
                true);
            coordinator.TryBeginNextMatchEvent(out MatchEventExecution result);
            return result;
        }

        private static DamageAction Additional(
            MatchEventActionContext context,
            long rootActionId,
            long sourceActionId,
            CharacterBattleState attacker = null,
            PartyBattleState party = null,
            BossBattleState boss = null,
            ElementType? element = null,
            AttackTag? attackTag = null,
            bool appliesCombo = true,
            int finalComboCount = 1)
        {
            return new DamageAction(
                context.ActionIds.Next(),
                ActionOrigin.Additional,
                Request(
                    context,
                    AttackType.Additional,
                    attacker ?? context.Character,
                    party ?? context.Party,
                    boss ?? context.Boss,
                    element ?? context.Character.Element,
                    attackTag ?? context.MatchAttackTag,
                    appliesCombo,
                    appliesCombo ? finalComboCount : 0),
                rootActionId,
                sourceActionId);
        }

        private static DamageAction MatchWithId(
            MatchEventActionContext context,
            long actionId)
        {
            return new DamageAction(
                actionId,
                ActionOrigin.Match,
                Request(
                    context,
                    AttackType.Match,
                    context.Character,
                    context.Party,
                    context.Boss,
                    context.Character.Element,
                    context.MatchAttackTag,
                    true,
                    context.FinalComboCount));
        }

        private static DamageContextBuildRequest Request(
            MatchEventActionContext context,
            AttackType attackType,
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            ElementType element,
            AttackTag attackTag,
            bool appliesCombo,
            int finalComboCount)
        {
            return new DamageContextBuildRequest(
                attacker,
                party,
                boss,
                element,
                attackType,
                attackTag,
                1d,
                appliesCombo,
                finalComboCount,
                false);
        }

        private static BattleFlowCoordinator StartBoardResolution(int turns)
        {
            var coordinator = new BattleFlowCoordinator(turns);
            coordinator.StartBattle();
            coordinator.TryBeginBoardResolution();
            return coordinator;
        }

        private static BoardCascadeResult SingleMatchCascade(
            ElementType element,
            int blockCount)
        {
            var positions = new BoardPosition[blockCount];
            for (int index = 0; index < blockCount; index++)
            {
                positions[index] = new BoardPosition(index, 0);
            }

            return BattleFlowTestSupport.CreateCascade(
                new[] { BattleFlowTestSupport.Match(element, positions) });
        }

        private static BoardCascadeResult FourMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(new[]
            {
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0),
                    new BoardPosition(2, 0)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 1),
                    new BoardPosition(1, 1),
                    new BoardPosition(2, 1)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 2),
                    new BoardPosition(1, 2),
                    new BoardPosition(2, 2)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 3),
                    new BoardPosition(1, 3),
                    new BoardPosition(2, 3))
            });
        }

        private static BoardCascadeResult ThreeTierCascade()
        {
            return BattleFlowTestSupport.CreateCascade(new[]
            {
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0),
                    new BoardPosition(2, 0)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 1),
                    new BoardPosition(1, 1),
                    new BoardPosition(2, 1),
                    new BoardPosition(3, 1)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 2),
                    new BoardPosition(1, 2),
                    new BoardPosition(2, 2),
                    new BoardPosition(3, 2),
                    new BoardPosition(4, 2))
            });
        }

        private static CharacterBattleState Character(
            string id,
            int slot,
            ElementType element)
        {
            return new CharacterBattleState(id, slot, element, 1000, 100d);
        }

        private static PartyBattleState Party(
            params CharacterBattleState[] characters)
        {
            return new PartyBattleState(characters);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "boss",
                ElementType.Fire,
                10000,
                100d);
        }
    }
}
