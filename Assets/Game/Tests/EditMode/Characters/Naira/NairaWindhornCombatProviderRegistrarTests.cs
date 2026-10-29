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
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornCombatProviderRegistrarTests
    {
        [TestCase(0, 0.05d, 0)]
        [TestCase(2, 0.05d, 0)]
        [TestCase(3, 0.06d, 0)]
        [TestCase(5, 0.06d, 0)]
        [TestCase(6, 0.06d, 1)]
        public void RegisterReturnsExpectedProvidersPassiveAndTrigger(
            int awakening,
            double expectedPassive,
            int expectedTriggerCount)
        {
            using (var battle = new RegistrationBattle(awakening))
            {
                CharacterCombatProviderRegistrationResult result =
                    battle.Registration;
                Assert.That(battle.Registrar.CharacterId,
                    Is.EqualTo(NairaWindhornRules.CharacterId));
                Assert.That(battle.Registrar.SupportedCombatConfigType,
                    Is.EqualTo(typeof(NairaWindhornCombatConfig)));
                Assert.That(result.ActiveAbilityId,
                    Is.EqualTo(NairaWindhornRules.ActiveAbilityId));
                Assert.That(result.ActiveCooldownTurns, Is.EqualTo(12));
                Assert.That(result.TriggerRules.Count,
                    Is.EqualTo(expectedTriggerCount));
                Assert.That(result.ShieldConditionalBossEffects.Count,
                    Is.EqualTo(1));

                ShieldConditionalBossEffectDefinition passive =
                    result.ShieldConditionalBossEffects[0];
                Assert.That(passive.EffectId,
                    Is.EqualTo(NairaWindhornRules.SpiritWardEffectId));
                Assert.That(passive.SourceId,
                    Is.EqualTo(NairaWindhornRules.CharacterId));
                Assert.That(passive.Category,
                    Is.EqualTo(EffectCategory.Debuff));
                Assert.That(passive.ModifierType,
                    Is.EqualTo(
                        EffectModifierType.TargetTakenDamageIncrease));
                Assert.That(passive.Magnitude,
                    Is.EqualTo(expectedPassive));
                Assert.That(passive.ModifierGroupId,
                    Is.EqualTo(CombatModifierGroupRules
                        .AllyTargetTakenDamageIncreaseGroupId));
                Assert.That(passive.ModifierGroupCap,
                    Is.EqualTo(CombatModifierGroupRules
                        .AllyTargetTakenDamageIncreaseCap));
                Assert.That(battle.MatchProviders.TryResolve(
                    NairaWindhornRules.CharacterId,
                    out IMatchEventActionProvider matchProvider), Is.True);
                Assert.That(matchProvider,
                    Is.TypeOf<NairaWindhornMatchActionProvider>());
                Assert.That(battle.ActiveProviders.TryResolve(
                    NairaWindhornRules.CharacterId,
                    NairaWindhornRules.ActiveAbilityId,
                    out IActiveAbilityActionProvider activeProvider),
                    Is.True);
                Assert.That(activeProvider,
                    Is.TypeOf<NairaWindhornActiveActionProvider>());
            }
        }

        [Test]
        public void SynchronizerTracksShieldLifecycleWithoutDuplicates()
        {
            using (var battle = new RegistrationBattle(0))
            using (ShieldConditionalBossEffectSynchronizer synchronizer =
                battle.CreateSynchronizer())
            {
                Assert.That(battle.Boss.Effects.Count, Is.Zero);

                battle.AddShield(100L, 1L, remainingTurns: 1);
                battle.AddShield(50L, 2L, remainingTurns: 3);
                Assert.That(battle.Boss.Effects.Count, Is.EqualTo(1));

                battle.Party.Shields.Absorb(120L);
                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(30L));
                Assert.That(battle.Boss.Effects.Count, Is.EqualTo(1));

                battle.Party.Shields.Absorb(30L);
                Assert.That(battle.Boss.Effects.Count, Is.Zero);

                battle.AddShield(40L, 3L, remainingTurns: 1);
                Assert.That(battle.Boss.Effects.Count, Is.EqualTo(1));
                battle.Party.Shields.ProcessTurnEnd();
                Assert.That(battle.Boss.Effects.Count, Is.Zero);
            }
        }

        [Test]
        public void ExistingShieldActivatesImmediatelyAndDisposeStopsRecreation()
        {
            using (var battle = new RegistrationBattle(3))
            {
                battle.AddShield(100L, 1L, remainingTurns: 3);
                var synchronizer = battle.CreateSynchronizer();
                Assert.That(battle.SpiritWard().Magnitude,
                    Is.EqualTo(0.06d));

                synchronizer.Dispose();
                Assert.That(battle.Boss.Effects.Count, Is.Zero);
                battle.AddShield(100L, 2L, remainingTurns: 3);
                Assert.That(battle.Boss.Effects.Count, Is.Zero);
            }
        }

        [TestCase(BoardMatchTier.Four)]
        [TestCase(BoardMatchTier.FiveOrMore)]
        public void MatchShieldActivatesPassiveBeforeFollowingDamage(
            BoardMatchTier tier)
        {
            using (var battle = new RegistrationBattle(0))
            using (battle.CreateSynchronizer())
            {
                CombatActionExecutionResult execution =
                    battle.ExecuteMatch(tier);
                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>()
                    .Single();

                Assert.That(execution.ActionResults[0],
                    Is.TypeOf<ShieldActionResult>());
                Assert.That(damage.Context
                    .TargetTakenDamageIncreaseRateSum, Is.EqualTo(0.05d));
                Assert.That(battle.Boss.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void ActiveAndOtherCharacterShieldBothActivatePassive()
        {
            using (var battle = new RegistrationBattle(0))
            using (battle.CreateSynchronizer())
            {
                battle.ExecuteActive();
                Assert.That(battle.Boss.Effects.Count, Is.EqualTo(1));

                battle.Party.Shields.Absorb(
                    battle.Party.Shields.TotalShield);
                Assert.That(battle.Boss.Effects.Count, Is.Zero);
                battle.AddShield(
                    10L,
                    999L,
                    remainingTurns: 2,
                    sourceId: "other_character_shield");
                Assert.That(battle.Boss.Effects.Count, Is.EqualTo(1));
            }
        }

        [TestCase(0, 0.35d)]
        [TestCase(3, 0.36d)]
        public void CoreExposureRemainsOutsideAllyGroup(
            int awakening,
            double expectedTotal)
        {
            using (var battle = new RegistrationBattle(awakening))
            using (battle.CreateSynchronizer())
            {
                battle.AddShield(10L, 1L, 2);
                battle.Boss.Effects.ApplyEffect(TargetIncrease(
                    9001L,
                    "core_exposure",
                    0.30d));

                Assert.That(battle.ExecuteDamage(AttackType.Match).Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(expectedTotal));
            }
        }

        [Test]
        public void SpiritWardSharesTwentyFivePercentAllyCap()
        {
            using (var battle = new RegistrationBattle(0))
            using (battle.CreateSynchronizer())
            {
                battle.AddShield(10L, 1L, 2);
                battle.Boss.Effects.ApplyEffect(TargetIncrease(
                    9001L,
                    "other_ally_debuff",
                    0.24d,
                    CombatModifierGroupRules
                        .AllyTargetTakenDamageIncreaseGroupId,
                    CombatModifierGroupRules
                        .AllyTargetTakenDamageIncreaseCap));

                Assert.That(battle.ExecuteDamage(AttackType.Match).Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.25d));
            }
        }

        [Test]
        public void RemovingSpiritWardPreservesOtherBossEffects()
        {
            using (var battle = new RegistrationBattle(0))
            using (battle.CreateSynchronizer())
            {
                battle.Boss.Effects.ApplyEffect(TargetIncrease(
                    9001L,
                    "unrelated_boss_effect",
                    0.10d));
                battle.AddShield(10L, 1L, 2);

                battle.Party.Shields.Absorb(10L);

                Assert.That(battle.Boss.Effects.Count, Is.EqualTo(1));
                Assert.That(battle.Boss.Effects.GetActiveEffects()[0]
                        .EffectId,
                    Is.EqualTo("unrelated_boss_effect"));
            }
        }

        [TestCase(AttackType.Match)]
        [TestCase(AttackType.Active)]
        [TestCase(AttackType.Additional)]
        [TestCase(AttackType.Chase)]
        [TestCase(AttackType.DamageOverTime)]
        public void SpiritWardAppliesThroughEveryExistingDamageType(
            AttackType attackType)
        {
            using (var battle = new RegistrationBattle(0))
            using (battle.CreateSynchronizer())
            {
                battle.AddShield(10L, 1L, 2);

                Assert.That(battle.ExecuteDamage(attackType).Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.05d));
            }
        }

        private static EffectInstance TargetIncrease(
            long runtimeId,
            string effectId,
            double magnitude,
            string groupId = null,
            double? groupCap = null)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "test_source",
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                magnitude,
                remainingTurns: null,
                creationOrder: runtimeId,
                stackPolicy: EffectStackPolicy.Unique,
                modifierGroupId: groupId,
                modifierGroupCap: groupCap);
        }

        internal sealed class RegistrationBattle : IDisposable
        {
            private long nextConditionalIdentity = 1000000L;

            public RegistrationBattle(
                int awakening,
                long maxHp = 1000L,
                long bossHp = 1000000L,
                bool includeOtherPartyMember = false)
            {
                Config = NairaWindhornTestConfig.Create();
                Definition = ScriptableObject
                    .CreateInstance<CharacterDefinition>();
                var serialized = new SerializedObject(Definition);
                serialized.FindProperty("id").stringValue =
                    NairaWindhornRules.CharacterId;
                serialized.FindProperty("element").enumValueIndex =
                    (int)ElementType.Grass;
                serialized.FindProperty("level1Hp").intValue = (int)maxHp;
                serialized.FindProperty("level1Attack").intValue = 100;
                serialized.FindProperty("level100Hp").intValue = (int)maxHp;
                serialized.FindProperty("level100Attack").intValue = 100;
                serialized.FindProperty("combatConfig").objectReferenceValue =
                    Config;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Character = new CharacterBattleState(
                    NairaWindhornRules.CharacterId,
                    0,
                    ElementType.Grass,
                    maxHp,
                    100d);
                OtherCharacter = includeOtherPartyMember
                    ? new CharacterBattleState(
                        "character_other",
                        1,
                        ElementType.Grass,
                        1000L,
                        100d)
                    : null;
                Party = new PartyBattleState(includeOtherPartyMember
                    ? new[] { Character, OtherCharacter }
                    : new[] { Character });
                Boss = new BossBattleState(
                    "naira_2d_test_boss",
                    ElementType.Grass,
                    bossHp,
                    1000d);
                ActionIds = new CombatActionIdSequence();
                MatchProviders = new MatchEventActionProviderRegistry();
                ActiveProviders = new ActiveAbilityActionProviderRegistry();
                Registrar = new NairaWindhornCombatProviderRegistrar();
                Registration = Registrar.Register(
                    new BattlePartyMemberInput(
                        NairaWindhornRules.CharacterId,
                        0,
                        level: 1,
                        awakening,
                        Definition),
                    Character,
                    Boss,
                    ActionIds,
                    MatchProviders,
                    ActiveProviders);
                Executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(new SeededRandomSource(123)),
                    new CombatTriggerResolver(Registration.TriggerRules));
            }

            public NairaWindhornCombatConfig Config { get; }
            public CharacterDefinition Definition { get; }
            public CharacterBattleState Character { get; }
            public CharacterBattleState OtherCharacter { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public MatchEventActionProviderRegistry MatchProviders { get; }
            public ActiveAbilityActionProviderRegistry ActiveProviders { get; }
            public NairaWindhornCombatProviderRegistrar Registrar { get; }
            public CharacterCombatProviderRegistrationResult Registration
            {
                get;
            }
            public CombatActionExecutor Executor { get; }

            public ShieldConditionalBossEffectSynchronizer
                CreateSynchronizer()
            {
                return new ShieldConditionalBossEffectSynchronizer(
                    Party.Shields,
                    Boss.Effects,
                    Registration.ShieldConditionalBossEffects[0],
                    () => ++nextConditionalIdentity);
            }

            public void AddShield(
                long amount,
                long identity,
                int remainingTurns,
                string sourceId = "test_shield")
            {
                Party.Shields.Add(new ShieldInstance(
                    identity,
                    sourceId,
                    amount,
                    createdTurn: 1,
                    remainingTurns,
                    creationOrder: identity));
            }

            public EffectInstance SpiritWard()
            {
                return Boss.Effects.FindByEffectId(
                    NairaWindhornRules.SpiritWardEffectId).Single();
            }

            public CombatActionExecutionResult ExecuteMatch(
                BoardMatchTier tier)
            {
                MatchProviders.TryResolve(
                    NairaWindhornRules.CharacterId,
                    out IMatchEventActionProvider provider);
                var naira = (NairaWindhornMatchActionProvider)provider;
                return Execute(naira.CreateActions(
                    Character,
                    Party,
                    Boss,
                    tier,
                    finalComboCount: 1,
                    playerTurnNumber: 1,
                    ActionIds));
            }

            public CombatActionExecutionResult ExecuteActive()
            {
                ActiveProviders.TryResolve(
                    NairaWindhornRules.CharacterId,
                    NairaWindhornRules.ActiveAbilityId,
                    out IActiveAbilityActionProvider provider);
                var naira = (NairaWindhornActiveActionProvider)provider;
                return Execute(naira.CreateActions(
                    Character,
                    Party,
                    playerTurnNumber: 1,
                    ActionIds));
            }

            public DamageActionResult ExecuteDamage(AttackType attackType)
            {
                var action = new DamageAction(
                    ActionIds.Next(),
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        Character,
                        Party,
                        Boss,
                        ElementType.Grass,
                        attackType,
                        AttackTag.None,
                        skillCoefficient: 1d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
                return (DamageActionResult)Execute(
                    new CombatAction[] { action }).ActionResults[0];
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions)
            {
                CombatActionExecutionSession session =
                    Executor.BeginExecution(actions, playerTurnNumber: 1);
                while (session.TryExecuteNext(out _))
                {
                }

                return session.Result;
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Definition);
                UnityEngine.Object.DestroyImmediate(Config);
            }
        }
    }
}
