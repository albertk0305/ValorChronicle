using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Kragmor
{
    [CreateAssetMenu(
        fileName = "KragmorCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Kragmor")]
    public sealed class KragmorCombatConfig : CombatConfigDefinition
    {
        [SerializeField]
        private double colossusIronFistCoefficient;

        [SerializeField]
        private double rockshardEruptionCoefficient;

        [SerializeField]
        private int rockshardRockCreationCount;

        [SerializeField]
        private int maximumRockCount;

        [SerializeField]
        private double earthCollapseCoefficient;

        [SerializeField]
        private double volcanicCarapaceReductionRate;

        [SerializeField]
        private double coreCompressionReductionRate;

        [SerializeField]
        private double coreExposureIncreaseRate;

        public double ColossusIronFistCoefficient =>
            colossusIronFistCoefficient;
        public double RockshardEruptionCoefficient =>
            rockshardEruptionCoefficient;
        public int RockshardRockCreationCount =>
            rockshardRockCreationCount;
        public int MaximumRockCount => maximumRockCount;
        public double EarthCollapseCoefficient =>
            earthCollapseCoefficient;
        public double VolcanicCarapaceReductionRate =>
            volcanicCarapaceReductionRate;
        public double CoreCompressionReductionRate =>
            coreCompressionReductionRate;
        public double CoreExposureIncreaseRate =>
            coreExposureIncreaseRate;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteNonNegative(colossusIronFistCoefficient))
            {
                errorMessage = "ColossusIronFistCoefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(rockshardEruptionCoefficient))
            {
                errorMessage = "RockshardEruptionCoefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(earthCollapseCoefficient))
            {
                errorMessage = "EarthCollapseCoefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(volcanicCarapaceReductionRate))
            {
                errorMessage = "VolcanicCarapaceReductionRate must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(coreCompressionReductionRate))
            {
                errorMessage = "CoreCompressionReductionRate must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(coreExposureIncreaseRate))
            {
                errorMessage = "CoreExposureIncreaseRate must be finite and non-negative.";
                return false;
            }

            if (rockshardRockCreationCount < 0)
            {
                errorMessage = "RockshardRockCreationCount cannot be negative.";
                return false;
            }

            if (maximumRockCount < 0)
            {
                errorMessage = "MaximumRockCount cannot be negative.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d;
        }
    }
}
