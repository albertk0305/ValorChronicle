using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Save.Copying
{
    /// <summary>
    /// Creates explicit deep copies of profile save DTO graphs without serialization.
    /// </summary>
    public sealed class SaveDataCloner
    {
        /// <summary>
        /// Creates a deep copy while preserving null values, null collection entries, and list order.
        /// </summary>
        /// <param name="source">The profile to copy.</param>
        /// <returns>A new profile graph that shares no mutable DTO or list instances with the source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public ProfileSaveData Clone(ProfileSaveData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ProfileSaveData
            {
                SaveVersion = source.SaveVersion,
                ProfileId = source.ProfileId,
                CreatedAtUtcUnixSeconds = source.CreatedAtUtcUnixSeconds,
                LastSavedAtUtcUnixSeconds = source.LastSavedAtUtcUnixSeconds,
                Currencies = Clone(source.Currencies),
                Characters = CloneList(source.Characters, Clone),
                RelicInstances = CloneList(source.RelicInstances, Clone),
                Party = Clone(source.Party),
                GachaStates = CloneList(source.GachaStates, Clone),
                BossRecords = CloneList(source.BossRecords, Clone),
                UnlockedContentIds = CloneStringList(source.UnlockedContentIds),
                CompletedTutorialIds = CloneStringList(source.CompletedTutorialIds)
            };
        }

        private static CurrencySaveData Clone(CurrencySaveData source)
        {
            return source == null
                ? null
                : new CurrencySaveData
                {
                    GachaCurrency = source.GachaCurrency,
                    BattleRecords = source.BattleRecords,
                    HeroTokens = source.HeroTokens,
                    RelicTokens = source.RelicTokens
                };
        }

        private static CharacterSaveData Clone(CharacterSaveData source)
        {
            return source == null
                ? null
                : new CharacterSaveData
                {
                    CharacterId = source.CharacterId,
                    Level = source.Level,
                    Awakening = source.Awakening,
                    IsFavorite = source.IsFavorite,
                    IsNew = source.IsNew
                };
        }

        private static RelicInstanceSaveData Clone(RelicInstanceSaveData source)
        {
            return source == null
                ? null
                : new RelicInstanceSaveData
                {
                    InstanceId = source.InstanceId,
                    RelicDefinitionId = source.RelicDefinitionId,
                    Awakening = source.Awakening,
                    EquippedCharacterId = source.EquippedCharacterId,
                    EquippedSlotIndex = source.EquippedSlotIndex,
                    IsLocked = source.IsLocked,
                    IsNew = source.IsNew
                };
        }

        private static PartySaveData Clone(PartySaveData source)
        {
            return source == null
                ? null
                : new PartySaveData
                {
                    ActivePresetIndex = source.ActivePresetIndex,
                    Presets = CloneList(source.Presets, Clone),
                    LastBossId = source.LastBossId,
                    LastDifficultyId = source.LastDifficultyId
                };
        }

        private static PartyPresetSaveData Clone(PartyPresetSaveData source)
        {
            return source == null
                ? null
                : new PartyPresetSaveData
                {
                    PresetId = source.PresetId,
                    CharacterSlotIds = CloneStringList(source.CharacterSlotIds)
                };
        }

        private static GachaStateSaveData Clone(GachaStateSaveData source)
        {
            return source == null
                ? null
                : new GachaStateSaveData
                {
                    GachaId = source.GachaId,
                    PityCount = source.PityCount,
                    TotalPullCount = source.TotalPullCount
                };
        }

        private static BossRecordSaveData Clone(BossRecordSaveData source)
        {
            return source == null
                ? null
                : new BossRecordSaveData
                {
                    BossId = source.BossId,
                    DifficultyId = source.DifficultyId,
                    HasAttempted = source.HasAttempted,
                    IsCleared = source.IsCleared,
                    HighScore = source.HighScore,
                    HighestGradeId = source.HighestGradeId,
                    BestDefeatTurn = source.BestDefeatTurn,
                    BestRemainingTurns = source.BestRemainingTurns,
                    ClaimedFirstRewardGradeIds =
                        CloneStringList(source.ClaimedFirstRewardGradeIds)
                };
        }

        private static List<T> CloneList<T>(List<T> source, Func<T, T> cloneItem)
        {
            if (source == null)
            {
                return null;
            }

            var result = new List<T>(source.Count);

            for (int i = 0; i < source.Count; i++)
            {
                result.Add(cloneItem(source[i]));
            }

            return result;
        }

        private static List<string> CloneStringList(List<string> source)
        {
            return source == null ? null : new List<string>(source);
        }
    }
}
