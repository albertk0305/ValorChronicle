using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Editor.SaveDebug
{
    public enum SaveDebugCurrency
    {
        GachaCurrency,
        BattleRecords,
        HeroTokens,
        RelicTokens
    }

    public enum Stage10SavePreset
    {
        FreshMarea,
        Insufficient,
        Level99Boundary,
        MaxAwakening
    }

    public enum SaveDebugOperationStatus
    {
        Success,
        NoChange,
        InvalidInput,
        CharacterNotRegistered,
        CharacterNotOwned,
        PersistenceFailed
    }

    public sealed class SaveDebugOperationResult
    {
        internal SaveDebugOperationResult(
            SaveDebugOperationStatus status,
            string message,
            SaveTransactionResult transactionResult = null)
        {
            Status = status;
            Message = message ?? string.Empty;
            TransactionResult = transactionResult;
        }

        public SaveDebugOperationStatus Status { get; }
        public string Message { get; }
        public SaveTransactionResult TransactionResult { get; }
        public bool IsSuccess => Status == SaveDebugOperationStatus.Success;
    }

    /// <summary>
    /// Applies Editor-only save-debug mutations through the production transaction boundary.
    /// </summary>
    public sealed class SaveDebugProfileService
    {
        public const string MareaCharacterId = "character_marea_bluefang";

        private readonly SaveService saveService;
        private readonly HashSet<string> registeredCharacterIds;

        public SaveDebugProfileService(
            SaveService saveService,
            IEnumerable<string> registeredCharacterIds)
        {
            this.saveService = saveService
                ?? throw new ArgumentNullException(nameof(saveService));
            if (registeredCharacterIds == null)
            {
                throw new ArgumentNullException(nameof(registeredCharacterIds));
            }

            this.registeredCharacterIds = new HashSet<string>(
                registeredCharacterIds,
                StringComparer.Ordinal);
        }

        public SaveDebugOperationResult SetCurrency(
            SaveDebugCurrency currency,
            long value)
        {
            if (value < 0)
            {
                return Invalid("Currency cannot be negative.");
            }

            return Execute(
                profile => SetCurrencyValue(profile.Currencies, currency, value),
                $"Set {currency} to {value}.");
        }

        public SaveDebugOperationResult AddCurrency(
            SaveDebugCurrency currency,
            long amount)
        {
            ProfileSaveData snapshot;
            try
            {
                snapshot = saveService.GetCurrentProfileSnapshot();
            }
            catch (Exception exception)
            {
                return PersistenceFailure(
                    $"Current profile is unavailable: {exception.Message}");
            }

            long result;
            try
            {
                result = checked(GetCurrencyValue(snapshot.Currencies, currency) + amount);
            }
            catch (OverflowException)
            {
                return Invalid("Currency addition is outside the Int64 range.");
            }

            if (result < 0)
            {
                return Invalid("Currency addition would produce a negative value.");
            }

            return Execute(
                profile =>
                {
                    long authoritativeResult = checked(
                        GetCurrencyValue(profile.Currencies, currency) + amount);
                    if (authoritativeResult < 0)
                    {
                        throw new InvalidOperationException(
                            "Currency addition would produce a negative value.");
                    }

                    SetCurrencyValue(
                        profile.Currencies,
                        currency,
                        authoritativeResult);
                },
                $"Added {amount} to {currency}.");
        }

        public SaveDebugOperationResult GrantCharacter(string characterId)
        {
            SaveDebugOperationResult validation = ValidateRegisteredCharacter(
                characterId);
            if (validation != null)
            {
                return validation;
            }

            ProfileSaveData snapshot = saveService.GetCurrentProfileSnapshot();
            if (FindCharacter(snapshot, characterId) != null)
            {
                return new SaveDebugOperationResult(
                    SaveDebugOperationStatus.NoChange,
                    $"'{characterId}' is already owned. No duplicate reward was applied.");
            }

            return Execute(
                profile =>
                {
                    if (FindCharacter(profile, characterId) != null)
                    {
                        throw new InvalidOperationException(
                            $"'{characterId}' became owned before the transaction ran.");
                    }

                    profile.Characters.Add(CreateCharacter(characterId));
                },
                $"Granted '{characterId}' at Lv.1 / Awakening 0.");
        }

        public SaveDebugOperationResult SetCharacterLevel(
            string characterId,
            int level)
        {
            if (level < SaveRules.CharacterMinLevel
                || level > SaveRules.CharacterMaxLevel)
            {
                return Invalid(
                    $"Level must be between {SaveRules.CharacterMinLevel} and "
                    + $"{SaveRules.CharacterMaxLevel}.");
            }

            return SetOwnedCharacterValue(
                characterId,
                character => character.Level = level,
                $"Set '{characterId}' to Lv.{level}.");
        }

        public SaveDebugOperationResult SetCharacterAwakening(
            string characterId,
            int awakening)
        {
            if (awakening < SaveRules.CharacterMinAwakening
                || awakening > SaveRules.CharacterMaxAwakening)
            {
                return Invalid(
                    $"Awakening must be between "
                    + $"{SaveRules.CharacterMinAwakening} and "
                    + $"{SaveRules.CharacterMaxAwakening}.");
            }

            return SetOwnedCharacterValue(
                characterId,
                character => character.Awakening = awakening,
                $"Set '{characterId}' to Awakening {awakening}.");
        }

        public SaveDebugOperationResult ApplyStage10Preset(
            Stage10SavePreset preset)
        {
            if (!registeredCharacterIds.Contains(MareaCharacterId))
            {
                return new SaveDebugOperationResult(
                    SaveDebugOperationStatus.CharacterNotRegistered,
                    $"'{MareaCharacterId}' is not registered in DefinitionDatabase.");
            }

            return Execute(
                profile =>
                {
                    CharacterSaveData marea = FindCharacter(
                        profile,
                        MareaCharacterId);
                    if (marea == null)
                    {
                        marea = CreateCharacter(MareaCharacterId);
                        profile.Characters.Add(marea);
                    }

                    switch (preset)
                    {
                        case Stage10SavePreset.FreshMarea:
                            marea.Level = 1;
                            marea.Awakening = 0;
                            profile.Currencies.BattleRecords = 150000;
                            break;
                        case Stage10SavePreset.Insufficient:
                            marea.Level = 1;
                            marea.Awakening = 0;
                            profile.Currencies.BattleRecords = 0;
                            break;
                        case Stage10SavePreset.Level99Boundary:
                            marea.Level = 99;
                            marea.Awakening = 0;
                            profile.Currencies.BattleRecords = 2080;
                            break;
                        case Stage10SavePreset.MaxAwakening:
                            marea.Level = 100;
                            marea.Awakening = 6;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(preset),
                                preset,
                                "Unknown Stage 10 preset.");
                    }
                },
                $"Applied Stage 10 preset: {preset}.");
        }

        private SaveDebugOperationResult SetOwnedCharacterValue(
            string characterId,
            Action<CharacterSaveData> mutation,
            string successMessage)
        {
            SaveDebugOperationResult validation = ValidateRegisteredCharacter(
                characterId);
            if (validation != null)
            {
                return validation;
            }

            ProfileSaveData snapshot = saveService.GetCurrentProfileSnapshot();
            if (FindCharacter(snapshot, characterId) == null)
            {
                return new SaveDebugOperationResult(
                    SaveDebugOperationStatus.CharacterNotOwned,
                    $"'{characterId}' is not owned.");
            }

            return Execute(
                profile =>
                {
                    CharacterSaveData character = FindCharacter(
                        profile,
                        characterId);
                    if (character == null)
                    {
                        throw new InvalidOperationException(
                            $"'{characterId}' is no longer owned.");
                    }

                    mutation(character);
                },
                successMessage);
        }

        private SaveDebugOperationResult ValidateRegisteredCharacter(
            string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)
                || !registeredCharacterIds.Contains(characterId))
            {
                return new SaveDebugOperationResult(
                    SaveDebugOperationStatus.CharacterNotRegistered,
                    $"'{characterId}' is not registered in DefinitionDatabase.");
            }

            return null;
        }

        private SaveDebugOperationResult Execute(
            Action<ProfileSaveData> mutation,
            string successMessage)
        {
            SaveTransactionResult transaction =
                saveService.ExecuteTransaction(mutation);
            if (transaction.IsSuccess)
            {
                return new SaveDebugOperationResult(
                    SaveDebugOperationStatus.Success,
                    successMessage,
                    transaction);
            }

            string detail = string.IsNullOrEmpty(transaction.Message)
                ? transaction.Status.ToString()
                : $"{transaction.Status}: {transaction.Message}";
            return new SaveDebugOperationResult(
                SaveDebugOperationStatus.PersistenceFailed,
                $"Save transaction failed ({detail}).",
                transaction);
        }

        private static CharacterSaveData CreateCharacter(string characterId)
        {
            return new CharacterSaveData
            {
                CharacterId = characterId,
                Level = SaveRules.CharacterMinLevel,
                Awakening = SaveRules.CharacterMinAwakening,
                IsFavorite = false,
                IsNew = false
            };
        }

        private static CharacterSaveData FindCharacter(
            ProfileSaveData profile,
            string characterId)
        {
            if (profile?.Characters == null)
            {
                return null;
            }

            for (int index = 0; index < profile.Characters.Count; index++)
            {
                CharacterSaveData character = profile.Characters[index];
                if (character != null
                    && string.Equals(
                        character.CharacterId,
                        characterId,
                        StringComparison.Ordinal))
                {
                    return character;
                }
            }

            return null;
        }

        private static long GetCurrencyValue(
            CurrencySaveData currencies,
            SaveDebugCurrency currency)
        {
            if (currencies == null)
            {
                throw new InvalidOperationException(
                    "The current profile has no currency data.");
            }

            switch (currency)
            {
                case SaveDebugCurrency.GachaCurrency:
                    return currencies.GachaCurrency;
                case SaveDebugCurrency.BattleRecords:
                    return currencies.BattleRecords;
                case SaveDebugCurrency.HeroTokens:
                    return currencies.HeroTokens;
                case SaveDebugCurrency.RelicTokens:
                    return currencies.RelicTokens;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(currency),
                        currency,
                        "Unknown currency.");
            }
        }

        private static void SetCurrencyValue(
            CurrencySaveData currencies,
            SaveDebugCurrency currency,
            long value)
        {
            if (currencies == null)
            {
                throw new InvalidOperationException(
                    "The current profile has no currency data.");
            }

            switch (currency)
            {
                case SaveDebugCurrency.GachaCurrency:
                    currencies.GachaCurrency = value;
                    return;
                case SaveDebugCurrency.BattleRecords:
                    currencies.BattleRecords = value;
                    return;
                case SaveDebugCurrency.HeroTokens:
                    currencies.HeroTokens = value;
                    return;
                case SaveDebugCurrency.RelicTokens:
                    currencies.RelicTokens = value;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(currency),
                        currency,
                        "Unknown currency.");
            }
        }

        private static SaveDebugOperationResult Invalid(string message)
        {
            return new SaveDebugOperationResult(
                SaveDebugOperationStatus.InvalidInput,
                message);
        }

        private static SaveDebugOperationResult PersistenceFailure(
            string message)
        {
            return new SaveDebugOperationResult(
                SaveDebugOperationStatus.PersistenceFailed,
                message);
        }
    }
}
