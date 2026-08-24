using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Results
{
    public enum BattleGrade
    {
        BelowC = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4,
        SS = 5,
        SSS = 6
    }

    public static class BattleGradeRules
    {
        private static readonly BattleGrade[] progressionGrades =
        {
            BattleGrade.C,
            BattleGrade.B,
            BattleGrade.A,
            BattleGrade.S,
            BattleGrade.SS,
            BattleGrade.SSS
        };

        private static readonly IReadOnlyList<BattleGrade>
            readOnlyProgressionGrades = Array.AsReadOnly(progressionGrades);

        public static IReadOnlyList<BattleGrade> ProgressionGrades =>
            readOnlyProgressionGrades;

        public static bool IsDefined(BattleGrade grade)
        {
            return grade >= BattleGrade.BelowC
                && grade <= BattleGrade.SSS;
        }

        public static int GetOrder(BattleGrade grade)
        {
            if (!IsDefined(grade))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grade),
                    grade,
                    "Grade must be a defined battle grade.");
            }

            return (int)grade;
        }
    }
}
