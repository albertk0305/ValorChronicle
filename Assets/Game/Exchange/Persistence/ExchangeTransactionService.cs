using System;
using System.Collections.Generic;
using System.Threading;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Rewards;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Exchange.Persistence
{
    public interface IExchangeTransactionExecutor
    {
        ExchangeTransactionOperationResult Execute(
            ExchangeTransactionRequest request);
    }

    public sealed class ExchangeTransactionService
        : IExchangeTransactionExecutor
    {
        private readonly SaveService saveService;
        private readonly DefinitionDatabase database;
        private readonly RewardGrantProfileUpdater rewardUpdater;
        private readonly SaveDataCloner cloner = new SaveDataCloner();
        private readonly SaveDataValueComparer comparer =
            new SaveDataValueComparer();
        private int transactionActive;

        public ExchangeTransactionService(
            SaveService saveService,
            DefinitionDatabase database,
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

            rewardUpdater = new RewardGrantProfileUpdater(
                database,
                relicInstanceIdProvider);
        }

        public ExchangeTransactionOperationResult Execute(
            ExchangeTransactionRequest request)
        {
            ExchangeTransactionOperationResult preflight =
                ValidateRequest(request);
            if (preflight != null)
                return preflight;

            if (Interlocked.CompareExchange(
                    ref transactionActive,
                    1,
                    0) != 0)
            {
                return Failure(
                    ExchangeTransactionStatus.TransactionBusy,
                    "An Exchange transaction is already active.");
            }

            try
            {
                return ExecuteExclusive(request);
            }
            finally
            {
                Volatile.Write(ref transactionActive, 0);
            }
        }

        private ExchangeTransactionOperationResult ExecuteExclusive(
            ExchangeTransactionRequest request)
        {
            ExchangeCatalogDefinition catalog = database.ExchangeCatalog;
            long cost = request.ContentKind
                == ExchangeContentKind.Character
                    ? catalog.CharacterExchangeCost
                    : catalog.RelicExchangeCost;
            ExchangeCurrencyKind currencyKind = request.ContentKind
                == ExchangeContentKind.Character
                    ? ExchangeCurrencyKind.HeroTokens
                    : ExchangeCurrencyKind.RelicTokens;
            ProfileSaveData expectedProfile = null;
            RewardGrantResult grant = null;
            long currencyBefore = 0L;
            long currencyAfter = 0L;

            SaveTransactionResult saveTransaction =
                saveService.ExecuteTransaction(profile =>
                {
                    ExchangeCatalogData list;
                    try
                    {
                        list = ExchangeCatalogBuilder.Build(
                            database,
                            catalog,
                            profile);
                    }
                    catch (ArgumentException exception)
                    {
                        Reject(
                            ExchangeTransactionStatus.InvalidProfile,
                            "The profile cannot be used for Exchange.",
                            exception);
                        return;
                    }
                    catch (InvalidOperationException exception)
                    {
                        Reject(
                            ExchangeTransactionStatus.NotExchangeable,
                            "The Exchange catalog is invalid.",
                            exception);
                        return;
                    }

                    ExchangeListItem item = FindItem(list, request);
                    if (item == null)
                    {
                        Reject(
                            ExchangeTransactionStatus.NotExchangeable,
                            "The requested content is not in the Exchange list.");
                    }

                    if (item.UnavailableReasons.HasFlag(
                            ExchangeUnavailableReason.MaximumAwakening))
                    {
                        Reject(
                            ExchangeTransactionStatus.MaximumAwakening,
                            "A maximum-awakening character cannot be exchanged.");
                    }

                    if (item.UnavailableReasons.HasFlag(
                            ExchangeUnavailableReason.InsufficientCurrency))
                    {
                        Reject(
                            ExchangeTransactionStatus.InsufficientCurrency,
                            "Exchange currency is insufficient.");
                    }

                    currencyBefore = GetCurrency(profile, currencyKind);
                    if (currencyBefore < cost)
                    {
                        Reject(
                            ExchangeTransactionStatus.InsufficientCurrency,
                            "Exchange currency is insufficient.");
                    }

                    currencyAfter = checked(currencyBefore - cost);
                    if (currencyAfter < 0L)
                    {
                        Reject(
                            ExchangeTransactionStatus.InsufficientCurrency,
                            "Exchange currency cannot become negative.");
                    }

                    SetCurrency(profile, currencyKind, currencyAfter);
                    try
                    {
                        RewardGrantBatchResult batch = rewardUpdater.Apply(
                            profile,
                            new[]
                            {
                                new RewardGrantRequest(
                                    0,
                                    GetRewardKind(request.ContentKind),
                                    request.ContentId,
                                    1L)
                            });
                        grant = ValidateGrant(request, batch);
                    }
                    catch (ExchangeTransactionRejectedException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        Reject(
                            ExchangeTransactionStatus.RewardGrantFailed,
                            "The Exchange reward could not be granted.",
                            exception);
                    }

                    expectedProfile = cloner.Clone(profile);
                });

            if (!saveTransaction.IsSuccess)
            {
                if (saveTransaction.Exception
                    is ExchangeTransactionRejectedException rejected)
                {
                    return Failure(
                        rejected.Status,
                        rejected.Message,
                        rejected.InnerException,
                        saveTransaction);
                }

                ExchangeTransactionStatus status;
                switch (saveTransaction.Status)
                {
                    case SaveTransactionStatus.TransactionAlreadyActive:
                        status = ExchangeTransactionStatus.TransactionBusy;
                        break;
                    case SaveTransactionStatus.NoCurrentProfile:
                        status = ExchangeTransactionStatus.InvalidProfile;
                        break;
                    default:
                        status = ExchangeTransactionStatus.PersistenceFailed;
                        break;
                }
                return Failure(
                    status,
                    saveTransaction.Message,
                    saveTransaction.Exception,
                    saveTransaction);
            }

            ProfileSaveData committed =
                saveService.GetCurrentProfileSnapshot();
            if (expectedProfile == null || committed == null)
            {
                return Failure(
                    ExchangeTransactionStatus.PersistedStateMismatch,
                    "The persisted Exchange profile is unavailable. Do not retry automatically.",
                    saveTransactionResult: saveTransaction);
            }

            expectedProfile.LastSavedAtUtcUnixSeconds =
                committed.LastSavedAtUtcUnixSeconds;
            if (!comparer.Equals(expectedProfile, committed))
            {
                return Failure(
                    ExchangeTransactionStatus.PersistedStateMismatch,
                    "The persisted profile differs from the confirmed Exchange mutation. Do not retry automatically.",
                    saveTransactionResult: saveTransaction);
            }

            ExchangeTransactionResult result;
            try
            {
                result = new ExchangeTransactionResult(
                    request.ContentKind,
                    request.ContentId,
                    currencyKind,
                    cost,
                    currencyBefore,
                    currencyAfter,
                    grant,
                    saveTransaction.WasSanitized);
            }
            catch (Exception exception)
            {
                return Failure(
                    ExchangeTransactionStatus.PersistedStateMismatch,
                    "The Exchange was persisted, but its result could not be constructed. Do not retry automatically.",
                    exception,
                    saveTransaction);
            }

            return new ExchangeTransactionOperationResult(
                ExchangeTransactionStatus.Success,
                result,
                saveTransaction,
                exception: null,
                message: string.Empty);
        }

        private ExchangeTransactionOperationResult ValidateRequest(
            ExchangeTransactionRequest request)
        {
            if (request == null
                || !Enum.IsDefined(
                    typeof(ExchangeContentKind),
                    request.ContentKind)
                || string.IsNullOrWhiteSpace(request.ContentId))
            {
                return Failure(
                    ExchangeTransactionStatus.InvalidRequest,
                    "A valid Exchange kind and content ID are required.");
            }

            ExchangeCatalogDefinition catalog = database.ExchangeCatalog;
            if (catalog == null)
            {
                return Failure(
                    ExchangeTransactionStatus.NotExchangeable,
                    "The Exchange catalog is unavailable.");
            }

            bool exists = request.ContentKind
                == ExchangeContentKind.Character
                    ? database.TryGetCharacter(request.ContentId, out _)
                    : database.TryGetRelic(request.ContentId, out _);
            if (!exists)
            {
                return Failure(
                    ExchangeTransactionStatus.UnknownContent,
                    $"Exchange content was not found: {request.ContentId}.");
            }

            IReadOnlyList<string> ids = request.ContentKind
                == ExchangeContentKind.Character
                    ? catalog.CharacterIds
                    : catalog.RelicIds;
            if (!ContainsOrdinal(ids, request.ContentId))
            {
                return Failure(
                    ExchangeTransactionStatus.NotExchangeable,
                    $"Content is not registered in the Exchange catalog: {request.ContentId}.");
            }

            long cost = request.ContentKind
                == ExchangeContentKind.Character
                    ? catalog.CharacterExchangeCost
                    : catalog.RelicExchangeCost;
            if (cost <= 0L)
            {
                return Failure(
                    ExchangeTransactionStatus.NotExchangeable,
                    "The configured Exchange cost is invalid.");
            }

            return null;
        }

        private static ExchangeListItem FindItem(
            ExchangeCatalogData list,
            ExchangeTransactionRequest request)
        {
            if (request.ContentKind == ExchangeContentKind.Character)
            {
                for (int index = 0; index < list.Characters.Count; index++)
                {
                    if (string.Equals(
                            list.Characters[index].CharacterId,
                            request.ContentId,
                            StringComparison.Ordinal))
                    {
                        return list.Characters[index];
                    }
                }

                return null;
            }

            for (int index = 0; index < list.Relics.Count; index++)
            {
                if (string.Equals(
                        list.Relics[index].RelicId,
                        request.ContentId,
                        StringComparison.Ordinal))
                {
                    return list.Relics[index];
                }
            }

            return null;
        }

        private static RewardGrantResult ValidateGrant(
            ExchangeTransactionRequest request,
            RewardGrantBatchResult batch)
        {
            if (batch == null || batch.Grants.Count != 1)
            {
                Reject(
                    ExchangeTransactionStatus.RewardGrantFailed,
                    "Exchange must produce exactly one reward grant.");
            }

            RewardGrantResult grant = batch.Grants[0];
            if (grant == null
                || grant.Index != 0
                || grant.OriginalAmount != 1L
                || !string.Equals(
                    grant.ContentId,
                    request.ContentId,
                    StringComparison.Ordinal))
            {
                Reject(
                    ExchangeTransactionStatus.RewardGrantFailed,
                    "The Exchange reward result does not match the request.");
            }

            bool validKind = request.ContentKind
                == ExchangeContentKind.Character
                    ? grant.GrantKind == RewardGrantKind.NewCharacter
                        || grant.GrantKind
                            == RewardGrantKind.CharacterAwakeningIncreased
                    : grant.GrantKind
                        == RewardGrantKind.NewRelicInstanceCreated
                        || grant.GrantKind
                            == RewardGrantKind.RelicAwakeningIncreased;
            if (!validKind)
            {
                Reject(
                    ExchangeTransactionStatus.RewardGrantFailed,
                    "The Exchange reward kind is invalid for the request.");
            }

            return grant;
        }

        private static GachaRewardKind GetRewardKind(
            ExchangeContentKind contentKind)
        {
            return contentKind == ExchangeContentKind.Character
                ? GachaRewardKind.RandomCharacter
                : GachaRewardKind.RandomRelic;
        }

        private static long GetCurrency(
            ProfileSaveData profile,
            ExchangeCurrencyKind currencyKind)
        {
            return currencyKind == ExchangeCurrencyKind.HeroTokens
                ? profile.Currencies.HeroTokens
                : profile.Currencies.RelicTokens;
        }

        private static void SetCurrency(
            ProfileSaveData profile,
            ExchangeCurrencyKind currencyKind,
            long amount)
        {
            if (currencyKind == ExchangeCurrencyKind.HeroTokens)
                profile.Currencies.HeroTokens = amount;
            else
                profile.Currencies.RelicTokens = amount;
        }

        private static bool ContainsOrdinal(
            IReadOnlyList<string> ids,
            string contentId)
        {
            for (int index = 0; index < ids.Count; index++)
            {
                if (string.Equals(
                        ids[index],
                        contentId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Reject(
            ExchangeTransactionStatus status,
            string message,
            Exception innerException = null)
        {
            throw new ExchangeTransactionRejectedException(
                status,
                message,
                innerException);
        }

        private static ExchangeTransactionOperationResult Failure(
            ExchangeTransactionStatus status,
            string message,
            Exception exception = null,
            SaveTransactionResult saveTransactionResult = null)
        {
            return new ExchangeTransactionOperationResult(
                status,
                transactionResult: null,
                saveTransactionResult,
                exception,
                message);
        }

        private sealed class ExchangeTransactionRejectedException
            : InvalidOperationException
        {
            public ExchangeTransactionRejectedException(
                ExchangeTransactionStatus status,
                string message,
                Exception innerException)
                : base(message, innerException)
            {
                Status = status;
            }

            public ExchangeTransactionStatus Status { get; }
        }
    }
}
