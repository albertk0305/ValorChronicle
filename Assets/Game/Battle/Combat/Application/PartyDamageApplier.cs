using System;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Application
{
    public static class PartyDamageApplier
    {
        public static PartyDamageApplicationResult Apply(
            PartyBattleState party,
            BossDamageResult damageResult)
        {
            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (damageResult == null)
            {
                throw new ArgumentNullException(nameof(damageResult));
            }

            long requestedDamage = damageResult.FinalDamageBeforeShield;
            long hpBefore = party.CurrentHp;
            long totalShieldBefore = party.Shields.TotalShield;
            bool wasIncapacitatedBefore = party.IsIncapacitated;
            if (wasIncapacitatedBefore)
            {
                return new PartyDamageApplicationResult(
                    requestedDamage,
                    0,
                    0,
                    0,
                    hpBefore,
                    hpBefore,
                    totalShieldBefore,
                    totalShieldBefore,
                    true,
                    true);
            }

            ShieldAbsorptionResult absorption =
                party.Shields.Absorb(requestedDamage);
            long hpDamage = party.ApplyHpDamage(
                absorption.RemainingDamage);
            long overkillDamage = absorption.RemainingDamage - hpDamage;
            return new PartyDamageApplicationResult(
                requestedDamage,
                absorption.AbsorbedDamage,
                hpDamage,
                overkillDamage,
                hpBefore,
                party.CurrentHp,
                absorption.TotalShieldBefore,
                absorption.TotalShieldAfter,
                false,
                party.IsIncapacitated);
        }
    }
}
