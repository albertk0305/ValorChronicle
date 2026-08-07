using System;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Damage
{
    public static class ElementAffinityResolver
    {
        public const double AdvantageMultiplier = 1.30d;
        public const double DisadvantageMultiplier = 0.70d;
        public const double NeutralMultiplier = 1.00d;

        public static double Resolve(
            ElementType attackElement,
            ElementType targetElement)
        {
            ValidateElement(attackElement, nameof(attackElement));
            ValidateElement(targetElement, nameof(targetElement));

            if (IsAdvantage(attackElement, targetElement))
            {
                return AdvantageMultiplier;
            }

            if (IsDisadvantage(attackElement, targetElement))
            {
                return DisadvantageMultiplier;
            }

            return NeutralMultiplier;
        }

        private static bool IsAdvantage(
            ElementType attackElement,
            ElementType targetElement)
        {
            return (attackElement == ElementType.Fire
                    && targetElement == ElementType.Grass)
                || (attackElement == ElementType.Water
                    && targetElement == ElementType.Fire)
                || (attackElement == ElementType.Grass
                    && targetElement == ElementType.Water)
                || (attackElement == ElementType.Light
                    && targetElement == ElementType.Dark)
                || (attackElement == ElementType.Dark
                    && targetElement == ElementType.Light);
        }

        private static bool IsDisadvantage(
            ElementType attackElement,
            ElementType targetElement)
        {
            return (attackElement == ElementType.Fire
                    && targetElement == ElementType.Water)
                || (attackElement == ElementType.Water
                    && targetElement == ElementType.Grass)
                || (attackElement == ElementType.Grass
                    && targetElement == ElementType.Fire);
        }

        private static void ValidateElement(
            ElementType element,
            string parameterName)
        {
            if (!Enum.IsDefined(typeof(ElementType), element))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    element,
                    "Element must be a defined value.");
            }
        }
    }
}
