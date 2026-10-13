using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics.Acquisition
{
    /// <summary>Applies deterministic relic acquisition rules to one profile.</summary>
    public sealed class RelicAcquisitionProfileUpdater
    {
        public const int InstanceIdGenerationAttemptLimit = 8;

        private readonly IRelicInstanceIdProvider instanceIdProvider;

        public RelicAcquisitionProfileUpdater(
            IRelicInstanceIdProvider instanceIdProvider)
        {
            this.instanceIdProvider = instanceIdProvider
                ?? throw new ArgumentNullException(
                    nameof(instanceIdProvider));
        }

        public bool TryApply(
            ProfileSaveData profile,
            string relicDefinitionId,
            out RelicAcquisitionResult result,
            out RelicAcquisitionPersistenceStatus failureStatus)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            result = null;
            failureStatus =
                RelicAcquisitionPersistenceStatus.InvalidProfile;
            if (string.IsNullOrWhiteSpace(relicDefinitionId))
            {
                failureStatus = RelicAcquisitionPersistenceStatus
                    .InvalidDefinitionId;
                return false;
            }

            if (profile.RelicInstances == null)
            {
                return false;
            }

            if (!RelicAcquisitionTargetResolver.TryResolve(
                    profile.RelicInstances,
                    relicDefinitionId,
                    out RelicAcquisitionTarget target))
            {
                return false;
            }

            if (target.CanIncreaseAwakening)
            {
                RelicInstanceSaveData awakeningTarget =
                    profile.RelicInstances[target.AwakeningTargetIndex];
                int previousAwakening = awakeningTarget.Awakening;
                awakeningTarget.Awakening = checked(
                    previousAwakening + 1);
                result = new RelicAcquisitionResult(
                    RelicAcquisitionKind.AwakeningIncreased,
                    relicDefinitionId,
                    awakeningTarget.InstanceId,
                    previousAwakening,
                    awakeningTarget.Awakening);
                return true;
            }

            var instanceIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < profile.RelicInstances.Count; index++)
            {
                instanceIds.Add(profile.RelicInstances[index].InstanceId);
            }

            if (!TryCreateUniqueInstanceId(
                    instanceIds,
                    out string instanceId))
            {
                failureStatus = RelicAcquisitionPersistenceStatus
                    .InstanceIdGenerationFailed;
                return false;
            }

            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = relicDefinitionId,
                Awakening = SaveRules.RelicMinAwakening,
                EquippedCharacterId = SaveRules.EmptyId,
                EquippedSlotIndex =
                    SaveRules.UnequippedRelicSlotIndex,
                IsLocked = false,
                IsNew = true
            });
            result = new RelicAcquisitionResult(
                target.HasMatchingInstance
                    ? RelicAcquisitionKind
                        .NewInstanceCreatedAfterMaxAwakening
                    : RelicAcquisitionKind.NewInstanceCreated,
                relicDefinitionId,
                instanceId,
                previousAwakening: null,
                SaveRules.RelicMinAwakening);
            return true;
        }

        private bool TryCreateUniqueInstanceId(
            ISet<string> existingInstanceIds,
            out string instanceId)
        {
            for (int attempt = 0;
                attempt < InstanceIdGenerationAttemptLimit;
                attempt++)
            {
                string candidate =
                    instanceIdProvider.CreateRelicInstanceId();
                if (!string.IsNullOrWhiteSpace(candidate)
                    && !existingInstanceIds.Contains(candidate))
                {
                    instanceId = candidate;
                    return true;
                }
            }

            instanceId = null;
            return false;
        }

    }
}
