using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Koharu;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.Koharu
{
    public sealed class SakuragiKoharuCombatProviderRegistrarTests
    {
        private SakuragiKoharuCombatConfig config;
        private CharacterDefinition definition;

        [SetUp]
        public void SetUp()
        {
            config = SakuragiKoharuTestConfig.Create();
            definition = SakuragiKoharuTestConfig.Definition(config);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void RegistrationProvidesMatchTriggerBoardActiveHandleAndStatus()
        {
            Registration registration = Register(awakening: 6, slot: 3);

            Assert.That(registration.Result.ActiveAbilityId,
                Is.EqualTo(SakuragiKoharuRules.ActiveAbilityId));
            Assert.That(registration.Result.ActiveCooldownTurns,
                Is.EqualTo(4));
            Assert.That(registration.MatchProviders.TryResolve(
                SakuragiKoharuRules.CharacterId,
                out IMatchEventActionProvider match), Is.True);
            Assert.That(match,
                Is.TypeOf<SakuragiKoharuMatchActionProvider>());
            Assert.That(registration.ActiveProviders.TryResolveBoard(
                SakuragiKoharuRules.CharacterId,
                SakuragiKoharuRules.ActiveAbilityId,
                out IActiveAbilityBoardPlanProvider active), Is.True);
            Assert.That(active,
                Is.TypeOf<SakuragiKoharuActiveBoardPlanProvider>());
            Assert.That(registration.Result.TriggerRules.Single(),
                Is.TypeOf<SakuragiKoharuSpiritEnergyRule>());
            Assert.That(registration.Result.ResourceRequirements, Is.Empty);

            var status = (SakuragiKoharuBattleState)registration.Result
                .CombatStatusSources.Single();
            Assert.That(status.CharacterId,
                Is.EqualTo(SakuragiKoharuRules.CharacterId));
            Assert.That(status.PartySlotIndex, Is.EqualTo(3));
            Assert.That(status.StatusId,
                Is.EqualTo(SakuragiKoharuRules.SpiritEnergyStatusId));
            Assert.That(status.CurrentCount, Is.Zero);
            Assert.That(status.IsVisible, Is.True);

            ActiveAbilityCooldownReductionHandle handle = registration
                .Result.ActiveAbilityCooldownReductionHandle;
            Assert.That(handle.CharacterId,
                Is.EqualTo(SakuragiKoharuRules.CharacterId));
            Assert.That(handle.PartySlotIndex, Is.EqualTo(3));
            Assert.That(handle.IsBound, Is.False);
        }

        [Test]
        public void TriggerActiveAndStatusShareStateAndHandle()
        {
            Registration registration = Register(awakening: 5, slot: 2);
            object status = registration.Result.CombatStatusSources.Single();
            object trigger = registration.Result.TriggerRules.Single();
            registration.ActiveProviders.TryResolveBoard(
                SakuragiKoharuRules.CharacterId,
                SakuragiKoharuRules.ActiveAbilityId,
                out IActiveAbilityBoardPlanProvider active);

            Assert.That(Field(trigger, "battleState"), Is.SameAs(status));
            Assert.That(Field(active, "battleState"), Is.SameAs(status));
            object handle = registration.Result
                .ActiveAbilityCooldownReductionHandle;
            Assert.That(Field(trigger, "cooldownReductionHandle"),
                Is.SameAs(handle));
            Assert.That(Field(active, "cooldownReductionHandle"),
                Is.SameAs(handle));
        }

        [Test]
        public void MatchEnergyIsConsumedBySubsequentActiveSuccess()
        {
            Registration registration = Register(awakening: 0, slot: 4);
            var party = new PartyBattleState(
                new[] { registration.Character });
            var ids = registration.ActionIds;
            var combatExecutor = new CombatActionExecutor(
                registration.Boss,
                party,
                new DamageContextFactory(new SeededRandomSource(10)),
                new CombatTriggerResolver(registration.Result.TriggerRules));
            registration.MatchProviders.TryResolve(
                SakuragiKoharuRules.CharacterId,
                out IMatchEventActionProvider match);
            var koharuMatch = (SakuragiKoharuMatchActionProvider)match;
            var coordinator = new BattleFlowCoordinator(25, new[] { 4 });
            Bind(
                registration.Result.ActiveAbilityCooldownReductionHandle,
                coordinator.Context.ActiveAbilities[0]);
            coordinator.StartBattle();

            for (int index = 0; index < 2; index++)
            {
                combatExecutor.Execute(new CombatActionQueue(
                    koharuMatch.CreateActions(
                        registration.Character,
                        party,
                        registration.Boss,
                        BoardMatchTier.FiveOrMore,
                        1,
                        ids)));
            }

            var state = (SakuragiKoharuBattleState)registration.Result
                .CombatStatusSources.Single();
            Assert.That(state.SpiritEnergy, Is.EqualTo(6));

            var activeExecutor = new ActiveAbilityCombatExecutor(
                coordinator,
                party,
                registration.Boss,
                combatExecutor,
                ids,
                new[]
                {
                    new ActiveAbilityBinding(
                        0,
                        registration.Character.PartySlotIndex,
                        registration.Character.CharacterId,
                        SakuragiKoharuRules.ActiveAbilityId)
                },
                registration.ActiveProviders,
                new ResolverSink(CreateBoard()));

            Assert.That(activeExecutor.TryExecute(0, out _), Is.True);
            Assert.That(state.SpiritEnergy, Is.Zero);
            Assert.That(state.CurrentCount, Is.Zero);
            Assert.That(coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(3));
        }

        [Test]
        public void SeparateRegistrationsOwnIndependentBattleStateAndHandle()
        {
            Registration first = Register(0, 0);
            Registration second = Register(0, 4);
            var firstState = (SakuragiKoharuBattleState)first.Result
                .CombatStatusSources.Single();
            var secondState = (SakuragiKoharuBattleState)second.Result
                .CombatStatusSources.Single();

            firstState.GainSpiritEnergy(9);

            Assert.That(firstState, Is.Not.SameAs(secondState));
            Assert.That(first.Result.ActiveAbilityCooldownReductionHandle,
                Is.Not.SameAs(second.Result
                    .ActiveAbilityCooldownReductionHandle));
            Assert.That(secondState.SpiritEnergy, Is.Zero);
        }

        [Test]
        public void SparseDenseBindingDoesNotChangeOtherActiveOrSpirit()
        {
            Registration registration = Register(0, 4);
            var other = new CharacterBattleState(
                "character_other_active",
                0,
                ElementType.Fire,
                1000,
                100d);
            var party = new PartyBattleState(
                new[] { other, registration.Character });
            registration.ActiveProviders.Register(
                other.CharacterId,
                "active_other",
                new NoOpActiveProvider());
            var coordinator = new BattleFlowCoordinator(25, new[] { 7, 4 });
            Bind(
                registration.Result.ActiveAbilityCooldownReductionHandle,
                coordinator.Context.ActiveAbilities[1]);
            coordinator.StartBattle();
            var state = (SakuragiKoharuBattleState)registration.Result
                .CombatStatusSources.Single();
            state.GainSpiritEnergy(6);
            var combatExecutor = new CombatActionExecutor(
                registration.Boss,
                party,
                new DamageContextFactory(new SeededRandomSource(10)));
            var activeExecutor = new ActiveAbilityCombatExecutor(
                coordinator,
                party,
                registration.Boss,
                combatExecutor,
                registration.ActionIds,
                new[]
                {
                    new ActiveAbilityBinding(
                        0, 0, other.CharacterId, "active_other"),
                    new ActiveAbilityBinding(
                        1, 4, registration.Character.CharacterId,
                        SakuragiKoharuRules.ActiveAbilityId)
                },
                registration.ActiveProviders,
                new ResolverSink(CreateBoard()));

            Assert.That(activeExecutor.TryExecute(1, out _), Is.True);
            Assert.That(coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.Zero);
            Assert.That(coordinator.Context.ActiveAbilities[1]
                .RemainingCooldown, Is.EqualTo(3));
            Assert.That(state.SpiritEnergy, Is.Zero);

            Assert.That(activeExecutor.TryExecute(0, out _), Is.True);
            Assert.That(coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(7));
            Assert.That(state.SpiritEnergy, Is.Zero);
        }

        [Test]
        public void KoharuFiveMatchBuffAffectsSerenaChaseOnlyAfterBuffAction()
        {
            Registration registration = Register(awakening: 4, slot: 0);
            var serena = new CharacterBattleState(
                "character_serena_coralspear",
                3,
                ElementType.Water,
                1000,
                1000d);
            var party = new PartyBattleState(
                new[] { registration.Character, serena });
            registration.MatchProviders.TryResolve(
                SakuragiKoharuRules.CharacterId,
                out IMatchEventActionProvider resolved);
            var match = (SakuragiKoharuMatchActionProvider)resolved;
            var chaseRule = new SerenaChaseAfterKoharuMatchRule(
                registration.Character,
                serena,
                registration.ActionIds);
            var executor = new CombatActionExecutor(
                registration.Boss,
                party,
                new DamageContextFactory(new SeededRandomSource(10)),
                new CombatTriggerResolver(
                    new ICombatTriggerRule[] { chaseRule }));

            CombatActionExecutionResult matchResult = executor.Execute(
                new CombatActionQueue(match.CreateActions(
                    registration.Character,
                    party,
                    registration.Boss,
                    BoardMatchTier.FiveOrMore,
                    1,
                    registration.ActionIds)));
            DamageActionResult earlyChase = matchResult.ActionResults
                .OfType<DamageActionResult>()
                .Single(item => item.Action.Origin == ActionOrigin.Chase);
            Assert.That(earlyChase.Context
                .AttackTypeDamageIncreaseRateSum, Is.Zero);

            DamageActionResult laterChase = ExecuteSerenaDamage(
                executor,
                registration,
                party,
                serena,
                AttackType.Chase,
                ActionOrigin.Chase);
            DamageActionResult laterAdditional = ExecuteSerenaDamage(
                executor,
                registration,
                party,
                serena,
                AttackType.Additional,
                ActionOrigin.Additional);
            DamageActionResult laterMatch = ExecuteSerenaDamage(
                executor,
                registration,
                party,
                serena,
                AttackType.Match,
                ActionOrigin.Match);

            Assert.That(laterChase.Context
                .AttackTypeDamageIncreaseRateSum, Is.EqualTo(0.15d));
            Assert.That(laterAdditional.Context
                .AttackTypeDamageIncreaseRateSum, Is.EqualTo(0.15d));
            Assert.That(laterMatch.Context
                .AttackTypeDamageIncreaseRateSum, Is.Zero);
        }

        [Test]
        public void RegistrationRejectsMismatchedPartySlotBeforeRegistering()
        {
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();

            Assert.Throws<InvalidOperationException>(() =>
                new SakuragiKoharuCombatProviderRegistrar().Register(
                    Member(3, 0),
                    Character(4),
                    Boss(),
                    new CombatActionIdSequence(),
                    matches,
                    actives));
            Assert.That(matches.Count, Is.Zero);
            Assert.That(actives.Count, Is.Zero);
        }

        private Registration Register(int awakening, int slot)
        {
            var character = Character(slot);
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();
            var ids = new CombatActionIdSequence();
            BossBattleState boss = Boss();
            CharacterCombatProviderRegistrationResult result =
                new SakuragiKoharuCombatProviderRegistrar().Register(
                    Member(slot, awakening),
                    character,
                    boss,
                    ids,
                    matches,
                    actives);
            return new Registration(
                character,
                boss,
                ids,
                matches,
                actives,
                result);
        }

        private BattlePartyMemberInput Member(int slot, int awakening)
        {
            return new BattlePartyMemberInput(
                SakuragiKoharuRules.CharacterId,
                slot,
                1,
                awakening,
                definition);
        }

        private static CharacterBattleState Character(int slot)
        {
            return new CharacterBattleState(
                SakuragiKoharuRules.CharacterId,
                slot,
                ElementType.Grass,
                1100,
                120d);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "koharu_registrar_test_boss",
                ElementType.Water,
                1000000,
                100d);
        }

        private static object Field(object value, string name)
        {
            FieldInfo field = value.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(value);
        }

        private static DamageActionResult ExecuteSerenaDamage(
            CombatActionExecutor executor,
            Registration registration,
            PartyBattleState party,
            CharacterBattleState serena,
            AttackType attackType,
            ActionOrigin origin)
        {
            long rootActionId = registration.ActionIds.Next();
            var root = new DamageAction(
                rootActionId,
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    serena,
                    party,
                    registration.Boss,
                    ElementType.Water,
                    AttackType.Match,
                    AttackTag.None,
                    origin == ActionOrigin.Match ? 1d : 0d,
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: false));
            if (origin == ActionOrigin.Match)
            {
                return (DamageActionResult)executor.Execute(
                    new CombatActionQueue(new CombatAction[] { root }))
                    .ActionResults.Single();
            }

            var followUp = new DamageAction(
                registration.ActionIds.Next(),
                origin,
                new DamageContextBuildRequest(
                    serena,
                    party,
                    registration.Boss,
                    ElementType.Water,
                    attackType,
                    AttackTag.None,
                    1d,
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: false),
                rootActionId,
                rootActionId);
            return executor.Execute(new CombatActionQueue(
                    new CombatAction[] { root, followUp }))
                .ActionResults
                .OfType<DamageActionResult>()
                .Single(result => ReferenceEquals(result.Action, followUp));
        }

        private static void Bind(
            ActiveAbilityCooldownReductionHandle handle,
            ActiveAbilityRuntimeState state)
        {
            MethodInfo method = handle.GetType().GetMethod(
                "Bind",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(handle, new object[] { state });
        }

        private static BoardState CreateBoard()
        {
            int[] elements =
            {
                3, 0, 1, 2, 0,
                0, 1, 3, 2, 1,
                4, 4, 0, 1, 4,
                2, 1, 0, 0, 4,
                0, 2, 1, 2, 1,
                0, 4, 4, 0, 3
            };
            var board = new BoardState();
            for (int index = 0; index < elements.Length; index++)
            {
                board.Set(
                    BoardPosition.FromIndex(index),
                    new BoardBlock(
                        index + 1,
                        BoardBlockType.Normal,
                        (ElementType)elements[index]));
            }

            return board;
        }

        private sealed class ResolverSink
            : IBattleBoardElementConversionSink
        {
            private BoardState board;

            public ResolverSink(BoardState board)
            {
                this.board = board;
            }

            public bool CanAcceptElementConversion => true;

            public bool TryConvertElements(
                ElementType targetElement,
                int requestedCount,
                out BoardElementConversionResult result)
            {
                result = new BoardElementConversionResolver(
                    new SeededRandomSource(20261029),
                    new BoardMoveAnalyzer()).Resolve(
                        board,
                        targetElement,
                        requestedCount);
                if (result.Succeeded)
                {
                    board = result.Board;
                }

                return result.Succeeded;
            }
        }

        private sealed class NoOpActiveProvider
            : IActiveAbilityActionProvider
        {
            public System.Collections.Generic.IReadOnlyList<CombatAction>
                CreateRootActions(ActiveAbilityActionContext context)
            {
                return new CombatAction[]
                {
                    new HealAction(
                        context.ActionIds.Next(),
                        ActionOrigin.Active,
                        new HealingContextBuildRequest(
                            context.Character,
                            context.Party,
                            healingCoefficient: 0d,
                            appliesCombo: false,
                            finalComboCount: 0))
                };
            }
        }

        private sealed class SerenaChaseAfterKoharuMatchRule
            : ICombatTriggerRule
        {
            private readonly CharacterBattleState koharu;
            private readonly CharacterBattleState serena;
            private readonly CombatActionIdSequence actionIds;
            private bool created;

            public SerenaChaseAfterKoharuMatchRule(
                CharacterBattleState koharu,
                CharacterBattleState serena,
                CombatActionIdSequence actionIds)
            {
                this.koharu = koharu;
                this.serena = serena;
                this.actionIds = actionIds;
            }

            public System.Collections.Generic.IReadOnlyList<CombatAction>
                CreateDerivedActions(CombatActionTriggerContext context)
            {
                if (created
                    || !(context.CompletedResult is DamageActionResult result)
                    || !(result.Action is DamageAction damage)
                    || damage.Origin != ActionOrigin.Match
                    || !ReferenceEquals(
                        damage.ContextRequest.Attacker,
                        koharu))
                {
                    return Array.Empty<CombatAction>();
                }

                created = true;
                long id = actionIds.Next();
                return new CombatAction[]
                {
                    new DamageAction(
                        id,
                        ActionOrigin.Chase,
                        new DamageContextBuildRequest(
                            serena,
                            context.Party,
                            context.Boss,
                            ElementType.Water,
                            AttackType.Chase,
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

        private sealed class Registration
        {
            public Registration(
                CharacterBattleState character,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders,
                CharacterCombatProviderRegistrationResult result)
            {
                Character = character;
                Boss = boss;
                ActionIds = actionIds;
                MatchProviders = matchProviders;
                ActiveProviders = activeProviders;
                Result = result;
            }

            public CharacterBattleState Character { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public MatchEventActionProviderRegistry MatchProviders { get; }
            public ActiveAbilityActionProviderRegistry ActiveProviders { get; }
            public CharacterCombatProviderRegistrationResult Result { get; }
        }
    }
}
