using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Koharu;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Koharu
{
    public sealed class SakuragiKoharuContentAssetTests
    {
        private const string CharacterPath =
            "Assets/Data/Characters/sakuragi_koharu.asset";
        private const string ConfigPath =
            "Assets/Data/CombatConfigs/Koharu/"
                + "sakuragi_koharu_combat_config.asset";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/"
                + "sakuragi_koharu_presentation.asset";
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
            SakuragiKoharuCombatConfig config = Load<
                SakuragiKoharuCombatConfig>(ConfigPath);

            Assert.That(character.Id,
                Is.EqualTo(SakuragiKoharuRules.CharacterId));
            Assert.That(character.DisplayNameKey, Is.EqualTo("koharu"));
            Assert.That(character.Element, Is.EqualTo(ElementType.Grass));
            Assert.That(character.Level1Hp, Is.EqualTo(1100));
            Assert.That(character.Level1Attack, Is.EqualTo(120));
            Assert.That(character.Level100Hp, Is.EqualTo(4300));
            Assert.That(character.Level100Attack, Is.EqualTo(650));
            Assert.That(character.CombatConfig, Is.SameAs(config));
            Assert.That(config.TryValidate(out string error),
                Is.True, error);

            Assert.That(config.Match3Coefficient, Is.EqualTo(0.50d));
            Assert.That(config.Match4Coefficient, Is.EqualTo(0.80d));
            Assert.That(config.Match5Coefficient, Is.EqualTo(1.30d));
            Assert.That(config.Match3SpiritEnergyGain, Is.EqualTo(1));
            Assert.That(config.Match4SpiritEnergyGain, Is.EqualTo(2));
            Assert.That(config.Match5SpiritEnergyGain, Is.EqualTo(3));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(4));
            Assert.That(config.ActiveBlockConversionCount, Is.EqualTo(2));

            SakuragiKoharuResolvedCombatRules baseRules =
                SakuragiKoharuCombatRuleResolver.Resolve(config, 0);
            SakuragiKoharuResolvedCombatRules maxRules =
                SakuragiKoharuCombatRuleResolver.Resolve(config, 6);
            Assert.That(baseRules.Match5AdditionalAndChaseDamageIncreaseRate,
                Is.EqualTo(0.12d));
            Assert.That(baseRules.Match5EffectDurationTurns, Is.EqualTo(2));
            Assert.That(baseRules.SpiritEnergyCostPerCooldownReduction,
                Is.EqualTo(6));
            Assert.That(maxRules.Match3Coefficient, Is.EqualTo(0.55d));
            Assert.That(maxRules.Match4Coefficient, Is.EqualTo(0.90d));
            Assert.That(maxRules.Match5Coefficient, Is.EqualTo(1.45d));
            Assert.That(maxRules.Match5AdditionalAndChaseDamageIncreaseRate,
                Is.EqualTo(0.15d));
            Assert.That(maxRules.SpiritEnergyCostPerCooldownReduction,
                Is.EqualTo(5));
            Assert.That(maxRules.Awakening6AdditionalAndChaseDamageIncreaseRate,
                Is.EqualTo(0.10d));
            Assert.That(maxRules.Awakening6EffectDurationTurns, Is.EqualTo(1));
        }

        [TestCase(
            SakuragiKoharuRules.Match3AbilityId,
            "skill_sakuragi_koharu_fox_spirits_whisper.asset",
            SkillKind.Match)]
        [TestCase(
            SakuragiKoharuRules.Match4AbilityId,
            "skill_sakuragi_koharu_dance_of_a_thousand_leaves.asset",
            SkillKind.Match)]
        [TestCase(
            SakuragiKoharuRules.Match5AbilityId,
            "skill_sakuragi_koharu_celestial_foxs_blessing.asset",
            SkillKind.Match)]
        [TestCase(
            SakuragiKoharuRules.PassiveAbilityId,
            "passive_sakuragi_koharu_spirit_energy.asset",
            SkillKind.Passive)]
        [TestCase(
            SakuragiKoharuRules.ActiveAbilityId,
            "active_sakuragi_koharu_fox_spirits_mischief.asset",
            SkillKind.Active)]
        public void SkillsUseCanonicalIdsAndKinds(
            string expectedId,
            string fileName,
            SkillKind expectedKind)
        {
            SkillDefinition skill = Load<SkillDefinition>(
                "Assets/Data/Skills/Koharu/" + fileName);

            Assert.That(skill.Id, Is.EqualTo(expectedId));
            Assert.That(skill.DisplayNameKey, Is.Not.Empty);
            Assert.That(skill.DescriptionKey, Is.Not.Empty);
            Assert.That(skill.SkillKind, Is.EqualTo(expectedKind));
            AssertPersistentIfAssigned(skill.Icon);
        }

        [TestCase(
            SakuragiKoharuRules.Match5EffectId,
            "effect_sakuragi_koharu_match5_blessing.asset")]
        [TestCase(
            SakuragiKoharuRules.Awakening6ActiveEffectId,
            "effect_sakuragi_koharu_fox_blessing.asset")]
        public void EffectsUseCanonicalIdsAndApprovedIcon(
            string expectedId,
            string fileName)
        {
            EffectDefinition effect = Load<EffectDefinition>(
                "Assets/Data/Effects/Koharu/" + fileName);

            Assert.That(effect.Id, Is.EqualTo(expectedId));
            Assert.That(effect.DisplayNameKey, Is.Not.Empty);
            Assert.That(effect.DescriptionKey, Is.Not.Empty);
            Assert.That(effect.Icon, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(effect.Icon),
                Is.EqualTo(
                    "Assets/Sprites/Effects/Koharu/FoxBlessingIcon.png"));
        }

        [Test]
        public void PassiveIdResolvesSpiritEnergyHudIcon()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            database.Initialize();

            Assert.That(SakuragiKoharuRules.SpiritEnergyStatusId,
                Is.EqualTo(SakuragiKoharuRules.PassiveAbilityId));
            Assert.That(database.TryGetSkill(
                SakuragiKoharuRules.SpiritEnergyStatusId,
                out SkillDefinition passive), Is.True);
            Assert.That(passive.Icon, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(passive.Icon),
                Is.EqualTo(
                    "Assets/Sprites/Effects/Koharu/SpiritEnergyIcon.png"));
        }

        [Test]
        public void DatabaseAndCatalogResolveEveryKoharuDefinition()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            CharacterPresentationCatalog catalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            database.Initialize();
            catalog.Initialize();

            Assert.That(database.TryGetCharacter(
                SakuragiKoharuRules.CharacterId,
                out CharacterDefinition character), Is.True);
            Assert.That(character.SkillIds,
                Is.EquivalentTo(new[]
                {
                    SakuragiKoharuRules.Match3AbilityId,
                    SakuragiKoharuRules.Match4AbilityId,
                    SakuragiKoharuRules.Match5AbilityId,
                    SakuragiKoharuRules.PassiveAbilityId,
                    SakuragiKoharuRules.ActiveAbilityId
                }));
            foreach (string skillId in character.SkillIds)
            {
                Assert.That(database.TryGetSkill(skillId, out _),
                    Is.True, skillId);
            }

            Assert.That(database.TryGetEffect(
                SakuragiKoharuRules.Match5EffectId, out _), Is.True);
            Assert.That(database.TryGetEffect(
                SakuragiKoharuRules.Awakening6ActiveEffectId, out _),
                Is.True);
            Assert.That(catalog.TryGet(
                SakuragiKoharuRules.CharacterId,
                out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation,
                Is.SameAs(Load<CharacterPresentationDefinition>(
                    PresentationPath)));
        }

        [Test]
        public void RuntimeRegistrationAndLocalizationResolveKoharu()
        {
            CharacterRuntimeRegistration registration =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Single(item => item.CharacterId
                        == SakuragiKoharuRules.CharacterId);
            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(SakuragiKoharuCombatConfig)));

            TextAsset table = Load<TextAsset>(LocalizationPath);
            var localization = new LocalizationService(new[]
            {
                LocalizationTableParser.Parse(table.name, table.text)
            });
            CharacterLookupDescriptionResolver resolver =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateLookupDescriptionResolver(
                        Load<DefinitionDatabase>(DatabasePath),
                        localization);

            foreach (CharacterLookupSkillType skillType in Enum.GetValues(
                typeof(CharacterLookupSkillType)))
            {
                AssertResolved(resolver.ResolveSkill(
                    SakuragiKoharuRules.CharacterId,
                    currentAwakening: 6,
                    skillType));
            }

            for (int stage = 1; stage <= 6; stage++)
            {
                AssertResolved(resolver.ResolveAwakening(
                    SakuragiKoharuRules.CharacterId,
                    stage));
            }
        }

        [Test]
        public void EveryNewAssetHasAUniqueGuid()
        {
            string[] paths =
            {
                CharacterPath,
                ConfigPath,
                PresentationPath,
                "Assets/Data/Skills/Koharu/"
                    + "skill_sakuragi_koharu_fox_spirits_whisper.asset",
                "Assets/Data/Skills/Koharu/"
                    + "skill_sakuragi_koharu_dance_of_a_thousand_leaves.asset",
                "Assets/Data/Skills/Koharu/"
                    + "skill_sakuragi_koharu_celestial_foxs_blessing.asset",
                "Assets/Data/Skills/Koharu/"
                    + "passive_sakuragi_koharu_spirit_energy.asset",
                "Assets/Data/Skills/Koharu/"
                    + "active_sakuragi_koharu_fox_spirits_mischief.asset",
                "Assets/Data/Effects/Koharu/"
                    + "effect_sakuragi_koharu_match5_blessing.asset",
                "Assets/Data/Effects/Koharu/"
                    + "effect_sakuragi_koharu_fox_blessing.asset"
            };
            string[] guids = paths.Select(
                AssetDatabase.AssetPathToGUID).ToArray();

            Assert.That(guids, Has.All.Not.Empty);
            Assert.That(guids.Distinct().Count(), Is.EqualTo(paths.Length));
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private static void AssertPersistentIfAssigned(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Assert.That(EditorUtility.IsPersistent(sprite), Is.True);
            Assert.That(AssetDatabase.GetAssetPath(sprite), Is.Not.Empty);
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
