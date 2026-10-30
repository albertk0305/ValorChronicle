using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
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
    public sealed class BaekHyeonExposureEffectFactoryTests
    {
        [Test]
        public void CreateUsesResolvedExposureDefinition()
        {
            using (var scope = new ConfigScope(6))
            {
                EffectInstance effect = BaekHyeonExposureEffectFactory.Create(
                    1L,
                    scope.Rules);

                Assert.That(effect.EffectId,
                    Is.EqualTo(BaekHyeonRules.ExposureEffectId));
                Assert.That(effect.SourceId,
                    Is.EqualTo(BaekHyeonRules.CharacterId));
                Assert.That(effect.Category, Is.EqualTo(EffectCategory.Debuff));
                Assert.That(effect.ModifierType,
                    Is.EqualTo(EffectModifierType.TargetTakenDamageIncrease));
                Assert.That(effect.Magnitude, Is.EqualTo(0.08d));
                Assert.That(effect.RemainingTurns, Is.EqualTo(2));
                Assert.That(effect.StackPolicy,
                    Is.EqualTo(EffectStackPolicy.RefreshDuration));
                Assert.That(effect.StackCount, Is.EqualTo(1));
                Assert.That(effect.MaxStackCount, Is.EqualTo(1));
                Assert.That(effect.AttackTypeMask,
                    Is.EqualTo(AttackTypeMask.None));
                Assert.That(effect.RequiredAttackTags,
                    Is.EqualTo(AttackTag.None));
                Assert.That(effect.ModifierGroupId, Is.Null);
            }
        }

        [Test]
        public void CreateRejectsRulesBeforeAwakeningSix()
        {
            using (var scope = new ConfigScope(5))
            {
                Assert.That(
                    () => BaekHyeonExposureEffectFactory.Create(
                        1L,
                        scope.Rules),
                    Throws.TypeOf<InvalidOperationException>());
            }
        }

        [Test]
        public void ReapplicationRefreshesOneInstanceWithoutStackingMagnitude()
        {
            using (var scope = new ConfigScope(6))
            {
                var boss = Boss();
                EffectInstance first = BaekHyeonExposureEffectFactory.Create(
                    1L,
                    scope.Rules);
                boss.Effects.ApplyEffect(first);
                boss.Effects.ProcessTurnEnd();

                EffectInstance active = boss.Effects.ApplyEffect(
                    BaekHyeonExposureEffectFactory.Create(
                        2L,
                        scope.Rules));

                Assert.That(active, Is.SameAs(first));
                Assert.That(boss.Effects.Count, Is.EqualTo(1));
                Assert.That(active.Magnitude, Is.EqualTo(0.08d));
                Assert.That(active.RemainingTurns, Is.EqualTo(2));
            }
        }

        [Test]
        public void TurnEndExpiresExposureAfterTwoTicks()
        {
            using (var scope = new ConfigScope(6))
            {
                var character = Character();
                var party = new PartyBattleState(new[] { character });
                var boss = Boss();
                EffectInstance exposure =
                    BaekHyeonExposureEffectFactory.Create(
                        1L,
                        scope.Rules);
                boss.Effects.ApplyEffect(exposure);
                var processor = new BattleTurnEndProcessor(party, boss);

                processor.ProcessTurnEnd();
                Assert.That(exposure.RemainingTurns, Is.EqualTo(1));
                Assert.That(boss.Effects.Count, Is.EqualTo(1));

                processor.ProcessTurnEnd();
                Assert.That(boss.Effects.Count, Is.Zero);
            }
        }

        [TestCase(AttackType.Match)]
        [TestCase(AttackType.Active)]
        [TestCase(AttackType.Additional)]
        [TestCase(AttackType.Chase)]
        [TestCase(AttackType.DamageOverTime)]
        public void ExposureAppliesToEveryAttackType(AttackType attackType)
        {
            using (var scope = new ConfigScope(6))
            {
                var character = Character();
                var party = new PartyBattleState(new[] { character });
                var boss = Boss();
                boss.Effects.ApplyEffect(
                    BaekHyeonExposureEffectFactory.Create(
                        100L,
                        scope.Rules));
                ActionOrigin origin = OriginFor(attackType);
                bool followUp = attackType == AttackType.Additional
                    || attackType == AttackType.Chase;
                var action = new DamageAction(
                    1L,
                    origin,
                    new DamageContextBuildRequest(
                        character,
                        party,
                        boss,
                        ElementType.Grass,
                        attackType,
                        AttackTag.None,
                        1d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false),
                    followUp ? 9000L : (long?)null,
                    followUp ? 9000L : (long?)null);
                var executor = new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(new SeededRandomSource(123)));

                DamageActionResult result = executor.Execute(
                        new CombatActionQueue(new CombatAction[] { action }))
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(result.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.08d).Within(0.000000001d));
            }
        }

        private static CharacterBattleState Character()
        {
            return new CharacterBattleState(
                BaekHyeonRules.CharacterId,
                0,
                ElementType.Grass,
                1000L,
                100d);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "training_boss",
                ElementType.Light,
                100000L,
                100d);
        }

        private static ActionOrigin OriginFor(AttackType attackType)
        {
            switch (attackType)
            {
                case AttackType.Match:
                    return ActionOrigin.Match;
                case AttackType.Active:
                    return ActionOrigin.Active;
                case AttackType.Additional:
                    return ActionOrigin.Additional;
                case AttackType.Chase:
                    return ActionOrigin.Chase;
                case AttackType.DamageOverTime:
                    return ActionOrigin.DamageOverTime;
                default:
                    throw new ArgumentOutOfRangeException(nameof(attackType));
            }
        }

        private sealed class ConfigScope : IDisposable
        {
            private readonly BaekHyeonCombatConfig config;

            public ConfigScope(int awakening)
            {
                config = BaekHyeonTestConfig.Create();
                Rules = BaekHyeonCombatRuleResolver.Resolve(
                    config,
                    awakening);
            }

            public BaekHyeonResolvedCombatRules Rules { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
