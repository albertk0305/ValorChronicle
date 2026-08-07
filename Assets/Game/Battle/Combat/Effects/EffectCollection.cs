using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Combat.Effects
{
    public sealed class EffectCollection
    {
        private readonly List<EffectInstance> activeEffects =
            new List<EffectInstance>();
        private readonly ReadOnlyCollection<EffectInstance>
            readOnlyActiveEffects;

        public EffectCollection()
        {
            readOnlyActiveEffects = activeEffects.AsReadOnly();
        }

        public int Count => activeEffects.Count;

        public EffectInstance ApplyEffect(EffectInstance effect)
        {
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            EffectInstance existing = FindFirst(effect.EffectId);
            if (existing == null)
            {
                AddNew(effect);
                return effect;
            }

            ValidateMatchingDefinition(existing, effect);
            switch (effect.StackPolicy)
            {
                case EffectStackPolicy.RefreshDuration:
                    if (existing.Magnitude != effect.Magnitude)
                    {
                        throw new InvalidOperationException(
                            "RefreshDuration requires matching magnitude.");
                    }

                    existing.RefreshDuration(effect.RemainingTurns);
                    return existing;

                case EffectStackPolicy.SeparateInstance:
                    AddNew(effect);
                    return effect;

                case EffectStackPolicy.StackMagnitude:
                    existing.AddMagnitude(effect.Magnitude);
                    existing.RefreshDuration(effect.RemainingTurns);
                    return existing;

                case EffectStackPolicy.ReplaceWithStronger:
                    if (effect.Magnitude > existing.Magnitude)
                    {
                        Replace(existing, effect);
                        return effect;
                    }

                    if (effect.Magnitude == existing.Magnitude)
                    {
                        existing.KeepLongerDuration(
                            effect.RemainingTurns);
                    }

                    return existing;

                case EffectStackPolicy.ReplaceWithNewest:
                    Replace(existing, effect);
                    return effect;

                case EffectStackPolicy.Unique:
                    return existing;

                case EffectStackPolicy.StackCount:
                    if (existing.Magnitude != effect.Magnitude)
                    {
                        throw new InvalidOperationException(
                            "StackCount requires matching magnitude.");
                    }

                    existing.IncrementStackCount();
                    existing.RefreshDuration(effect.RemainingTurns);
                    return existing;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(effect),
                        effect.StackPolicy,
                        "Unsupported effect stack policy.");
            }
        }

        public bool RemoveEffect(long runtimeId)
        {
            for (int index = 0; index < activeEffects.Count; index++)
            {
                if (activeEffects[index].RuntimeId == runtimeId)
                {
                    activeEffects.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<EffectInstance> GetActiveEffects()
        {
            return readOnlyActiveEffects;
        }

        public IReadOnlyList<EffectInstance> FindByEffectId(
            string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                throw new ArgumentException(
                    "Effect ID cannot be null or whitespace.",
                    nameof(effectId));
            }

            var matches = new List<EffectInstance>();
            for (int index = 0; index < activeEffects.Count; index++)
            {
                EffectInstance effect = activeEffects[index];
                if (string.Equals(
                    effect.EffectId,
                    effectId,
                    StringComparison.Ordinal))
                {
                    matches.Add(effect);
                }
            }

            return Array.AsReadOnly(matches.ToArray());
        }

        /// <summary>
        /// Decrements finite durations and removes expired effects. Flow should
        /// call this after the boss action and all follow-up work at turn end.
        /// </summary>
        public void ProcessTurnEnd()
        {
            for (int index = 0; index < activeEffects.Count; index++)
            {
                activeEffects[index].ProcessTurnEnd();
            }

            activeEffects.RemoveAll(effect => effect.IsExpired);
        }

        public void Clear()
        {
            activeEffects.Clear();
        }

        private EffectInstance FindFirst(string effectId)
        {
            for (int index = 0; index < activeEffects.Count; index++)
            {
                EffectInstance effect = activeEffects[index];
                if (string.Equals(
                    effect.EffectId,
                    effectId,
                    StringComparison.Ordinal))
                {
                    return effect;
                }
            }

            return null;
        }

        private void AddNew(EffectInstance effect)
        {
            ValidateUniqueRuntimeValues(effect);
            effect.RegisterWithCollection();
            activeEffects.Add(effect);
            activeEffects.Sort((left, right) =>
                left.CreationOrder.CompareTo(right.CreationOrder));
        }

        private void Replace(
            EffectInstance existing,
            EffectInstance replacement)
        {
            ValidateUniqueRuntimeValues(replacement, existing);
            replacement.RegisterWithCollection();
            activeEffects.Remove(existing);
            activeEffects.Add(replacement);
            activeEffects.Sort((left, right) =>
                left.CreationOrder.CompareTo(right.CreationOrder));
        }

        private void ValidateUniqueRuntimeValues(
            EffectInstance candidate,
            EffectInstance ignored = null)
        {
            for (int index = 0; index < activeEffects.Count; index++)
            {
                EffectInstance existing = activeEffects[index];
                if (ReferenceEquals(existing, ignored))
                {
                    continue;
                }

                if (existing.RuntimeId == candidate.RuntimeId)
                {
                    throw new ArgumentException(
                        $"Duplicate effect runtime ID: "
                            + $"{candidate.RuntimeId}.",
                        nameof(candidate));
                }

                if (existing.CreationOrder == candidate.CreationOrder)
                {
                    throw new ArgumentException(
                        $"Duplicate effect creation order: "
                            + $"{candidate.CreationOrder}.",
                        nameof(candidate));
                }
            }
        }

        private static void ValidateMatchingDefinition(
            EffectInstance existing,
            EffectInstance incoming)
        {
            if (existing.Category != incoming.Category
                || existing.ModifierType != incoming.ModifierType
                || existing.StackPolicy != incoming.StackPolicy
                || existing.ElementFilter != incoming.ElementFilter
                || existing.MaxStackCount != incoming.MaxStackCount)
            {
                throw new InvalidOperationException(
                    "Effects with the same ID must share definition metadata.");
            }
        }
    }
}
