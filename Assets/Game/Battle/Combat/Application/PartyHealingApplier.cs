using System;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Application
{
    public static class PartyHealingApplier
    {
        public static PartyHealingApplicationResult Apply(
            PartyBattleState party,
            HealingResult healingResult)
        {
            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (healingResult == null)
            {
                throw new ArgumentNullException(nameof(healingResult));
            }

            long requestedHealing = healingResult.FinalHealing;
            long hpBefore = party.CurrentHp;
            bool wasFullBefore = hpBefore == party.MaxHp;
            long appliedHealing = party.IsIncapacitated
                ? 0L
                : party.ApplyHpHealing(requestedHealing);
            long overhealAmount = requestedHealing - appliedHealing;
            return new PartyHealingApplicationResult(
                requestedHealing,
                appliedHealing,
                overhealAmount,
                hpBefore,
                party.CurrentHp,
                wasFullBefore,
                party.CurrentHp == party.MaxHp);
        }
    }
}
