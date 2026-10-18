using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Relics.Stats;

namespace ValorChronicle.Tests.EditMode.Characters.Presentation
{
    public sealed class CharacterStatsLookupTests
    {
        private const string MareaId = "character_marea_bluefang";
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(createdObjects[index]);
            }
        }

        [Test]
        public void Build_UsesFinalStatsBaseCriticalsAndActualModifiers()
        {
            LocalizationService localization = CreateLocalization();
            CharacterDefinition definition = GetMareaDefinition();
            var relics = new RelicStatBonusSummary(
                0d,
                0.12d,
                new[]
                {
                    new RelicStatBonusSource(
                        "relic_victors_broken_sword",
                        "relic.victors_broken_sword.name",
                        "relic.victors_broken_sword.description",
                        RelicEffectType.AttackIncrease,
                        2,
                        0.12d)
                });
            CharacterUpgradePresentationModel model = Model(
                awakening: 1,
                maxHp: 4321,
                attack: 987,
                attackMultiplier: 1.05d,
                relics: relics);

            string text = new CharacterStatsLookupTextBuilder(localization)
                .Build(model, definition);

            Assert.That(text, Does.Contain("Level: 50"));
            Assert.That(text, Does.Contain("Final ATK: 987"));
            Assert.That(text, Does.Contain("Final Max HP: 4321"));
            Assert.That(text, Does.Contain("Critical Chance: 0%"));
            Assert.That(text, Does.Contain("Critical Damage: 150%"));
            Assert.That(text, Does.Contain("Awakening: ATK +5%"));
            Assert.That(text, Does.Contain(
                "Victor's Broken Sword (Awakening 2): ATK +12%"));
            Assert.That(text, Does.Not.Contain("Max HP +0%"));
        }

        [Test]
        public void Build_LocalizesAndOnlyShowsUnlockedConditionalEffects()
        {
            LocalizationService localization = CreateLocalization();
            CharacterDefinition definition = GetMareaDefinition();
            var builder = new CharacterStatsLookupTextBuilder(localization);

            string awakeningFive = builder.Build(
                Model(awakening: 5),
                definition);
            Assert.That(awakeningFive, Does.Contain("Predator's Instinct"));
            Assert.That(awakeningFive, Does.Not.Contain("Lingering Surge"));

            localization.SetLocale("ko");
            string awakeningSix = builder.Build(
                Model(awakening: 6),
                definition);
            Assert.That(awakeningSix, Does.Contain("상세 능력치"));
            Assert.That(awakeningSix, Does.Contain("포식자의 본능"));
            Assert.That(awakeningSix, Does.Contain("여류 파도"));
            Assert.That(awakeningSix, Does.Contain("30%"));
        }

        [Test]
        public void Panel_ShowConfiguresVerticalScrollResetsTopAndCloseIsNotDuplicated()
        {
            GameObject root = Track(new GameObject(
                "StatsPanel",
                typeof(RectTransform)));
            GameObject closeObject = Track(new GameObject(
                "Close",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)));
            closeObject.transform.SetParent(root.transform, false);
            GameObject viewportObject = Track(new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image)));
            viewportObject.transform.SetParent(root.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.sizeDelta = new Vector2(500f, 300f);
            GameObject textObject = Track(new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)));
            textObject.transform.SetParent(viewport.transform, false);
            TMP_Text content = textObject.GetComponent<TMP_Text>();
            content.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fonts/TMP/Pretendard-Regular SDF.asset");
            Assert.That(content.font, Is.Not.Null);
            CharacterStatsLookupPanelView panel =
                root.AddComponent<CharacterStatsLookupPanelView>();
            panel.Configure(
                closeObject.GetComponent<Button>(),
                content,
                viewport);
            root.SetActive(false);
            int closeCount = 0;
            panel.CloseRequested += () => closeCount++;

            panel.Show("Line 1\nLine 2\nLine 3");
            panel.ScrollRect.verticalNormalizedPosition = 0f;
            panel.Show("Line 1\nLine 2\nLine 3");
            closeObject.GetComponent<Button>().onClick.Invoke();

            Assert.That(panel.IsVisible, Is.True);
            Assert.That(panel.ScrollRect.vertical, Is.True);
            Assert.That(panel.ScrollRect.horizontal, Is.False);
            Assert.That(panel.ScrollRect.movementType,
                Is.EqualTo(ScrollRect.MovementType.Clamped));
            Assert.That(panel.ScrollRect.elasticity, Is.EqualTo(0.1f));
            Assert.That(panel.ScrollRect.inertia, Is.True);
            Assert.That(panel.ScrollRect.decelerationRate,
                Is.EqualTo(0.135f));
            Assert.That(panel.ScrollRect.scrollSensitivity, Is.EqualTo(1f));
            Assert.That(panel.ScrollRect.content,
                Is.SameAs(content.rectTransform));
            Assert.That(content.rectTransform.rect.height,
                Is.GreaterThanOrEqualTo(viewport.rect.height));
            Assert.That(closeCount, Is.EqualTo(1));

            var longText = new System.Text.StringBuilder();
            for (int index = 0; index < 80; index++)
            {
                longText.AppendLine("A long detailed stat line");
            }

            panel.Show(longText.ToString());
            Assert.That(content.rectTransform.rect.height,
                Is.GreaterThan(viewport.rect.height));
            panel.ScrollRect.verticalNormalizedPosition = 0f;
            panel.Hide();
            panel.Show(longText.ToString());
            Assert.That(panel.ScrollRect.verticalNormalizedPosition,
                Is.EqualTo(1f).Within(0.001f));

            panel.Hide();
            panel.Show("Short");
            Assert.That(content.rectTransform.rect.height,
                Is.EqualTo(viewport.rect.height).Within(0.01f));
        }

        private static CharacterUpgradePresentationModel Model(
            int awakening,
            long maxHp = 100,
            long attack = 20,
            double attackMultiplier = 1d,
            RelicStatBonusSummary relics = null)
        {
            return new CharacterUpgradePresentationModel(
                MareaId,
                "Marea Bluefang",
                null,
                null,
                50,
                awakening,
                maxHp,
                attack,
                0,
                0,
                false,
                false,
                characterAttackMultiplier: attackMultiplier,
                relicStatBonuses: relics);
        }

        private static LocalizationService CreateLocalization()
        {
            return new LocalizationService(new[]
            {
                Parse("Assets/Data/Localization/ui.tsv"),
                Parse("Assets/Data/Localization/characters.tsv"),
                Parse("Assets/Data/Localization/relics.tsv")
            });
        }

        private static LocalizationTable Parse(string path)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Assert.That(asset, Is.Not.Null, path);
            return LocalizationTableParser.Parse(asset.name, asset.text);
        }

        private static CharacterDefinition GetMareaDefinition()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    "Assets/Data/Database/DefinitionDatabase.asset");
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            Assert.That(database.TryGetCharacter(
                MareaId,
                out CharacterDefinition definition), Is.True);
            return definition;
        }

        private T Track<T>(T value) where T : Object
        {
            createdObjects.Add(value);
            return value;
        }
    }
}
