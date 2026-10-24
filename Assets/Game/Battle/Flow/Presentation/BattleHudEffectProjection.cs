using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.Fiora;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleHudEffectProjection
    {
        public BattleHudEffectProjection(
            string effectId,
            string badgeText,
            bool badgeVisible)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                throw new ArgumentException(
                    "Effect ID cannot be null or whitespace.",
                    nameof(effectId));
            }

            EffectId = effectId;
            BadgeText = badgeText ?? string.Empty;
            BadgeVisible = badgeVisible;
        }

        public string EffectId { get; }
        public string BadgeText { get; }
        public bool BadgeVisible { get; }
    }

    /// <summary>
    /// Creates presentation-only effect rows without changing combat effect
    /// instances or their lifetime. Grouping is opt-in by effect ID because a
    /// modifier group is a calculation contract, not a general HUD contract.
    /// </summary>
    public sealed class BattleHudEffectProjector
    {
        private readonly HashSet<string> cappedMagnitudeEffectIds;

        public BattleHudEffectProjector(
            IEnumerable<string> cappedMagnitudeEffectIds)
        {
            if (cappedMagnitudeEffectIds == null)
            {
                throw new ArgumentNullException(
                    nameof(cappedMagnitudeEffectIds));
            }

            this.cappedMagnitudeEffectIds = new HashSet<string>(
                cappedMagnitudeEffectIds,
                StringComparer.Ordinal);
            if (this.cappedMagnitudeEffectIds.Contains(null)
                || this.cappedMagnitudeEffectIds.Contains(string.Empty))
            {
                throw new ArgumentException(
                    "Projection effect IDs cannot be null or empty.",
                    nameof(cappedMagnitudeEffectIds));
            }
        }

        public static BattleHudEffectProjector CreateDefault()
        {
            return new BattleHudEffectProjector(
                new[] { FioraAmberwingRules.PassiveBlessingEffectId });
        }

        public IReadOnlyList<BattleHudEffectProjection> Project(
            IReadOnlyList<EffectInstance> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                return Array.Empty<BattleHudEffectProjection>();
            }

            var projections = new List<BattleHudEffectProjection>(
                effects.Count);
            var projectedGroups = new HashSet<string>(
                StringComparer.Ordinal);
            for (int index = 0; index < effects.Count; index++)
            {
                EffectInstance effect = effects[index];
                if (effect == null)
                {
                    continue;
                }

                if (!cappedMagnitudeEffectIds.Contains(effect.EffectId))
                {
                    projections.Add(CreateDefaultProjection(effect));
                    continue;
                }

                if (!projectedGroups.Add(effect.EffectId))
                {
                    continue;
                }

                projections.Add(CreateCappedMagnitudeProjection(
                    effect.EffectId,
                    effects));
            }

            return new ReadOnlyCollection<BattleHudEffectProjection>(
                projections);
        }

        private static BattleHudEffectProjection CreateDefaultProjection(
            EffectInstance effect)
        {
            if (effect.StackPolicy == EffectStackPolicy.StackCount)
            {
                return new BattleHudEffectProjection(
                    effect.EffectId,
                    effect.StackCount.ToString(CultureInfo.InvariantCulture),
                    badgeVisible: true);
            }

            if (effect.RemainingTurns.HasValue)
            {
                return new BattleHudEffectProjection(
                    effect.EffectId,
                    effect.RemainingTurns.Value.ToString(
                        CultureInfo.InvariantCulture),
                    badgeVisible: true);
            }

            return new BattleHudEffectProjection(
                effect.EffectId,
                string.Empty,
                badgeVisible: false);
        }

        private static BattleHudEffectProjection
            CreateCappedMagnitudeProjection(
                string effectId,
                IReadOnlyList<EffectInstance> effects)
        {
            double total = 0d;
            double? cap = null;
            for (int index = 0; index < effects.Count; index++)
            {
                EffectInstance effect = effects[index];
                if (effect == null
                    || !string.Equals(
                        effect.EffectId,
                        effectId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                total += effect.EffectiveMagnitude;
                if (effect.ModifierGroupCap.HasValue)
                {
                    cap = cap.HasValue
                        ? Math.Min(cap.Value, effect.ModifierGroupCap.Value)
                        : effect.ModifierGroupCap.Value;
                }
            }

            double applied = cap.HasValue
                ? Math.Min(total, cap.Value)
                : total;
            double percentage = Math.Round(
                applied * 100d,
                MidpointRounding.AwayFromZero);
            string badge = percentage.ToString(
                "0",
                CultureInfo.InvariantCulture);
            return new BattleHudEffectProjection(
                effectId,
                badge,
                badgeVisible: true);
        }
    }
}
