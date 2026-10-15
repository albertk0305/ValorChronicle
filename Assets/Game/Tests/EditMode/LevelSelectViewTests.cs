using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Battle.Results;
using ValorChronicle.Main.Presentation;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class LevelSelectViewTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void DifficultyButtons_MapToCanonicalDifficultyIds()
        {
            ViewFixture fixture = CreateView();

            AssertMapping(
                fixture,
                fixture.BeginnerButton,
                BattleDifficultyIds.Intro);
            AssertMapping(
                fixture,
                fixture.StandardButton,
                BattleDifficultyIds.Normal);
            AssertMapping(
                fixture,
                fixture.AdvancedButton,
                BattleDifficultyIds.Advanced);
            AssertMapping(
                fixture,
                fixture.ExpertButton,
                BattleDifficultyIds.Hard);
            AssertMapping(
                fixture,
                fixture.MasterButton,
                BattleDifficultyIds.Challenge);
            Assert.That(
                fixture.View.TryGetDifficultyId(null, out _),
                Is.False);
        }

        [Test]
        public void DifficultyButtons_RaiseMappedSelectionOncePerClick()
        {
            ViewFixture fixture = CreateView();
            var received = new List<string>();
            fixture.View.DifficultySelected += received.Add;

            fixture.BeginnerButton.onClick.Invoke();
            fixture.StandardButton.onClick.Invoke();
            fixture.AdvancedButton.onClick.Invoke();
            fixture.ExpertButton.onClick.Invoke();
            fixture.MasterButton.onClick.Invoke();

            Assert.That(received, Is.EqualTo(new[]
            {
                BattleDifficultyIds.Intro,
                BattleDifficultyIds.Normal,
                BattleDifficultyIds.Advanced,
                BattleDifficultyIds.Hard,
                BattleDifficultyIds.Challenge
            }));
        }

        [Test]
        public void Bind_ShowsBossSummaryAndPerDifficultyHighestGrades()
        {
            ViewFixture fixture = CreateView();
            var profile = new ProfileSaveData
            {
                BossRecords = new List<BossRecordSaveData>
                {
                    Record(
                        BattleDifficultyIds.Intro,
                        true,
                        BattleGradeIds.C,
                        BattleGradeIds.SSS),
                    Record(
                        BattleDifficultyIds.Normal,
                        true,
                        BattleGradeIds.B),
                    Record(
                        BattleDifficultyIds.Advanced,
                        false,
                        BattleGradeIds.A),
                    Record(
                        BattleDifficultyIds.Hard,
                        true,
                        string.Empty),
                    Record(
                        BattleDifficultyIds.Challenge,
                        true,
                        BattleGradeIds.SSS)
                }
            };
            Sprite face = LoadSprite(
                "Assets/Sprites/Boss/Kragmor/KragmorFace.png");
            Sprite element = LoadSprite(
                "Assets/Sprites/UI/Blocks/FireBlock.png");

            fixture.View.Bind(
                new BossSelectionItem(
                    "kragmor",
                    "Kragmor",
                    face,
                    element,
                    "1,234",
                    fixture.GradeSSS),
                profile);

            Assert.That(fixture.View.BossId, Is.EqualTo("kragmor"));
            Assert.That(fixture.View.BossNameText.text,
                Is.EqualTo("Kragmor"));
            Assert.That(fixture.View.BestScoreText.text,
                Is.EqualTo("1,234"));
            Assert.That(fixture.View.BossFaceImage.sprite, Is.SameAs(face));
            Assert.That(fixture.View.BossType.sprite, Is.SameAs(element));
            Assert.That(fixture.BeginnerGrade.sprite,
                Is.SameAs(fixture.GradeC));
            Assert.That(fixture.StandardGrade.sprite,
                Is.SameAs(fixture.GradeB));
            Assert.That(fixture.AdvancedGrade.gameObject.activeSelf,
                Is.False);
            Assert.That(fixture.ExpertGrade.gameObject.activeSelf,
                Is.False);
            Assert.That(fixture.MasterGrade.sprite,
                Is.SameAs(fixture.GradeSSS));
        }

        [Test]
        public void Bind_NullProfile_HidesEveryDifficultyGrade()
        {
            ViewFixture fixture = CreateView();
            fixture.View.Bind(
                CreateItem(fixture),
                new ProfileSaveData
                {
                    BossRecords = new List<BossRecordSaveData>
                    {
                        Record(
                            BattleDifficultyIds.Intro,
                            true,
                            BattleGradeIds.C)
                    }
                });
            Assert.That(fixture.BeginnerGrade.gameObject.activeSelf, Is.True);

            fixture.View.Bind(CreateItem(fixture), null);

            Assert.That(fixture.BeginnerGrade.gameObject.activeSelf, Is.False);
            Assert.That(fixture.StandardGrade.gameObject.activeSelf, Is.False);
            Assert.That(fixture.AdvancedGrade.gameObject.activeSelf, Is.False);
            Assert.That(fixture.ExpertGrade.gameObject.activeSelf, Is.False);
            Assert.That(fixture.MasterGrade.gameObject.activeSelf, Is.False);
        }

        [TestCase(BattleGradeIds.C)]
        [TestCase(BattleGradeIds.B)]
        [TestCase(BattleGradeIds.A)]
        [TestCase(BattleGradeIds.S)]
        [TestCase(BattleGradeIds.SS)]
        [TestCase(BattleGradeIds.SSS)]
        public void Bind_ValidGrade_UsesMatchingConfiguredSprite(
            string gradeId)
        {
            ViewFixture fixture = CreateView();
            var profile = new ProfileSaveData
            {
                BossRecords = new List<BossRecordSaveData>
                {
                    new BossRecordSaveData
                    {
                        BossId = "other_boss",
                        DifficultyId = BattleDifficultyIds.Intro,
                        HasAttempted = true,
                        HighestGradeId = BattleGradeIds.SSS
                    },
                    Record(BattleDifficultyIds.Intro, true, gradeId)
                }
            };

            fixture.View.Bind(CreateItem(fixture), profile);

            Assert.That(
                fixture.BeginnerGrade.sprite,
                Is.SameAs(fixture.GradeSource.ResolveGradeSprite(gradeId)));
        }

        [Test]
        public void Bind_InvalidGrade_HidesIconAndLogsError()
        {
            ViewFixture fixture = CreateView();
            var profile = new ProfileSaveData
            {
                BossRecords = new List<BossRecordSaveData>
                {
                    Record(
                        BattleDifficultyIds.Intro,
                        true,
                        "grade_invalid")
                }
            };
            LogAssert.Expect(
                LogType.Error,
                "[BossSelection] Unknown grade ID 'grade_invalid' "
                    + "for boss 'kragmor'.");

            fixture.View.Bind(CreateItem(fixture), profile);

            Assert.That(fixture.BeginnerGrade.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ResetAndReturn_AreSafeAndReturnListenerIsNotDuplicated()
        {
            ViewFixture fixture = CreateView();
            int returnCount = 0;
            fixture.View.ReturnRequested += () => returnCount++;
            fixture.View.Bind(CreateItem(fixture), null);

            fixture.ReturnButton.onClick.Invoke();
            fixture.View.ResetPresentation();
            fixture.View.ResetPresentation();

            Assert.That(returnCount, Is.EqualTo(1));
            Assert.That(fixture.View.BossId, Is.Null);
            Assert.That(fixture.View.BossNameText.text, Is.Empty);
            Assert.That(fixture.View.BestScoreText.text, Is.Empty);
            Assert.That(fixture.View.BossFaceImage.gameObject.activeSelf,
                Is.False);
        }

        private ViewFixture CreateView()
        {
            var root = new GameObject("LevelSelect", typeof(RectTransform));
            createdObjects.Add(root);
            var gradeSourceObject = new GameObject(
                "GradeSource",
                typeof(RectTransform),
                typeof(BossSelectionListView));
            gradeSourceObject.transform.SetParent(root.transform, false);
            BossSelectionListView gradeSource =
                gradeSourceObject.GetComponent<BossSelectionListView>();
            Sprite gradeC = LoadSprite("Assets/Sprites/UI/Battle/C.png");
            Sprite gradeB = LoadSprite("Assets/Sprites/UI/Battle/B.png");
            Sprite gradeA = LoadSprite("Assets/Sprites/UI/Battle/A.png");
            Sprite gradeS = LoadSprite("Assets/Sprites/UI/Battle/S.png");
            Sprite gradeSS = LoadSprite("Assets/Sprites/UI/Battle/SS.png");
            Sprite gradeSSS = LoadSprite("Assets/Sprites/UI/Battle/SSS.png");
            gradeSource.Configure(
                null,
                null,
                null,
                null,
                gradeC,
                gradeB,
                gradeA,
                gradeS,
                gradeSS,
                gradeSSS);

            LevelSelectView view = root.AddComponent<LevelSelectView>();
            Image face = CreateImage("Face", root.transform);
            TMP_Text name = CreateText("Name", root.transform);
            Image type = CreateImage("Type", root.transform);
            TMP_Text score = CreateText("Score", root.transform);
            Image bestGrade = CreateImage("BestGrade", root.transform);
            Button beginner = CreateButton("Beginner", root.transform);
            Button standard = CreateButton("Standard", root.transform);
            Button advanced = CreateButton("Advanced", root.transform);
            Button expert = CreateButton("Expert", root.transform);
            Button master = CreateButton("Master", root.transform);
            Image beginnerGrade = CreateImage("BeginnerGrade", root.transform);
            Image standardGrade = CreateImage("StandardGrade", root.transform);
            Image advancedGrade = CreateImage("AdvancedGrade", root.transform);
            Image expertGrade = CreateImage("ExpertGrade", root.transform);
            Image masterGrade = CreateImage("MasterGrade", root.transform);
            Button returnButton = CreateButton("Return", root.transform);
            view.Configure(
                face,
                name,
                type,
                score,
                bestGrade,
                gradeSource,
                beginner,
                standard,
                advanced,
                expert,
                master,
                beginnerGrade,
                standardGrade,
                advancedGrade,
                expertGrade,
                masterGrade,
                returnButton);

            return new ViewFixture(
                view,
                gradeSource,
                beginner,
                standard,
                advanced,
                expert,
                master,
                beginnerGrade,
                standardGrade,
                advancedGrade,
                expertGrade,
                masterGrade,
                returnButton,
                gradeC,
                gradeB,
                gradeSSS);
        }

        private static BossRecordSaveData Record(
            string difficultyId,
            bool attempted,
            string gradeId,
            params string[] claimedGradeIds)
        {
            return new BossRecordSaveData
            {
                BossId = "kragmor",
                DifficultyId = difficultyId,
                HasAttempted = attempted,
                HighestGradeId = gradeId,
                ClaimedFirstRewardGradeIds =
                    new List<string>(claimedGradeIds)
            };
        }

        private static BossSelectionItem CreateItem(ViewFixture fixture)
        {
            return new BossSelectionItem(
                "kragmor",
                "Kragmor",
                fixture.GradeC,
                fixture.GradeB,
                "0",
                fixture.GradeSSS);
        }

        private static void AssertMapping(
            ViewFixture fixture,
            Button button,
            string expectedId)
        {
            Assert.That(
                fixture.View.TryGetDifficultyId(button, out string actualId),
                Is.True);
            Assert.That(actualId, Is.EqualTo(expectedId));
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.That(sprite, Is.Not.Null, path);
            return sprite;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Image>();
        }

        private static TMP_Text CreateText(string name, Transform parent)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<TMP_Text>();
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Button>();
        }

        private sealed class ViewFixture
        {
            public ViewFixture(
                LevelSelectView view,
                BossSelectionListView gradeSource,
                Button beginnerButton,
                Button standardButton,
                Button advancedButton,
                Button expertButton,
                Button masterButton,
                Image beginnerGrade,
                Image standardGrade,
                Image advancedGrade,
                Image expertGrade,
                Image masterGrade,
                Button returnButton,
                Sprite gradeC,
                Sprite gradeB,
                Sprite gradeSSS)
            {
                View = view;
                GradeSource = gradeSource;
                BeginnerButton = beginnerButton;
                StandardButton = standardButton;
                AdvancedButton = advancedButton;
                ExpertButton = expertButton;
                MasterButton = masterButton;
                BeginnerGrade = beginnerGrade;
                StandardGrade = standardGrade;
                AdvancedGrade = advancedGrade;
                ExpertGrade = expertGrade;
                MasterGrade = masterGrade;
                ReturnButton = returnButton;
                GradeC = gradeC;
                GradeB = gradeB;
                GradeSSS = gradeSSS;
            }

            public LevelSelectView View { get; }
            public BossSelectionListView GradeSource { get; }
            public Button BeginnerButton { get; }
            public Button StandardButton { get; }
            public Button AdvancedButton { get; }
            public Button ExpertButton { get; }
            public Button MasterButton { get; }
            public Image BeginnerGrade { get; }
            public Image StandardGrade { get; }
            public Image AdvancedGrade { get; }
            public Image ExpertGrade { get; }
            public Image MasterGrade { get; }
            public Button ReturnButton { get; }
            public Sprite GradeC { get; }
            public Sprite GradeB { get; }
            public Sprite GradeSSS { get; }
        }
    }
}
