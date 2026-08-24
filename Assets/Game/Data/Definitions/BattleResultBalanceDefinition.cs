using UnityEngine;
using ValorChronicle.Battle.Results;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "BattleResultBalanceDefinition",
        menuName = "Valor Chronicle/Definitions/Battle Result Balance")]
    public sealed class BattleResultBalanceDefinition : ScriptableObject
    {
        [SerializeField]
        private int remainingTurnBonusPercent;

        [SerializeField]
        private BattleGradeThresholdEntry[] gradeThresholds =
            System.Array.Empty<BattleGradeThresholdEntry>();

        [SerializeField]
        private BattleGradeRewardEntry[] firstGradeRewards =
            System.Array.Empty<BattleGradeRewardEntry>();

        [SerializeField]
        private BattleDifficultyRepeatRewardTable[] difficultyRepeatRewards =
            System.Array.Empty<BattleDifficultyRepeatRewardTable>();

        public BattleResultBalance CreateBalance()
        {
            return new BattleResultBalance(
                remainingTurnBonusPercent / 100m,
                gradeThresholds,
                firstGradeRewards,
                difficultyRepeatRewards);
        }

        public bool TryValidate(out string errorMessage)
        {
            return BattleResultBalanceValidator.TryValidate(
                CreateBalance(),
                out errorMessage);
        }

    }
}
