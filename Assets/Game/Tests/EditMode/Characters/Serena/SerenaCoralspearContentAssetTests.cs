using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Serena
{
    public sealed class SerenaCoralspearContentAssetTests
    {
        private const string CombatConfigPath =
            "Assets/Data/CombatConfigs/Serena/"
            + "serena_coralspear_combat_config.asset";
        private const string CharacterPath =
            "Assets/Data/Characters/serena_coralspear.asset";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/"
            + "serena_coralspear_presentation.asset";
        private const string EffectPath =
            "Assets/Data/Effects/Serena/"
            + "effect_serena_coralspear_encore_aria.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";
        private const string DefinitionDatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string FaceSpritePath =
            "Assets/Sprites/Character/Face/SerenaFace.png";
        private const string PreviewSpritePath =
            "Assets/Sprites/Character/Full/SerenaFull.png";
        private const string ChaseIconPath =
            "Assets/Sprites/Effects/Serena/ChaseIcon.png";
        private const string EncoreAriaIconPath =
            "Assets/Sprites/Effects/Serena/EncoreAriaIcon.png";

        [Test]
        public void CombatConfigAssetContainsCanonicalValues()
        {
            SerenaCoralspearCombatConfig config = Load<
                SerenaCoralspearCombatConfig>(CombatConfigPath);

            Assert.That(config.TryValidate(out string error), Is.True, error);
            Assert.That(config.Match3Coefficient, Is.EqualTo(0.75d));
            Assert.That(config.Match4BaseCoefficient, Is.EqualTo(1.35d));
            Assert.That(config.Match4AdditionalCoefficient,
                Is.EqualTo(0.90d));
            Assert.That(config.Match5Coefficient, Is.EqualTo(2.20d));
            Assert.That(config.PassiveChaseCoefficientPerWaterElement,
                Is.EqualTo(0.75d));
            Assert.That(config.EmpoweredChaseDamageIncreaseRate,
                Is.EqualTo(0.80d));
            Assert.That(config.EmpoweredChaseMaxCharges, Is.EqualTo(3));
            Assert.That(config.ActiveAdditionalAndChaseDamageIncreaseRate,
                Is.EqualTo(0.35d));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(config.ActiveDurationTurns, Is.EqualTo(3));
            Assert.That(config.Awakening2AdditionalAndChaseDamageIncreaseRate,
                Is.EqualTo(0.10d));
            Assert.That(config.Awakening3PassiveChaseCoefficientPerWaterElement,
                Is.EqualTo(0.85d));
            Assert.That(config.Awakening4Match3Coefficient,
                Is.EqualTo(0.85d));
            Assert.That(config.Awakening4Match4BaseCoefficient,
                Is.EqualTo(1.50d));
            Assert.That(config.Awakening4Match4AdditionalCoefficient,
                Is.EqualTo(1.00d));
            Assert.That(config.Awakening5ActiveAdditionalAndChaseDamageIncreaseRate,
                Is.EqualTo(0.45d));
            Assert.That(config.Match3WaterElementGainAmount, Is.EqualTo(1));
            Assert.That(config.Awakening6Match5WaterElementGainAmount,
                Is.EqualTo(1));
            Assert.That(config.WaterElementMaxAmount,
                Is.EqualTo(WaterElementResource.MaxAmount));
        }

        [Test]
        public void CharacterDefinitionUsesCanonicalIdentityStatsAndSkills()
        {
            CharacterDefinition definition = Load<CharacterDefinition>(
                CharacterPath);
            SerenaCoralspearCombatConfig config = Load<
                SerenaCoralspearCombatConfig>(CombatConfigPath);

            Assert.That(definition.Id,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(definition.DisplayNameKey, Is.EqualTo("serena"));
            Assert.That(definition.Element, Is.EqualTo(ElementType.Water));
            Assert.That(definition.Level1Hp, Is.EqualTo(870));
            Assert.That(definition.Level1Attack, Is.EqualTo(170));
            Assert.That(definition.Level100Hp, Is.EqualTo(3200));
            Assert.That(definition.Level100Attack, Is.EqualTo(980));
            Assert.That(definition.CombatConfig, Is.SameAs(config));
            Assert.That(definition.SkillIds, Is.EqualTo(new[]
            {
                "skill_serena_coralspear_match3",
                "skill_serena_coralspear_match4",
                "skill_serena_coralspear_match5",
                "passive_serena_deep_sea_resonance",
                SerenaCoralspearRules.ActiveAbilityId
            }));
            Assert.That(definition.SkillIds.Distinct().Count(),
                Is.EqualTo(definition.SkillIds.Count));
        }

        [TestCase(
            "skill_serena_coralspear_match3",
            "skill_serena_coralspear_match3.asset",
            "serena.skill.match3.name",
            "serena.skill.match3.description",
            SkillKind.Match,
            null)]
        [TestCase(
            "skill_serena_coralspear_match4",
            "skill_serena_coralspear_match4.asset",
            "serena.skill.match4.name",
            "serena.skill.match4.description",
            SkillKind.Match,
            null)]
        [TestCase(
            "skill_serena_coralspear_match5",
            "skill_serena_coralspear_match5.asset",
            "serena.skill.match5.name",
            "serena.skill.match5.description",
            SkillKind.Match,
            null)]
        [TestCase(
            "passive_serena_deep_sea_resonance",
            "passive_serena_deep_sea_resonance.asset",
            "serena.skill.passive.name",
            "serena.skill.passive.description",
            SkillKind.Passive,
            ChaseIconPath)]
        [TestCase(
            SerenaCoralspearRules.ActiveAbilityId,
            "active_serena_coralspear_encore_aria.asset",
            "serena.skill.active.name",
            "serena.skill.active.description",
            SkillKind.Active,
            EncoreAriaIconPath)]
        public void SkillDefinitionUsesExpectedMetadata(
            string expectedId,
            string fileName,
            string expectedNameKey,
            string expectedDescriptionKey,
            SkillKind expectedKind,
            string expectedIconPath)
        {
            SkillDefinition skill = Load<SkillDefinition>(
                "Assets/Data/Skills/Serena/" + fileName);

            Assert.That(skill.Id, Is.EqualTo(expectedId));
            Assert.That(skill.DisplayNameKey, Is.EqualTo(expectedNameKey));
            Assert.That(skill.DescriptionKey,
                Is.EqualTo(expectedDescriptionKey));
            Assert.That(skill.SkillKind, Is.EqualTo(expectedKind));
            if (expectedIconPath == null)
            {
                AssertOptionalSpriteAsset(skill.Icon);
            }
            else
            {
                Assert.That(AssetDatabase.GetAssetPath(skill.Icon),
                    Is.EqualTo(expectedIconPath));
            }
        }

        [Test]
        public void PresentationAndEffectUseExistingSerenaSprites()
        {
            CharacterPresentationDefinition presentation = Load<
                CharacterPresentationDefinition>(PresentationPath);
            EffectDefinition effect = Load<EffectDefinition>(EffectPath);

            Assert.That(presentation.CharacterId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(AssetDatabase.GetAssetPath(presentation.FaceSprite),
                Is.EqualTo(FaceSpritePath));
            Assert.That(AssetDatabase.GetAssetPath(presentation.PreviewSprite),
                Is.EqualTo(PreviewSpritePath));
            Assert.That(effect.Id,
                Is.EqualTo(SerenaCoralspearRules.ActiveEffectId));
            Assert.That(effect.DisplayNameKey,
                Is.EqualTo("effect.serena.encore_aria.name"));
            Assert.That(effect.DescriptionKey,
                Is.EqualTo("effect.serena.encore_aria.description"));
            Assert.That(AssetDatabase.GetAssetPath(effect.Icon),
                Is.EqualTo(EncoreAriaIconPath));
        }

        [TestCase(ChaseIconPath)]
        [TestCase(EncoreAriaIconPath)]
        public void SerenaEffectIconIsImportedAsSingleSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType,
                Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode,
                Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>()
                    .Count(),
                Is.EqualTo(1));
        }

        [Test]
        public void LocalizationContainsEverySerenaContentKeyInBothLocales()
        {
            TextAsset asset = Load<TextAsset>(LocalizationPath);
            LocalizationTable table = LocalizationTableParser.Parse(
                asset.name,
                asset.text);
            string[] expectedKeys =
            {
                "serena",
                "serena.skill.match3.name",
                "serena.skill.match3.description",
                "serena.skill.match4.name",
                "serena.skill.match4.description",
                "serena.skill.match5.name",
                "serena.skill.match5.description",
                "serena.skill.passive.name",
                "serena.skill.passive.description",
                "serena.skill.active.name",
                "serena.skill.active.description",
                "serena.awakening.1.name",
                "serena.awakening.1.description",
                "serena.awakening.2.name",
                "serena.awakening.2.description",
                "serena.awakening.3.name",
                "serena.awakening.3.description",
                "serena.awakening.4.name",
                "serena.awakening.4.description",
                "serena.awakening.5.name",
                "serena.awakening.5.description",
                "serena.awakening.6.name",
                "serena.awakening.6.description",
                "effect.serena.encore_aria.name",
                "effect.serena.encore_aria.description"
            };

            Assert.That(table.Locales, Is.EquivalentTo(new[] { "en", "ko" }));
            foreach (string key in expectedKeys)
            {
                Assert.That(table.Entries.ContainsKey(key), Is.True, key);
                Assert.That(table.Entries[key]["en"], Is.Not.Empty, key);
                Assert.That(table.Entries[key]["ko"], Is.Not.Empty, key);
            }
        }

        [Test]
        public void SerenaAssetsArePresentInDefaultRegistrationCatalogs()
        {
            var serenaSkillIds = new HashSet<string>(StringComparer.Ordinal)
            {
                "skill_serena_coralspear_match3",
                "skill_serena_coralspear_match4",
                "skill_serena_coralspear_match5",
                "passive_serena_deep_sea_resonance",
                SerenaCoralspearRules.ActiveAbilityId
            };
            CharacterRuntimeRegistrationCatalog runtimeCatalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DefinitionDatabasePath);
            CharacterPresentationCatalog presentationCatalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);

            Assert.That(runtimeCatalog.Registrations.Any(registration =>
                registration.CharacterId == SerenaCoralspearRules.CharacterId),
                Is.True);
            Assert.That(database.Characters.Any(definition =>
                definition != null
                && definition.Id == SerenaCoralspearRules.CharacterId),
                Is.True);
            Assert.That(database.Skills.Any(definition =>
                definition != null
                && serenaSkillIds.Contains(definition.Id)),
                Is.True);
            Assert.That(database.Skills.Count(definition =>
                    definition != null
                    && serenaSkillIds.Contains(definition.Id)),
                Is.EqualTo(serenaSkillIds.Count));
            Assert.That(database.Effects.Any(definition =>
                definition != null
                && definition.Id == SerenaCoralspearRules.ActiveEffectId),
                Is.True);
            Assert.That(presentationCatalog.Definitions.Any(definition =>
                definition != null
                && definition.CharacterId
                    == SerenaCoralspearRules.CharacterId),
                Is.True);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private static void AssertOptionalSpriteAsset(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Assert.That(EditorUtility.IsPersistent(sprite), Is.True);
            Assert.That(AssetDatabase.GetAssetPath(sprite), Is.Not.Empty);
        }
    }
}
