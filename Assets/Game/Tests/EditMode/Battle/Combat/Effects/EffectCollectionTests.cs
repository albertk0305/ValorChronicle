using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Effects;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Effects
{
    public sealed class EffectCollectionTests
    {
        [Test]
        public void RefreshDuration_KeepsMagnitudeAndRefreshesDuration()
        {
            var effects = new EffectCollection();
            EffectInstance existing = Effect(1, 1, magnitude: 0.2d, turns: 1);
            effects.ApplyEffect(existing);

            EffectInstance active = effects.ApplyEffect(
                Effect(2, 2, magnitude: 0.2d, turns: 3));

            Assert.That(effects.Count, Is.EqualTo(1));
            Assert.That(active, Is.SameAs(existing));
            Assert.That(active.Magnitude, Is.EqualTo(0.2d));
            Assert.That(active.RemainingTurns, Is.EqualTo(3));
        }

        [Test]
        public void RefreshDuration_RejectsMagnitudeMismatch()
        {
            var effects = new EffectCollection();
            EffectInstance existing = Effect(1, 1, magnitude: 0.2d);
            effects.ApplyEffect(existing);

            Assert.Throws<InvalidOperationException>(() =>
                effects.ApplyEffect(Effect(2, 2, magnitude: 0.3d)));
            Assert.That(existing.Magnitude, Is.EqualTo(0.2d));
            Assert.That(effects.Count, Is.EqualTo(1));
        }

        [Test]
        public void SeparateInstance_KeepsIndependentOrderedInstances()
        {
            var effects = new EffectCollection();
            EffectInstance later = Effect(
                2,
                2,
                magnitude: 0.3d,
                turns: 3,
                policy: EffectStackPolicy.SeparateInstance);
            EffectInstance earlier = Effect(
                1,
                1,
                magnitude: 0.2d,
                turns: 1,
                policy: EffectStackPolicy.SeparateInstance);
            effects.ApplyEffect(later);
            effects.ApplyEffect(earlier);

            IReadOnlyList<EffectInstance> active =
                effects.GetActiveEffects();
            Assert.That(active, Has.Count.EqualTo(2));
            Assert.That(active[0], Is.SameAs(earlier));
            Assert.That(active[1], Is.SameAs(later));
            Assert.That(active[0].RemainingTurns, Is.EqualTo(1));
            Assert.That(active[1].RemainingTurns, Is.EqualTo(3));
        }

        [Test]
        public void StackMagnitude_AddsWithoutImplicitCapAndRefreshes()
        {
            var effects = new EffectCollection();
            EffectInstance existing = Effect(
                1,
                1,
                magnitude: 0.7d,
                turns: 1,
                policy: EffectStackPolicy.StackMagnitude);
            effects.ApplyEffect(existing);

            effects.ApplyEffect(Effect(
                2,
                2,
                magnitude: 0.8d,
                turns: 4,
                policy: EffectStackPolicy.StackMagnitude));

            Assert.That(existing.Magnitude, Is.EqualTo(1.5d));
            Assert.That(existing.RemainingTurns, Is.EqualTo(4));
            Assert.That(effects.Count, Is.EqualTo(1));
        }

        [Test]
        public void ReplaceWithStronger_ReplacesOnlyWithStrongerEffect()
        {
            var effects = new EffectCollection();
            EffectInstance original = Effect(
                1,
                1,
                magnitude: 0.2d,
                policy: EffectStackPolicy.ReplaceWithStronger);
            EffectInstance stronger = Effect(
                2,
                2,
                magnitude: 0.4d,
                policy: EffectStackPolicy.ReplaceWithStronger);
            effects.ApplyEffect(original);

            Assert.That(effects.ApplyEffect(stronger), Is.SameAs(stronger));
            Assert.That(effects.ApplyEffect(Effect(
                3,
                3,
                magnitude: 0.1d,
                policy: EffectStackPolicy.ReplaceWithStronger)),
                Is.SameAs(stronger));
            Assert.That(effects.GetActiveEffects(),
                Is.EqualTo(new[] { stronger }));
        }

        [Test]
        public void ReplaceWithStronger_EqualMagnitudeKeepsOldAndLongerDuration()
        {
            var effects = new EffectCollection();
            EffectInstance existing = Effect(
                1,
                1,
                magnitude: 0.3d,
                turns: 2,
                policy: EffectStackPolicy.ReplaceWithStronger);
            effects.ApplyEffect(existing);

            EffectInstance active = effects.ApplyEffect(Effect(
                2,
                2,
                magnitude: 0.3d,
                turns: 5,
                policy: EffectStackPolicy.ReplaceWithStronger));

            Assert.That(active, Is.SameAs(existing));
            Assert.That(active.RuntimeId, Is.EqualTo(1L));
            Assert.That(active.RemainingTurns, Is.EqualTo(5));
        }

        [Test]
        public void ReplaceWithNewest_AlwaysUsesIncomingInstance()
        {
            var effects = new EffectCollection();
            effects.ApplyEffect(Effect(
                1,
                1,
                magnitude: 0.9d,
                policy: EffectStackPolicy.ReplaceWithNewest));
            EffectInstance newest = Effect(
                2,
                2,
                magnitude: 0.1d,
                policy: EffectStackPolicy.ReplaceWithNewest);

            Assert.That(effects.ApplyEffect(newest), Is.SameAs(newest));
            Assert.That(effects.GetActiveEffects(),
                Is.EqualTo(new[] { newest }));
        }

        [Test]
        public void Unique_IgnoresSecondApplicationWithoutChangingExisting()
        {
            var effects = new EffectCollection();
            EffectInstance existing = Effect(
                1,
                1,
                magnitude: 0.2d,
                turns: 1,
                policy: EffectStackPolicy.Unique);
            effects.ApplyEffect(existing);

            EffectInstance active = effects.ApplyEffect(Effect(
                2,
                2,
                magnitude: 0.8d,
                turns: 5,
                policy: EffectStackPolicy.Unique));

            Assert.That(active, Is.SameAs(existing));
            Assert.That(active.Magnitude, Is.EqualTo(0.2d));
            Assert.That(active.RemainingTurns, Is.EqualTo(1));
        }

        [Test]
        public void StackCount_IncreasesToMaximumAndRefreshesDuration()
        {
            var effects = new EffectCollection();
            EffectInstance existing = Effect(
                1,
                1,
                magnitude: 0.1d,
                turns: 1,
                policy: EffectStackPolicy.StackCount,
                maxStacks: 3);
            effects.ApplyEffect(existing);

            effects.ApplyEffect(StackCandidate(2, 2, 2));
            effects.ApplyEffect(StackCandidate(3, 3, 3));
            effects.ApplyEffect(StackCandidate(4, 4, 4));

            Assert.That(existing.StackCount, Is.EqualTo(3));
            Assert.That(existing.EffectiveMagnitude,
                Is.EqualTo(0.3d).Within(0.000000001d));
            Assert.That(existing.RemainingTurns, Is.EqualTo(4));
        }

        [Test]
        public void ProcessTurnEnd_DecrementsThenRemovesExpiredEffect()
        {
            var effects = new EffectCollection();
            EffectInstance finite = Effect(1, 1, turns: 2);
            EffectInstance indefinite = Effect(
                2,
                2,
                effectId: "permanent",
                turns: null);
            effects.ApplyEffect(finite);
            effects.ApplyEffect(indefinite);

            effects.ProcessTurnEnd();
            Assert.That(finite.RemainingTurns, Is.EqualTo(1));
            Assert.That(effects.Count, Is.EqualTo(2));

            effects.ProcessTurnEnd();
            Assert.That(finite.IsExpired, Is.True);
            Assert.That(effects.GetActiveEffects(),
                Is.EqualTo(new[] { indefinite }));
        }

        [Test]
        public void ApplyEffect_IsImmediatelyVisibleAndSupportsRemoveAndClear()
        {
            var effects = new EffectCollection();
            EffectInstance effect = Effect(1, 1);

            effects.ApplyEffect(effect);
            Assert.That(effects.FindByEffectId("effect"),
                Is.EqualTo(new[] { effect }));
            Assert.That(effects.RemoveEffect(1), Is.True);
            Assert.That(effects.GetActiveEffects(), Is.Empty);

            effects.ApplyEffect(Effect(2, 2, effectId: "other"));
            effects.Clear();
            Assert.That(effects.Count, Is.Zero);
        }

        private static EffectInstance StackCandidate(
            long runtimeId,
            long creationOrder,
            int turns)
        {
            return Effect(
                runtimeId,
                creationOrder,
                magnitude: 0.1d,
                turns: turns,
                policy: EffectStackPolicy.StackCount,
                maxStacks: 3);
        }

        private static EffectInstance Effect(
            long runtimeId,
            long creationOrder,
            string effectId = "effect",
            double magnitude = 0.2d,
            int? turns = 2,
            EffectStackPolicy policy = EffectStackPolicy.RefreshDuration,
            int maxStacks = 1)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "source",
                EffectCategory.Buff,
                EffectModifierType.AttackIncrease,
                magnitude,
                turns,
                creationOrder,
                policy,
                1,
                maxStacks);
        }
    }
}
