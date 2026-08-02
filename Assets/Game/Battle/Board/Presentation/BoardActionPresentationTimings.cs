using System;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardActionPresentationTimings
    {
        public BoardActionPresentationTimings(
            float swapDuration,
            float removalDuration,
            float collapseDuration,
            float refillDuration,
            float shuffleDuration)
        {
            Validate(swapDuration, nameof(swapDuration));
            Validate(removalDuration, nameof(removalDuration));
            Validate(collapseDuration, nameof(collapseDuration));
            Validate(refillDuration, nameof(refillDuration));
            Validate(shuffleDuration, nameof(shuffleDuration));

            SwapDuration = swapDuration;
            RemovalDuration = removalDuration;
            CollapseDuration = collapseDuration;
            RefillDuration = refillDuration;
            ShuffleDuration = shuffleDuration;
        }

        public float SwapDuration { get; }
        public float RemovalDuration { get; }
        public float CollapseDuration { get; }
        public float RefillDuration { get; }
        public float ShuffleDuration { get; }

        private static void Validate(float value, string parameterName)
        {
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Presentation timing cannot be negative.");
            }
        }
    }
}
