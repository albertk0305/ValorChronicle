using System;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Characters.Koharu
{
    public static class SakuragiKoharuSpiritCooldownReductionRule
    {
        public static int ApplyAvailableReductions(
            SakuragiKoharuBattleState battleState,
            IActiveAbilityCooldownReducer cooldownReducer,
            int spiritEnergyCost)
        {
            if (battleState == null)
            {
                throw new ArgumentNullException(nameof(battleState));
            }

            if (cooldownReducer == null)
            {
                throw new ArgumentNullException(nameof(cooldownReducer));
            }

            if (spiritEnergyCost <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(spiritEnergyCost));
            }

            int reductionCount = 0;
            while (battleState.SpiritEnergy >= spiritEnergyCost)
            {
                int appliedAmount =
                    cooldownReducer.ReduceRemainingCooldown(1);
                if (appliedAmount == 0)
                {
                    break;
                }

                if (appliedAmount != 1)
                {
                    throw new InvalidOperationException(
                        "A one-turn cooldown reduction must apply either "
                            + "zero or one turn.");
                }

                if (!battleState.TryConsumeSpiritEnergy(spiritEnergyCost))
                {
                    throw new InvalidOperationException(
                        "Spirit Energy changed during cooldown reduction.");
                }

                reductionCount = checked(reductionCount + 1);
            }

            return reductionCount;
        }
    }
}
