using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;

namespace ValorChronicle.Tests.EditMode.Localization
{
    public sealed class LocalizationServiceTests
    {
        [Test]
        public void Parser_ReadsHeaderLocalesAndMultilineEscape()
        {
            LocalizationTable table = LocalizationTableParser.Parse(
                "test.tsv",
                "key\ten\tko\tja\nline\tFirst\\nSecond\t\tJapanese");

            Assert.That(table.Locales, Is.EqualTo(new[] { "en", "ko", "ja" }));
            Assert.That(table.Entries["line"]["en"],
                Is.EqualTo("First\nSecond"));
            Assert.That(table.Entries["line"]["ko"], Is.Empty);
            Assert.That(table.Entries["line"]["ja"],
                Is.EqualTo("Japanese"));
        }

        [Test]
        public void MultipleTablesMergeAndEnglishLookupSucceeds()
        {
            LocalizationService service = Service(
                "key\ten\tko\nfirst\tFirst\t",
                "key\ten\tko\nsecond\tSecond\t");

            Assert.That(service.GetText("first"), Is.EqualTo("First"));
            Assert.That(service.GetText("second"), Is.EqualTo("Second"));
        }

        [Test]
        public void DuplicateKeyAcrossTablesIsRejected()
        {
            LocalizationTable first = Table(
                "key\ten\nshared\tFirst",
                "first.tsv");
            LocalizationTable second = Table(
                "key\ten\nshared\tSecond",
                "second.tsv");

            Assert.Throws<LocalizationDataException>(
                () => new LocalizationService(
                    new[] { first, second }));
        }

        [Test]
        public void EmptyKeyIsRejected()
        {
            Assert.Throws<LocalizationDataException>(
                () => Table("key\ten\n\tValue"));
        }

        [TestCase("key\tko\nname\tName")]
        [TestCase("key\ten\tko\nname\t\tName")]
        public void MissingDefaultLocaleIsRejected(string tsv)
        {
            Assert.Throws<LocalizationDataException>(
                () => Service(tsv));
        }

        [Test]
        public void EmptyCurrentLocaleFallsBackToEnglish()
        {
            LocalizationService service = Service(
                "key\ten\tko\nname\tEnglish Name\t");

            service.SetLocale("ko");

            Assert.That(service.GetText("name"),
                Is.EqualTo("English Name"));
        }

        [Test]
        public void MissingKeyUsesExplicitFallback()
        {
            LocalizationService service = Service(
                "key\ten\nknown\tKnown");

            Assert.That(service.GetText("unknown"),
                Is.EqualTo("[[unknown]]"));
        }

        [Test]
        public void FormatSubstitutesNamedArgumentsInvariantly()
        {
            LocalizationService service = Service(
                "key\ten\nskill\tDeals {coefficient}% for {duration} turns");
            var arguments = new Dictionary<string, object>
            {
                ["coefficient"] = 25.5,
                ["duration"] = 3
            };

            Assert.That(service.Format("skill", arguments),
                Is.EqualTo("Deals 25.5% for 3 turns"));
        }

        [Test]
        public void MissingFormatArgumentIsNonFatalAndReported()
        {
            LocalizationService service = Service(
                "key\ten\nskill\tLasts {duration} turns");

            bool success = service.TryFormat(
                "skill",
                new Dictionary<string, object>(),
                out string result,
                out string error);

            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo("Lasts {duration} turns"));
            Assert.That(error, Does.Contain("duration"));
        }

        [Test]
        public void LocalePlaceholderMismatchIsRejected()
        {
            Assert.Throws<LocalizationDataException>(() => Service(
                "key\ten\tko\nskill\tLasts {duration} turns\t{turns}턴 지속"));
        }

        [Test]
        public void SetLocaleRaisesExactlyOnceOnlyWhenChanged()
        {
            LocalizationService service = Service(
                "key\ten\tko\nname\tEnglish\tKorean");
            int eventCount = 0;
            string receivedLocale = null;
            service.LocaleChanged += locale =>
            {
                eventCount++;
                receivedLocale = locale;
            };

            Assert.That(service.SetLocale("ko"), Is.True);
            Assert.That(service.SetLocale("ko"), Is.False);

            Assert.That(service.CurrentLocale, Is.EqualTo("ko"));
            Assert.That(receivedLocale, Is.EqualTo("ko"));
            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public void UnknownLocaleIsRejectedWithoutChangingCurrentLocale()
        {
            LocalizationService service = Service(
                "key\ten\tko\nname\tEnglish\tKorean");

            Assert.Throws<System.ArgumentException>(
                () => service.SetLocale("fr"));
            Assert.That(service.CurrentLocale, Is.EqualTo("en"));
        }

        [Test]
        public void ProductionCatalogResolvesCharacterContentInBothLocales()
        {
            CharacterDefinition definition = AssetDatabase.LoadAssetAtPath<
                CharacterDefinition>(
                "Assets/Data/Characters/marea_bluefang.asset");
            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<
                LocalizationCatalog>(
                "Assets/Data/Localization/LocalizationCatalog.asset");

            Assert.That(definition, Is.Not.Null);
            Assert.That(catalog, Is.Not.Null);
            LocalizationService service = catalog.CreateService();
            Assert.That(service.GetText(definition.DisplayNameKey),
                Is.EqualTo("Marea Bluefang"));
            Assert.That(service.GetText("marea.skill.match3.name"),
                Is.EqualTo("Wave Bullet - 3 Match"));
            Assert.That(service.Format(
                    "marea.skill.match3.description",
                    new Dictionary<string, object>
                    {
                        ["coefficient"] = 90
                    }),
                Is.EqualTo("Deals 90% ATK as Water damage."));

            service.SetLocale("ko");

            Assert.That(service.GetText(definition.DisplayNameKey),
                Is.EqualTo("마레아 블루팽"));
            Assert.That(service.GetText("marea.skill.match3.name"),
                Is.EqualTo("파도 탄환 - 3매치"));
            Assert.That(service.Format(
                    "marea.skill.match3.description",
                    new Dictionary<string, object>
                    {
                        ["coefficient"] = 90
                    }),
                Is.EqualTo("공격력의 90%만큼 물속성 피해를 줍니다."));

            service.SetLocale("en");
            Assert.That(service.GetText(definition.DisplayNameKey),
                Is.EqualTo("Marea Bluefang"));
        }

        [Test]
        public void ProductionCatalogResolvesInitAndMainUiInBothLocales()
        {
            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<
                LocalizationCatalog>(
                "Assets/Data/Localization/LocalizationCatalog.asset");
            LocalizationService service = catalog.CreateService();

            Assert.That(service.GetText("ui.init.press_to_start"),
                Is.EqualTo("Press to Start"));
            Assert.That(service.GetText(
                    "ui.init.initialization_failed"),
                Is.EqualTo(
                    "Game initialization failed. Please restart the game."));
            Assert.That(service.GetText(
                    "main.boss_selection.master_challenge_prompt"),
                Is.EqualTo("Try Master Mode"));
            Assert.That(service.GetText("main.party.empty_required"),
                Is.EqualTo(
                    "At least one character is required in your party."));
            Assert.That(service.GetText("ui.character.stats"),
                Is.EqualTo("Stats"));
            Assert.That(service.GetText("ui.gacha.skip"),
                Is.EqualTo("Skip"));
            Assert.That(service.Format(
                    "ui.battle.turn_format",
                    new Dictionary<string, object>
                    {
                        ["current"] = 3,
                        ["limit"] = 25
                    }),
                Is.EqualTo("Turn\n3 / 25"));
            Assert.That(service.Format(
                    "ui.battle.combo_format",
                    new Dictionary<string, object>
                    {
                        ["count"] = 4
                    }),
                Is.EqualTo("Combo 4"));
            Assert.That(service.Format(
                    "ui.battle.score_format",
                    new Dictionary<string, object>
                    {
                        ["score"] = "12,000"
                    }),
                Is.EqualTo("Score 12,000"));
            Assert.That(service.Format(
                    "ui.battle.result.best_score_format",
                    new Dictionary<string, object>
                    {
                        ["score"] = "12,000"
                    }),
                Is.EqualTo("Best: 12,000"));
            Assert.That(service.Format(
                    "ui.battle.result.score_format",
                    new Dictionary<string, object>
                    {
                        ["score"] = "12,000"
                    }),
                Is.EqualTo("Score: 12,000"));
            Assert.That(service.Format(
                    "ui.battle.result.turn_format",
                    new Dictionary<string, object>
                    {
                        ["current"] = 10,
                        ["limit"] = 25
                    }),
                Is.EqualTo("Turn: 10/25"));
            Assert.That(service.Format(
                    "ui.battle.result.damage_format",
                    new Dictionary<string, object>
                    {
                        ["score"] = "10,000"
                    }),
                Is.EqualTo("Damage: 10,000"));
            Assert.That(service.Format(
                    "ui.battle.result.turn_bonus_format",
                    new Dictionary<string, object>
                    {
                        ["score"] = "2,000"
                    }),
                Is.EqualTo("Turn Bonus: 2,000"));
            Assert.That(service.GetText("ui.battle.result.reward"),
                Is.EqualTo("Reward"));
            Assert.That(service.GetText(
                    "ui.battle.result.first_grade_bonus"),
                Is.EqualTo("First Grade Bonus"));
            Assert.That(service.GetText("ui.battle.pause.paused"),
                Is.EqualTo("Game Paused"));

            service.SetLocale("ko");

            Assert.That(service.GetText("ui.init.press_to_start"),
                Is.EqualTo("시작하려면 누르세요"));
            Assert.That(service.GetText(
                    "ui.init.initialization_failed"),
                Is.EqualTo(
                    "게임 초기화에 실패했습니다. 게임을 다시 실행해 주세요."));
            Assert.That(service.GetText(
                    "main.boss_selection.master_challenge_prompt"),
                Is.EqualTo("마스터 모드 도전"));
            Assert.That(service.GetText("main.party.empty_required"),
                Is.EqualTo("파티에 캐릭터가 한 명 이상 필요합니다."));
            Assert.That(service.GetText("ui.character.stats"),
                Is.EqualTo("능력치"));
            Assert.That(service.GetText("ui.gacha.skip"),
                Is.EqualTo("건너뛰기"));
            Assert.That(service.Format(
                    "ui.battle.turn_format",
                    new Dictionary<string, object>
                    {
                        ["current"] = 3,
                        ["limit"] = 25
                    }),
                Is.EqualTo("턴\n3 / 25"));
            Assert.That(service.Format(
                    "ui.battle.combo_format",
                    new Dictionary<string, object>
                    {
                        ["count"] = 4
                    }),
                Is.EqualTo("콤보 4"));
            Assert.That(service.Format(
                    "ui.battle.score_format",
                    new Dictionary<string, object>
                    {
                        ["score"] = "12,000"
                    }),
                Is.EqualTo("점수 12,000"));
            Assert.That(service.Format(
                    "ui.battle.result.best_score_format",
                    new Dictionary<string, object>
                    {
                        ["score"] = "12,000"
                    }),
                Is.EqualTo("최고 기록: 12,000"));
            Assert.That(service.Format(
                    "ui.battle.result.score_format",
                    new Dictionary<string, object>
                    {
                        ["score"] = "12,000"
                    }),
                Is.EqualTo("점수: 12,000"));
            Assert.That(service.Format(
                    "ui.battle.result.turn_format",
                    new Dictionary<string, object>
                    {
                        ["current"] = 10,
                        ["limit"] = 25
                    }),
                Is.EqualTo("턴: 10/25"));
            Assert.That(service.Format(
                    "ui.battle.result.damage_format",
                    new Dictionary<string, object>
                    {
                        ["score"] = "10,000"
                    }),
                Is.EqualTo("피해 점수: 10,000"));
            Assert.That(service.Format(
                    "ui.battle.result.turn_bonus_format",
                    new Dictionary<string, object>
                    {
                        ["score"] = "2,000"
                    }),
                Is.EqualTo("잔여 턴 보너스: 2,000"));
            Assert.That(service.GetText("ui.battle.result.reward"),
                Is.EqualTo("보상"));
            Assert.That(service.GetText(
                    "ui.battle.result.first_grade_bonus"),
                Is.EqualTo("최초 등급 달성 보상"));
            Assert.That(service.GetText("ui.battle.pause.paused"),
                Is.EqualTo("일시정지"));
        }

        private static LocalizationService Service(params string[] tables)
        {
            var parsed = new LocalizationTable[tables.Length];
            for (int index = 0; index < tables.Length; index++)
            {
                parsed[index] = Table(tables[index], $"table{index}.tsv");
            }

            return new LocalizationService(parsed);
        }

        private static LocalizationTable Table(
            string tsv,
            string sourceName = "test.tsv")
        {
            return LocalizationTableParser.Parse(sourceName, tsv);
        }
    }
}
