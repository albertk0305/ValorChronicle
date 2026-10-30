using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonExposureIntegrationTests
    {
        [Test]
        public void AwakeningSixMatch5ExecutesDamageMarksThenExposure()
        {
            using (var battle = new ExposureBattle(6))
            {
                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));

                Assert.That(execution.ActionResults.Count, Is.EqualTo(5));
                Assert.That(execution.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                string[] effectIds = execution.ActionResults.Skip(1)
                    .Cast<ApplyEffectActionResult>()
                    .Select(result => result.AppliedEffect.EffectId)
                    .ToArray();
                Assert.That(effectIds, Is.EqualTo(new[]
                {
                    BaekHyeonRules.MarkEffectId,
                    BaekHyeonRules.MarkEffectId,
                    BaekHyeonRules.MarkEffectId,
                    BaekHyeonRules.ExposureEffectId
                }));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(3));
                Assert.That(battle.Exposure().RemainingTurns, Is.EqualTo(2));
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void AwakeningZeroThroughFiveNeverAppliesExposure(int awakening)
        {
            using (var battle = new ExposureBattle(awakening))
            {
                battle.Execute(battle.Match(BoardMatchTier.FiveOrMore));

                Assert.That(battle.TryExposure(out _), Is.False);
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(3));
            }
        }

        [TestCase(BoardMatchTier.Three, 1)]
        [TestCase(BoardMatchTier.Four, 2)]
        public void AwakeningSixMatch3And4DoNotApplyExposure(
            BoardMatchTier tier,
            int expectedMarks)
        {
            using (var battle = new ExposureBattle(6))
            {
                battle.Execute(battle.Match(tier));

                Assert.That(battle.TryExposure(out _), Is.False);
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(expectedMarks));
            }
        }

        [Test]
        public void ZeroAppliedDamageAddsMarksButNotExposure()
        {
            using (var battle = new ExposureBattle(6, attack: 0d))
            {
                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));

                Assert.That(execution.ActionResults
                    .OfType<DamageActionResult>().Single().AppliedDamage,
                    Is.Zero);
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(3));
                Assert.That(battle.TryExposure(out _), Is.False);
            }
        }

        [Test]
        public void NewlyAppliedExposureDoesNotAffectItsOwnMatchDamage()
        {
            using (var battle = new ExposureBattle(6))
            {
                DamageActionResult damage = battle.Execute(
                        battle.Match(BoardMatchTier.FiveOrMore))
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(battle.TryExposure(out _), Is.True);
            }
        }

        [Test]
        public void ExistingExposureAffectsMatchAndRefreshesAfterDamage()
        {
            using (var battle = new ExposureBattle(6))
            {
                battle.ApplyExposure(9000L);
                battle.Boss.Effects.ProcessTurnEnd();

                DamageActionResult damage = battle.Execute(
                        battle.Match(BoardMatchTier.FiveOrMore))
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.08d).Within(0.000000001d));
                Assert.That(battle.Boss.Effects.FindByEffectId(
                    BaekHyeonRules.ExposureEffectId).Count, Is.EqualTo(1));
                Assert.That(battle.Exposure().Magnitude, Is.EqualTo(0.08d));
                Assert.That(battle.Exposure().RemainingTurns, Is.EqualTo(2));
            }
        }

        [Test]
        public void SubsequentActiveReceivesExposureBonus()
        {
            using (var battle = new ExposureBattle(6))
            {
                battle.Execute(battle.Match(BoardMatchTier.FiveOrMore));

                DamageActionResult active = battle.Execute(battle.Active())
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(active.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.08d).Within(0.000000001d));
            }
        }

        [Test]
        public void TurnEndExpiresExposureButKeepsIndefiniteMarks()
        {
            using (var battle = new ExposureBattle(6))
            {
                battle.Execute(battle.Match(BoardMatchTier.FiveOrMore));
                var processor = new BattleTurnEndProcessor(
                    battle.Party,
                    battle.Boss);

                processor.ProcessTurnEnd();
                Assert.That(battle.Exposure().RemainingTurns, Is.EqualTo(1));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(3));

                processor.ProcessTurnEnd();
                Assert.That(battle.TryExposure(out _), Is.False);
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(3));
            }
        }

        [Test]
        public void MarkAndExposureStackAndMarkRemovalPreservesExposure()
        {
            using (var battle = new ExposureBattle(6))
            {
                battle.AddMarks(5);
                battle.ApplyExposure(9000L);
                var consumption = new BaekHyeonMarkConsumptionRule(
                    battle.Config,
                    6,
                    battle.ActionIds);

                CombatActionExecutionResult execution = battle.Execute(
                    new CombatAction[] { battle.Additional() },
                    consumption);
                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>().Single();

                Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.255d).Within(0.000000001d));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.Zero);
                Assert.That(battle.TryExposure(out _), Is.True);
            }
        }

        [Test]
        public void ExposureStacksWithOtherUnfilteredTargetModifier()
        {
            using (var battle = new ExposureBattle(6))
            {
                battle.ApplyExposure(9000L);
                battle.Boss.Effects.ApplyEffect(new EffectInstance(
                    9001L,
                    "effect_test_other_taken_damage",
                    "test_source",
                    EffectCategory.Debuff,
                    EffectModifierType.TargetTakenDamageIncrease,
                    0.10d,
                    remainingTurns: null,
                    creationOrder: 9001L));

                DamageActionResult damage = battle.Execute(battle.Active())
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.18d).Within(0.000000001d));
            }
        }

        [Test]
        public void LethalMatchKeepsExistingTerminalActionPolicy()
        {
            using (var battle = new ExposureBattle(6, bossHp: 1L))
            {
                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));

                Assert.That(execution.BossDefeated, Is.True);
                Assert.That(execution.CompletedActionCount, Is.EqualTo(1));
                Assert.That(execution.ClearedRemainingActions, Is.True);
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.Zero);
                Assert.That(battle.TryExposure(out _), Is.False);
            }
        }

        [Test]
        public void DamageDerivedAttackRunsBeforePendingMarksAndExposure()
        {
            using (var battle = new ExposureBattle(6))
            {
                var derivedAttackRule = new DerivedAdditionalRule(
                    battle.Character,
                    battle.ActionIds);

                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore),
                    derivedAttackRule);
                DamageActionResult[] damages = execution.ActionResults
                    .OfType<DamageActionResult>().ToArray();

                Assert.That(damages.Length, Is.EqualTo(2));
                Assert.That(((DamageAction)damages[1].Action)
                    .ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Additional));
                Assert.That(damages[1].Context
                    .TargetTakenDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(battle.TryExposure(out _), Is.True);
            }
        }

        private sealed class ExposureBattle : IDisposable
        {
            private readonly BaekHyeonResolvedCombatRules rules;
            private readonly BaekHyeonExposureApplicationRule exposureRule;
            private long nextEffectId = 1000L;

            public ExposureBattle(
                int awakening,
                double attack = 100d,
                long bossHp = 100000L)
            {
                Config = BaekHyeonTestConfig.Create();
                rules = BaekHyeonCombatRuleResolver.Resolve(
                    Config,
                    awakening);
                Character = new CharacterBattleState(
                    BaekHyeonRules.CharacterId,
                    0,
                    ElementType.Grass,
                    1000L,
                    attack);
                Party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    bossHp,
                    100d);
                ActionIds = new CombatActionIdSequence();
                MatchProvider = new BaekHyeonMatchActionProvider(
                    Config,
                    awakening);
                ActiveProvider = new BaekHyeonActiveActionProvider(
                    Config,
                    awakening);
                exposureRule = new BaekHyeonExposureApplicationRule(
                    Config,
                    awakening,
                    ActionIds);
            }

            public BaekHyeonCombatConfig Config { get; }
            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public BaekHyeonMatchActionProvider MatchProvider { get; }
            public BaekHyeonActiveActionProvider ActiveProvider { get; }

            public IReadOnlyList<CombatAction> Match(BoardMatchTier tier)
            {
                return MatchProvider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    tier,
                    1,
                    ActionIds);
            }

            public IReadOnlyList<CombatAction> Active()
            {
                return ActiveProvider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    ActionIds);
            }

            public DamageAction Additional()
            {
                return new DamageAction(
                    ActionIds.Next(),
                    ActionOrigin.Additional,
                    new DamageContextBuildRequest(
                        Character,
                        Party,
                        Boss,
                        ElementType.Grass,
                        AttackType.Additional,
                        AttackTag.None,
                        1d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false),
                    9000L,
                    9000L);
            }

            public void AddMarks(int count)
            {
                for (int index = 0; index < count; index++)
                {
                    Boss.Effects.ApplyEffect(
                        BaekHyeonMarkEffectFactory.Create(
                            nextEffectId++,
                            rules));
                }
            }

            public void ApplyExposure(long runtimeId)
            {
                Boss.Effects.ApplyEffect(
                    BaekHyeonExposureEffectFactory.Create(
                        runtimeId,
                        rules));
            }

            public bool TryExposure(out EffectInstance exposure)
            {
                IReadOnlyList<EffectInstance> matches =
                    Boss.Effects.FindByEffectId(
                        BaekHyeonRules.ExposureEffectId);
                exposure = matches.Count == 1 ? matches[0] : null;
                return exposure != null;
            }

            public EffectInstance Exposure()
            {
                Assert.That(TryExposure(out EffectInstance exposure), Is.True);
                return exposure;
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions,
                params ICombatTriggerRule[] additionalRules)
            {
                var triggerRules = new ICombatTriggerRule[
                    1 + additionalRules.Length];
                triggerRules[0] = exposureRule;
                Array.Copy(
                    additionalRules,
                    0,
                    triggerRules,
                    1,
                    additionalRules.Length);
                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(new SeededRandomSource(123)),
                    new CombatTriggerResolver(triggerRules));
                return executor.Execute(new CombatActionQueue(actions));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Config);
            }
        }

        private sealed class DerivedAdditionalRule : ICombatTriggerRule
        {
            private readonly CharacterBattleState attacker;
            private readonly CombatActionIdSequence actionIds;
            private bool created;

            public DerivedAdditionalRule(
                CharacterBattleState attacker,
                CombatActionIdSequence actionIds)
            {
                this.attacker = attacker;
                this.actionIds = actionIds;
            }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (created
                    || !(context.CompletedResult is DamageActionResult result)
                    || !(result.Action is DamageAction damage)
                    || damage.Origin != ActionOrigin.Match)
                {
                    return Array.Empty<CombatAction>();
                }

                created = true;
                return new CombatAction[]
                {
                    new DamageAction(
                        actionIds.Next(),
                        ActionOrigin.Additional,
                        new DamageContextBuildRequest(
                            attacker,
                            context.Party,
                            context.Boss,
                            ElementType.Grass,
                            AttackType.Additional,
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
    }
}
