using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Effects
{
    public sealed class ShieldConditionalBossEffectDefinition
    {
        public ShieldConditionalBossEffectDefinition(
            string effectId,
            string sourceId,
            EffectCategory category,
            EffectModifierType modifierType,
            double magnitude,
            string modifierGroupId = null,
            double? modifierGroupCap = null)
        {
            var validated = new EffectInstance(
                runtimeId: 1,
                effectId,
                sourceId,
                category,
                modifierType,
                magnitude,
                remainingTurns: null,
                creationOrder: 1,
                EffectStackPolicy.Unique,
                modifierGroupId: modifierGroupId,
                modifierGroupCap: modifierGroupCap);
            EffectId = validated.EffectId;
            SourceId = validated.SourceId;
            Category = validated.Category;
            ModifierType = validated.ModifierType;
            Magnitude = validated.Magnitude;
            ModifierGroupId = validated.ModifierGroupId;
            ModifierGroupCap = validated.ModifierGroupCap;
        }

        public string EffectId { get; }
        public string SourceId { get; }
        public EffectCategory Category { get; }
        public EffectModifierType ModifierType { get; }
        public double Magnitude { get; }
        public string ModifierGroupId { get; }
        public double? ModifierGroupCap { get; }

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
                remainingTurns: null,
                creationOrder,
                EffectStackPolicy.Unique,
                modifierGroupId: ModifierGroupId,
                modifierGroupCap: ModifierGroupCap);
        }
    }
}
