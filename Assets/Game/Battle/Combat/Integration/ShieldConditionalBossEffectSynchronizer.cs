using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class ShieldConditionalBossEffectSynchronizer : IDisposable
    {
        private readonly ShieldCollection shields;
        private readonly EffectCollection bossEffects;
        private readonly ShieldConditionalBossEffectDefinition definition;
        private readonly Func<long> nextEffectIdentity;
        private bool isDisposed;

        public ShieldConditionalBossEffectSynchronizer(
            ShieldCollection shields,
            EffectCollection bossEffects,
            ShieldConditionalBossEffectDefinition definition,
            Func<long> nextEffectIdentity)
        {
            this.shields = shields
                ?? throw new ArgumentNullException(nameof(shields));
            this.bossEffects = bossEffects
                ?? throw new ArgumentNullException(nameof(bossEffects));
            this.definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
            this.nextEffectIdentity = nextEffectIdentity
                ?? throw new ArgumentNullException(nameof(nextEffectIdentity));

            Synchronize(shields.TotalShield);
            shields.TotalShieldChanged += HandleTotalShieldChanged;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            shields.TotalShieldChanged -= HandleTotalShieldChanged;
            RemoveConditionalEffect();
        }

        private void HandleTotalShieldChanged(long totalShield)
        {
            Synchronize(totalShield);
        }

        private void Synchronize(long totalShield)
        {
            IReadOnlyList<EffectInstance> matches =
                bossEffects.FindByEffectId(definition.EffectId);
            if (totalShield <= 0)
            {
                RemoveEffects(matches);
                return;
            }

            if (matches.Count > 0)
            {
                return;
            }

            long identity = nextEffectIdentity();
            bossEffects.ApplyEffect(definition.CreateInstance(
                identity,
                identity));
        }

        private void RemoveConditionalEffect()
        {
            RemoveEffects(bossEffects.FindByEffectId(definition.EffectId));
        }

        private void RemoveEffects(IReadOnlyList<EffectInstance> effects)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                bossEffects.RemoveEffect(effects[index].RuntimeId);
            }
        }
    }
}
