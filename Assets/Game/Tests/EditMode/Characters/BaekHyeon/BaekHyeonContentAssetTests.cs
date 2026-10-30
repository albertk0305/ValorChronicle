using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.BaekHyeon;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Editor.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonContentAssetTests
    {
        private const string CharacterPath =
            "Assets/Data/Characters/baek_hyeon.asset";
        private const string ConfigPath =
            "Assets/Data/CombatConfigs/BaekHyeon/"
                + "baek_hyeon_combat_config.asset";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/"
                + "baek_hyeon_presentation.asset";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";

        [Test]
        public void CharacterAndConfigUseCanonicalValues()
        {
            CharacterDefinition character = Load<CharacterDefinition>(
                CharacterPath);
            BaekHyeonCombatConfig config = Load<BaekHyeonCombatConfig>(
                ConfigPath);

            Assert.That(character.Id,
                Is.EqualTo(BaekHyeonRules.CharacterId));
            Assert.That(character.DisplayNameKey,
                Is.EqualTo("baek_hyeon"));
            Assert.That(character.Element, Is.EqualTo(ElementType.Grass));
            Assert.That(character.Level1Hp, Is.EqualTo(870));
            Assert.That(character.Level1Attack, Is.EqualTo(170));
            Assert.That(character.Level100Hp, Is.EqualTo(3200));
            Assert.That(character.Level100Attack, Is.EqualTo(980));
            Assert.That(character.CombatConfig, Is.SameAs(config));
            Assert.That(config.TryValidate(out string error),
                Is.True, error);

            Assert.That(config.InitialMarkCount, Is.Zero);
            Assert.That(config.MarkMaxStackCount, Is.EqualTo(10));
            Assert.That(config.Match3MarkGainAmount, Is.EqualTo(1));
            Assert.That(config.Match4MarkGainAmount, Is.EqualTo(2));
            Assert.That(config.Match5MarkGainAmount, Is.EqualTo(3));
            Assert.That(config.MarkDamageIncreaseRate,
                Is.EqualTo(0.03d));
            Assert.That(config.Awakening3MarkDamageIncreaseRate,
                Is.EqualTo(0.035d));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(config.ExposureDamageIncreaseRate,
                Is.EqualTo(0.08d));
            Assert.That(config.ExposureDurationTurns, Is.EqualTo(2));
        }

        [TestCase(
            BaekHyeonRules.Match3SkillId,
            "skill_baek_hyeon_match3.asset",
            SkillKind.Match)]
        [TestCase(
            BaekHyeonRules.Match4SkillId,
            "skill_baek_hyeon_match4.asset",
            SkillKind.Match)]
        [TestCase(
            BaekHyeonRules.Match5SkillId,
            "skill_baek_hyeon_match5.asset",
            SkillKind.Match)]
        [TestCase(
            BaekHyeonRules.PassiveSkillId,
            "passive_baek_hyeon_thread_of_weakness.asset",
            SkillKind.Passive)]
        [TestCase(
            BaekHyeonRules.ActiveAbilityId,
            "active_baek_hyeon_one_arrow_is_enough.asset",
            SkillKind.Active)]
        public void SkillsUseCanonicalIdsKindsAndUnassignedIcons(
            string expectedId,
            string fileName,
            SkillKind expectedKind)
        {
            SkillDefinition skill = Load<SkillDefinition>(
                "Assets/Data/Skills/BaekHyeon/" + fileName);

            Assert.That(skill.Id, Is.EqualTo(expectedId));
            Assert.That(skill.DisplayNameKey, Is.Not.Empty);
            Assert.That(skill.DescriptionKey, Is.Not.Empty);
            Assert.That(skill.SkillKind, Is.EqualTo(expectedKind));
            Assert.That(skill.Icon, Is.Null);
        }

        [TestCase(
            BaekHyeonRules.MarkEffectId,
            "effect_baek_hyeon_mark.asset")]
        [TestCase(
            BaekHyeonRules.ExposureEffectId,
            "effect_baek_hyeon_exposure.asset")]
        public void EffectsUseCanonicalIdsAndOptionalValidIcons(
            string expectedId,
            string fileName)
        {
            EffectDefinition effect = Load<EffectDefinition>(
                "Assets/Data/Effects/BaekHyeon/" + fileName);

            Assert.That(effect.Id, Is.EqualTo(expectedId));
            Assert.That(effect.DisplayNameKey, Is.Not.Empty);
            Assert.That(effect.DescriptionKey, Is.Not.Empty);
            Assert.That(
                effect.Icon == null
                    || !string.IsNullOrEmpty(
                        AssetDatabase.GetAssetPath(effect.Icon)),
                Is.True,
                "An assigned effect icon must be a persisted Sprite asset.");
        }

        [Test]
        public void DatabaseAndPresentationCatalogResolveAllContent()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            CharacterPresentationCatalog presentationCatalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            database.Initialize();
            presentationCatalog.Initialize();

            Assert.That(database.TryGetCharacter(
                BaekHyeonRules.CharacterId,
                out CharacterDefinition character), Is.True);
            Assert.That(character.SkillIds,
                Is.EquivalentTo(new[]
                {
                    BaekHyeonRules.Match3SkillId,
                    BaekHyeonRules.Match4SkillId,
                    BaekHyeonRules.Match5SkillId,
                    BaekHyeonRules.PassiveSkillId,
                    BaekHyeonRules.ActiveAbilityId
                }));
            foreach (string skillId in character.SkillIds)
            {
                Assert.That(database.TryGetSkill(skillId, out _),
                    Is.True, skillId);
            }

            Assert.That(database.TryGetEffect(
                BaekHyeonRules.MarkEffectId, out _), Is.True);
            Assert.That(database.TryGetEffect(
                BaekHyeonRules.ExposureEffectId, out _), Is.True);
            Assert.That(presentationCatalog.TryGet(
                BaekHyeonRules.CharacterId,
                out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation,
                Is.SameAs(Load<CharacterPresentationDefinition>(
                    PresentationPath)));
            Assert.That(AssetDatabase.GetAssetPath(
                presentation.FaceSprite),
                Is.EqualTo(
                    "Assets/Sprites/Character/Face/HyunFace.png"));
            Assert.That(AssetDatabase.GetAssetPath(
                presentation.PreviewSprite),
                Is.EqualTo(
                    "Assets/Sprites/Character/Full/HyunFull.png"));
        }

        [Test]
        public void RuntimeRegistrationAndLocalizationAreComplete()
        {
            CharacterRuntimeRegistrationCatalog runtimeCatalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            CharacterRuntimeRegistration registration = runtimeCatalog
                .Registrations.Single(item => item.CharacterId
                    == BaekHyeonRules.CharacterId);

            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(BaekHyeonCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<BaekHyeonBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<BaekHyeonCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<BaekHyeonLookupDescriptionProvider>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.Not.SameAs(registration.CreateCombatRegistrar()));

            TextAsset table = Load<TextAsset>(LocalizationPath);
            LocalizationTable parsed = LocalizationTableParser.Parse(
                table.name,
                table.text);
            var localization = new LocalizationService(new[] { parsed });
            string[] requiredKeys =
            {
                "baek_hyeon",
                "baek_hyeon.skill.passive.name",
                "baek_hyeon.skill.passive.description",
                "baek_hyeon.skill.match3.name",
                "baek_hyeon.skill.match3.description",
                "baek_hyeon.skill.match4.name",
                "baek_hyeon.skill.match4.description",
                "baek_hyeon.skill.match5.name",
                "baek_hyeon.skill.match5.description",
                "baek_hyeon.skill.active.name",
                "baek_hyeon.skill.active.description",
                "baek_hyeon.effect.mark.name",
                "baek_hyeon.effect.mark.description",
                "baek_hyeon.effect.exposure.name",
                "baek_hyeon.effect.exposure.description"
            };
            for (int index = 0; index < requiredKeys.Length; index++)
            {
                Assert.That(localization.GetText(requiredKeys[index]),
                    Does.Not.StartWith("[["), requiredKeys[index]);
            }

            CharacterLookupDescriptionResolver resolver = runtimeCatalog
                .CreateLookupDescriptionResolver(
                    Load<DefinitionDatabase>(DatabasePath),
                    localization);
            foreach (CharacterLookupSkillType skillType in Enum.GetValues(
                typeof(CharacterLookupSkillType)))
            {
                AssertResolved(resolver.ResolveSkill(
                    BaekHyeonRules.CharacterId,
                    currentAwakening: 6,
                    skillType));
            }

            for (int stage = 1; stage <= 6; stage++)
            {
                string nameKey = $"baek_hyeon.awakening.{stage}.name";
                string descriptionKey =
                    $"baek_hyeon.awakening.{stage}.description";
                Assert.That(localization.GetText(nameKey),
                    Does.Not.StartWith("[["), nameKey);
                Assert.That(localization.GetText(descriptionKey),
                    Does.Not.StartWith("[["), descriptionKey);
                AssertResolved(resolver.ResolveAwakening(
                    BaekHyeonRules.CharacterId,
                    stage));
            }
        }

        [Test]
        public void RegistrationIntegrityPassesWithCanonicalAcquisitionIds()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            CharacterPresentationCatalog presentationCatalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            CharacterRuntimeRegistrationCatalog runtimeCatalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            database.Initialize();
            presentationCatalog.Initialize();

            Assert.That(CharacterContentRegistrationValidator.Validate(
                database,
                presentationCatalog,
                runtimeCatalog), Is.Empty);
            Assert.That(database.Gachas.SelectMany(
                gacha => gacha.CharacterPoolIds),
                Does.Contain(BaekHyeonRules.CharacterId));
            Assert.That(database.Gachas.SelectMany(
                gacha => gacha.CharacterPoolIds),
                Does.Not.Contain("baek_hyeon"));
            Assert.That(database.ExchangeCatalog.CharacterIds,
                Does.Contain(BaekHyeonRules.CharacterId));
            Assert.That(database.ExchangeCatalog.CharacterIds,
                Does.Not.Contain("baek_hyeon"));
        }

        [Test]
        public void EveryNewAssetHasAUniqueGuid()
        {
            string[] paths =
            {
                CharacterPath,
                ConfigPath,
                PresentationPath,
                "Assets/Data/Skills/BaekHyeon/"
                    + "skill_baek_hyeon_match3.asset",
                "Assets/Data/Skills/BaekHyeon/"
                    + "skill_baek_hyeon_match4.asset",
                "Assets/Data/Skills/BaekHyeon/"
                    + "skill_baek_hyeon_match5.asset",
                "Assets/Data/Skills/BaekHyeon/"
                    + "passive_baek_hyeon_thread_of_weakness.asset",
                "Assets/Data/Skills/BaekHyeon/"
                    + "active_baek_hyeon_one_arrow_is_enough.asset",
                "Assets/Data/Effects/BaekHyeon/"
                    + "effect_baek_hyeon_mark.asset",
                "Assets/Data/Effects/BaekHyeon/"
                    + "effect_baek_hyeon_exposure.asset"
            };
            string[] guids = paths.Select(
                AssetDatabase.AssetPathToGUID).ToArray();

            Assert.That(guids, Has.All.Not.Empty);
            Assert.That(guids.Distinct().Count(),
                Is.EqualTo(paths.Length));
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private static void AssertResolved(
            CharacterLookupDescriptionResult result)
        {
            Assert.That(result.IsSuccess, Is.True, result.Error);
            Assert.That(result.Text, Does.Not.Contain("[["));
            Assert.That(result.Text, Does.Not.Contain("{"));
        }
    }
}
