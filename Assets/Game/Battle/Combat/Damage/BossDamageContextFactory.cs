using System;
using ValorChronicle.Battle.Combat.Modifiers;

namespace ValorChronicle.Battle.Combat.Damage
{
    public static class BossDamageContextFactory
    {
        public static BossDamageContext Build(
            BossDamageContextBuildRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            BossAttackModifierSnapshot modifiers =
                CombatModifierCollector.CollectBossAttack(
                    request.Boss,
                    request.Party);
            return new BossDamageContext(
                request.Boss.Attack,
                modifiers.AttackIncreaseRateSum,
                modifiers.AttackReductionRateSum,
                request.AttackCoefficient,
                modifiers.DealtDamageIncreaseRateSum,
                modifiers.DealtDamageReductionRateSum,
                modifiers.PartyTakenDamageIncreaseRateSum,
                modifiers.PartyDamageReductionRateSum);
        }
    }
}
