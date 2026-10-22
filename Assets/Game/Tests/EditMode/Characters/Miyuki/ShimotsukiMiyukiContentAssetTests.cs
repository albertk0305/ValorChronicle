using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Party.Battle;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiContentAssetTests
    {
        private const string CombatConfigPath =
            "Assets/Data/CombatConfigs/Miyuki/"
                + "shimotsuki_miyuki_combat_config.asset";
        private const string CharacterPath =
            "Assets/Data/Characters/shimotsuki_miyuki.asset";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/"
                + "shimotsuki_miyuki_presentation.asset";
        private const string DefinitionDatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";

        [Test]
        public void CombatConfigContainsCanonicalValues()
        {
            ShimotsukiMiyukiCombatConfig config = Config();

            Assert.That(config.TryValidate(out string error), Is.True, error);
            Assert.That(config.Match3Coefficient, Is.EqualTo(0.55d));
            Assert.That(config.Match4Coefficient, Is.EqualTo(0.90d));
            Assert.That(config.Match5Coefficient, Is.EqualTo(1.45d));
            Assert.That(config.Match3WaterAmount, Is.EqualTo(1));
            Assert.That(config.Match4WaterAmount, Is.EqualTo(2));
            Assert.That(config.Match5WaterAmount, Is.EqualTo(3));
            Assert.That(config.PassiveWaterDamageIncreaseRate,
                Is.EqualTo(0.08d));
            Assert.That(config.EnhancedPassiveWaterDamageIncreaseRate,
                Is.EqualTo(0.10d));
            Assert.That(config.Match5WaterDamageIncreaseRate,
                Is.EqualTo(0.12d));
            Assert.That(config.Match5EffectDurationTurns, Is.EqualTo(2));
            Assert.That(config.EnhancedMatch3Coefficient,
                Is.EqualTo(0.60d));
            Assert.That(config.EnhancedMatch4Coefficient,
                Is.EqualTo(1.00d));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(config.ActiveBlockConversionCount, Is.EqualTo(4));
            Assert.That(config.EnhancedActiveBlockConversionCount,
                Is.EqualTo(5));
            Assert.That(config.Awakening6ExtraWaterAmount, Is.EqualTo(1));
            Assert.That(config.WaterElementMaxAmount,
                Is.EqualTo(WaterElementResource.MaxAmount));
        }

        [Test]
        public void CharacterDefinitionUsesCanonicalIdentityStatsAndSkills()
        {
            CharacterDefinition definition = Character();

            Assert.That(definition.Id,
                Is.EqualTo(ShimotsukiMiyukiRules.CharacterId));
            Assert.That(definition.DisplayNameKey, Is.EqualTo("miyuki"));
            Assert.That(definition.Element, Is.EqualTo(ElementType.Water));
            Assert.That(definition.Level1Hp, Is.EqualTo(1100));
            Assert.That(definition.Level1Attack, Is.EqualTo(120));
            Assert.That(definition.Level100Hp, Is.EqualTo(4300));
            Assert.That(definition.Level100Attack, Is.EqualTo(650));
            Assert.That(definition.CombatConfig, Is.SameAs(Config()));
            Assert.That(definition.SkillIds, Is.EqualTo(new[]
            {
                ShimotsukiMiyukiRules.Match3SkillId,
                ShimotsukiMiyukiRules.Match4SkillId,
                ShimotsukiMiyukiRules.Match5SkillId,
                ShimotsukiMiyukiRules.PassiveSkillId,
                ShimotsukiMiyukiRules.ActiveAbilityId
            }));
        }

        [TestCase(
            ShimotsukiMiyukiRules.Match3SkillId,
            "skill_miyuki_match3.asset",
            "miyuki.skill.match3.name",
            SkillKind.Match)]
        [TestCase(
            ShimotsukiMiyukiRules.Match4SkillId,
            "skill_miyuki_match4.asset",
            "miyuki.skill.match4.name",
            SkillKind.Match)]
        [TestCase(
            ShimotsukiMiyukiRules.Match5SkillId,
            "skill_miyuki_match5.asset",
            "miyuki.skill.match5.name",
            SkillKind.Match)]
        [TestCase(
            ShimotsukiMiyukiRules.PassiveSkillId,
            "passive_miyuki_ice_mirror.asset",
            "miyuki.skill.passive.name",
            SkillKind.Passive)]
        [TestCase(
            ShimotsukiMiyukiRules.ActiveAbilityId,
            "active_miyuki_phantom_snow_fragrance.asset",
            "miyuki.skill.active.name",
            SkillKind.Active)]
        [TestCase(
            ShimotsukiMiyukiRules.FrostEchoStatusId,
            "passive_miyuki_frost_echo.asset",
            "miyuki.status.frost_echo.name",
            SkillKind.Passive)]
        public void SkillDefinitionsUseCanonicalIdsAndValidOptionalIcons(
            string expectedId,
            string fileName,
            string expectedNameKey,
            SkillKind expectedKind)
        {
            SkillDefinition definition = Load<SkillDefinition>(
                "Assets/Data/Skills/Miyuki/" + fileName);

            Assert.That(definition.Id, Is.EqualTo(expectedId));
            Assert.That(definition.DisplayNameKey,
                Is.EqualTo(expectedNameKey));
            Assert.That(definition.DescriptionKey, Is.Not.Empty);
            Assert.That(definition.SkillKind, Is.EqualTo(expectedKind));
            AssertOptionalSpriteAsset(definition.Icon);
        }

        [TestCase(
            ShimotsukiMiyukiRules.PassiveEffectId,
            "effect_miyuki_ice_mirror.asset")]
        [TestCase(
            ShimotsukiMiyukiRules.Match5EffectId,
            "effect_miyuki_crystal_light.asset")]
        public void EffectDefinitionsUseCanonicalIdsAndValidOptionalIcons(
            string expectedId,
            string fileName)
        {
            EffectDefinition definition = Load<EffectDefinition>(
                "Assets/Data/Effects/Miyuki/" + fileName);

            Assert.That(definition.Id, Is.EqualTo(expectedId));
            Assert.That(definition.DisplayNameKey, Is.Not.Empty);
            Assert.That(definition.DescriptionKey, Is.Not.Empty);
            AssertOptionalSpriteAsset(definition.Icon);
        }

        [Test]
        public void DatabaseAndPresentationCatalogResolveMiyukiContent()
        {
            DefinitionDatabase database = Database();
            CharacterPresentationCatalog presentationCatalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            database.Initialize();
            presentationCatalog.Initialize();

            Assert.That(database.TryGetCharacter(
                ShimotsukiMiyukiRules.CharacterId,
                out CharacterDefinition character), Is.True);
            Assert.That(character, Is.SameAs(Character()));
            foreach (string skillId in character.SkillIds.Concat(new[]
            {
                ShimotsukiMiyukiRules.FrostEchoStatusId
            }))
            {
                Assert.That(database.TryGetSkill(skillId, out _),
                    Is.True,
                    skillId);
            }

            Assert.That(database.TryGetEffect(
                ShimotsukiMiyukiRules.PassiveEffectId,
                out _), Is.True);
            Assert.That(database.TryGetEffect(
                ShimotsukiMiyukiRules.Match5EffectId,
                out _), Is.True);
            Assert.That(presentationCatalog.TryGet(
                ShimotsukiMiyukiRules.CharacterId,
                out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation, Is.SameAs(Load<
                CharacterPresentationDefinition>(PresentationPath)));
            AssertOptionalSpriteAsset(presentation.FaceSprite);
            AssertOptionalSpriteAsset(presentation.PreviewSprite);
        }

        [Test]
        public void RuntimeCatalogProjectsAllMiyukiFactories()
        {
            CharacterRuntimeRegistration registration =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Single(item => item.CharacterId
                        == ShimotsukiMiyukiRules.CharacterId);

            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(ShimotsukiMiyukiCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<ShimotsukiMiyukiBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<ShimotsukiMiyukiCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<ShimotsukiMiyukiLookupDescriptionProvider>());
        }

        [TestCase(1, 4515L, 650L)]
        [TestCase(2, 4515L, 683L)]
        [TestCase(3, 4515L, 683L)]
        [TestCase(4, 4515L, 683L)]
        [TestCase(5, 4515L, 683L)]
        [TestCase(6, 4515L, 683L)]
        public void BuildUsesCanonicalContentAssetAtEveryAwakening(
            int awakening,
            long expectedHp,
            long expectedAttack)
        {
            CharacterDefinition definition = Character();
            CharacterBuildResolver buildResolver =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateBuildResolver();
            Assert.That(buildResolver.Resolve(
                    definition,
                    level: 100,
                    awakening: awakening).MaxHp,
                Is.EqualTo(expectedHp));
            Assert.That(buildResolver.Resolve(
                    definition,
                    level: 100,
                    awakening: awakening).Attack,
                Is.EqualTo(expectedAttack));
        }

        [Test]
        public void RegistrarUsesCanonicalContentAsset()
        {
            CharacterDefinition definition = Character();

            ICharacterCombatProviderRegistrar registrar =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Single(item => item.CharacterId
                        == ShimotsukiMiyukiRules.CharacterId)
                    .CreateCombatRegistrar();
            var character = new CharacterBattleState(
                definition.Id,
                0,
                definition.Element,
                4515,
                683d);
            var matchProviders = new MatchEventActionProviderRegistry();
            var activeProviders = new ActiveAbilityActionProviderRegistry();
            CharacterCombatProviderRegistrationResult result =
                registrar.Register(
                    new BattlePartyMemberInput(
                        definition.Id,
                        0,
                        100,
                        6,
                        definition),
                    character,
                    new BossBattleState(
                        "miyuki_content_test_boss",
                        ElementType.Fire,
                        100000,
                        100d),
                    new CombatActionIdSequence(),
                    matchProviders,
                    activeProviders);

            Assert.That(matchProviders.TryResolve(
                definition.Id,
                out _), Is.True);
            Assert.That(activeProviders.TryResolveBoard(
                definition.Id,
                ShimotsukiMiyukiRules.ActiveAbilityId,
                out _), Is.True);
            Assert.That(result.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(result.InitialPartyEffects.Single().EffectId,
                Is.EqualTo(ShimotsukiMiyukiRules.PassiveEffectId));
            Assert.That(result.ResourceRequirements.Single().ResourceId,
                Is.EqualTo(WaterElementResource.Id));
            Assert.That(result.CombatStatusSources.Single().StatusId,
                Is.EqualTo(ShimotsukiMiyukiRules.FrostEchoStatusId));
        }

        [Test]
        public void LocalizationAndLookupResolveEveryMiyukiEntry()
        {
            TextAsset localizationAsset = Load<TextAsset>(LocalizationPath);
            var localization = new LocalizationService(new[]
            {
                LocalizationTableParser.Parse(
                    localizationAsset.name,
                    localizationAsset.text)
            });
            CharacterLookupDescriptionResolver resolver =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateLookupDescriptionResolver(
                        Database(),
                        localization);

            foreach (CharacterLookupSkillType skillType in Enum.GetValues(
                typeof(CharacterLookupSkillType)))
            {
                CharacterLookupDescriptionResult result =
                    resolver.ResolveSkill(
                        ShimotsukiMiyukiRules.CharacterId,
                        6,
                        skillType);
                Assert.That(result.IsSuccess, Is.True, result.Error);
                Assert.That(result.Text, Does.Not.Contain("[["));
                Assert.That(result.Text, Does.Not.Contain("{"));
            }

            for (int awakening = 1; awakening <= 6; awakening++)
            {
                CharacterLookupDescriptionResult result =
                    resolver.ResolveAwakening(
                        ShimotsukiMiyukiRules.CharacterId,
                        awakening);
                Assert.That(result.IsSuccess, Is.True, result.Error);
                Assert.That(result.Text, Does.Not.Contain("[["));
                Assert.That(result.Text, Does.Not.Contain("{"));
            }

            Assert.That(localization.GetText("miyuki"),
                Is.EqualTo("Shimotsuki Miyuki"));
            localization.SetLocale("ko");
            Assert.That(localization.GetText("miyuki"), Is.Not.Empty);
        }

        [Test]
        public void CanonicalDatabaseHasNoValidationErrors()
        {
            DefinitionDatabase database = Database();
            database.Initialize();

            ValidationReport report = DataValidator.Validate(database);

            Assert.That(report.HasErrors, Is.False);
            Assert.That(report.Issues.Where(issue =>
                issue.Severity == ValidationSeverity.Error), Is.Empty);
        }

        [Test]
        public void AssetReferencesUseDistinctValidGuids()
        {
            string[] paths =
            {
                CharacterPath,
                CombatConfigPath,
                PresentationPath,
                "Assets/Data/Skills/Miyuki/skill_miyuki_match3.asset",
                "Assets/Data/Skills/Miyuki/skill_miyuki_match4.asset",
                "Assets/Data/Skills/Miyuki/skill_miyuki_match5.asset",
                "Assets/Data/Skills/Miyuki/passive_miyuki_ice_mirror.asset",
                "Assets/Data/Skills/Miyuki/"
                    + "active_miyuki_phantom_snow_fragrance.asset",
                "Assets/Data/Skills/Miyuki/passive_miyuki_frost_echo.asset",
                "Assets/Data/Effects/Miyuki/effect_miyuki_ice_mirror.asset",
                "Assets/Data/Effects/Miyuki/"
                    + "effect_miyuki_crystal_light.asset"
            };
            string[] guids = paths.Select(
                AssetDatabase.AssetPathToGUID).ToArray();

            Assert.That(guids, Has.All.Not.Empty);
            Assert.That(guids.Distinct().Count(), Is.EqualTo(paths.Length));
            Assert.That(Character().CombatConfig, Is.SameAs(Config()));
        }

        private static CharacterDefinition Character()
        {
            return Load<CharacterDefinition>(CharacterPath);
        }

        private static ShimotsukiMiyukiCombatConfig Config()
        {
            return Load<ShimotsukiMiyukiCombatConfig>(CombatConfigPath);
        }

        private static DefinitionDatabase Database()
        {
            return Load<DefinitionDatabase>(DefinitionDatabasePath);
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
