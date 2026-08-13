using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Bosses.Kragmor
{
    public sealed class KragmorBattleRuntimeState : IBossVisualStateSource
    {
        private readonly KragmorCombatConfig config;
        private readonly KragmorBossIntent[] intents;

        public KragmorBattleRuntimeState(KragmorCombatConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            this.config = config;
            intents = new[]
            {
                new KragmorBossIntent(
                    KragmorActionKind.ColossusIronFist,
                    KragmorRules.GetSkillId(
                        KragmorActionKind.ColossusIronFist),
                    true,
                    false,
                    config.ColossusIronFistCoefficient),
                new KragmorBossIntent(
                    KragmorActionKind.RockshardEruption,
                    KragmorRules.GetSkillId(
                        KragmorActionKind.RockshardEruption),
                    true,
                    false,
                    config.RockshardEruptionCoefficient),
                new KragmorBossIntent(
                    KragmorActionKind.CoreCompression,
                    KragmorRules.GetSkillId(
                        KragmorActionKind.CoreCompression),
                    false,
                    false,
                    0d),
                new KragmorBossIntent(
                    KragmorActionKind.EarthCollapse,
                    KragmorRules.GetSkillId(
                        KragmorActionKind.EarthCollapse),
                    true,
                    true,
                    config.EarthCollapseCoefficient)
            };
        }

        public KragmorCombatConfig Config => config;
        public int PatternIndex { get; private set; }
        public KragmorDefenseState CurrentDefenseState { get; private set; } =
            KragmorDefenseState.VolcanicCarapace;
        public string CurrentDefenseEffectId =>
            KragmorDefenseEffectFactory.GetEffectId(CurrentDefenseState);
        public string CurrentVisualStateId =>
            KragmorRules.GetVisualStateId(CurrentDefenseState);
        public KragmorActionKind NextActionKind =>
            (KragmorActionKind)PatternIndex;
        public KragmorBossIntent NextIntent =>
            ResolveIntent(NextActionKind);

        public IReadOnlyList<KragmorBossIntentPreview> GetIntentForecast(
            int count)
        {
            if (count <= 0)
            {
                return Array.Empty<KragmorBossIntentPreview>();
            }

            var forecast = new KragmorBossIntentPreview[count];
            for (int offset = 0; offset < forecast.Length; offset++)
            {
                int patternIndex =
                    (PatternIndex + offset) % KragmorRules.PatternCount;
                var actionKind = (KragmorActionKind)patternIndex;
                forecast[offset] = new KragmorBossIntentPreview(
                    offset + 1,
                    ResolveIntent(actionKind));
            }

            return Array.AsReadOnly(forecast);
        }

        private KragmorBossIntent ResolveIntent(
            KragmorActionKind actionKind)
        {
            if (!Enum.IsDefined(typeof(KragmorActionKind), actionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(actionKind));
            }

            return intents[(int)actionKind];
        }

        public void CommitAction(
            KragmorDefenseState completedDefenseState)
        {
            KragmorDefenseState expectedState = ResolveCompletedDefenseState();
            if (completedDefenseState != expectedState)
            {
                throw new InvalidOperationException(
                    $"Action {NextActionKind} must complete in defense state "
                        + $"{expectedState}, not {completedDefenseState}.");
            }

            CurrentDefenseState = completedDefenseState;
            PatternIndex = (PatternIndex + 1) % KragmorRules.PatternCount;
        }

        private KragmorDefenseState ResolveCompletedDefenseState()
        {
            switch (NextActionKind)
            {
                case KragmorActionKind.ColossusIronFist:
                case KragmorActionKind.RockshardEruption:
                    return KragmorDefenseState.VolcanicCarapace;
                case KragmorActionKind.CoreCompression:
                    return KragmorDefenseState.CoreCompression;
                case KragmorActionKind.EarthCollapse:
                    return KragmorDefenseState.CoreExposure;
                default:
                    throw new InvalidOperationException(
                        "Kragmor pattern contains an unsupported action.");
            }
        }
    }
}
