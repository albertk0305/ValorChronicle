using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.BaekHyeon;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonCombatProviderRegistrarTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void RegisterReturnsCompleteContract(int awakening)
        {
            RegistrationFixture fixture = Register(awakening, slot: 1);
            CharacterCombatProviderRegistrationResult result =
                fixture.Result;

            Assert.That(fixture.Registrar.CharacterId,
                Is.EqualTo(BaekHyeonRules.CharacterId));
            Assert.That(fixture.Registrar.SupportedCombatConfigType,
                Is.EqualTo(typeof(BaekHyeonCombatConfig)));
            Assert.That(result.HasActiveAbility, Is.True);
            Assert.That(result.ActiveAbilityId,
                Is.EqualTo(BaekHyeonRules.ActiveAbilityId));
            Assert.That(result.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(result.TriggerRules.Count, Is.EqualTo(2));
            Assert.That(result.TriggerRules[0],
                Is.TypeOf<BaekHyeonMarkConsumptionRule>());
            Assert.That(result.TriggerRules[1],
                Is.TypeOf<BaekHyeonExposureApplicationRule>());
            Assert.That(result.ResourceRequirements.Count, Is.Zero);
            Assert.That(result.CombatStatusSources.Count, Is.Zero);
            Assert.That(result.InitialPartyEffects.Count, Is.Zero);
            Assert.That(result.PreMatchQueueActionProviders.Count, Is.Zero);

            Assert.That(fixture.MatchProviders.TryResolve(
                BaekHyeonRules.CharacterId,
                out IMatchEventActionProvider matchProvider), Is.True);
            Assert.That(matchProvider,
                Is.TypeOf<BaekHyeonMatchActionProvider>());
            Assert.That(fixture.ActiveProviders.TryResolve(
                BaekHyeonRules.CharacterId,
                BaekHyeonRules.ActiveAbilityId,
                out IActiveAbilityActionProvider activeProvider), Is.True);
            Assert.That(activeProvider,
                Is.TypeOf<BaekHyeonActiveActionProvider>());
        }

        [Test]
        public void RegisteredProvidersAndTriggersUseSharedActionIds()
        {
            RegistrationFixture fixture = Register(6, slot: 0);

            CombatActionExecutionResult match = fixture.ExecuteMatch(
                BoardMatchTier.FiveOrMore);
            CombatActionExecutionResult active = fixture.ExecuteActive();
            long[] ids = match.ActionResults.Concat(active.ActionResults)
                .Select(result => result.Action.ActionId)
                .ToArray();

            Assert.That(ids,
                Is.EqualTo(new long[] { 1L, 2L, 3L, 4L, 5L, 6L }));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            Assert.That(fixture.ActionIds.LastIssuedId, Is.EqualTo(6L));
            Assert.That(fixture.Boss.Effects.FindByEffectId(
                BaekHyeonRules.ExposureEffectId).Count, Is.EqualTo(1));
        }

        [Test]
        public void AwakeningValuesFlowThroughRegisteredProviders()
        {
            RegistrationFixture awakening0 = Register(0, slot: 0);
            RegistrationFixture awakening4 = Register(4, slot: 0);
            RegistrationFixture awakening5 = Register(5, slot: 0);
            RegistrationFixture awakening6 = Register(6, slot: 0);

            Assert.That(awakening0.MatchDamageCoefficient(
                BoardMatchTier.Three), Is.EqualTo(0.7d));
            Assert.That(awakening4.MatchDamageCoefficient(
                BoardMatchTier.Three), Is.EqualTo(0.75d));
            Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(
                awakening0.Boss), Is.EqualTo(1));
            Assert.That(awakening0.ActiveDamageCoefficient(),
                Is.EqualTo(2.7d).Within(0.000000001d));
            Assert.That(awakening5.ActiveDamageCoefficient(),
                Is.EqualTo(2.8d));

            awakening0.ExecuteMatch(BoardMatchTier.FiveOrMore);
            awakening6.ExecuteMatch(BoardMatchTier.FiveOrMore);
            Assert.That(awakening0.Boss.Effects.FindByEffectId(
                BaekHyeonRules.ExposureEffectId).Count, Is.Zero);
            Assert.That(awakening6.Boss.Effects.FindByEffectId(
                BaekHyeonRules.ExposureEffectId).Count, Is.EqualTo(1));
        }

        [Test]
        public void RegisteredExposureHandlesMaxMarksAndConsecutiveMatches()
        {
            RegistrationFixture fixture = Register(6, slot: 0);
            fixture.AddMarks(10);

            CombatActionExecutionResult first = fixture.ExecuteMatch(
                BoardMatchTier.FiveOrMore);
            ApplyEffectAction firstExposure = first.ActionResults
                .OfType<ApplyEffectActionResult>()
                .Select(result => (ApplyEffectAction)result.Action)
                .Single(action => action.Effect.EffectId
                    == BaekHyeonRules.ExposureEffectId);
            EffectInstance exposure = fixture.Boss.Effects
                .FindByEffectId(BaekHyeonRules.ExposureEffectId)
                .Single();
            long exposureRuntimeId = exposure.RuntimeId;
            Assert.That(exposure.RemainingTurns, Is.EqualTo(2));

            fixture.Boss.Effects.ProcessTurnEnd();
            Assert.That(exposure.RemainingTurns, Is.EqualTo(1));

            CombatActionExecutionResult second = fixture.ExecuteMatch(
                BoardMatchTier.FiveOrMore);
            ApplyEffectAction secondExposure = second.ActionResults
                .OfType<ApplyEffectActionResult>()
                .Select(result => (ApplyEffectAction)result.Action)
                .Single(action => action.Effect.EffectId
                    == BaekHyeonRules.ExposureEffectId);
            EffectInstance refreshedExposure = fixture.Boss.Effects
                .FindByEffectId(BaekHyeonRules.ExposureEffectId)
                .Single();

            Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(
                fixture.Boss), Is.EqualTo(10));
            Assert.That(fixture.Boss.Effects.FindByEffectId(
                BaekHyeonRules.ExposureEffectId).Count, Is.EqualTo(1));
            Assert.That(refreshedExposure.RuntimeId,
                Is.EqualTo(exposureRuntimeId));
            Assert.That(refreshedExposure.RemainingTurns, Is.EqualTo(2));
            Assert.That(firstExposure.RootActionId,
                Is.EqualTo(first.ActionResults[0].Action.ActionId));
            Assert.That(secondExposure.RootActionId,
                Is.EqualTo(second.ActionResults[0].Action.ActionId));
            Assert.That(secondExposure.RootActionId,
                Is.Not.EqualTo(firstExposure.RootActionId));
            Assert.That(fixture.ActionIds.LastIssuedId, Is.EqualTo(10L));
        }

        [Test]
        public void RegisteredConsumptionRemovesMarksAfterOtherAttack()
        {
            RegistrationFixture fixture = Register(3, slot: 1);
            fixture.AddMarks(5);

            CombatActionExecutionResult execution =
                fixture.ExecuteAdditionalAttack();
            DamageActionResult damage = execution.ActionResults
                .OfType<DamageActionResult>()
                .Single(result => ((DamageAction)result.Action)
                    .ContextRequest.AttackType
                    == AttackType.Additional);
            DamageAction additional = (DamageAction)damage.Action;

            Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.175d).Within(0.000000001d));
            Assert.That(additional.RootActionId,
                Is.EqualTo(execution.ActionResults[0].Action.ActionId));
            Assert.That(additional.SourceActionId,
                Is.EqualTo(execution.ActionResults[0].Action.ActionId));
            Assert.That(execution.ActionResults[2],
                Is.TypeOf<RemoveEffectActionResult>());
            Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(
                fixture.Boss), Is.Zero);
        }

        [Test]
        public void LethalMatchSkipsMarksAndExposure()
        {
            RegistrationFixture fixture = Register(
                6,
                slot: 0,
                bossHp: 1L);

            CombatActionExecutionResult execution = fixture.ExecuteMatch(
                BoardMatchTier.FiveOrMore);

            Assert.That(execution.BossDefeated, Is.True);
            Assert.That(execution.ActionResults.Count, Is.EqualTo(1));
            Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(
                fixture.Boss), Is.Zero);
            Assert.That(fixture.Boss.Effects.FindByEffectId(
                BaekHyeonRules.ExposureEffectId).Count, Is.Zero);
        }

        [Test]
        public void SeparateRegistrationsHaveFreshTriggerState()
        {
            RegistrationFixture first = Register(6, slot: 0);
            RegistrationFixture second = Register(6, slot: 0);

            first.ExecuteMatch(BoardMatchTier.FiveOrMore);
            second.ExecuteMatch(BoardMatchTier.FiveOrMore);

            Assert.That(first.Result.TriggerRules[0],
                Is.Not.SameAs(second.Result.TriggerRules[0]));
            Assert.That(first.Result.TriggerRules[1],
                Is.Not.SameAs(second.Result.TriggerRules[1]));
            Assert.That(first.Boss.Effects.FindByEffectId(
                BaekHyeonRules.ExposureEffectId).Count, Is.EqualTo(1));
            Assert.That(second.Boss.Effects.FindByEffectId(
                BaekHyeonRules.ExposureEffectId).Count, Is.EqualTo(1));
        }

        [Test]
        public void SameRegistriesRejectDuplicateRegistration()
        {
            RegistrationFixture fixture = Register(0, slot: 0);
            CharacterDefinition definition = Definition(Config());

            Assert.Throws<InvalidOperationException>(() =>
                fixture.Registrar.Register(
                    Member(definition, 0, slot: 0),
                    fixture.Character,
                    fixture.Boss,
                    fixture.ActionIds,
                    fixture.MatchProviders,
                    fixture.ActiveProviders));
        }

        [Test]
        public void RegisterRejectsMismatchedMemberStateAndConfig()
        {
            BaekHyeonCombatConfig config = Config();
            CharacterDefinition definition = Definition(config);
            var registrar = new BaekHyeonCombatProviderRegistrar();
            BossBattleState boss = Boss();

            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(definition, 0, slot: 1),
                    Character(slot: 0),
                    boss,
                    new CombatActionIdSequence(),
                    new MatchEventActionProviderRegistry(),
                    new ActiveAbilityActionProviderRegistry()));

            CharacterDefinition wrongConfig = Definition(null);
            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(wrongConfig, 0, slot: 0),
                    Character(slot: 0),
                    boss,
                    new CombatActionIdSequence(),
                    new MatchEventActionProviderRegistry(),
                    new ActiveAbilityActionProviderRegistry()));
        }

        private RegistrationFixture Register(
            int awakening,
            int slot,
            long bossHp = 1000000L)
        {
            BaekHyeonCombatConfig config = Config();
            CharacterDefinition definition = Definition(config);
            CharacterBattleState character = Character(slot);
            BossBattleState boss = Boss(bossHp);
            var actionIds = new CombatActionIdSequence();
            var matchProviders = new MatchEventActionProviderRegistry();
            var activeProviders = new ActiveAbilityActionProviderRegistry();
            var registrar = new BaekHyeonCombatProviderRegistrar();
            CharacterCombatProviderRegistrationResult result =
                registrar.Register(
                    Member(definition, awakening, slot),
                    character,
                    boss,
                    actionIds,
                    matchProviders,
                    activeProviders);
            return new RegistrationFixture(
                config,
                awakening,
                registrar,
                result,
                character,
                boss,
                actionIds,
                matchProviders,
                activeProviders);
        }

        private BaekHyeonCombatConfig Config()
        {
            BaekHyeonCombatConfig config = BaekHyeonTestConfig.Create();
            createdObjects.Add(config);
            return config;
        }

        private CharacterDefinition Definition(
            CombatConfigDefinition config)
        {
            CharacterDefinition definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                BaekHyeonRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Grass;
            serialized.FindProperty("level1Hp").intValue = 870;
            serialized.FindProperty("level1Attack").intValue = 170;
            serialized.FindProperty("level100Hp").intValue = 3200;
            serialized.FindProperty("level100Attack").intValue = 980;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static BattlePartyMemberInput Member(
            CharacterDefinition definition,
            int awakening,
            int slot)
        {
            return new BattlePartyMemberInput(
                BaekHyeonRules.CharacterId,
                slot,
                level: 1,
                awakening,
                definition);
        }

        private static CharacterBattleState Character(int slot)
        {
            return new CharacterBattleState(
                BaekHyeonRules.CharacterId,
                slot,
                ElementType.Grass,
                1000L,
                100d);
        }

        private static BossBattleState Boss(long hp = 1000000L)
        {
            return new BossBattleState(
                "registrar_test_boss",
                ElementType.Light,
                hp,
                0d);
        }

        private sealed class RegistrationFixture
        {
            private readonly CombatActionExecutor executor;

            public RegistrationFixture(
                BaekHyeonCombatConfig config,
                int awakening,
                BaekHyeonCombatProviderRegistrar registrar,
                CharacterCombatProviderRegistrationResult result,
                CharacterBattleState character,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders)
            {
                CombatRules = BaekHyeonCombatRuleResolver.Resolve(
                    config,
                    awakening);
                Registrar = registrar;
                Result = result;
                Character = character;
                Boss = boss;
                ActionIds = actionIds;
                MatchProviders = matchProviders;
                ActiveProviders = activeProviders;
                Ally = new CharacterBattleState(
                    "registrar_test_ally",
                    character.PartySlotIndex == 0 ? 1 : 0,
                    ElementType.Fire,
                    1000L,
                    100d);
                Party = new PartyBattleState(
                    new[] { character, Ally }
                        .OrderBy(member => member.PartySlotIndex)
                        .ToArray());
                executor = new CombatActionExecutor(
                    boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(result.TriggerRules));
            }

            public BaekHyeonCombatProviderRegistrar Registrar { get; }
            public BaekHyeonResolvedCombatRules CombatRules { get; }
            public CharacterCombatProviderRegistrationResult Result { get; }
            public CharacterBattleState Character { get; }
            public CharacterBattleState Ally { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public MatchEventActionProviderRegistry MatchProviders { get; }
            public ActiveAbilityActionProviderRegistry ActiveProviders { get; }

            public double MatchDamageCoefficient(BoardMatchTier tier)
            {
                return ExecuteMatch(tier).ActionResults
                    .OfType<DamageActionResult>()
                    .First().Context.SkillCoefficient;
            }

            public double ActiveDamageCoefficient()
            {
                return ExecuteActive().ActionResults
                    .OfType<DamageActionResult>()
                    .Single().Context.SkillCoefficient;
            }

            public CombatActionExecutionResult ExecuteMatch(
                BoardMatchTier tier)
            {
                return Execute(CreateMatch(tier));
            }

            public CombatActionExecutionResult ExecuteActive()
            {
                return Execute(CreateActive());
            }

            public CombatActionExecutionResult ExecuteAdditionalAttack()
            {
                long rootActionId = ActionIds.Next();
                long additionalActionId = ActionIds.Next();
                return Execute(new CombatAction[]
                {
                    new DamageAction(
                        rootActionId,
                        ActionOrigin.Match,
                        new DamageContextBuildRequest(
                            Ally,
                            Party,
                            Boss,
                            ElementType.Fire,
                            AttackType.Match,
                            AttackTag.Match3,
                            1d,
                            appliesCombo: true,
                            finalComboCount: 1,
                            canCritical: false)),
                    new DamageAction(
                        additionalActionId,
                        ActionOrigin.Additional,
                        new DamageContextBuildRequest(
                            Ally,
                            Party,
                            Boss,
                            ElementType.Grass,
                            AttackType.Additional,
                            AttackTag.None,
                            1d,
                            appliesCombo: false,
                            finalComboCount: 0,
                            canCritical: false),
                        rootActionId,
                        rootActionId)
                });
            }

            public void AddMarks(int count)
            {
                for (int index = 0; index < count; index++)
                {
                    long runtimeId = 1000L + index;
                    Boss.Effects.ApplyEffect(
                        BaekHyeonMarkEffectFactory.Create(
                            runtimeId,
                            CombatRules));
                }
            }

            private IReadOnlyList<CombatAction> CreateMatch(
                BoardMatchTier tier)
            {
                MatchProviders.TryResolve(
                    BaekHyeonRules.CharacterId,
                    out IMatchEventActionProvider provider);
                return ((BaekHyeonMatchActionProvider)provider)
                    .CreateActions(
                        Character,
                        Party,
                        Boss,
                        tier,
                        finalComboCount: 1,
                        actionIds: ActionIds);
            }

            private IReadOnlyList<CombatAction> CreateActive()
            {
                ActiveProviders.TryResolve(
                    BaekHyeonRules.CharacterId,
                    BaekHyeonRules.ActiveAbilityId,
                    out IActiveAbilityActionProvider provider);
                return ((BaekHyeonActiveActionProvider)provider)
                    .CreateActions(Character, Party, Boss, ActionIds);
            }

            private CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions)
            {
                return executor.Execute(new CombatActionQueue(actions));
            }
        }
    }
}
