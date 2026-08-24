using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Battle.Results;
using ValorChronicle.Battle.Results.Persistence;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleResultView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text bestScoreText = null;

        [SerializeField]
        private Image bestScoreIcon = null;

        [SerializeField]
        private TMP_Text resultText = null;

        [SerializeField]
        private TMP_Text turnText = null;

        [SerializeField]
        private TMP_Text damageScoreText = null;

        [SerializeField]
        private TMP_Text turnLeftScoreText = null;

        [SerializeField]
        private Image scoreGrade = null;

        [SerializeField]
        private TMP_Text rewardText = null;

        [SerializeField]
        private TMP_Text initialRewardText = null;

        [SerializeField]
        private Sprite gradeC = null;

        [SerializeField]
        private Sprite gradeB = null;

        [SerializeField]
        private Sprite gradeA = null;

        [SerializeField]
        private Sprite gradeS = null;

        [SerializeField]
        private Sprite gradeSS = null;

        [SerializeField]
        private Sprite gradeSSS = null;

        public TMP_Text BestScoreText => bestScoreText;
        public Image BestScoreIcon => bestScoreIcon;
        public TMP_Text ResultText => resultText;
        public TMP_Text TurnText => turnText;
        public TMP_Text DamageScoreText => damageScoreText;
        public TMP_Text TurnLeftScoreText => turnLeftScoreText;
        public Image ScoreGrade => scoreGrade;
        public TMP_Text RewardText => rewardText;
        public TMP_Text InitialRewardText => initialRewardText;

        public bool IsConfigured =>
            bestScoreText != null
            && bestScoreIcon != null
            && resultText != null
            && turnText != null
            && damageScoreText != null
            && turnLeftScoreText != null
            && scoreGrade != null
            && rewardText != null
            && initialRewardText != null
            && gradeC != null
            && gradeB != null
            && gradeA != null
            && gradeS != null
            && gradeSS != null
            && gradeSSS != null;

        public void Render(BattlePersistedResult persistedResult)
        {
            if (persistedResult == null)
            {
                throw new ArgumentNullException(nameof(persistedResult));
            }

            BattleFinalResult finalResult =
                persistedResult.BattleFinalResult
                ?? throw new ArgumentException(
                    "A persisted result must contain its final result.",
                    nameof(persistedResult));

            bestScoreText.text =
                $"Best: {Format(persistedResult.SavedHighScore)}";
            resultText.text = $"Score: {Format(finalResult.FinalScore)}";
            turnText.text =
                $"Turn: {finalResult.FinishedTurn}/{finalResult.TurnLimit}";
            damageScoreText.text =
                $"Damage: {Format(finalResult.DamageScore)}";
            turnLeftScoreText.text =
                $"Turn Bonus: {Format(finalResult.RemainingTurnBonus)}";
            rewardText.text = Format(persistedResult.RepeatRewardAmount);
            initialRewardText.text =
                Format(persistedResult.FirstGradeRewardAmount);

            bestScoreIcon.gameObject.SetActive(
                persistedResult.IsNewHighScore);
            RenderGrade(finalResult.Grade);
        }

        public void ResetPresentation()
        {
            gameObject.SetActive(false);
            bestScoreText.text = string.Empty;
            resultText.text = string.Empty;
            turnText.text = string.Empty;
            damageScoreText.text = string.Empty;
            turnLeftScoreText.text = string.Empty;
            rewardText.text = string.Empty;
            initialRewardText.text = string.Empty;
            bestScoreIcon.gameObject.SetActive(false);
            scoreGrade.sprite = null;
            scoreGrade.gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public Sprite GetGradeSprite(BattleGrade grade)
        {
            switch (grade)
            {
                case BattleGrade.C:
                    return gradeC;
                case BattleGrade.B:
                    return gradeB;
                case BattleGrade.A:
                    return gradeA;
                case BattleGrade.S:
                    return gradeS;
                case BattleGrade.SS:
                    return gradeSS;
                case BattleGrade.SSS:
                    return gradeSSS;
                case BattleGrade.BelowC:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(grade),
                        grade,
                        "Unsupported battle grade.");
            }
        }

        private void RenderGrade(BattleGrade grade)
        {
            Sprite sprite = GetGradeSprite(grade);
            scoreGrade.sprite = sprite;
            scoreGrade.gameObject.SetActive(sprite != null);
        }

        private static string Format(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
