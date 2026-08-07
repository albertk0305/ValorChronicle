using System;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Effects
{
    public sealed class EffectInstance
    {
        private bool isRegistered;

        public EffectInstance(
            long runtimeId,
            string effectId,
            string sourceId,
            EffectCategory category,
            EffectModifierType modifierType,
            double magnitude,
            int? remainingTurns,
            long creationOrder,
            EffectStackPolicy stackPolicy =
                EffectStackPolicy.RefreshDuration,
            int stackCount = 1,
            int maxStackCount = 1,
            ElementType? elementFilter = null)
        {
            if (runtimeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(runtimeId),
                    runtimeId,
                    "Runtime ID must be positive.");
            }

            ValidateId(effectId, nameof(effectId));
            ValidateId(sourceId, nameof(sourceId));
            ValidateEnum(category, nameof(category));
            ValidateEnum(modifierType, nameof(modifierType));
            ValidateEnum(stackPolicy, nameof(stackPolicy));
            if (modifierType == EffectModifierType.AttackTypeDamageIncrease)
            {
                throw new NotSupportedException(
                    "Attack type modifiers require a confirmed AttackType.");
            }

            if (double.IsNaN(magnitude)
                || double.IsInfinity(magnitude)
                || magnitude < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(magnitude),
                    magnitude,
                    "Magnitude must be finite and non-negative.");
            }

            if (remainingTurns.HasValue
                && remainingTurns.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(remainingTurns),
                    remainingTurns,
                    "Finite remaining turns must be positive.");
            }

            if (creationOrder <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(creationOrder),
                    creationOrder,
                    "Creation order must be positive.");
            }

            ValidateStackValues(
                stackPolicy,
                stackCount,
                maxStackCount);
            ValidateElementFilter(modifierType, elementFilter);

            RuntimeId = runtimeId;
            EffectId = effectId;
            SourceId = sourceId;
            Category = category;
            ModifierType = modifierType;
            StackPolicy = stackPolicy;
            Magnitude = magnitude;
            RemainingTurns = remainingTurns;
            StackCount = stackCount;
            MaxStackCount = maxStackCount;
            CreationOrder = creationOrder;
            ElementFilter = elementFilter;
        }

        public long RuntimeId { get; }
        public string EffectId { get; }
        public string SourceId { get; }
        public EffectCategory Category { get; }
        public EffectModifierType ModifierType { get; }
        public EffectStackPolicy StackPolicy { get; }
        public double Magnitude { get; private set; }
        public int? RemainingTurns { get; private set; }
        public int StackCount { get; private set; }
        public int MaxStackCount { get; }
        public long CreationOrder { get; }
        public ElementType? ElementFilter { get; }
        public bool IsExpired =>
            RemainingTurns.HasValue && RemainingTurns.Value == 0;
        public double EffectiveMagnitude => Magnitude * StackCount;

        internal void RegisterWithCollection()
        {
            if (isRegistered)
            {
                throw new InvalidOperationException(
                    "An effect instance can belong to only one collection.");
            }

            isRegistered = true;
        }

        internal void RefreshDuration(int? remainingTurns)
        {
            RemainingTurns = remainingTurns;
        }

        internal void KeepLongerDuration(int? remainingTurns)
        {
            if (!RemainingTurns.HasValue || !remainingTurns.HasValue)
            {
                RemainingTurns = null;
                return;
            }

            RemainingTurns = Math.Max(
                RemainingTurns.Value,
                remainingTurns.Value);
        }

        internal void AddMagnitude(double magnitude)
        {
            double combined = Magnitude + magnitude;
            if (double.IsInfinity(combined) || double.IsNaN(combined))
            {
                throw new OverflowException(
                    "Combined effect magnitude must remain finite.");
            }

            Magnitude = combined;
        }

        internal void IncrementStackCount()
        {
            if (StackCount < MaxStackCount)
            {
                StackCount++;
            }
        }

        internal void ProcessTurnEnd()
        {
            if (RemainingTurns.HasValue && !IsExpired)
            {
                RemainingTurns--;
            }
        }

        private static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "ID cannot be null or whitespace.",
                    parameterName);
            }
        }

        private static void ValidateStackValues(
            EffectStackPolicy stackPolicy,
            int stackCount,
            int maxStackCount)
        {
            if (stackCount <= 0 || maxStackCount <= 0
                || stackCount > maxStackCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stackCount),
                    stackCount,
                    "Stack count must be between one and its maximum.");
            }

            if (stackPolicy == EffectStackPolicy.StackCount)
            {
                if (stackCount != 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(stackCount),
                        stackCount,
                        "A new stack-count effect must start at one stack.");
                }

                return;
            }

            if (stackCount != 1 || maxStackCount != 1)
            {
                throw new ArgumentException(
                    "Only StackCount effects can configure stack counts.");
            }
        }

        private static void ValidateElementFilter(
            EffectModifierType modifierType,
            ElementType? elementFilter)
        {
            if (modifierType == EffectModifierType.ElementDamageIncrease)
            {
                if (!elementFilter.HasValue)
                {
                    throw new ArgumentException(
                        "Element damage effects require an element filter.",
                        nameof(elementFilter));
                }
            }
            else if (elementFilter.HasValue)
            {
                throw new ArgumentException(
                    "Only element damage effects can use an element filter.",
                    nameof(elementFilter));
            }

            if (elementFilter.HasValue
                && !Enum.IsDefined(
                    typeof(ElementType),
                    elementFilter.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elementFilter),
                    elementFilter,
                    "Element filter must be a defined value.");
            }
        }

        private static void ValidateEnum<TEnum>(
            TEnum value,
            string parameterName)
            where TEnum : struct
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be defined.");
            }
        }
    }
}
