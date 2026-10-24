using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Fiora;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Editor.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Party.Battle;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Fiora
{
    public sealed class FioraAmberwingContentAssetTests
    {
        private const string CombatConfigPath =
            "Assets/Data/CombatConfigs/Fiora/"
                + "fiora_amberwing_combat_config.asset";
        private const string CharacterPath =
            "Assets/Data/Characters/fiora_amberwing.asset";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/"
                + "fiora_amberwing_presentation.asset";
        private const string DefinitionDatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";
        private const string FaceSpritePath =
            "Assets/Sprites/Character/Face/FioraFace.png";
        private const string PreviewSpritePath =
            "Assets/Sprites/Character/Full/FioraFull.png";

        [Test]
        public void CombatConfigAssetContainsCanonicalValues()
        {
            FioraAmberwingCombatConfig config = Config();

            Assert.That(config.TryValidate(out string error), Is.True, error);
            Assert.That(config.Match3HealingCoefficient, Is.EqualTo(0.08d));
            Assert.That(config.Match4HealingCoefficient, Is.EqualTo(0.14d));
            Assert.That(config.Match5HealingCoefficient, Is.EqualTo(0.22d));
            Assert.That(config.Match4AttackIncreaseRate, Is.EqualTo(0.05d));
            Assert.That(config.Match5AttackIncreaseRate, Is.EqualTo(0.09d));
            Assert.That(config.Match4BuffDurationTurns, Is.EqualTo(2));
            Assert.That(config.Match5BuffDurationTurns, Is.EqualTo(3));
            Assert.That(config.PassiveConversionRate, Is.EqualTo(0.50d));
            Assert.That(config.PassivePerInstanceCap, Is.EqualTo(0.05d));
            Assert.That(config.PassiveGroupCap, Is.EqualTo(0.20d));
            Assert.That(config.PassiveDurationTurns, Is.EqualTo(2));
            Assert.That(config.ActiveInstantHealingCoefficient,
                Is.EqualTo(0.24d));
            Assert.That(config.ActiveFollowUpHealingCoefficient,
                Is.EqualTo(0.06d));
            Assert.That(config.ActiveFollowUpHealGrantCount, Is.EqualTo(2));
            Assert.That(config.ActiveFollowUpHealMaxCount, Is.EqualTo(4));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(config.Awakening2HealingIncreaseRate,
                Is.EqualTo(0.05d));
            Assert.That(config.Awakening3PassiveConversionRate,
                Is.EqualTo(0.55d));
            Assert.That(config.Awakening4Match3HealingCoefficient,
                Is.EqualTo(0.09d));
            Assert.That(config.Awakening4Match4HealingCoefficient,
                Is.EqualTo(0.15d));
            Assert.That(config.Awakening5ActiveInstantHealingCoefficient,
                Is.EqualTo(0.26d));
            Assert.That(config.Awakening5ActiveFollowUpHealingCoefficient,
                Is.EqualTo(0.07d));
            Assert.That(config.Awakening6FullOverhealConversionRate,
                Is.EqualTo(0.70d));
        }

        [Test]
        public void CharacterDefinitionUsesCanonicalIdentityStatsAndSkills()
        {
            CharacterDefinition definition = Character();

            Assert.That(definition.Id,
                Is.EqualTo(FioraAmberwingRules.CharacterId));
            Assert.That(definition.DisplayNameKey, Is.EqualTo("fiora"));
            Assert.That(definition.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(definition.Level1Hp, Is.EqualTo(1450));
            Assert.That(definition.Level1Attack, Is.EqualTo(80));
            Assert.That(definition.Level100Hp, Is.EqualTo(5800));
            Assert.That(definition.Level100Attack, Is.EqualTo(400));
            Assert.That(definition.CombatConfig, Is.SameAs(Config()));
            Assert.That(definition.SkillIds, Is.EqualTo(new[]
            {
                FioraAmberwingRules.Match3SkillId,
                FioraAmberwingRules.Match4SkillId,
                FioraAmberwingRules.Match5SkillId,
                FioraAmberwingRules.PassiveSkillId,
                FioraAmberwingRules.ActiveAbilityId
            }));
            Assert.That(definition.SkillIds.Distinct().Count(),
                Is.EqualTo(5));
        }

        [TestCase(
            FioraAmberwingRules.Match3SkillId,
            "skill_fiora_amberwing_match3.asset",
            "fiora.skill.match3.name",
            SkillKind.Match)]
        [TestCase(
            FioraAmberwingRules.Match4SkillId,
            "skill_fiora_amberwing_match4.asset",
            "fiora.skill.match4.name",
            SkillKind.Match)]
        [TestCase(
            FioraAmberwingRules.Match5SkillId,
            "skill_fiora_amberwing_match5.asset",
            "fiora.skill.match5.name",
            SkillKind.Match)]
        [TestCase(
            FioraAmberwingRules.PassiveSkillId,
            "passive_fiora_overflown_ember_of_life.asset",
            "fiora.skill.passive.name",
            SkillKind.Passive)]
        [TestCase(
            FioraAmberwingRules.ActiveAbilityId,
            "active_fiora_flame_of_rebirth.asset",
            "fiora.skill.active.name",
            SkillKind.Active)]
        [TestCase(
            FioraAmberwingRules.FollowUpHealingStatusId,
            "status_fiora_flame_of_rebirth.asset",
            "fiora.status.flame_of_rebirth.name",
            SkillKind.Passive)]
        public void SkillDefinitionsUseCanonicalIdsAndValidOptionalIcons(
            string expectedId,
            string fileName,
            string expectedNameKey,
            SkillKind expectedKind)
        {
            SkillDefinition definition = Load<SkillDefinition>(
                "Assets/Data/Skills/Fiora/" + fileName);

            Assert.That(definition.Id, Is.EqualTo(expectedId));
            Assert.That(definition.DisplayNameKey,
                Is.EqualTo(expectedNameKey));
            Assert.That(definition.DescriptionKey, Is.Not.Empty);
            Assert.That(definition.SkillKind, Is.EqualTo(expectedKind));
            AssertOptionalSpriteAsset(definition.Icon);
        }

        [TestCase(
            FioraAmberwingRules.PassiveBlessingEffectId,
            "effect_fiora_phoenix_blessing.asset")]
        [TestCase(
            FioraAmberwingRules.Match4AttackEffectId,
            "effect_fiora_flame_of_elation.asset")]
        [TestCase(
            FioraAmberwingRules.Match5AttackEffectId,
            "effect_fiora_hymn_of_the_phoenix.asset")]
        public void EffectDefinitionsUseCanonicalIdsAndValidOptionalIcons(
            string expectedId,
            string fileName)
        {
            EffectDefinition definition = Load<EffectDefinition>(
                "Assets/Data/Effects/Fiora/" + fileName);

            Assert.That(definition.Id, Is.EqualTo(expectedId));
            Assert.That(definition.DisplayNameKey, Is.Not.Empty);
            Assert.That(definition.DescriptionKey, Is.Not.Empty);
            AssertOptionalSpriteAsset(definition.Icon);
        }

        [Test]
        public void DatabaseAndPresentationCatalogResolveFioraContent()
        {
            DefinitionDatabase database = Database();
            CharacterPresentationCatalog presentationCatalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            database.Initialize();
            presentationCatalog.Initialize();

            Assert.That(database.TryGetCharacter(
                FioraAmberwingRules.CharacterId,
                out CharacterDefinition character), Is.True);
            Assert.That(character, Is.SameAs(Character()));
            foreach (string skillId in character.SkillIds.Concat(new[]
            {
                FioraAmberwingRules.FollowUpHealingStatusId
            }))
            {
                Assert.That(database.TryGetSkill(skillId, out _),
                    Is.True,
                    skillId);
            }

            foreach (string effectId in new[]
            {
                FioraAmberwingRules.PassiveBlessingEffectId,
                FioraAmberwingRules.Match4AttackEffectId,
                FioraAmberwingRules.Match5AttackEffectId
            })
            {
                Assert.That(database.TryGetEffect(effectId, out _),
                    Is.True,
                    effectId);
            }

            Assert.That(presentationCatalog.TryGet(
                FioraAmberwingRules.CharacterId,
                out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation, Is.SameAs(Load<
                CharacterPresentationDefinition>(PresentationPath)));
            Assert.That(AssetDatabase.GetAssetPath(presentation.FaceSprite),
                Is.EqualTo(FaceSpritePath));
            Assert.That(AssetDatabase.GetAssetPath(
                    presentation.PreviewSprite),
                Is.EqualTo(PreviewSpritePath));
        }

        [Test]
        public void RuntimeCatalogRegistersExactlyOneFreshFioraFactorySet()
        {
            CharacterRuntimeRegistrationCatalog catalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            CharacterRuntimeRegistration[] registrations = catalog
                .Registrations
                .Where(item => item.CharacterId
                    == FioraAmberwingRules.CharacterId)
                .ToArray();

            Assert.That(registrations.Length, Is.EqualTo(1));
            CharacterRuntimeRegistration registration = registrations[0];
            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(FioraAmberwingCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<FioraAmberwingBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<FioraAmberwingCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<FioraAmberwingLookupDescriptionProvider>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.Not.SameAs(registration.CreateCombatRegistrar()));
        }

        [Test]
        public void SeparateRegistrationsCreateIndependentBattleStates()
        {
            CharacterDefinition definition = Character();
            CharacterRuntimeRegistration registration =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Single(item => item.CharacterId
                        == FioraAmberwingRules.CharacterId);
            var boss = new BossBattleState(
                "fiora_content_test_boss",
                ElementType.Grass,
                100000,
                100d);

            CharacterCombatProviderRegistrationResult first = Register(
                registration.CreateCombatRegistrar(),
                definition,
                boss,
                partySlotIndex: 0);
            CharacterCombatProviderRegistrationResult second = Register(
                registration.CreateCombatRegistrar(),
                definition,
                boss,
                partySlotIndex: 1);

            Assert.That(first.CombatStatusSources.Single(),
                Is.TypeOf<FioraAmberwingBattleState>());
            Assert.That(second.CombatStatusSources.Single(),
                Is.TypeOf<FioraAmberwingBattleState>());
            Assert.That(first.CombatStatusSources.Single(),
                Is.Not.SameAs(second.CombatStatusSources.Single()));
            Assert.That(first.PreMatchQueueActionProviders.Single(),
                Is.Not.SameAs(second.PreMatchQueueActionProviders.Single()));
        }

        [Test]
        public void LocalizationAndLookupResolveEveryFioraEntry()
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
                for (int awakening = 0; awakening <= 6; awakening++)
                {
                    CharacterLookupDescriptionResult result =
                        resolver.ResolveSkill(
                            FioraAmberwingRules.CharacterId,
                            awakening,
                            skillType);
                    AssertResolved(result);
                }
            }

            for (int awakening = 1; awakening <= 6; awakening++)
            {
                CharacterLookupDescriptionResult result =
                    resolver.ResolveAwakening(
                        FioraAmberwingRules.CharacterId,
                        awakening);
                AssertResolved(result);
            }

            CharacterLookupDescriptionResult awakening4 =
                resolver.ResolveAwakening(
                    FioraAmberwingRules.CharacterId,
                    4);
            CharacterLookupDescriptionResult awakening5 =
                resolver.ResolveAwakening(
                    FioraAmberwingRules.CharacterId,
                    5);
            CharacterLookupDescriptionResult awakening6 =
                resolver.ResolveAwakening(
                    FioraAmberwingRules.CharacterId,
                    6);
            Assert.That(awakening4.Text, Does.Contain("15%"));
            Assert.That(awakening5.Text, Does.Contain("7%"));
            Assert.That(awakening6.Text, Does.Contain("70%"));
            Assert.That(localization.GetText("fiora"),
                Is.EqualTo("Fiora Amberwing"));
            Assert.That(localization.GetText("fiora.description"),
                Is.Not.Empty);

            localization.SetLocale("ko");
            CharacterLookupDescriptionResult korean =
                resolver.ResolveAwakening(
                    FioraAmberwingRules.CharacterId,
                    6);
            AssertResolved(korean);
            Assert.That(korean.Text, Does.Contain("영원히 불타는 광휘"));
            Assert.That(localization.GetText("fiora"),
                Is.EqualTo("피오라 앰버윙"));
        }

        [Test]
        public void CanonicalContentValidationHasNoErrors()
        {
            DefinitionDatabase database = Database();
            CharacterPresentationCatalog presentationCatalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            database.Initialize();
            presentationCatalog.Initialize();

            ValidationReport report = DataValidator.Validate(database);
            Assert.That(report.Issues.Where(issue =>
                issue.Severity == ValidationSeverity.Error), Is.Empty);
            IReadOnlyList<string> registrationIssues =
                CharacterContentRegistrationValidator.Validate(
                    database,
                    presentationCatalog,
                    CharacterRuntimeRegistrationCatalog.CreateDefault());
            Assert.That(registrationIssues, Is.Empty);
        }

        [Test]
        public void FioraAssetsHaveDistinctValidGuids()
        {
            string[] paths =
            {
                CharacterPath,
                CombatConfigPath,
                PresentationPath,
                "Assets/Data/Skills/Fiora/skill_fiora_amberwing_match3.asset",
                "Assets/Data/Skills/Fiora/skill_fiora_amberwing_match4.asset",
                "Assets/Data/Skills/Fiora/skill_fiora_amberwing_match5.asset",
                "Assets/Data/Skills/Fiora/passive_fiora_overflown_ember_of_life.asset",
                "Assets/Data/Skills/Fiora/active_fiora_flame_of_rebirth.asset",
                "Assets/Data/Skills/Fiora/status_fiora_flame_of_rebirth.asset",
                "Assets/Data/Effects/Fiora/effect_fiora_phoenix_blessing.asset",
                "Assets/Data/Effects/Fiora/effect_fiora_flame_of_elation.asset",
                "Assets/Data/Effects/Fiora/effect_fiora_hymn_of_the_phoenix.asset"
            };
            string[] guids = paths.Select(
                AssetDatabase.AssetPathToGUID).ToArray();

            Assert.That(guids, Has.All.Not.Empty);
            Assert.That(guids.Distinct().Count(), Is.EqualTo(paths.Length));
        }

        private static CharacterCombatProviderRegistrationResult Register(
            ICharacterCombatProviderRegistrar registrar,
            CharacterDefinition definition,
            BossBattleState boss,
            int partySlotIndex)
        {
            var character = new CharacterBattleState(
                definition.Id,
                partySlotIndex,
                definition.Element,
                definition.Level100Hp,
                definition.Level100Attack);
            return registrar.Register(
                new BattlePartyMemberInput(
                    definition.Id,
                    partySlotIndex,
                    level: 100,
                    awakening: 6,
                    definition),
                character,
                boss,
                new CombatActionIdSequence(),
                new MatchEventActionProviderRegistry(),
                new ActiveAbilityActionProviderRegistry());
        }

        private static void AssertResolved(
            CharacterLookupDescriptionResult result)
        {
            Assert.That(result.IsSuccess, Is.True, result.Error);
            Assert.That(result.Text, Does.Not.Contain("[["));
            Assert.That(result.Text, Does.Not.Contain("{"));
        }

        private static FioraAmberwingCombatConfig Config()
        {
            return Load<FioraAmberwingCombatConfig>(CombatConfigPath);
        }

        private static CharacterDefinition Character()
        {
            return Load<CharacterDefinition>(CharacterPath);
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
            string assetPath = AssetDatabase.GetAssetPath(sprite);
            Assert.That(assetPath, Is.Not.Empty);
            Assert.That(
                AssetDatabase.LoadAllAssetsAtPath(assetPath),
                Does.Contain(sprite));
        }
    }
}
