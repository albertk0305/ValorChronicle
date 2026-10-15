using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Battle.Results;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Main.Presentation;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class MainBossSelectionTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";
        private const string ElementIconsPath =
            "Assets/Data/Party/ElementIconSet.asset";

        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void Builder_WithNoBosses_ReturnsEmptyList()
        {
            DefinitionDatabase database = CreateDatabase();

            IReadOnlyList<BossSelectionItem> items = Build(
                database,
                profile: null);

            Assert.That(items, Is.Empty);
        }

        [Test]
        public void Builder_FiltersUnsupportedBossAndUsesOrdinalIdOrder()
        {
            DefinitionDatabase canonical = LoadCanonicalDatabase();
            Assert.That(canonical.TryGetBoss(
                "kragmor",
                out BossDefinition kragmor), Is.True);
            BossDefinition unsupported =
                ScriptableObject.CreateInstance<BossDefinition>();
            createdObjects.Add(unsupported);
            SetObjectProperty(unsupported, "id", "aaa_unimplemented");
            DefinitionDatabase database = CreateDatabase(
                kragmor,
                unsupported);

            IReadOnlyList<BossSelectionItem> items = Build(
                database,
                profile: null);

            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].BossId, Is.EqualTo("kragmor"));
            Assert.That(
                GetIds(items),
                Is.Ordered.Using<string>(StringComparer.Ordinal));
        }

        [Test]
        public void Builder_BindsCanonicalFaceNameAndFireIcon()
        {
            DefinitionDatabase database = LoadCanonicalDatabase();

            BossSelectionItem item = Build(database, null)[0];

            Assert.That(item.DisplayName, Is.EqualTo("Kragmor"));
            Assert.That(item.FaceSprite, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(item.FaceSprite),
                Is.EqualTo(
                    "Assets/Sprites/Boss/Kragmor/KragmorFace.png"));
            Assert.That(item.ElementSprite, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(item.ElementSprite),
                Is.EqualTo("Assets/Sprites/UI/Blocks/FireBlock.png"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Builder_NoMasterAttempt_ShowsChallengePrompt(
            bool includeUnattemptedRecord)
        {
            ProfileSaveData profile = CreateProfile();
            if (includeUnattemptedRecord)
            {
                profile.BossRecords.Add(new BossRecordSaveData
                {
                    BossId = "kragmor",
                    DifficultyId = BattleDifficultyIds.Challenge,
                    HasAttempted = false,
                    HighScore = 999999,
                    HighestGradeId = BattleGradeIds.SSS
                });
            }

            BossSelectionItem item = Build(
                LoadCanonicalDatabase(),
                profile)[0];

            Assert.That(item.BestScoreText,
                Is.EqualTo(BossSelectionListBuilder.MasterChallengePrompt));
            Assert.That(item.BestGradeSprite, Is.Null);
        }

        [TestCase("en")]
        [TestCase("ko")]
        public void Builder_NoMasterAttempt_UsesLocalizedEnglishPrompt(
            string locale)
        {
            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<
                LocalizationCatalog>(LocalizationPath);
            LocalizationService localization = catalog.CreateService();
            localization.SetLocale(locale);

            BossSelectionItem item = Build(
                LoadCanonicalDatabase(),
                profile: null,
                localization: localization)[0];

            Assert.That(item.BestScoreText,
                Is.EqualTo(BossSelectionListBuilder.MasterChallengePrompt));
            Assert.That(item.BestScoreText, Does.Not.StartWith("[["));
        }

        [Test]
        public void Builder_MissingPromptKey_UsesEnglishFallback()
        {
            LocalizationService localization = new LocalizationService(
                new[]
                {
                    LocalizationTableParser.Parse(
                        "missing_prompt_test",
                        "key\ten\tko\nkragmor\tKragmor\tKragmor")
                });
            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "Localization is missing.*Using the English fallback"));

            BossSelectionItem item = Build(
                LoadCanonicalDatabase(),
                profile: null,
                localization: localization)[0];

            Assert.That(item.BestScoreText,
                Is.EqualTo(BossSelectionListBuilder.MasterChallengePrompt));
        }

        [Test]
        public void Builder_MasterRecord_FormatsScoreAndUsesHighestGrade()
        {
            ProfileSaveData profile = CreateProfile();
            profile.BossRecords.Add(new BossRecordSaveData
            {
                BossId = "kragmor",
                DifficultyId = BattleDifficultyIds.Challenge,
                HasAttempted = true,
                HighScore = 1234567,
                HighestGradeId = BattleGradeIds.B,
                ClaimedFirstRewardGradeIds = new List<string>
                {
                    BattleGradeIds.SSS
                }
            });
            Sprite expectedGrade = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Sprites/UI/Battle/B.png");

            BossSelectionItem item = Build(
                LoadCanonicalDatabase(),
                profile,
                gradeId => gradeId == BattleGradeIds.B
                    ? expectedGrade
                    : null)[0];

            Assert.That(item.BestScoreText, Is.EqualTo("1,234,567"));
            Assert.That(item.BestGradeSprite, Is.SameAs(expectedGrade));
        }

        [Test]
        public void Builder_AttemptWithoutGrade_ShowsScoreOnly()
        {
            ProfileSaveData profile = CreateProfile();
            profile.BossRecords.Add(new BossRecordSaveData
            {
                BossId = "kragmor",
                DifficultyId = BattleDifficultyIds.Challenge,
                HasAttempted = true,
                HighScore = 4321,
                HighestGradeId = string.Empty
            });

            BossSelectionItem item = Build(
                LoadCanonicalDatabase(),
                profile)[0];

            Assert.That(item.BestScoreText, Is.EqualTo("4,321"));
            Assert.That(item.BestGradeSprite, Is.Null);
        }

        [Test]
        public void Builder_IgnoresRecordsForOtherDifficulties()
        {
            ProfileSaveData profile = CreateProfile();
            profile.BossRecords.Add(new BossRecordSaveData
            {
                BossId = "kragmor",
                DifficultyId = BattleDifficultyIds.Hard,
                HasAttempted = true,
                HighScore = 999999,
                HighestGradeId = BattleGradeIds.SSS
            });

            BossSelectionItem item = Build(
                LoadCanonicalDatabase(),
                profile)[0];

            Assert.That(item.BestScoreText,
                Is.EqualTo(BossSelectionListBuilder.MasterChallengePrompt));
            Assert.That(item.BestGradeSprite, Is.Null);
        }

        [Test]
        public void ListView_ReusesExpandsAndDoesNotDuplicateSelection()
        {
            BossSelectionListView view = CreateListView(3);
            IReadOnlyList<BossSelectionItem> items = CreateItems(4);
            int selectionCount = 0;
            string selectedId = null;
            view.SelectionRequested += id =>
            {
                selectionCount++;
                selectedId = id;
            };

            view.Show(items);
            Assert.That(view.ActiveSlotCount, Is.EqualTo(4));
            Assert.That(view.PoolCount, Is.EqualTo(4));
            Assert.That(view.ScrollRect.verticalNormalizedPosition,
                Is.EqualTo(1f));
            Assert.That(GetBoundIds(view),
                Is.EqualTo(new[] { "boss_a", "boss_b", "boss_c", "boss_d" }));

            view.Show(items);
            Assert.That(view.PoolCount, Is.EqualTo(4));
            view.Slots[2].OnPointerClick(new PointerEventData(null)
            {
                button = PointerEventData.InputButton.Left
            });

            Assert.That(selectionCount, Is.EqualTo(1));
            Assert.That(selectedId, Is.EqualTo("boss_c"));
        }

        [Test]
        public void ListView_ZeroAndOneItemsToggleInitialPoolSafely()
        {
            BossSelectionListView view = CreateListView(3);

            view.Show(Array.Empty<BossSelectionItem>());
            Assert.That(view.ActiveSlotCount, Is.Zero);
            Assert.That(view.Slots, Has.All.Matches<BossSlotView>(
                slot => !slot.gameObject.activeSelf));

            view.Show(CreateItems(1));
            Assert.That(view.ActiveSlotCount, Is.EqualTo(1));
            Assert.That(view.Slots[0].gameObject.activeSelf, Is.True);
            Assert.That(view.Slots[1].gameObject.activeSelf, Is.False);
            Assert.That(view.Content.rect.height,
                Is.EqualTo(view.ScrollRect.viewport.rect.height)
                    .Within(0.001f));
        }

        [Test]
        public void SlotView_MissingSpritesHideOnlyOptionalImages()
        {
            BossSlotView slot = CreateSlot(null);

            slot.Bind(
                new BossSelectionItem(
                    "boss_missing_art",
                    "Boss",
                    null,
                    null,
                    "1,000",
                    null),
                _ => { });

            Assert.That(slot.gameObject.activeSelf, Is.True);
            Assert.That(slot.BossFaceImage.gameObject.activeSelf, Is.False);
            Assert.That(slot.BossType.gameObject.activeSelf, Is.False);
            Assert.That(slot.BestGrade.gameObject.activeSelf, Is.False);
            Assert.That(slot.BossNameText.text, Is.EqualTo("Boss"));
            Assert.That(slot.BestScoreText.text, Is.EqualTo("1,000"));
        }

        [Test]
        public void SlotView_DraggingPointerDoesNotSelectBoss()
        {
            BossSlotView slot = CreateSlot(null);
            int count = 0;
            slot.Bind(CreateItems(1)[0], _ => count++);

            slot.OnPointerClick(new PointerEventData(null)
            {
                button = PointerEventData.InputButton.Left,
                dragging = true
            });

            Assert.That(count, Is.Zero);
        }

        private IReadOnlyList<BossSelectionItem> Build(
            DefinitionDatabase database,
            ProfileSaveData profile,
            Func<string, Sprite> gradeResolver = null,
            LocalizationService localization = null)
        {
            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<
                LocalizationCatalog>(LocalizationPath);
            ElementIconSet icons = AssetDatabase.LoadAssetAtPath<
                ElementIconSet>(ElementIconsPath);
            return BossSelectionListBuilder.Build(
                database,
                localization ?? catalog.CreateService(),
                icons,
                profile,
                gradeResolver);
        }

        private static DefinitionDatabase LoadCanonicalDatabase()
        {
            DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                DefinitionDatabase>(DatabasePath);
            database.Initialize();
            return database;
        }

        private DefinitionDatabase CreateDatabase(
            params BossDefinition[] bosses)
        {
            DefinitionDatabase database =
                ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            var serialized = new SerializedObject(database);
            SerializedProperty property = serialized.FindProperty("bosses");
            property.arraySize = bosses.Length;
            for (int index = 0; index < bosses.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    bosses[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            database.Initialize();
            return database;
        }

        private static ProfileSaveData CreateProfile()
        {
            return new ProfileSaveData
            {
                BossRecords = new List<BossRecordSaveData>()
            };
        }

        private BossSelectionListView CreateListView(int initialCount)
        {
            var viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(RectMask2D),
                typeof(ScrollRect));
            createdObjects.Add(viewportObject);
            var viewport = (RectTransform)viewportObject.transform;
            viewport.sizeDelta = new Vector2(520f, 600f);
            var contentObject = new GameObject(
                "Content",
                typeof(RectTransform));
            var content = (RectTransform)contentObject.transform;
            content.SetParent(viewport, false);
            BossSelectionListView view =
                contentObject.AddComponent<BossSelectionListView>();
            var initial = new BossSlotView[initialCount];
            for (int index = 0; index < initialCount; index++)
            {
                initial[index] = CreateSlot(content);
            }

            ScrollRect scrollRect = viewportObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            view.Configure(scrollRect, content, initial, null);
            return view;
        }

        private BossSlotView CreateSlot(RectTransform parent)
        {
            var slotObject = new GameObject(
                "Slot",
                typeof(RectTransform),
                typeof(BossSlotView));
            if (parent != null)
            {
                slotObject.transform.SetParent(parent, false);
            }
            else
            {
                createdObjects.Add(slotObject);
            }

            BossSlotView slot = slotObject.GetComponent<BossSlotView>();
            Image face = CreateImage("Face", slotObject.transform);
            TMP_Text name = CreateText("Name", slotObject.transform);
            Image type = CreateImage("Type", slotObject.transform);
            TMP_Text score = CreateText("Score", slotObject.transform);
            Image grade = CreateImage("Grade", slotObject.transform);
            Image touch = CreateImage("Touch", slotObject.transform);
            slot.Configure(face, name, type, score, grade, touch);
            return slot;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var value = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            value.transform.SetParent(parent, false);
            return value.GetComponent<Image>();
        }

        private static TMP_Text CreateText(string name, Transform parent)
        {
            var value = new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            value.transform.SetParent(parent, false);
            return value.GetComponent<TMP_Text>();
        }

        private static IReadOnlyList<BossSelectionItem> CreateItems(int count)
        {
            var items = new List<BossSelectionItem>(count);
            for (int index = 0; index < count; index++)
            {
                string id = $"boss_{(char)('a' + index)}";
                items.Add(new BossSelectionItem(
                    id,
                    id,
                    null,
                    null,
                    "0",
                    null));
            }

            return items;
        }

        private static string[] GetIds(
            IReadOnlyList<BossSelectionItem> items)
        {
            var ids = new string[items.Count];
            for (int index = 0; index < items.Count; index++)
            {
                ids[index] = items[index].BossId;
            }

            return ids;
        }

        private static string[] GetBoundIds(BossSelectionListView view)
        {
            var ids = new string[view.ActiveSlotCount];
            for (int index = 0; index < ids.Length; index++)
            {
                ids[index] = view.Slots[index].BossId;
            }

            return ids;
        }

        private static void SetObjectProperty(
            UnityEngine.Object target,
            string name,
            string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
