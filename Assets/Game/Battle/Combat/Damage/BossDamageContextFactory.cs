using System;
using ValorChronicle.Battle.Combat.Modifiers;

namespace ValorChronicle.Battle.Combat.Damage
{
    public static class BossDamageContextFactory
    {
        public static BossDamageContext Build(
            BossDamageContextBuildRequest request,
            double nextBossHitDamageReductionRate = 0d)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (double.IsNaN(nextBossHitDamageReductionRate)
                || double.IsInfinity(nextBossHitDamageReductionRate)
                || nextBossHitDamageReductionRate < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nextBossHitDamageReductionRate),
                    nextBossHitDamageReductionRate,
                    "Damage reduction rate must be finite and non-negative.");
            }

            BossAttackModifierSnapshot modifiers =
                CombatModifierCollector.CollectBossAttack(
                    request.Boss,
                    request.Party);
            double partyDamageReductionRateSum =
                modifiers.PartyDamageReductionRateSum
                    + nextBossHitDamageReductionRate;
            if (double.IsInfinity(partyDamageReductionRateSum))
            {
                throw new OverflowException(
                    "Party damage reduction rate sum must remain finite.");
            }

            return new BossDamageContext(
                request.Boss.Attack,
                modifiers.AttackIncreaseRateSum,
                modifiers.AttackReductionRateSum,
                request.AttackCoefficient,
                modifiers.DealtDamageIncreaseRateSum,
                modifiers.DealtDamageReductionRateSum,
                modifiers.PartyTakenDamageIncreaseRateSum,
                partyDamageReductionRateSum);
        }
    }
}
