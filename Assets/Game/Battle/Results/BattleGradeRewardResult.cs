using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Results
{
    public sealed class BattleFirstGradeRewardEntry
    {
        public BattleFirstGradeRewardEntry(
            BattleGrade grade,
            string gradeId,
            long amount)
        {
            Grade = grade;
            GradeId = gradeId;
            Amount = amount;
        }

        public BattleGrade Grade { get; }
        public string GradeId { get; }
        public long Amount { get; }
    }

    public sealed class BattleGradeRewardResult
    {
        private readonly BattleFirstGradeRewardEntry[]
            firstGradeProgression;

        internal BattleGradeRewardResult(
            BattleGrade grade,
            string gradeId,
            long repeatRewardAmount,
            BattleFirstGradeRewardEntry[] firstGradeProgression)
        {
            Grade = grade;
            GradeId = gradeId;
            RepeatRewardAmount = repeatRewardAmount;
            this.firstGradeProgression = firstGradeProgression == null
                ? Array.Empty<BattleFirstGradeRewardEntry>()
                : (BattleFirstGradeRewardEntry[])
                    firstGradeProgression.Clone();

            long total = 0L;
            for (int index = 0;
                index < this.firstGradeProgression.Length;
                index++)
            {
                total = checked(
                    total + this.firstGradeProgression[index].Amount);
            }

            FirstGradeProgressionTotal = total;
        }

        public BattleGrade Grade { get; }
        public string GradeId { get; }
        public long RepeatRewardAmount { get; }
        public IReadOnlyList<BattleFirstGradeRewardEntry>
            FirstGradeProgression =>
                Array.AsReadOnly(firstGradeProgression);
        public long FirstGradeProgressionTotal { get; }
    }
}
