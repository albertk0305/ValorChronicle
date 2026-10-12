using System;
using System.Collections.Generic;
using ValorChronicle.Characters.Acquisition;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Gacha;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Rewards
{
    public sealed class RewardGrantProfileUpdater
    {
        private readonly DefinitionDatabase database;
        private readonly CharacterAcquisitionProfileUpdater characterUpdater;
        private readonly RelicAcquisitionProfileUpdater relicUpdater;
        private readonly SaveDataCloner cloner = new SaveDataCloner();

        public RewardGrantProfileUpdater(
            DefinitionDatabase database,
            IRelicInstanceIdProvider relicInstanceIdProvider)
        {
            this.database = database
                ?? throw new ArgumentNullException(nameof(database));
            if (!database.IsInitialized)
            {
                throw new ArgumentException(
                    "Definition database must be initialized.",
                    nameof(database));
            }

            characterUpdater =
                new CharacterAcquisitionProfileUpdater(database);
            relicUpdater = new RelicAcquisitionProfileUpdater(
                relicInstanceIdProvider);
        }

        public RewardGrantBatchResult Apply(
            ProfileSaveData profile,
            GachaCalculationResult calculation)
        {
            if (calculation == null)
            {
                throw new ArgumentNullException(nameof(calculation));
            }

            IReadOnlyList<GachaPullResult> pulls = calculation.Pulls;
            var requests = new RewardGrantRequest[pulls.Count];
            for (int index = 0; index < pulls.Count; index++)
            {
                GachaPullResult pull = pulls[index];
                if (pull == null || pull.Index != index)
                {
                    throw new ArgumentException(
                        "Gacha pulls must be non-null and ordered by index.",
                        nameof(calculation));
                }

                requests[index] = RewardGrantRequest.FromGachaPull(pull);
            }

            return Apply(profile, requests);
        }

        public RewardGrantBatchResult Apply(
            ProfileSaveData profile,
            IReadOnlyList<RewardGrantRequest> requests)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            if (requests.Count == 0)
            {
                throw new ArgumentException(
                    "At least one reward request is required.",
                    nameof(requests));
            }

            ValidateRequestOrder(requests);
            ProfileSaveData staging = cloner.Clone(profile);
            var results = new RewardGrantResult[requests.Count];
            for (int index = 0; index < requests.Count; index++)
            {
                results[index] = ApplyOne(staging, requests[index]);
            }

            profile.Currencies = staging.Currencies;
            profile.Characters = staging.Characters;
            profile.RelicInstances = staging.RelicInstances;
            return new RewardGrantBatchResult(results);
        }

        private RewardGrantResult ApplyOne(
            ProfileSaveData profile,
            RewardGrantRequest request)
        {
            if (request.Amount <= 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.Amount,
                    "Reward amount must be positive.");
            }

            switch (request.RewardKind)
            {
                case GachaRewardKind.RandomCharacter:
                    return ApplyCharacter(profile, request);
                case GachaRewardKind.RandomRelic:
                    return ApplyRelic(profile, request);
                case GachaRewardKind.HeroTokens:
                    return ApplyCurrency(
                        profile,
                        request,
                        RewardCurrencyKind.HeroTokens);
                case GachaRewardKind.RelicTokens:
                    return ApplyCurrency(
                        profile,
                        request,
                        RewardCurrencyKind.RelicTokens);
                case GachaRewardKind.BattleRecords:
                    return ApplyCurrency(
                        profile,
                        request,
                        RewardCurrencyKind.BattleRecords);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(request),
                        request.RewardKind,
                        "Reward kind is invalid.");
            }
        }

        private RewardGrantResult ApplyCharacter(
            ProfileSaveData profile,
            RewardGrantRequest request)
        {
            ValidateContentReward(request, "character");
            CharacterAcquisitionResult result = characterUpdater.Apply(
                profile,
                request.RewardId);
            RewardGrantKind kind;
            switch (result.Kind)
            {
                case CharacterAcquisitionKind.NewCharacter:
                    kind = RewardGrantKind.NewCharacter;
                    break;
                case CharacterAcquisitionKind.AwakeningIncreased:
                    kind = RewardGrantKind.CharacterAwakeningIncreased;
                    break;
                case CharacterAcquisitionKind
                        .MaxAwakeningConvertedToHeroTokens:
                    kind = RewardGrantKind
                        .MaxAwakeningCharacterConvertedToHeroTokens;
                    break;
                default:
                    throw new InvalidOperationException(
                        "Character acquisition returned an invalid kind.");
            }

            return new RewardGrantResult(
                request,
                kind,
                result.CharacterId,
                string.Empty,
                result.PreviousAwakening,
                result.NewAwakening,
                result.CurrencyGrant,
                false);
        }

        private RewardGrantResult ApplyRelic(
            ProfileSaveData profile,
            RewardGrantRequest request)
        {
            ValidateContentReward(request, "relic");
            if (!database.TryGetRelic(request.RewardId, out _))
            {
                throw new KeyNotFoundException(
                    $"Relic definition was not found: {request.RewardId}.");
            }

            if (!relicUpdater.TryApply(
                    profile,
                    request.RewardId,
                    out RelicAcquisitionResult result,
                    out RelicAcquisitionPersistenceStatus failure))
            {
                throw new InvalidOperationException(
                    $"Relic acquisition was rejected: {failure}.");
            }

            bool awakened = result.Kind
                == RelicAcquisitionKind.AwakeningIncreased;
            return new RewardGrantResult(
                request,
                awakened
                    ? RewardGrantKind.RelicAwakeningIncreased
                    : RewardGrantKind.NewRelicInstanceCreated,
                result.RelicDefinitionId,
                result.AffectedInstanceId,
                result.PreviousAwakening,
                result.NewAwakening,
                null,
                result.Kind == RelicAcquisitionKind
                    .NewInstanceCreatedAfterMaxAwakening);
        }

        private static RewardGrantResult ApplyCurrency(
            ProfileSaveData profile,
            RewardGrantRequest request,
            RewardCurrencyKind currencyKind)
        {
            CurrencyGrantResult result = CurrencyProfileUpdater.Add(
                profile,
                currencyKind,
                request.Amount);
            return new RewardGrantResult(
                request,
                RewardGrantKind.CurrencyGranted,
                string.Empty,
                string.Empty,
                null,
                null,
                result,
                false);
        }

        private static void ValidateRequestOrder(
            IReadOnlyList<RewardGrantRequest> requests)
        {
            for (int index = 0; index < requests.Count; index++)
            {
                if (requests[index] == null
                    || requests[index].Index != index)
                {
                    throw new ArgumentException(
                        "Reward requests must be non-null and ordered by "
                            + "index.",
                        nameof(requests));
                }
            }
        }

        private static void ValidateContentReward(
            RewardGrantRequest request,
            string contentType)
        {
            if (request.Amount != 1L)
            {
                throw new ArgumentException(
                    $"A {contentType} reward amount must be one.",
                    nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.RewardId))
            {
                throw new ArgumentException(
                    $"A {contentType} reward ID is required.",
                    nameof(request));
            }
        }
    }
}
