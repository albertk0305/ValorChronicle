using System;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Application
{
    public static class BossHealthDamageApplier
    {
        public static BossDamageApplicationResult Apply(
            BossBattleState boss,
            DamageResult damageResult)
        {
            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (damageResult == null)
            {
                throw new ArgumentNullException(nameof(damageResult));
            }

            long requestedDamage = damageResult.FinalDamage;
            long hpBefore = boss.CurrentHp;
            bool wasDefeatedBefore = boss.IsDefeated;
            if (wasDefeatedBefore)
            {
                return new BossDamageApplicationResult(
                    requestedDamage,
                    0,
                    0,
                    hpBefore,
                    hpBefore,
                    true,
                    true);
            }

            long appliedDamage = boss.ApplyDamage(requestedDamage);
            long overkillDamage = requestedDamage - appliedDamage;
            return new BossDamageApplicationResult(
                requestedDamage,
                appliedDamage,
                overkillDamage,
                hpBefore,
                boss.CurrentHp,
                false,
                boss.IsDefeated);
        }
    }
}
