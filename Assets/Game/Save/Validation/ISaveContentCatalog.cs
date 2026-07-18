namespace ValorChronicle.Save.Validation
{
    /// <summary>Describes whether a content reference can be resolved.</summary>
    public enum SaveContentLookupResult
    {
        Exists,
        Missing,
        Unavailable
    }

    /// <summary>Provides content reference checks without depending on a concrete definition database.</summary>
    public interface ISaveContentCatalog
    {
        /// <summary>Looks up a character definition.</summary>
        SaveContentLookupResult LookupCharacter(string characterId);
        /// <summary>Looks up a relic definition.</summary>
        SaveContentLookupResult LookupRelic(string relicDefinitionId);
        /// <summary>Looks up a boss definition.</summary>
        SaveContentLookupResult LookupBoss(string bossId);
        /// <summary>Looks up a difficulty belonging to a boss.</summary>
        SaveContentLookupResult LookupBossDifficulty(string bossId, string difficultyId);
        /// <summary>Looks up a gacha definition.</summary>
        SaveContentLookupResult LookupGacha(string gachaId);
        /// <summary>Looks up a first-clear reward grade.</summary>
        SaveContentLookupResult LookupRewardGrade(string bossId, string difficultyId, string gradeId);
    }
}
