using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics.Acquisition
{
    public sealed class RelicAcquisitionTarget
    {
        internal RelicAcquisitionTarget(
            bool hasMatchingInstance,
            int awakeningTargetIndex,
            string awakeningTargetInstanceId,
            int currentAwakening)
        {
            HasMatchingInstance = hasMatchingInstance;
            AwakeningTargetIndex = awakeningTargetIndex;
            AwakeningTargetInstanceId = awakeningTargetInstanceId
                ?? string.Empty;
            CurrentAwakening = currentAwakening;
        }

        public bool HasMatchingInstance { get; }
        public bool CanIncreaseAwakening => AwakeningTargetIndex >= 0;
        public bool WillCreateNewInstance => !CanIncreaseAwakening;
        public int AwakeningTargetIndex { get; }
        public string AwakeningTargetInstanceId { get; }
        public int CurrentAwakening { get; }
    }

    public static class RelicAcquisitionTargetResolver
    {
        public static bool TryResolve(
            IReadOnlyList<RelicInstanceSaveData> relicInstances,
            string relicDefinitionId,
            out RelicAcquisitionTarget target)
        {
            target = null;
            if (relicInstances == null
                || string.IsNullOrWhiteSpace(relicDefinitionId))
            {
                return false;
            }

            var instanceIds = new HashSet<string>(StringComparer.Ordinal);
            bool hasMatchingInstance = false;
            int targetIndex = -1;
            int targetAwakening = SaveRules.RelicMinAwakening;

            for (int index = 0; index < relicInstances.Count; index++)
            {
                RelicInstanceSaveData relic = relicInstances[index];
                if (!IsStructurallyUsable(relic)
                    || !instanceIds.Add(relic.InstanceId))
                {
                    return false;
                }

                if (!string.Equals(
                        relic.RelicDefinitionId,
                        relicDefinitionId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                hasMatchingInstance = true;
                if (relic.Awakening >= SaveRules.RelicMaxAwakening)
                {
                    continue;
                }

                if (targetIndex < 0
                    || relic.Awakening > targetAwakening)
                {
                    targetIndex = index;
                    targetAwakening = relic.Awakening;
                }
            }

            if (targetIndex < 0 && hasMatchingInstance)
            {
                targetAwakening = SaveRules.RelicMaxAwakening;
            }

            target = new RelicAcquisitionTarget(
                hasMatchingInstance,
                targetIndex,
                targetIndex >= 0
                    ? relicInstances[targetIndex].InstanceId
                    : string.Empty,
                targetAwakening);
            return true;
        }

        private static bool IsStructurallyUsable(
            RelicInstanceSaveData relic)
        {
            return relic != null
                && !string.IsNullOrWhiteSpace(relic.InstanceId)
                && !string.IsNullOrWhiteSpace(relic.RelicDefinitionId)
                && relic.Awakening >= SaveRules.RelicMinAwakening
                && relic.Awakening <= SaveRules.RelicMaxAwakening;
        }
    }
}
