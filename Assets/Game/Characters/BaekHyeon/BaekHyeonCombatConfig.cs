using UnityEngine;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.BaekHyeon
{
    [CreateAssetMenu(
        fileName = "BaekHyeonCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Baek Hyeon")]
    public sealed class BaekHyeonCombatConfig
        : CombatConfigDefinition
    {
        public const AttackTypeMask RequiredMarkDamageAttackTypes =
            AttackTypeMask.Additional | AttackTypeMask.Chase;

        [SerializeField] private int initialMarkCount;
        [SerializeField] private int markMaxStackCount = 10;
        [SerializeField] private int match3MarkGainAmount = 1;
        [SerializeField] private int match4MarkGainAmount = 2;
        [SerializeField] private int match5MarkGainAmount = 3;
        [SerializeField] private double markDamageIncreaseRate = 0.03d;
        [SerializeField] private double awakening3MarkDamageIncreaseRate =
            0.035d;
        [SerializeField] private AttackTypeMask markDamageAttackTypes =
            RequiredMarkDamageAttackTypes;
        [SerializeField]
        private int minimumActualDamageToConsumeMarks = 1;

        [SerializeField] private double match3BaseCoefficient = 0.70d;
        [SerializeField] private double match4BaseCoefficient = 1.25d;
        [SerializeField] private double match5BaseCoefficient = 2.00d;
        [SerializeField] private double match3MarkCoefficient = 0.03d;
        [SerializeField] private double match4MarkCoefficient = 0.05d;
        [SerializeField] private double match5MarkCoefficient = 0.08d;
        [SerializeField]
        private double awakening4Match3BaseCoefficient = 0.75d;
        [SerializeField]
        private double awakening4Match4BaseCoefficient = 1.35d;

        [SerializeField] private double activeBaseCoefficient = 2.50d;
        [SerializeField] private double activeMarkCoefficient = 0.20d;
        [SerializeField] private int activeCooldownTurns = 8;
        [SerializeField]
        private double awakening5ActiveBaseCoefficient = 2.80d;
        [SerializeField]
        private double awakening5ActiveMarkCoefficient = 0.22d;

        [SerializeField] private double exposureDamageIncreaseRate = 0.08d;
        [SerializeField] private int exposureDurationTurns = 2;
        [SerializeField] private int exposureMaxStackCount = 1;
        [SerializeField] private EffectStackPolicy exposureStackPolicy =
            EffectStackPolicy.RefreshDuration;
        [SerializeField] private AttackTypeMask exposureAttackTypeMask =
            AttackTypeMask.None;

        public int InitialMarkCount => initialMarkCount;
        public int MarkMaxStackCount => markMaxStackCount;
        public int? MarkRemainingTurns => null;
        public EffectStackPolicy MarkStackPolicy =>
            EffectStackPolicy.StackCount;
        public int Match3MarkGainAmount => match3MarkGainAmount;
        public int Match4MarkGainAmount => match4MarkGainAmount;
        public int Match5MarkGainAmount => match5MarkGainAmount;
        public double MarkDamageIncreaseRate => markDamageIncreaseRate;
        public double Awakening3MarkDamageIncreaseRate =>
            awakening3MarkDamageIncreaseRate;
        public AttackTypeMask MarkDamageAttackTypes =>
            markDamageAttackTypes;
        public AttackTypeMask MarkConsumptionAttackTypes =>
            markDamageAttackTypes;
        public int MinimumActualDamageToConsumeMarks =>
            minimumActualDamageToConsumeMarks;
        public bool ConsumeAllMarksOnQualifyingDamage => true;
        public bool ConsumeMarksAfterAttackCompletion => true;
        public bool ApplyMatchMarksAfterDamage => true;

        public double Match3BaseCoefficient => match3BaseCoefficient;
        public double Match4BaseCoefficient => match4BaseCoefficient;
        public double Match5BaseCoefficient => match5BaseCoefficient;
        public double Match3MarkCoefficient => match3MarkCoefficient;
        public double Match4MarkCoefficient => match4MarkCoefficient;
        public double Match5MarkCoefficient => match5MarkCoefficient;
        public double Awakening4Match3BaseCoefficient =>
            awakening4Match3BaseCoefficient;
        public double Awakening4Match4BaseCoefficient =>
            awakening4Match4BaseCoefficient;
        public AttackType MatchAttackType => AttackType.Match;
        public bool MatchUsesCombo => true;

        public double ActiveBaseCoefficient => activeBaseCoefficient;
        public double ActiveMarkCoefficient => activeMarkCoefficient;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public double Awakening5ActiveBaseCoefficient =>
            awakening5ActiveBaseCoefficient;
        public double Awakening5ActiveMarkCoefficient =>
            awakening5ActiveMarkCoefficient;
        public AttackType ActiveAttackType => AttackType.Active;
        public bool ActiveAvailableAtBattleStart => true;
        public bool ActiveUsesCombo => false;

        public double ExposureDamageIncreaseRate =>
            exposureDamageIncreaseRate;
        public int ExposureDurationTurns => exposureDurationTurns;
        public int ExposureMaxStackCount => exposureMaxStackCount;
        public EffectStackPolicy ExposureStackPolicy => exposureStackPolicy;
        public AttackTypeMask ExposureAttackTypeMask =>
            exposureAttackTypeMask;
        public int ExposureMinimumMatchSize => 5;
        public bool ApplyExposureAfterMarkGain => true;

        public override bool TryValidate(out string errorMessage)
        {
            if (initialMarkCount != 0)
            {
                errorMessage = "InitialMarkCount must be zero.";
                return false;
            }

            if (markMaxStackCount <= 0)
            {
                errorMessage =
                    "MarkMaxStackCount must be greater than zero.";
                return false;
            }

            if (!IsValidMarkGain(match3MarkGainAmount)
                || !IsValidMarkGain(match4MarkGainAmount)
                || !IsValidMarkGain(match5MarkGainAmount))
            {
                errorMessage = "Mark gain amounts must be between one and "
                    + "MarkMaxStackCount.";
                return false;
            }

            if (!IsFiniteNonNegative(markDamageIncreaseRate)
                || !IsFiniteNonNegative(
                    awakening3MarkDamageIncreaseRate))
            {
                errorMessage = "Mark damage increase rates must be finite "
                    + "and non-negative.";
                return false;
            }

            if (awakening3MarkDamageIncreaseRate
                < markDamageIncreaseRate)
            {
                errorMessage = "Awakening3MarkDamageIncreaseRate cannot be "
                    + "less than MarkDamageIncreaseRate.";
                return false;
            }

            if (markDamageAttackTypes != RequiredMarkDamageAttackTypes)
            {
                errorMessage = "MarkDamageAttackTypes must contain exactly "
                    + "Additional and Chase.";
                return false;
            }

            if (minimumActualDamageToConsumeMarks <= 0)
            {
                errorMessage = "MinimumActualDamageToConsumeMarks must be "
                    + "greater than zero.";
                return false;
            }

            if (!ValidateCoefficients(out errorMessage))
            {
                return false;
            }

            if (activeCooldownTurns < 0)
            {
                errorMessage = "ActiveCooldownTurns cannot be negative.";
                return false;
            }

            if (!IsFiniteNonNegative(exposureDamageIncreaseRate))
            {
                errorMessage = "ExposureDamageIncreaseRate must be finite "
                    + "and non-negative.";
                return false;
            }

            if (exposureDurationTurns <= 0)
            {
                errorMessage =
                    "ExposureDurationTurns must be greater than zero.";
                return false;
            }

            if (exposureMaxStackCount != 1)
            {
                errorMessage = "ExposureMaxStackCount must be one.";
                return false;
            }

            if (exposureStackPolicy != EffectStackPolicy.RefreshDuration)
            {
                errorMessage = "ExposureStackPolicy must refresh duration.";
                return false;
            }

            if (exposureAttackTypeMask != AttackTypeMask.None)
            {
                errorMessage = "ExposureAttackTypeMask must be unfiltered.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private bool IsValidMarkGain(int value)
        {
            return value > 0 && value <= markMaxStackCount;
        }

        private bool ValidateCoefficients(out string errorMessage)
        {
            double[] values =
            {
                match3BaseCoefficient,
                match4BaseCoefficient,
                match5BaseCoefficient,
                match3MarkCoefficient,
                match4MarkCoefficient,
                match5MarkCoefficient,
                awakening4Match3BaseCoefficient,
                awakening4Match4BaseCoefficient,
                activeBaseCoefficient,
                activeMarkCoefficient,
                awakening5ActiveBaseCoefficient,
                awakening5ActiveMarkCoefficient
            };
            for (int index = 0; index < values.Length; index++)
            {
                if (!IsFiniteNonNegative(values[index]))
                {
                    errorMessage = "All damage coefficients must be finite "
                        + "and non-negative.";
                    return false;
                }
            }

            if (awakening4Match3BaseCoefficient < match3BaseCoefficient
                || awakening4Match4BaseCoefficient < match4BaseCoefficient)
            {
                errorMessage = "Awakening 4 match coefficients cannot be "
                    + "less than their base values.";
                return false;
            }

            if (awakening5ActiveBaseCoefficient < activeBaseCoefficient
                || awakening5ActiveMarkCoefficient
                    < activeMarkCoefficient)
            {
                errorMessage = "Awakening 5 active coefficients cannot be "
                    + "less than their base values.";
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
