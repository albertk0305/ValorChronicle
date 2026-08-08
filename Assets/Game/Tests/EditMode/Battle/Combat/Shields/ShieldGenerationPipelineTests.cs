using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Shields
{
    public sealed class ShieldGenerationPipelineTests
    {
        [Test]
        public void Factory_CapturesExecutionTimeModifierSnapshot()
        {
            CharacterBattleState source = Character(4000);
            var party = new PartyBattleState(new[] { source });
            source.Effects.ApplyEffect(Effect(
                1,
                EffectModifierType.ShieldAmountIncrease,
                0.10d));
            var request = new ShieldGenerationContextBuildRequest(
                source,
                party,
                0.25d,
                true,
                3);

            ShieldGenerationContext first =
                ShieldGenerationContextFactory.Build(request);
            party.Effects.ApplyEffect(Effect(
                2,
                EffectModifierType.ShieldAmountIncrease,
                0.08d));
            ShieldGenerationContext second =
                ShieldGenerationContextFactory.Build(request);

            Assert.That(first.ShieldAmountIncreaseRateSum,
                Is.EqualTo(0.10d));
            Assert.That(second.ShieldAmountIncreaseRateSum,
                Is.EqualTo(0.18d).Within(0.000000001d));
            Assert.That(first.ShieldAmountIncreaseRateSum,
                Is.EqualTo(0.10d));
        }

        [Test]
        public void FullPipeline_CollectsCalculatesCreatesAndRegistersShield()
        {
            CharacterBattleState source = Character(6200);
            var party = new PartyBattleState(new[] { source });
            source.Effects.ApplyEffect(Effect(
                1,
                EffectModifierType.ShieldAmountIncrease,
                0.10d));
            party.Effects.ApplyEffect(Effect(
                2,
                EffectModifierType.ShieldAmountIncrease,
                0.05d));

            ShieldGenerationContext context =
                ShieldGenerationContextFactory.Build(
                    new ShieldGenerationContextBuildRequest(
                        source,
                        party,
                        0.07d,
                        true,
                        3));
            ShieldGenerationResult generation =
                ShieldGenerationCalculator.Calculate(context);
            ShieldGrantApplicationResult result = ShieldGrantApplier.Apply(
                party,
                generation,
                new ShieldGrantRequest(10, "source", 4, 3, 20));

            Assert.That(context.ShieldAmountIncreaseRateSum,
                Is.EqualTo(0.15d).Within(0.000000001d));
            Assert.That(generation.RawShieldAmount,
                Is.EqualTo(558.992d).Within(0.000000001d));
            Assert.That(result.RequestedShieldAmount, Is.EqualTo(558L));
            Assert.That(result.GrantedShieldAmount, Is.EqualTo(558L));
            Assert.That(result.TotalShieldBefore, Is.Zero);
            Assert.That(result.TotalShieldAfter, Is.EqualTo(558L));
            Assert.That(result.CreatedShieldRuntimeId, Is.EqualTo(10L));
            Assert.That(party.Shields.ActiveShields, Has.Count.EqualTo(1));
            ShieldInstance shield = party.Shields.ActiveShields[0];
            Assert.That(shield.SourceId, Is.EqualTo("source"));
            Assert.That(shield.CreatedTurn, Is.EqualTo(4));
            Assert.That(shield.RemainingTurns, Is.EqualTo(3));
            Assert.That(shield.CreationOrder, Is.EqualTo(20L));
        }

        [Test]
        public void Apply_AddsAllPositiveShieldWithoutCap()
        {
            CharacterBattleState source = Character(1000);
            var party = new PartyBattleState(new[] { source });
            party.Shields.Add(new ShieldInstance(
                1,
                "old",
                900,
                1,
                null,
                1));
            ShieldGenerationResult generation =
                ShieldGenerationCalculator.Calculate(
                    new ShieldGenerationContext(
                        1000,
                        2d,
                        false,
                        0,
                        0d));

            ShieldGrantApplicationResult result = ShieldGrantApplier.Apply(
                party,
                generation,
                new ShieldGrantRequest(2, "source", 2, null, 2));

            Assert.That(result.TotalShieldBefore, Is.EqualTo(900L));
            Assert.That(result.GrantedShieldAmount, Is.EqualTo(2000L));
            Assert.That(result.TotalShieldAfter, Is.EqualTo(2900L));
            Assert.That(party.Shields.ActiveShields, Has.Count.EqualTo(2));
        }

        [Test]
        public void Apply_ZeroAmountDoesNotCreateShield()
        {
            CharacterBattleState source = Character(1000);
            var party = new PartyBattleState(new[] { source });
            ShieldGenerationResult generation =
                ShieldGenerationCalculator.Calculate(
                    new ShieldGenerationContext(
                        1000,
                        0d,
                        false,
                        0,
                        0d));

            ShieldGrantApplicationResult result = ShieldGrantApplier.Apply(
                party,
                generation,
                new ShieldGrantRequest(1, "source", 1, 2, 1));

            Assert.That(result.RequestedShieldAmount, Is.Zero);
            Assert.That(result.GrantedShieldAmount, Is.Zero);
            Assert.That(result.TotalShieldAfter, Is.Zero);
            Assert.That(result.CreatedShieldRuntimeId, Is.Null);
            Assert.That(party.Shields.ActiveShields, Is.Empty);
        }

        [Test]
        public void FactoryAndApplier_RejectNullInputs()
        {
            CharacterBattleState source = Character(1000);
            var party = new PartyBattleState(new[] { source });
            ShieldGenerationResult generation =
                ShieldGenerationCalculator.Calculate(
                    new ShieldGenerationContext(
                        1000,
                        0.1d,
                        false,
                        0,
                        0d));
            var request = new ShieldGrantRequest(
                1,
                "source",
                1,
                2,
                1);

            Assert.Throws<ArgumentNullException>(() =>
                ShieldGenerationContextFactory.Build(null));
            Assert.Throws<ArgumentNullException>(() =>
                ShieldGrantApplier.Apply(null, generation, request));
            Assert.Throws<ArgumentNullException>(() =>
                ShieldGrantApplier.Apply(party, null, request));
            Assert.Throws<ArgumentNullException>(() =>
                ShieldGrantApplier.Apply(party, generation, null));
        }

        private static CharacterBattleState Character(long maximumHp)
        {
            return new CharacterBattleState(
                "source",
                0,
                ElementType.Water,
                maximumHp,
                100d);
        }

        private static EffectInstance Effect(
            long runtimeId,
            EffectModifierType modifierType,
            double magnitude)
        {
            return new EffectInstance(
                runtimeId,
                $"effect_{runtimeId}",
                "source",
                EffectCategory.Buff,
                modifierType,
                magnitude,
                2,
                runtimeId);
        }
    }
}
