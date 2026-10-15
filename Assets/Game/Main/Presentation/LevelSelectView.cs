using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Battle.Results;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Main.Presentation
{
    public sealed class LevelSelectView : MonoBehaviour
    {
        [SerializeField] private Image bossFaceImage = null;
        [SerializeField] private TMP_Text bossNameText = null;
        [SerializeField] private Image bossType = null;
        [SerializeField] private TMP_Text bestScoreText = null;
        [SerializeField] private Image bestGrade = null;
        [SerializeField] private BossSelectionListView gradeSpriteSource = null;
        [SerializeField] private Button beginnerButton = null;
        [SerializeField] private Button standardButton = null;
        [SerializeField] private Button advancedButton = null;
        [SerializeField] private Button expertButton = null;
        [SerializeField] private Button masterButton = null;
        [SerializeField] private Image beginnerBestGrade = null;
        [SerializeField] private Image standardBestGrade = null;
        [SerializeField] private Image advancedBestGrade = null;
        [SerializeField] private Image expertBestGrade = null;
        [SerializeField] private Image masterBestGrade = null;
        [SerializeField] private Button returnButton = null;

        private bool returnListenerRegistered;
        private bool difficultyListenersRegistered;

        public event Action ReturnRequested;
        public event Action<string> DifficultySelected;

        public string BossId { get; private set; }
        public Image BossFaceImage => bossFaceImage;
        public TMP_Text BossNameText => bossNameText;
        public Image BossType => bossType;
        public TMP_Text BestScoreText => bestScoreText;
        public Image BestGrade => bestGrade;
        public Button ReturnButton => returnButton;
        public bool IsConfigured =>
            bossFaceImage != null
            && bossNameText != null
            && bossType != null
            && bestScoreText != null
            && bestGrade != null
            && gradeSpriteSource != null
            && beginnerButton != null
            && standardButton != null
            && advancedButton != null
            && expertButton != null
            && masterButton != null
            && beginnerBestGrade != null
            && standardBestGrade != null
            && advancedBestGrade != null
            && expertBestGrade != null
            && masterBestGrade != null
            && returnButton != null;

        public void Configure(
            Image configuredBossFaceImage,
            TMP_Text configuredBossNameText,
            Image configuredBossType,
            TMP_Text configuredBestScoreText,
            Image configuredBestGrade,
            BossSelectionListView configuredGradeSpriteSource,
            Button configuredBeginnerButton,
            Button configuredStandardButton,
            Button configuredAdvancedButton,
            Button configuredExpertButton,
            Button configuredMasterButton,
            Image configuredBeginnerBestGrade,
            Image configuredStandardBestGrade,
            Image configuredAdvancedBestGrade,
            Image configuredExpertBestGrade,
            Image configuredMasterBestGrade,
            Button configuredReturnButton)
        {
            RemoveReturnListener();
            RemoveDifficultyListeners();
            bossFaceImage = configuredBossFaceImage;
            bossNameText = configuredBossNameText;
            bossType = configuredBossType;
            bestScoreText = configuredBestScoreText;
            bestGrade = configuredBestGrade;
            gradeSpriteSource = configuredGradeSpriteSource;
            beginnerButton = configuredBeginnerButton;
            standardButton = configuredStandardButton;
            advancedButton = configuredAdvancedButton;
            expertButton = configuredExpertButton;
            masterButton = configuredMasterButton;
            beginnerBestGrade = configuredBeginnerBestGrade;
            standardBestGrade = configuredStandardBestGrade;
            advancedBestGrade = configuredAdvancedBestGrade;
            expertBestGrade = configuredExpertBestGrade;
            masterBestGrade = configuredMasterBestGrade;
            returnButton = configuredReturnButton;
            AddReturnListener();
            AddDifficultyListeners();
        }

        public void Bind(
            BossSelectionItem item,
            ProfileSaveData profile)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            BossId = item.BossId;
            bossNameText.text = item.DisplayName;
            bestScoreText.text = item.BestScoreText;
            SetOptionalSprite(bossFaceImage, item.FaceSprite);
            SetOptionalSprite(bossType, item.ElementSprite);
            SetOptionalSprite(bestGrade, item.BestGradeSprite);

            BindDifficultyGrade(
                beginnerBestGrade,
                profile,
                BattleDifficultyIds.Intro);
            BindDifficultyGrade(
                standardBestGrade,
                profile,
                BattleDifficultyIds.Normal);
            BindDifficultyGrade(
                advancedBestGrade,
                profile,
                BattleDifficultyIds.Advanced);
            BindDifficultyGrade(
                expertBestGrade,
                profile,
                BattleDifficultyIds.Hard);
            BindDifficultyGrade(
                masterBestGrade,
                profile,
                BattleDifficultyIds.Challenge);
        }

        public void ResetPresentation()
        {
            BossId = null;
            if (bossNameText != null)
            {
                bossNameText.text = string.Empty;
            }

            if (bestScoreText != null)
            {
                bestScoreText.text = string.Empty;
            }

            SetOptionalSpriteIfAssigned(bossFaceImage, null);
            SetOptionalSpriteIfAssigned(bossType, null);
            SetOptionalSpriteIfAssigned(bestGrade, null);
            SetOptionalSpriteIfAssigned(beginnerBestGrade, null);
            SetOptionalSpriteIfAssigned(standardBestGrade, null);
            SetOptionalSpriteIfAssigned(advancedBestGrade, null);
            SetOptionalSpriteIfAssigned(expertBestGrade, null);
            SetOptionalSpriteIfAssigned(masterBestGrade, null);
        }

        public bool TryGetDifficultyId(
            Button button,
            out string difficultyId)
        {
            if (button != null && button == beginnerButton)
            {
                difficultyId = BattleDifficultyIds.Intro;
                return true;
            }

            if (button != null && button == standardButton)
            {
                difficultyId = BattleDifficultyIds.Normal;
                return true;
            }

            if (button != null && button == advancedButton)
            {
                difficultyId = BattleDifficultyIds.Advanced;
                return true;
            }

            if (button != null && button == expertButton)
            {
                difficultyId = BattleDifficultyIds.Hard;
                return true;
            }

            if (button != null && button == masterButton)
            {
                difficultyId = BattleDifficultyIds.Challenge;
                return true;
            }

            difficultyId = null;
            return false;
        }

        private void Awake()
        {
            AddReturnListener();
            AddDifficultyListeners();
        }

        private void OnDestroy()
        {
            RemoveReturnListener();
            RemoveDifficultyListeners();
        }

        private void BindDifficultyGrade(
            Image image,
            ProfileSaveData profile,
            string difficultyId)
        {
            BossRecordSaveData record = BossSelectionListBuilder.FindRecord(
                profile,
                BossId,
                difficultyId);
            if (gradeSpriteSource == null)
            {
                SetOptionalSprite(image, null);
                return;
            }

            Sprite gradeSprite = BossSelectionListBuilder
                .ResolveBestGradeSprite(
                    record,
                    gradeSpriteSource.ResolveGradeSprite,
                    this);
            SetOptionalSprite(image, gradeSprite);
        }

        private void AddReturnListener()
        {
            if (returnListenerRegistered || returnButton == null)
            {
                return;
            }

            returnButton.onClick.AddListener(HandleReturnClicked);
            returnListenerRegistered = true;
        }

        private void RemoveReturnListener()
        {
            if (!returnListenerRegistered || returnButton == null)
            {
                returnListenerRegistered = false;
                return;
            }

            returnButton.onClick.RemoveListener(HandleReturnClicked);
            returnListenerRegistered = false;
        }

        private void HandleReturnClicked()
        {
            ReturnRequested?.Invoke();
        }

        private void AddDifficultyListeners()
        {
            if (difficultyListenersRegistered
                || beginnerButton == null
                || standardButton == null
                || advancedButton == null
                || expertButton == null
                || masterButton == null)
            {
                return;
            }

            beginnerButton.onClick.AddListener(HandleBeginnerClicked);
            standardButton.onClick.AddListener(HandleStandardClicked);
            advancedButton.onClick.AddListener(HandleAdvancedClicked);
            expertButton.onClick.AddListener(HandleExpertClicked);
            masterButton.onClick.AddListener(HandleMasterClicked);
            difficultyListenersRegistered = true;
        }

        private void RemoveDifficultyListeners()
        {
            if (!difficultyListenersRegistered)
            {
                return;
            }

            beginnerButton?.onClick.RemoveListener(HandleBeginnerClicked);
            standardButton?.onClick.RemoveListener(HandleStandardClicked);
            advancedButton?.onClick.RemoveListener(HandleAdvancedClicked);
            expertButton?.onClick.RemoveListener(HandleExpertClicked);
            masterButton?.onClick.RemoveListener(HandleMasterClicked);
            difficultyListenersRegistered = false;
        }

        private void HandleBeginnerClicked()
        {
            RaiseDifficultySelected(beginnerButton);
        }

        private void HandleStandardClicked()
        {
            RaiseDifficultySelected(standardButton);
        }

        private void HandleAdvancedClicked()
        {
            RaiseDifficultySelected(advancedButton);
        }

        private void HandleExpertClicked()
        {
            RaiseDifficultySelected(expertButton);
        }

        private void HandleMasterClicked()
        {
            RaiseDifficultySelected(masterButton);
        }

        private void RaiseDifficultySelected(Button button)
        {
            if (TryGetDifficultyId(button, out string difficultyId))
            {
                DifficultySelected?.Invoke(difficultyId);
            }
        }

        private static void SetOptionalSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.gameObject.SetActive(sprite != null);
        }

        private static void SetOptionalSpriteIfAssigned(
            Image image,
            Sprite sprite)
        {
            if (image != null)
            {
                SetOptionalSprite(image, sprite);
            }
        }
    }
}
