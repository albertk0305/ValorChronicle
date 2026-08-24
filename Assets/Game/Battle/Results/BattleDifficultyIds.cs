using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Results
{
    public static class BattleDifficultyIds
    {
        public const string Intro = "difficulty_intro";
        public const string Normal = "difficulty_normal";
        public const string Advanced = "difficulty_advanced";
        public const string Hard = "difficulty_hard";
        public const string Challenge = "difficulty_challenge";

        private static readonly string[] requiredIds =
        {
            Intro,
            Normal,
            Advanced,
            Hard,
            Challenge
        };

        private static readonly IReadOnlyList<string> readOnlyRequiredIds =
            Array.AsReadOnly(requiredIds);

        public static IReadOnlyList<string> RequiredIds =>
            readOnlyRequiredIds;
    }
}
