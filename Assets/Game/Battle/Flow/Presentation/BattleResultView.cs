using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Battle.Results;
using ValorChronicle.Battle.Results.Persistence;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Localization;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleResultView : MonoBehaviour
    {
        private const string BestScoreFormatKey =
            "ui.battle.result.best_score_format";
        private const string ScoreFormatKey =
            "ui.battle.result.score_format";
        private const string TurnFormatKey =
            "ui.battle.result.turn_format";
        private const string DamageFormatKey =
            "ui.battle.result.damage_format";
        private const string TurnBonusFormatKey =
            "ui.battle.result.turn_bonus_format";

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

        private LocalizationService localizationService;
        private GameBootstrapper subscribedBootstrapper;
        private BattlePersistedResult currentResult;
        private bool localeSubscribed;

        public TMP_Text BestScoreText => bestScoreText;
        public Image BestScoreIcon => bestScoreIcon;
        public TMP_Text ResultText => resultText;
        public TMP_Text TurnText => turnText;
        public TMP_Text DamageScoreText => damageScoreText;
        public TMP_Text TurnLeftScoreText => turnLeftScoreText;
        public Image ScoreGrade => scoreGrade;
        public TMP_Text RewardText => rewardText;
        public TMP_Text InitialRewardText => initialRewardText;
        public BattlePersistedResult CurrentResult => currentResult;

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

        private void OnEnable()
        {
            TryResolveLocalizationService();
            SubscribeLocalization();
            RenderCurrentResult();
        }

        private void OnDisable()
        {
            UnsubscribeLocalization();
            UnsubscribeBootstrapper();
        }

        private void OnDestroy()
        {
            UnsubscribeLocalization();
            UnsubscribeBootstrapper();
        }

        public void InitializeLocalization(LocalizationService service)
        {
            UnsubscribeLocalization();
            UnsubscribeBootstrapper();
            localizationService = service
                ?? throw new ArgumentNullException(nameof(service));

            if (isActiveAndEnabled)
            {
                SubscribeLocalization();
            }

            RenderCurrentResult();
        }

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

            currentResult = persistedResult;
            TryResolveLocalizationService();
            RenderCurrentResult();
        }

        private void RenderCurrentResult()
        {
            BattlePersistedResult persistedResult = currentResult;
            BattleFinalResult finalResult =
                persistedResult?.BattleFinalResult;
            if (finalResult == null)
            {
                return;
            }

            string bestScore = Format(persistedResult.SavedHighScore);
            string finalScore = Format(finalResult.FinalScore);
            string damageScore = Format(finalResult.DamageScore);
            string turnBonus = Format(finalResult.RemainingTurnBonus);
            bestScoreText.text = FormatLocalized(
                BestScoreFormatKey,
                "Best: " + bestScore,
                new Dictionary<string, object>
                {
                    ["score"] = bestScore
                });
            resultText.text = FormatLocalized(
                ScoreFormatKey,
                "Score: " + finalScore,
                new Dictionary<string, object>
                {
                    ["score"] = finalScore
                });
            turnText.text = FormatLocalized(
                TurnFormatKey,
                string.Concat(
                    "Turn: ",
                    finalResult.FinishedTurn.ToString(
                        CultureInfo.InvariantCulture),
                    "/",
                    finalResult.TurnLimit.ToString(
                        CultureInfo.InvariantCulture)),
                new Dictionary<string, object>
                {
                    ["current"] = finalResult.FinishedTurn,
                    ["limit"] = finalResult.TurnLimit
                });
            damageScoreText.text = FormatLocalized(
                DamageFormatKey,
                "Damage: " + damageScore,
                new Dictionary<string, object>
                {
                    ["score"] = damageScore
                });
            turnLeftScoreText.text = FormatLocalized(
                TurnBonusFormatKey,
                "Turn Bonus: " + turnBonus,
                new Dictionary<string, object>
                {
                    ["score"] = turnBonus
                });
            rewardText.text = Format(persistedResult.RepeatRewardAmount);
            initialRewardText.text =
                Format(persistedResult.FirstGradeRewardAmount);

            bestScoreIcon.gameObject.SetActive(
                persistedResult.IsNewHighScore);
            RenderGrade(finalResult.Grade);
        }

        public void ResetPresentation()
        {
            currentResult = null;
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

        private string FormatLocalized(
            string key,
            string fallback,
            IReadOnlyDictionary<string, object> arguments)
        {
            return localizationService != null
                ? localizationService.Format(key, arguments)
                : fallback;
        }

        private void TryResolveLocalizationService()
        {
            if (localizationService != null)
            {
                return;
            }

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
            {
                return;
            }

            if (bootstrapper.LocalizationService != null)
            {
                localizationService = bootstrapper.LocalizationService;
                UnsubscribeBootstrapper();
                return;
            }

            if (!isActiveAndEnabled
                || ReferenceEquals(subscribedBootstrapper, bootstrapper))
            {
                return;
            }

            UnsubscribeBootstrapper();
            subscribedBootstrapper = bootstrapper;
            subscribedBootstrapper.InitializationCompleted +=
                HandleBootstrapInitializationCompleted;
        }

        private void HandleBootstrapInitializationCompleted(
            BootstrapInitializationResult _)
        {
            GameBootstrapper bootstrapper = subscribedBootstrapper;
            UnsubscribeBootstrapper();
            if (bootstrapper?.LocalizationService == null)
            {
                return;
            }

            localizationService = bootstrapper.LocalizationService;
            SubscribeLocalization();
            RenderCurrentResult();
        }

        private void SubscribeLocalization()
        {
            if (localeSubscribed || localizationService == null)
            {
                return;
            }

            localizationService.LocaleChanged += HandleLocaleChanged;
            localeSubscribed = true;
        }

        private void UnsubscribeLocalization()
        {
            if (!localeSubscribed || localizationService == null)
            {
                return;
            }

            localizationService.LocaleChanged -= HandleLocaleChanged;
            localeSubscribed = false;
        }

        private void UnsubscribeBootstrapper()
        {
            if (subscribedBootstrapper == null)
            {
                return;
            }

            subscribedBootstrapper.InitializationCompleted -=
                HandleBootstrapInitializationCompleted;
            subscribedBootstrapper = null;
        }

        private void HandleLocaleChanged(string _)
        {
            RenderCurrentResult();
        }
    }
}
