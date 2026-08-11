using System;
using System.Collections.Generic;

namespace ValorChronicle.Bosses.Kragmor
{
    public sealed class KragmorBattleRuntimeState
    {
        public int PatternIndex { get; private set; }
        public KragmorDefenseState CurrentDefenseState { get; private set; } =
            KragmorDefenseState.VolcanicCarapace;
        public string CurrentDefenseEffectId =>
            KragmorDefenseEffectFactory.GetEffectId(CurrentDefenseState);
        public KragmorActionKind NextActionKind =>
            (KragmorActionKind)PatternIndex;
        public KragmorBossIntent NextIntent =>
            KragmorRules.GetIntent(NextActionKind);

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
                    KragmorRules.GetIntent(actionKind));
            }

            return Array.AsReadOnly(forecast);
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
