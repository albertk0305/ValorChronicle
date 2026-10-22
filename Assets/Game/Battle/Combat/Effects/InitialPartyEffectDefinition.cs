using System;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Effects
{
    public sealed class InitialPartyEffectDefinition
    {
        public InitialPartyEffectDefinition(
            string effectId,
            string sourceId,
            EffectCategory category,
            EffectModifierType modifierType,
            double magnitude,
            int? remainingTurns,
            EffectStackPolicy stackPolicy =
                EffectStackPolicy.RefreshDuration,
            int stackCount = 1,
            int maxStackCount = 1,
            ElementType? elementFilter = null,
            AttackTypeMask attackTypeMask = AttackTypeMask.None,
            AttackTag requiredAttackTags = AttackTag.None)
        {
            var validated = new EffectInstance(
                runtimeId: 1,
                effectId,
                sourceId,
                category,
                modifierType,
                magnitude,
                remainingTurns,
                creationOrder: 1,
                stackPolicy,
                stackCount,
                maxStackCount,
                elementFilter,
                attackTypeMask,
                requiredAttackTags);
            EffectId = validated.EffectId;
            SourceId = validated.SourceId;
            Category = validated.Category;
            ModifierType = validated.ModifierType;
            Magnitude = validated.Magnitude;
            RemainingTurns = validated.RemainingTurns;
            StackPolicy = validated.StackPolicy;
            StackCount = validated.StackCount;
            MaxStackCount = validated.MaxStackCount;
            ElementFilter = validated.ElementFilter;
            AttackTypeMask = validated.AttackTypeMask;
            RequiredAttackTags = validated.RequiredAttackTags;
        }

        public string EffectId { get; }
        public string SourceId { get; }
        public EffectCategory Category { get; }
        public EffectModifierType ModifierType { get; }
        public double Magnitude { get; }
        public int? RemainingTurns { get; }
        public EffectStackPolicy StackPolicy { get; }
        public int StackCount { get; }
        public int MaxStackCount { get; }
        public ElementType? ElementFilter { get; }
        public AttackTypeMask AttackTypeMask { get; }
        public AttackTag RequiredAttackTags { get; }

        internal EffectInstance CreateInstance(
            long runtimeId,
            long creationOrder)
        {
            return new EffectInstance(
                runtimeId,
                EffectId,
                SourceId,
                Category,
                ModifierType,
                Magnitude,
                RemainingTurns,
                creationOrder,
                StackPolicy,
                StackCount,
                MaxStackCount,
                ElementFilter,
                AttackTypeMask,
                RequiredAttackTags);
        }
    }
}
