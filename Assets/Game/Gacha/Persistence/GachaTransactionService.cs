using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Rewards;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Gacha.Persistence
{
    public sealed class GachaTransactionService
    {
        private readonly object syncRoot = new object();
        private readonly SaveService saveService;
        private readonly DefinitionDatabase database;
        private readonly GachaCalculator calculator;
        private readonly RewardGrantProfileUpdater rewardUpdater;
        private readonly SaveDataCloner cloner = new SaveDataCloner();
        private readonly SaveDataValueComparer comparer =
            new SaveDataValueComparer();

        public GachaTransactionService(
            SaveService saveService,
            DefinitionDatabase database,
            IRandomSource randomSource,
            IRelicInstanceIdProvider relicInstanceIdProvider)
        {
            this.saveService = saveService
                ?? throw new ArgumentNullException(nameof(saveService));
            this.database = database
                ?? throw new ArgumentNullException(nameof(database));
            if (!database.IsInitialized)
            {
                throw new ArgumentException(
                    "Definition database must be initialized.",
                    nameof(database));
            }

            calculator = new GachaCalculator(database, randomSource);
            rewardUpdater = new RewardGrantProfileUpdater(
                database,
                relicInstanceIdProvider);
        }

        public GachaTransactionOperationResult PullSingle(string gachaId)
        {
            return Execute(gachaId, 1);
        }

        public GachaTransactionOperationResult PullTen(string gachaId)
        {
            return Execute(gachaId, GachaCalculator.TenPullCount);
        }

        public GachaTransactionOperationResult Execute(
            string gachaId,
            int pullCount)
        {
            if (string.IsNullOrWhiteSpace(gachaId))
            {
                return Failure(
                    GachaTransactionStatus.InvalidGachaId,
                    "Gacha ID cannot be null or whitespace.");
            }

            if (pullCount != 1
                && pullCount != GachaCalculator.TenPullCount)
            {
                return Failure(
                    GachaTransactionStatus.UnsupportedPullCount,
                    "Only one or ten pulls are supported.");
            }

            if (!database.TryGetGacha(
                    gachaId,
                    out GachaDefinition definition))
            {
                return Failure(
                    GachaTransactionStatus.DefinitionNotFound,
                    $"Gacha definition was not found: {gachaId}.");
            }

            if (!TryGetValidatedCost(
                    definition,
                    pullCount,
                    out long cost,
                    out Exception definitionException))
            {
                return Failure(
                    GachaTransactionStatus.InvalidDefinition,
                    "Gacha definition or cost data is invalid.",
                    definitionException);
            }

            lock (syncRoot)
            {
                return ExecuteLocked(definition, pullCount, cost);
            }
        }

        private GachaTransactionOperationResult ExecuteLocked(
            GachaDefinition definition,
            int pullCount,
            long cost)
        {
            GachaCalculationResult calculation = null;
            RewardGrantBatchResult grantBatch = null;
            ProfileSaveData expectedProfile = null;
            long goldBefore = 0L;
            long goldAfter = 0L;

            SaveTransactionResult saveTransaction =
                saveService.ExecuteTransaction(profile =>
                {
                    ValidateProfileForTransaction(profile);
                    goldBefore = profile.Currencies.GachaCurrency;
                    if (goldBefore < 0L)
                    {
                        Reject(
                            GachaTransactionStatus.InvalidProfile,
                            "Gacha currency cannot be negative.");
                    }

                    if (goldBefore < cost)
                    {
                        Reject(
                            GachaTransactionStatus
                                .InsufficientGachaCurrency,
                            "Gacha currency is insufficient.");
                    }

                    GachaStateSaveData state = FindOrCreateState(
                        profile,
                        definition);
                    ValidateState(definition, state);

                    try
                    {
                        calculation = calculator.GenerateFromDefinition(
                            definition,
                            state,
                            pullCount);
                    }
                    catch (Exception exception)
                    {
                        Reject(
                            GachaTransactionStatus.CalculationFailed,
                            "Gacha calculation failed.",
                            exception);
                    }

                    try
                    {
                        grantBatch = rewardUpdater.Apply(
                            profile,
                            calculation);
                    }
                    catch (Exception exception)
                    {
                        Reject(
                            GachaTransactionStatus.RewardGrantFailed,
                            "Gacha reward application failed.",
                            exception);
                    }

                    ApplyResultingState(state, calculation.ResultingState);
                    try
                    {
                        goldAfter = checked(goldBefore - cost);
                    }
                    catch (Exception exception)
                    {
                        Reject(
                            GachaTransactionStatus.CurrencyDeductionFailed,
                            "Gacha currency deduction failed.",
                            exception);
                    }

                    if (goldAfter < 0L)
                    {
                        Reject(
                            GachaTransactionStatus.CurrencyDeductionFailed,
                            "Gacha currency deduction became negative.");
                    }

                    profile.Currencies.GachaCurrency = goldAfter;
                    expectedProfile = cloner.Clone(profile);
                });

            if (!saveTransaction.IsSuccess)
            {
                if (saveTransaction.Exception
                    is GachaTransactionRejectedException rejected)
                {
                    return Failure(
                        rejected.Status,
                        rejected.Message,
                        rejected.InnerException,
                        saveTransaction);
                }

                return Failure(
                    GachaTransactionStatus.PersistenceFailed,
                    saveTransaction.Message,
                    saveTransaction.Exception,
                    saveTransaction);
            }

            ProfileSaveData committed =
                saveService.GetCurrentProfileSnapshot();
            expectedProfile.LastSavedAtUtcUnixSeconds =
                committed.LastSavedAtUtcUnixSeconds;
            if (!comparer.Equals(expectedProfile, committed))
            {
                return Failure(
                    GachaTransactionStatus.PersistedResultMismatch,
                    "The persisted profile differs from the confirmed "
                        + "Gacha mutation. Do not retry automatically.",
                    saveTransactionResult: saveTransaction);
            }

            GachaTransactionResult result;
            try
            {
                result = BuildResult(
                    definition,
                    pullCount,
                    cost,
                    goldBefore,
                    goldAfter,
                    calculation,
                    grantBatch,
                    saveTransaction.WasSanitized);
            }
            catch (Exception exception)
            {
                return Failure(
                    GachaTransactionStatus.PersistedResultMismatch,
                    "The Gacha transaction was persisted, but its stable "
                        + "result could not be constructed. Do not retry "
                        + "automatically.",
                    exception,
                    saveTransaction);
            }

            return new GachaTransactionOperationResult(
                GachaTransactionStatus.Success,
                result,
                saveTransaction,
                exception: null,
                message: string.Empty);
        }

        private static GachaTransactionResult BuildResult(
            GachaDefinition definition,
            int pullCount,
            long cost,
            long goldBefore,
            long goldAfter,
            GachaCalculationResult calculation,
            RewardGrantBatchResult grantBatch,
            bool wasSanitized)
        {
            if (calculation == null || grantBatch == null)
            {
                throw new InvalidOperationException(
                    "Gacha calculation and grant results are required.");
            }

            if (calculation.Pulls.Count != grantBatch.Grants.Count)
            {
                throw new InvalidOperationException(
                    "Gacha pull and reward grant counts must match.");
            }

            var pulls = new GachaTransactionPullResult[pullCount];
            for (int index = 0; index < pullCount; index++)
            {
                pulls[index] = new GachaTransactionPullResult(
                    calculation.Pulls[index],
                    grantBatch.Grants[index]);
            }

            GachaStateSaveData state = calculation.ResultingState;
            return new GachaTransactionResult(
                definition.Id,
                definition.GachaKind,
                pullCount,
                cost,
                goldBefore,
                goldAfter,
                pulls,
                new GachaTransactionStateResult(
                    state.GachaId,
                    state.PityCount,
                    state.TotalPullCount),
                wasSanitized);
        }

        private static GachaStateSaveData FindOrCreateState(
            ProfileSaveData profile,
            GachaDefinition definition)
        {
            GachaStateSaveData match = null;
            for (int index = 0; index < profile.GachaStates.Count; index++)
            {
                GachaStateSaveData candidate = profile.GachaStates[index];
                if (candidate == null)
                {
                    Reject(
                        GachaTransactionStatus.InvalidGachaState,
                        $"Gacha state at index {index} is null.");
                }

                if (!string.Equals(
                        candidate.GachaId,
                        definition.Id,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (match != null)
                {
                    Reject(
                        GachaTransactionStatus.InvalidGachaState,
                        $"Duplicate Gacha state ID: {definition.Id}.");
                }

                match = candidate;
            }

            if (match != null)
            {
                return match;
            }

            match = new GachaStateSaveData
            {
                GachaId = definition.Id,
                PityCount = 0,
                TotalPullCount = 0L
            };
            profile.GachaStates.Add(match);
            return match;
        }

        private static void ValidateProfileForTransaction(
            ProfileSaveData profile)
        {
            if (profile == null
                || profile.Currencies == null
                || profile.GachaStates == null)
            {
                Reject(
                    GachaTransactionStatus.InvalidProfile,
                    "Profile currencies and Gacha states are required.");
            }
        }

        private static void ValidateState(
            GachaDefinition definition,
            GachaStateSaveData state)
        {
            if (!string.Equals(
                    state.GachaId,
                    definition.Id,
                    StringComparison.Ordinal)
                || state.PityCount < 0
                || state.PityCount >= definition.PityPullCount
                || state.TotalPullCount < 0L
                || state.PityCount > state.TotalPullCount)
            {
                Reject(
                    GachaTransactionStatus.InvalidGachaState,
                    "The saved Gacha state is invalid for its banner.");
            }
        }

        private static void ApplyResultingState(
            GachaStateSaveData destination,
            GachaStateSaveData source)
        {
            destination.GachaId = source.GachaId;
            destination.PityCount = source.PityCount;
            destination.TotalPullCount = source.TotalPullCount;
        }

        private static bool TryGetValidatedCost(
            GachaDefinition definition,
            int pullCount,
            out long cost,
            out Exception exception)
        {
            try
            {
                if (definition == null
                    || string.IsNullOrWhiteSpace(definition.Id)
                    || !Enum.IsDefined(
                        typeof(GachaKind),
                        definition.GachaKind)
                    || definition.PityPullCount <= 0
                    || definition.SinglePullCost <= 0L
                    || definition.SinglePullCost
                        > long.MaxValue / GachaCalculator.TenPullCount
                    || definition.TenPullCost <= 0L
                    || definition.TenPullCost
                        != definition.SinglePullCost
                            * GachaCalculator.TenPullCount)
                {
                    throw new ArgumentException(
                        "Gacha definition costs or rules are invalid.",
                        nameof(definition));
                }

                GachaRewardSelector.ValidateOrThrow(definition);
                cost = pullCount == 1
                    ? definition.SinglePullCost
                    : definition.TenPullCost;
                exception = null;
                return true;
            }
            catch (Exception caught)
            {
                cost = 0L;
                exception = caught;
                return false;
            }
        }

        private static void Reject(
            GachaTransactionStatus status,
            string message,
            Exception innerException = null)
        {
            throw new GachaTransactionRejectedException(
                status,
                message,
                innerException);
        }

        private static GachaTransactionOperationResult Failure(
            GachaTransactionStatus status,
            string message,
            Exception exception = null,
            SaveTransactionResult saveTransactionResult = null)
        {
            return new GachaTransactionOperationResult(
                status,
                transactionResult: null,
                saveTransactionResult,
                exception,
                message);
        }

        private sealed class GachaTransactionRejectedException
            : InvalidOperationException
        {
            public GachaTransactionRejectedException(
                GachaTransactionStatus status,
                string message,
                Exception innerException)
                : base(message, innerException)
            {
                Status = status;
            }

            public GachaTransactionStatus Status { get; }
        }
    }
}
