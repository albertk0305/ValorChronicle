using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;

namespace ValorChronicle.Tests.EditMode.Characters.Presentation
{
    public sealed class CharacterLookupDescriptionResolverTests
    {
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";

        private DefinitionDatabase database;
        private LocalizationService localization;
        private CharacterLookupDescriptionResolver resolver;
        private MareaBluefangCombatConfig config;

        [SetUp]
        public void SetUp()
        {
            database = AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            TextAsset table = AssetDatabase.LoadAssetAtPath<TextAsset>(
                LocalizationPath);
            Assert.That(table, Is.Not.Null);
            localization = new LocalizationService(new[]
            {
                LocalizationTableParser.Parse(table.name, table.text)
            });
            resolver = CharacterLookupDescriptionResolverFactory.CreateDefault(
                database,
                localization);
            config = MareaBluefangTestConfig.Canonical;
            Assert.That(config, Is.Not.Null);
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(6)]
        public void Match3UsesResolvedCombatCoefficient(int awakening)
        {
            MareaBluefangResolvedCombatRules rules =
                MareaBluefangCombatRuleResolver.Resolve(config, awakening);

            CharacterLookupDescriptionResult result = resolver.ResolveSkill(
                MareaBluefangRules.CharacterId,
                awakening,
                CharacterLookupSkillType.Match3);

            Assert.That(result.IsSuccess, Is.True, result.Error);
            Assert.That(result.Text,
                Does.Contain(Percent(rules.Match3Coefficient) + "%"));
        }

        [Test]
        public void Match4UsesResolvedBaseAndConditionalCoefficients()
        {
            MareaBluefangResolvedCombatRules rules =
                MareaBluefangCombatRuleResolver.Resolve(config, 4);

            CharacterLookupDescriptionResult result = resolver.ResolveSkill(
                MareaBluefangRules.CharacterId,
                4,
                CharacterLookupSkillType.Match4);

            Assert.That(result.IsSuccess, Is.True, result.Error);
            Assert.That(result.Text,
                Does.Contain(Percent(rules.Match4BaseCoefficient) + "%"));
            Assert.That(result.Text, Does.Contain(
                Percent(rules.Match4WaterElementBonusCoefficient) + "%"));
        }

        [TestCase(0)]
        [TestCase(5)]
        public void Match5UsesResolvedBaseAndPerWaterCoefficients(
            int awakening)
        {
            MareaBluefangResolvedCombatRules rules =
                MareaBluefangCombatRuleResolver.Resolve(config, awakening);

            CharacterLookupDescriptionResult result = resolver.ResolveSkill(
                MareaBluefangRules.CharacterId,
                awakening,
                CharacterLookupSkillType.Match5);

            Assert.That(result.IsSuccess, Is.True, result.Error);
            Assert.That(result.Text,
                Does.Contain(Percent(rules.Match5BaseCoefficient) + "%"));
            Assert.That(result.Text, Does.Contain(
                Percent(rules.Match5CoefficientPerWaterElement) + "%"));
        }

        [TestCase(0)]
        [TestCase(3)]
        public void PassiveUsesResolvedAwakeningRate(int awakening)
        {
            MareaBluefangResolvedCombatRules rules =
                MareaBluefangCombatRuleResolver.Resolve(config, awakening);

            CharacterLookupDescriptionResult result = resolver.ResolveSkill(
                MareaBluefangRules.CharacterId,
                awakening,
                CharacterLookupSkillType.Passive);

            Assert.That(result.IsSuccess, Is.True, result.Error);
            Assert.That(result.Text, Does.Contain(
                Percent(rules.PassiveDealtDamageIncreaseRate) + "%"));
        }

        [Test]
        public void ActiveUsesCanonicalConfigValues()
        {
            CharacterLookupDescriptionResult result = resolver.ResolveSkill(
                MareaBluefangRules.CharacterId,
                6,
                CharacterLookupSkillType.Active);

            Assert.That(result.IsSuccess, Is.True, result.Error);
            Assert.That(result.Text, Does.Contain(
                Percent(config.ActiveWaterDamageIncreaseRate) + "%"));
            Assert.That(result.Text,
                Does.Contain(config.ActiveDurationTurns + " turns"));
            Assert.That(result.Text,
                Does.Contain("Cooldown: " + config.ActiveCooldownTurns));
        }

        [Test]
        public void LingeringSurgeFragmentsOnlyAppearAtAwakeningSix()
        {
            CharacterLookupDescriptionResult awakeningFive =
                resolver.ResolveSkill(
                    MareaBluefangRules.CharacterId,
                    5,
                    CharacterLookupSkillType.Match5);
            CharacterLookupDescriptionResult awakeningSix =
                resolver.ResolveSkill(
                    MareaBluefangRules.CharacterId,
                    6,
                    CharacterLookupSkillType.Match5);
            MareaBluefangResolvedCombatRules rules =
                MareaBluefangCombatRuleResolver.Resolve(config, 6);

            Assert.That(awakeningFive.Text,
                Does.Not.Contain("Lingering Surge"));
            Assert.That(awakeningSix.Text,
                Does.Contain("Lingering Surge"));
            Assert.That(awakeningSix.Text, Does.Contain(
                MareaBluefangCombatRuleResolver
                    .LingeringSurgeRequiredWaterAmount.ToString()));
            Assert.That(awakeningSix.Text, Does.Contain(
                Percent(rules.LingeringSurgeDamageIncreaseRate) + "%"));
        }

        [TestCase(1, "Sharpened Edge")]
        [TestCase(2, "Tough Mariner")]
        [TestCase(3, "Blood Banquet")]
        [TestCase(4, "Razor Cuts")]
        [TestCase(5, "Storm Devour")]
        [TestCase(6, "Lingering Surge")]
        public void AwakeningDescriptionsUseLocalizedNamesAndCanonicalRules(
            int stage,
            string awakeningName)
        {
            CharacterLookupDescriptionResult result =
                resolver.ResolveAwakening(
                    MareaBluefangRules.CharacterId,
                    stage);

            Assert.That(result.IsSuccess, Is.True, result.Error);
            Assert.That(result.Text, Does.StartWith(
                $"Awakening {stage} — {awakeningName}\n\n"));
            AssertAdjacentValues(stage, result.Text);
        }

        [Test]
        public void AwakeningNameFallsBackToEnglishForEmptyLocaleValue()
        {
            localization.SetLocale("ko");

            CharacterLookupDescriptionResult result =
                resolver.ResolveAwakening(
                    MareaBluefangRules.CharacterId,
                    4);

            Assert.That(result.IsSuccess, Is.True, result.Error);
            Assert.That(result.Text,
                Does.StartWith("Awakening 4 — Razor Cuts\n\n"));
        }

        [Test]
        public void MissingAwakeningNameUsesExistingDescriptionFallback()
        {
            var invalid = new CharacterLookupDescriptionResolver(
                database,
                localization,
                new ICharacterLookupDescriptionProvider[]
                {
                    new MissingAwakeningNameProvider()
                });

            CharacterLookupDescriptionResult result =
                invalid.ResolveAwakening(
                    MareaBluefangRules.CharacterId,
                    1);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Text, Is.EqualTo("Description unavailable."));
            Assert.That(result.Error,
                Does.Contain("missing.awakening.name"));
            Assert.That(result.Error, Does.Contain("not found"));
        }

        [Test]
        public void InvalidRequestsReturnLocalizedFallback()
        {
            CharacterLookupDescriptionResult unknown = resolver.ResolveSkill(
                "missing_character",
                0,
                CharacterLookupSkillType.Match3);
            CharacterLookupDescriptionResult awakening = resolver.ResolveSkill(
                MareaBluefangRules.CharacterId,
                7,
                CharacterLookupSkillType.Match3);
            CharacterLookupDescriptionResult stage =
                resolver.ResolveAwakening(
                    MareaBluefangRules.CharacterId,
                    0);
            CharacterLookupDescriptionResult skill = resolver.ResolveSkill(
                MareaBluefangRules.CharacterId,
                0,
                (CharacterLookupSkillType)999);

            foreach (CharacterLookupDescriptionResult result in new[]
            {
                unknown,
                awakening,
                stage,
                skill
            })
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Text, Is.EqualTo("Description unavailable."));
                Assert.That(result.Error, Is.Not.Empty);
            }
        }

        [Test]
        public void MissingProviderReturnsFallback()
        {
            var withoutProviders = new CharacterLookupDescriptionResolver(
                database,
                localization,
                Array.Empty<ICharacterLookupDescriptionProvider>());

            CharacterLookupDescriptionResult result =
                withoutProviders.ResolveSkill(
                    MareaBluefangRules.CharacterId,
                    0,
                    CharacterLookupSkillType.Match3);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Does.Contain("provider"));
        }

        [TestCase(true, "not found")]
        [TestCase(false, "missing named argument")]
        public void InvalidLocalizationRequestReturnsFallback(
            bool missingKey,
            string expectedError)
        {
            var invalid = new CharacterLookupDescriptionResolver(
                database,
                localization,
                new ICharacterLookupDescriptionProvider[]
                {
                    new InvalidLocalizationProvider(missingKey)
                });

            CharacterLookupDescriptionResult result = invalid.ResolveSkill(
                MareaBluefangRules.CharacterId,
                0,
                CharacterLookupSkillType.Match3);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Text, Is.EqualTo("Description unavailable."));
            Assert.That(result.Error, Does.Contain(expectedError));
        }

        private void AssertAdjacentValues(int stage, string text)
        {
            if (stage <= 2)
            {
                var modifier = new MareaBluefangBuildModifier();
                var before = modifier.GetStatMultipliers(stage - 1);
                var after = modifier.GetStatMultipliers(stage);
                double beforeValue = stage == 1
                    ? before.AttackMultiplier
                    : before.MaxHpMultiplier;
                double afterValue = stage == 1
                    ? after.AttackMultiplier
                    : after.MaxHpMultiplier;
                Assert.That(text, Does.Contain(Percent(beforeValue) + "%"));
                Assert.That(text, Does.Contain(Percent(afterValue) + "%"));
                return;
            }

            MareaBluefangResolvedCombatRules previous =
                MareaBluefangCombatRuleResolver.Resolve(config, stage - 1);
            MareaBluefangResolvedCombatRules current =
                MareaBluefangCombatRuleResolver.Resolve(config, stage);
            switch (stage)
            {
                case 3:
                    Assert.That(text, Does.Contain(Percent(
                        previous.PassiveDealtDamageIncreaseRate) + "%"));
                    Assert.That(text, Does.Contain(Percent(
                        current.PassiveDealtDamageIncreaseRate) + "%"));
                    break;
                case 4:
                    Assert.That(text, Does.Contain(
                        Percent(previous.Match3Coefficient) + "%"));
                    Assert.That(text, Does.Contain(
                        Percent(current.Match3Coefficient) + "%"));
                    Assert.That(text, Does.Contain(
                        Percent(previous.Match4BaseCoefficient) + "%"));
                    Assert.That(text, Does.Contain(
                        Percent(current.Match4BaseCoefficient) + "%"));
                    break;
                case 5:
                    Assert.That(text, Does.Contain(Percent(
                        previous.Match5CoefficientPerWaterElement) + "%"));
                    Assert.That(text, Does.Contain(Percent(
                        current.Match5CoefficientPerWaterElement) + "%"));
                    break;
                case 6:
                    Assert.That(previous.HasLingeringSurge, Is.False);
                    Assert.That(current.HasLingeringSurge, Is.True);
                    Assert.That(text, Does.Contain(Percent(
                        current.LingeringSurgeDamageIncreaseRate) + "%"));
                    break;
            }
        }

        private static string Percent(double value)
        {
            return (value * 100d).ToString(
                "0.##",
                CultureInfo.InvariantCulture);
        }

        private sealed class InvalidLocalizationProvider
            : ICharacterLookupDescriptionProvider
        {
            private readonly bool missingKey;

            public InvalidLocalizationProvider(bool missingKey)
            {
                this.missingKey = missingKey;
            }

            public string CharacterId => MareaBluefangRules.CharacterId;

            public CharacterLookupDescriptionDefinition ResolveSkill(
                CharacterDefinition definition,
                int awakening,
                CharacterLookupSkillType skillType)
            {
                return new CharacterLookupDescriptionDefinition(
                    new CharacterLookupLocalizedText(
                        "marea.skill.match3.name"),
                    new[]
                    {
                        new CharacterLookupLocalizedText(
                            missingKey
                                ? "missing.localization.key"
                                : "marea.skill.match3.description")
                    });
            }

            public CharacterLookupDescriptionDefinition ResolveAwakening(
                CharacterDefinition definition,
                int awakeningStage)
            {
                return ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3);
            }
        }

        private sealed class MissingAwakeningNameProvider
            : ICharacterLookupDescriptionProvider
        {
            public string CharacterId => MareaBluefangRules.CharacterId;

            public CharacterLookupDescriptionDefinition ResolveSkill(
                CharacterDefinition definition,
                int awakening,
                CharacterLookupSkillType skillType)
            {
                throw new NotSupportedException();
            }

            public CharacterLookupDescriptionDefinition ResolveAwakening(
                CharacterDefinition definition,
                int awakeningStage)
            {
                return new CharacterLookupDescriptionDefinition(
                    new CharacterLookupLocalizedText(
                        "character.lookup.awakening.title",
                        new Dictionary<string, object>
                        {
                            ["stage"] = awakeningStage,
                            ["name"] = new CharacterLookupLocalizedText(
                                "missing.awakening.name")
                        }),
                    new[]
                    {
                        new CharacterLookupLocalizedText(
                            "marea.awakening.1.description",
                            new Dictionary<string, object>
                            {
                                ["multiplier_before"] = "100",
                                ["multiplier_after"] = "105"
                            })
                    });
            }
        }
    }
}
