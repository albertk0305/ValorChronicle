using System.Collections.Generic;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Save.Copying
{
    /// <summary>Compares complete profile DTO graphs by value and list order.</summary>
    public sealed class SaveDataValueComparer : IEqualityComparer<ProfileSaveData>
    {
        /// <inheritdoc />
        public bool Equals(ProfileSaveData x, ProfileSaveData y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.SaveVersion == y.SaveVersion && x.ProfileId == y.ProfileId &&
                   x.CreatedAtUtcUnixSeconds == y.CreatedAtUtcUnixSeconds &&
                   x.LastSavedAtUtcUnixSeconds == y.LastSavedAtUtcUnixSeconds &&
                   Equals(x.Currencies, y.Currencies) &&
                   SequenceEqual(x.Characters, y.Characters, Equals) &&
                   SequenceEqual(x.RelicInstances, y.RelicInstances, Equals) &&
                   Equals(x.Party, y.Party) &&
                   SequenceEqual(x.GachaStates, y.GachaStates, Equals) &&
                   SequenceEqual(x.BossRecords, y.BossRecords, Equals) &&
                   SequenceEqual(x.UnlockedContentIds, y.UnlockedContentIds) &&
                   SequenceEqual(x.CompletedTutorialIds, y.CompletedTutorialIds);
        }

        /// <inheritdoc />
        public int GetHashCode(ProfileSaveData obj) => obj == null ? 0 : obj.ProfileId?.GetHashCode() ?? 0;

        private static bool Equals(CurrencySaveData x, CurrencySaveData y) =>
            ReferenceEquals(x, y) || x != null && y != null &&
            x.GachaCurrency == y.GachaCurrency && x.BattleRecords == y.BattleRecords &&
            x.HeroTokens == y.HeroTokens && x.RelicTokens == y.RelicTokens;

        private static bool Equals(CharacterSaveData x, CharacterSaveData y) =>
            ReferenceEquals(x, y) || x != null && y != null && x.CharacterId == y.CharacterId &&
            x.Level == y.Level && x.Awakening == y.Awakening && x.IsFavorite == y.IsFavorite && x.IsNew == y.IsNew;

        private static bool Equals(RelicInstanceSaveData x, RelicInstanceSaveData y) =>
            ReferenceEquals(x, y) || x != null && y != null && x.InstanceId == y.InstanceId &&
            x.RelicDefinitionId == y.RelicDefinitionId && x.Awakening == y.Awakening &&
            x.EquippedCharacterId == y.EquippedCharacterId && x.EquippedSlotIndex == y.EquippedSlotIndex &&
            x.IsLocked == y.IsLocked && x.IsNew == y.IsNew;

        private static bool Equals(PartySaveData x, PartySaveData y) =>
            ReferenceEquals(x, y) || x != null && y != null && x.ActivePresetIndex == y.ActivePresetIndex &&
            x.LastBossId == y.LastBossId && x.LastDifficultyId == y.LastDifficultyId &&
            SequenceEqual(x.Presets, y.Presets, Equals);

        private static bool Equals(PartyPresetSaveData x, PartyPresetSaveData y) =>
            ReferenceEquals(x, y) || x != null && y != null && x.PresetId == y.PresetId &&
            SequenceEqual(x.CharacterSlotIds, y.CharacterSlotIds);

        private static bool Equals(GachaStateSaveData x, GachaStateSaveData y) =>
            ReferenceEquals(x, y) || x != null && y != null && x.GachaId == y.GachaId &&
            x.PityCount == y.PityCount && x.TotalPullCount == y.TotalPullCount;

        private static bool Equals(BossRecordSaveData x, BossRecordSaveData y) =>
            ReferenceEquals(x, y) || x != null && y != null && x.BossId == y.BossId &&
            x.DifficultyId == y.DifficultyId && x.HasAttempted == y.HasAttempted && x.IsCleared == y.IsCleared &&
            x.HighScore == y.HighScore && x.HighestGradeId == y.HighestGradeId &&
            x.BestDefeatTurn == y.BestDefeatTurn && x.BestRemainingTurns == y.BestRemainingTurns &&
            SequenceEqual(x.ClaimedFirstRewardGradeIds, y.ClaimedFirstRewardGradeIds);

        private static bool SequenceEqual<T>(IList<T> x, IList<T> y, System.Func<T, T, bool> equals)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null || x.Count != y.Count) return false;
            for (int i = 0; i < x.Count; i++) if (!equals(x[i], y[i])) return false;
            return true;
        }

        private static bool SequenceEqual<T>(IList<T> x, IList<T> y)
        {
            return SequenceEqual(x, y, EqualityComparer<T>.Default.Equals);
        }
    }
}
