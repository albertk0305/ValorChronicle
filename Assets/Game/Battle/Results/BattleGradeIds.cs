using System;

namespace ValorChronicle.Battle.Results
{
    public static class BattleGradeIds
    {
        public const string C = "grade_c";
        public const string B = "grade_b";
        public const string A = "grade_a";
        public const string S = "grade_s";
        public const string SS = "grade_ss";
        public const string SSS = "grade_sss";

        public static bool TryGetRewardGradeId(
            BattleGrade grade,
            out string gradeId)
        {
            switch (grade)
            {
                case BattleGrade.C:
                    gradeId = C;
                    return true;
                case BattleGrade.B:
                    gradeId = B;
                    return true;
                case BattleGrade.A:
                    gradeId = A;
                    return true;
                case BattleGrade.S:
                    gradeId = S;
                    return true;
                case BattleGrade.SS:
                    gradeId = SS;
                    return true;
                case BattleGrade.SSS:
                    gradeId = SSS;
                    return true;
                case BattleGrade.BelowC:
                    gradeId = string.Empty;
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(grade),
                        grade,
                        "Grade must be a defined battle grade.");
            }
        }

        public static string GetRequiredRewardGradeId(BattleGrade grade)
        {
            if (!TryGetRewardGradeId(grade, out string gradeId))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grade),
                    grade,
                    "BelowC does not have a reward grade ID.");
            }

            return gradeId;
        }

        public static bool TryGetRewardGrade(
            string gradeId,
            out BattleGrade grade)
        {
            switch (gradeId)
            {
                case C:
                    grade = BattleGrade.C;
                    return true;
                case B:
                    grade = BattleGrade.B;
                    return true;
                case A:
                    grade = BattleGrade.A;
                    return true;
                case S:
                    grade = BattleGrade.S;
                    return true;
                case SS:
                    grade = BattleGrade.SS;
                    return true;
                case SSS:
                    grade = BattleGrade.SSS;
                    return true;
                default:
                    grade = BattleGrade.BelowC;
                    return false;
            }
        }
    }
}
