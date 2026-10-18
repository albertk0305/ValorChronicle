using System;
using System.Collections.Generic;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics.Stats
{
    public sealed class RelicStatBonusResolver
    {
        private readonly DefinitionDatabase definitionDatabase;

        public RelicStatBonusResolver(
            DefinitionDatabase definitionDatabase)
        {
            this.definitionDatabase = definitionDatabase
                ?? throw new ArgumentNullException(nameof(definitionDatabase));
        }

        public RelicStatBonusSummary Resolve(
            ProfileSaveData profile,
            CharacterSaveData character)
        {
            if (!definitionDatabase.IsInitialized
                || profile?.RelicInstances == null
                || character?.EquippedRelicInstanceIds == null)
            {
                return RelicStatBonusSummary.None;
            }

            double maxHpIncreaseRateSum = 0d;
            double attackIncreaseRateSum = 0d;
            var sources = new List<RelicStatBonusSource>();
            var resolvedInstanceIds = new HashSet<string>(
                StringComparer.Ordinal);
            int slotCount = Math.Min(
                character.EquippedRelicInstanceIds.Count,
                SaveRules.RelicSlotCountPerCharacter);
            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                string instanceId =
                    character.EquippedRelicInstanceIds[slotIndex];
                if (string.IsNullOrEmpty(instanceId)
                    || !resolvedInstanceIds.Add(instanceId)
                    || !TryFindUniqueInstance(
                        profile.RelicInstances,
                        instanceId,
                        out RelicInstanceSaveData instance)
                    || instance.Awakening < SaveRules.RelicMinAwakening
                    || instance.Awakening > SaveRules.RelicMaxAwakening
                    || !definitionDatabase.TryGetRelic(
                        instance.RelicDefinitionId,
                        out RelicDefinition definition)
                    || definition == null
                    || !Enum.IsDefined(
                        typeof(RelicEffectType),
                        definition.EffectType)
                    || !TryGetValidRate(
                        definition,
                        instance.Awakening,
                        out double rate))
                {
                    continue;
                }

                switch (definition.EffectType)
                {
                    case RelicEffectType.MaxHpIncrease:
                        maxHpIncreaseRateSum += rate;
                        break;
                    case RelicEffectType.AttackIncrease:
                        attackIncreaseRateSum += rate;
                        break;
                }

                sources.Add(new RelicStatBonusSource(
                    definition.Id,
                    definition.DisplayNameKey,
                    definition.DescriptionKey,
                    definition.EffectType,
                    instance.Awakening,
                    rate));
            }

            if (!IsValidRate(maxHpIncreaseRateSum)
                || !IsValidRate(attackIncreaseRateSum))
            {
                return RelicStatBonusSummary.None;
            }

            return new RelicStatBonusSummary(
                maxHpIncreaseRateSum,
                attackIncreaseRateSum,
                sources.AsReadOnly());
        }

        private static bool TryFindUniqueInstance(
            IReadOnlyList<RelicInstanceSaveData> instances,
            string instanceId,
            out RelicInstanceSaveData match)
        {
            match = null;
            for (int index = 0; index < instances.Count; index++)
            {
                RelicInstanceSaveData candidate = instances[index];
                if (candidate == null
                    || !string.Equals(
                        candidate.InstanceId,
                        instanceId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (match != null)
                {
                    match = null;
                    return false;
                }

                match = candidate;
            }

            return match != null;
        }

        private static bool TryGetValidRate(
            RelicDefinition definition,
            int awakening,
            out double rate)
        {
            try
            {
                rate = definition.GetEffectRate(awakening);
                return IsValidRate(rate);
            }
            catch (InvalidOperationException)
            {
                rate = 0d;
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                rate = 0d;
                return false;
            }
        }

        private static bool IsValidRate(double rate)
        {
            return !double.IsNaN(rate)
                && !double.IsInfinity(rate)
                && rate >= 0d;
        }
    }
}
