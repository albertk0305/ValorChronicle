using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Editor.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Party.Battle;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodContentAssetTests
    {
        private const string CombatConfigPath =
            "Assets/Data/CombatConfigs/Brina/"
                + "brina_ashwood_combat_config.asset";
        private const string CharacterPath =
            "Assets/Data/Characters/brina_ashwood.asset";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/"
                + "brina_ashwood_presentation.asset";
        private const string DefinitionDatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";
        private const string FaceSpritePath =
            "Assets/Sprites/Character/Face/BrinaFace.png";
        private const string PreviewSpritePath =
            "Assets/Sprites/Character/Full/BrinaFull.png";

        [Test]
        public void CombatConfigAssetContainsCanonicalValues()
        {
            BrinaAshwoodCombatConfig config = Config();

            Assert.That(config.TryValidate(out string error), Is.True, error);
            Assert.That(config.Match3Coefficient, Is.EqualTo(1.30d));
            Assert.That(config.Match4Coefficient, Is.EqualTo(2.40d));
            Assert.That(config.Match5Coefficient, Is.EqualTo(4.00d));
            Assert.That(config.Match3WoodcutGainAmount, Is.EqualTo(1));
            Assert.That(config.Match4WoodcutGainAmount, Is.EqualTo(2));
            Assert.That(config.Match5WoodcutGainAmount, Is.EqualTo(3));
            Assert.That(config.WoodcutTriggerThreshold, Is.EqualTo(10));
            Assert.That(config.PassiveAdditionalCoefficient,
                Is.EqualTo(5.00d));
            Assert.That(config.ActiveWoodcutGainMultiplier,
                Is.EqualTo(2.00d));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(config.ActiveDurationTurns, Is.EqualTo(1));
            Assert.That(config.Awakening3PassiveAdditionalCoefficient,
                Is.EqualTo(5.25d));
            Assert.That(config.Awakening4Match3Coefficient,
                Is.EqualTo(1.35d));
            Assert.That(config.Awakening4Match4Coefficient,
                Is.EqualTo(2.50d));
            Assert.That(config.Awakening5ActiveCooldownTurns, Is.EqualTo(7));
            Assert.That(config.Awakening6FirstWoodcutBonusAmount,
                Is.EqualTo(1));
        }

        [Test]
        public void CharacterDefinitionUsesCanonicalIdentityStatsAndSkills()
        {
            CharacterDefinition definition = Character();

            Assert.That(definition.Id,
                Is.EqualTo(BrinaAshwoodRules.CharacterId));
            Assert.That(definition.DisplayNameKey, Is.EqualTo("brina"));
            Assert.That(definition.Element, Is.EqualTo(ElementType.Grass));
            Assert.That(definition.Level1Hp, Is.EqualTo(900));
            Assert.That(definition.Level1Attack, Is.EqualTo(180));
            Assert.That(definition.Level100Hp, Is.EqualTo(3400));
            Assert.That(definition.Level100Attack, Is.EqualTo(1050));
            Assert.That(definition.CombatConfig, Is.SameAs(Config()));
            Assert.That(definition.SkillIds, Is.EqualTo(new[]
            {
                BrinaAshwoodRules.Match3SkillId,
                BrinaAshwoodRules.Match4SkillId,
                BrinaAshwoodRules.Match5SkillId,
                BrinaAshwoodRules.PassiveSkillId,
                BrinaAshwoodRules.ActiveAbilityId
            }));
            Assert.That(definition.SkillIds,
                Does.Not.Contain(BrinaAshwoodRules.WoodcutStatusId));
        }

        [TestCase(
            BrinaAshwoodRules.Match3SkillId,
            "skill_brina_ashwood_match3.asset",
            SkillKind.Match)]
        [TestCase(
            BrinaAshwoodRules.Match4SkillId,
            "skill_brina_ashwood_match4.asset",
            SkillKind.Match)]
        [TestCase(
            BrinaAshwoodRules.Match5SkillId,
            "skill_brina_ashwood_match5.asset",
            SkillKind.Match)]
        [TestCase(
            BrinaAshwoodRules.PassiveSkillId,
            "passive_brina_ten_chops.asset",
            SkillKind.Passive)]
        [TestCase(
            BrinaAshwoodRules.ActiveAbilityId,
            "active_brina_swing_hard.asset",
            SkillKind.Active)]
        [TestCase(
            BrinaAshwoodRules.WoodcutStatusId,
            "status_brina_woodcut.asset",
            SkillKind.Passive)]
        public void SkillDefinitionsUseCanonicalIdsKindsAndOptionalIcons(
            string expectedId,
            string fileName,
            SkillKind expectedKind)
        {
            SkillDefinition definition = Load<SkillDefinition>(
                "Assets/Data/Skills/Brina/" + fileName);

            Assert.That(definition.Id, Is.EqualTo(expectedId));
            Assert.That(definition.DisplayNameKey, Is.Not.Empty);
            Assert.That(definition.DescriptionKey, Is.Not.Empty);
            Assert.That(definition.SkillKind, Is.EqualTo(expectedKind));
            AssertOptionalIconIsValid(definition.Icon);
        }

        [Test]
        public void ActiveEffectDefinitionAndRuntimeMetadataMatch()
        {
            EffectDefinition definition = Load<EffectDefinition>(
                "Assets/Data/Effects/Brina/effect_brina_swing_hard.asset");
            var actor = new CharacterBattleState(
                BrinaAshwoodRules.CharacterId,
                partySlotIndex: 0,
                ElementType.Grass,
                maxHp: 900,
                attack: 180d);
            var provider = new BrinaAshwoodActiveActionProvider(Config());
            ApplyEffectAction action = provider.CreateActions(
                    actor,
                    new CombatActionIdSequence())
                .Single() as ApplyEffectAction;

            Assert.That(definition.Id,
                Is.EqualTo(BrinaAshwoodRules.ActiveEffectId));
            AssertOptionalIconIsValid(definition.Icon);
            Assert.That(action, Is.Not.Null);
            Assert.That(action.Effect.EffectId,
                Is.EqualTo(definition.Id));
            Assert.That(action.Effect.Category,
                Is.EqualTo(EffectCategory.Buff));
            Assert.That(action.Effect.ModifierType,
                Is.EqualTo(EffectModifierType.AttackIncrease));
            Assert.That(action.Effect.Magnitude, Is.Zero);
            Assert.That(action.Effect.RemainingTurns, Is.EqualTo(1));
            Assert.That(action.Effect.StackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
        }

        [Test]
        public void DatabasePresentationAndValidationResolveBrinaContent()
        {
            DefinitionDatabase database = Database();
            CharacterPresentationCatalog presentationCatalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            database.Initialize();
            presentationCatalog.Initialize();

            Assert.That(database.TryGetCharacter(
                BrinaAshwoodRules.CharacterId,
                out CharacterDefinition character), Is.True);
            Assert.That(character, Is.SameAs(Character()));
            foreach (string skillId in character.SkillIds.Concat(new[]
            {
                BrinaAshwoodRules.WoodcutStatusId
            }))
            {
                Assert.That(database.TryGetSkill(skillId, out _),
                    Is.True,
                    skillId);
            }

            Assert.That(database.TryGetEffect(
                BrinaAshwoodRules.ActiveEffectId,
                out _), Is.True);
            Assert.That(presentationCatalog.TryGet(
                BrinaAshwoodRules.CharacterId,
                out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation, Is.SameAs(Load<
                CharacterPresentationDefinition>(PresentationPath)));
            Assert.That(AssetDatabase.GetAssetPath(presentation.FaceSprite),
                Is.EqualTo(FaceSpritePath));
            Assert.That(AssetDatabase.GetAssetPath(
                    presentation.PreviewSprite),
                Is.EqualTo(PreviewSpritePath));
            Assert.That(
                AssetDatabase.LoadAllAssetsAtPath(FaceSpritePath),
                Does.Contain(presentation.FaceSprite));

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
        public void RuntimeCatalogCreatesFreshBrinaComponentsAndBattleStates()
        {
            CharacterRuntimeRegistration registration =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Single(item => item.CharacterId
                        == BrinaAshwoodRules.CharacterId);

            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(BrinaAshwoodCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<BrinaAshwoodBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<BrinaAshwoodCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<BrinaAshwoodLookupDescriptionProvider>());

            CharacterCombatProviderRegistrationResult first = Register(
                registration.CreateCombatRegistrar(),
                partySlotIndex: 0);
            CharacterCombatProviderRegistrationResult second = Register(
                registration.CreateCombatRegistrar(),
                partySlotIndex: 1);
            Assert.That(first.CombatStatusSources.Single(),
                Is.TypeOf<BrinaAshwoodBattleState>());
            Assert.That(first.CombatStatusSources.Single(),
                Is.Not.SameAs(second.CombatStatusSources.Single()));
        }

        [Test]
        public void LocalizationAndLookupResolveEveryBrinaEntry()
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
                        BrinaAshwoodRules.CharacterId,
                        currentAwakening: 6,
                        skillType);
                AssertResolved(result);
            }

            for (int awakening = 1; awakening <= 6; awakening++)
            {
                AssertResolved(resolver.ResolveAwakening(
                    BrinaAshwoodRules.CharacterId,
                    awakening));
            }

            Assert.That(localization.GetText("brina"),
                Is.EqualTo("Brina Ashwood"));
            Assert.That(localization.GetText("brina.skill.match3.name"),
                Is.EqualTo("Branch Cut - 3 Match"));
            Assert.That(localization.GetText("brina.skill.match4.name"),
                Is.EqualTo("Heavy Chop - 4 Match"));
            Assert.That(localization.GetText("brina.skill.match5.name"),
                Is.EqualTo("Felling Wedge - 5+ Match"));
            Assert.That(localization.GetText(
                "brina.status.woodcut.name"), Is.Not.Empty);
            Assert.That(localization.GetText(
                "brina.effect.swing_hard.name"), Is.Not.Empty);

            localization.SetLocale("ko");
            Assert.That(localization.GetText("brina"),
                Is.EqualTo("브리나 애쉬우드"));
            Assert.That(localization.GetText("brina.awakening.6.name"),
                Is.EqualTo("첫 자국은 깊게"));
            AssertResolved(resolver.ResolveAwakening(
                BrinaAshwoodRules.CharacterId,
                awakeningStage: 6));
        }

        [Test]
        public void BrinaAssetsHaveDistinctValidGuids()
        {
            string[] paths =
            {
                CharacterPath,
                CombatConfigPath,
                PresentationPath,
                "Assets/Data/Skills/Brina/skill_brina_ashwood_match3.asset",
                "Assets/Data/Skills/Brina/skill_brina_ashwood_match4.asset",
                "Assets/Data/Skills/Brina/skill_brina_ashwood_match5.asset",
                "Assets/Data/Skills/Brina/passive_brina_ten_chops.asset",
                "Assets/Data/Skills/Brina/active_brina_swing_hard.asset",
                "Assets/Data/Skills/Brina/status_brina_woodcut.asset",
                "Assets/Data/Effects/Brina/effect_brina_swing_hard.asset"
            };
            string[] guids = paths.Select(
                AssetDatabase.AssetPathToGUID).ToArray();

            Assert.That(guids, Has.All.Not.Empty);
            Assert.That(guids.Distinct().Count(), Is.EqualTo(paths.Length));
        }

        private static CharacterCombatProviderRegistrationResult Register(
            ICharacterCombatProviderRegistrar registrar,
            int partySlotIndex)
        {
            CharacterDefinition definition = Character();
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
                new BossBattleState(
                    "brina_content_test_boss",
                    ElementType.Water,
                    maxHp: 100000,
                    attack: 100d),
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

        private static void AssertOptionalIconIsValid(Sprite icon)
        {
            if (icon == null)
            {
                return;
            }

            Assert.That(icon.name, Is.Not.Empty);
            Assert.That(AssetDatabase.Contains(icon), Is.True);
        }

        private static BrinaAshwoodCombatConfig Config()
        {
            return Load<BrinaAshwoodCombatConfig>(CombatConfigPath);
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
    }
}
