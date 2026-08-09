using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Marea
{
    public static class MareaBluefangRules
    {
        public const string CharacterId = "character_marea_bluefang";
        public const string ActiveAbilityId = "active_marea_charge_order";
        public const string ActiveEffectId = "effect_marea_charge_order";
        public const double Match3Coefficient = 0.90d;
        public const double Match4BaseCoefficient = 1.50d;
        public const double Match4WaterElementBonusCoefficient = 0.40d;
        public const double Match5BaseCoefficient = 2.40d;
        public const double Match5CoefficientPerWaterElement = 1.40d;
        public const double PassiveDealtDamageIncreaseRate = 0.15d;
        public const double ActiveWaterDamageIncreaseRate = 0.25d;
        public const int ActiveDurationTurns = 3;
        public const int ActiveCooldownTurns = 8;
        public const int WaterElementMaxAmount =
            WaterElementResource.MaxAmount;
    }
}
