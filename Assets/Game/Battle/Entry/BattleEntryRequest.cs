using System;

namespace ValorChronicle.Battle.Entry
{
    /// <summary>Identifies the content selected for one battle entry attempt.</summary>
    public sealed class BattleEntryRequest
    {
        public BattleEntryRequest(string bossId, string difficultyId)
        {
            BossId = RequireId(bossId, nameof(bossId));
            DifficultyId = RequireId(difficultyId, nameof(difficultyId));
        }

        public string BossId { get; }
        public string DifficultyId { get; }

        private static string RequireId(string value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A non-empty internal content ID is required.",
                    parameterName);
            }

            return value;
        }
    }
}
