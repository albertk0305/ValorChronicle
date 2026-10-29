using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Editor.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Party.Battle;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornContentRegistrationTests
    {
        private const string CharacterPath =
            "Assets/Data/Characters/naira_windhorn.asset";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/"
                + "naira_windhorn_presentation.asset";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";

        [TestCase(
            NairaWindhornRules.Match3AbilityId,
            "skill_naira_windhorn_grove_veil.asset",
            SkillKind.Match)]
        [TestCase(
            NairaWindhornRules.Match4AbilityId,
            "skill_naira_windhorn_antler_reprisal.asset",
            SkillKind.Match)]
        [TestCase(
            NairaWindhornRules.Match5AbilityId,
            "skill_naira_windhorn_sacred_stag_charge.asset",
            SkillKind.Match)]
        [TestCase(
            NairaWindhornRules.PassiveAbilityId,
            "passive_naira_windhorn_spirit_ward.asset",
            SkillKind.Passive)]
        [TestCase(
            NairaWindhornRules.ActiveAbilityId,
            "active_naira_windhorn_sacred_stag_descent.asset",
            SkillKind.Active)]
        public void SkillsUseCanonicalIdsKindsAndSupportOptionalIcons(
            string expectedId,
            string fileName,
            SkillKind expectedKind)
        {
            SkillDefinition definition = Load<SkillDefinition>(
                "Assets/Data/Skills/Naira/" + fileName);

            Assert.That(definition.Id, Is.EqualTo(expectedId));
            Assert.That(definition.DisplayNameKey, Is.Not.Empty);
            Assert.That(definition.DescriptionKey, Is.Not.Empty);
            Assert.That(definition.SkillKind, Is.EqualTo(expectedKind));
            AssertOptionalSpriteAsset(definition.Icon);
            Assert.That(new SerializedObject(definition)
                .FindProperty("icon"), Is.Not.Null);
        }

        [TestCase(
            NairaWindhornRules.SpiritWardEffectId,
            "effect_naira_windhorn_spirit_ward.asset")]
        [TestCase(
            NairaWindhornRules.WhiteStagGraceEffectId,
            "effect_naira_windhorn_white_stag_grace.asset")]
        [TestCase(
            NairaWindhornRules.AncientGuardianEffectId,
            "effect_naira_windhorn_ancient_guardian.asset")]
        public void EffectsUseCanonicalIdsAndSupportOptionalIcons(
            string expectedId,
            string fileName)
        {
            EffectDefinition definition = Load<EffectDefinition>(
                "Assets/Data/Effects/Naira/" + fileName);

            Assert.That(definition.Id, Is.EqualTo(expectedId));
            Assert.That(definition.DisplayNameKey, Is.Not.Empty);
            Assert.That(definition.DescriptionKey, Is.Not.Empty);
            AssertOptionalSpriteAsset(definition.Icon);
            Assert.That(new SerializedObject(definition)
                .FindProperty("icon"), Is.Not.Null);
        }

        [Test]
        public void DatabasePresentationAndValidatorsResolveNaira()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            CharacterPresentationCatalog presentationCatalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            database.Initialize();
            presentationCatalog.Initialize();

            Assert.That(database.TryGetCharacter(
                NairaWindhornRules.CharacterId,
                out CharacterDefinition character), Is.True);
            foreach (string skillId in character.SkillIds)
            {
                Assert.That(database.TryGetSkill(skillId, out _),
                    Is.True,
                    skillId);
            }

            foreach (string effectId in new[]
            {
                NairaWindhornRules.SpiritWardEffectId,
                NairaWindhornRules.WhiteStagGraceEffectId,
                NairaWindhornRules.AncientGuardianEffectId
            })
            {
                Assert.That(database.TryGetEffect(effectId, out _),
                    Is.True,
                    effectId);
            }

            Assert.That(presentationCatalog.TryGet(
                NairaWindhornRules.CharacterId,
                out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation, Is.SameAs(Load<
                CharacterPresentationDefinition>(PresentationPath)));
            Assert.That(AssetDatabase.GetAssetPath(presentation.FaceSprite),
                Is.EqualTo("Assets/Sprites/Character/Face/NairaFace.png"));
            Assert.That(AssetDatabase.GetAssetPath(
                    presentation.PreviewSprite),
                Is.EqualTo("Assets/Sprites/Character/Full/NairaFull.png"));

            ValidationReport report = DataValidator.Validate(database);
            Assert.That(report.Issues.Where(issue =>
                issue.Severity == ValidationSeverity.Error), Is.Empty);
            IReadOnlyList<string> issues =
                CharacterContentRegistrationValidator.Validate(
                    database,
                    presentationCatalog,
                    CharacterRuntimeRegistrationCatalog.CreateDefault());
            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void RuntimeRegistrationCreatesFreshComponentsAndRules()
        {
            CharacterRuntimeRegistration registration =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Single(item => item.CharacterId
                        == NairaWindhornRules.CharacterId);

            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(NairaWindhornCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<NairaWindhornBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<NairaWindhornCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<NairaWindhornLookupDescriptionProvider>());
            Assert.That(registration.CreateBuildModifier(),
                Is.Not.SameAs(registration.CreateBuildModifier()));
            Assert.That(registration.CreateCombatRegistrar(),
                Is.Not.SameAs(registration.CreateCombatRegistrar()));
            Assert.That(registration.CreateLookupProvider(),
                Is.Not.SameAs(registration.CreateLookupProvider()));

            CharacterCombatProviderRegistrationResult first = Register(
                registration.CreateCombatRegistrar(),
                partySlotIndex: 0);
            CharacterCombatProviderRegistrationResult second = Register(
                registration.CreateCombatRegistrar(),
                partySlotIndex: 1);
            Assert.That(first.TriggerRules.Single(),
                Is.TypeOf<NairaWindhornAncientGuardianRule>());
            Assert.That(first.TriggerRules.Single(),
                Is.Not.SameAs(second.TriggerRules.Single()));
        }

        [Test]
        public void LocalizationAndLookupResolveEveryEntry()
        {
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
                    NairaWindhornRules.CharacterId,
                    currentAwakening: 6,
                    skillType));
            }

            for (int awakening = 1; awakening <= 6; awakening++)
            {
                AssertResolved(resolver.ResolveAwakening(
                    NairaWindhornRules.CharacterId,
                    awakening));
            }

            Assert.That(localization.GetText("naira"),
                Is.EqualTo("Naira Windhorn"));
            Assert.That(localization.GetText(
                "naira.effect.white_stag_grace.name"),
                Is.EqualTo("White Stag's Grace"));
            localization.SetLocale("ko");
            Assert.That(localization.GetText("naira"),
                Is.EqualTo("나이라 윈드혼"));
            Assert.That(localization.GetText("naira.awakening.6.name"),
                Is.EqualTo("영령의 품"));
        }

        [Test]
        public void NairaContentAssetsHaveDistinctGuids()
        {
            string[] paths =
            {
                CharacterPath,
                PresentationPath,
                "Assets/Data/Skills/Naira/"
                    + "skill_naira_windhorn_grove_veil.asset",
                "Assets/Data/Skills/Naira/"
                    + "skill_naira_windhorn_antler_reprisal.asset",
                "Assets/Data/Skills/Naira/"
                    + "skill_naira_windhorn_sacred_stag_charge.asset",
                "Assets/Data/Skills/Naira/"
                    + "passive_naira_windhorn_spirit_ward.asset",
                "Assets/Data/Skills/Naira/"
                    + "active_naira_windhorn_sacred_stag_descent.asset",
                "Assets/Data/Effects/Naira/"
                    + "effect_naira_windhorn_spirit_ward.asset",
                "Assets/Data/Effects/Naira/"
                    + "effect_naira_windhorn_white_stag_grace.asset",
                "Assets/Data/Effects/Naira/"
                    + "effect_naira_windhorn_ancient_guardian.asset"
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
            CharacterDefinition definition = Load<CharacterDefinition>(
                CharacterPath);
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
                    "naira_content_test_boss",
                    ElementType.Fire,
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

        private static void AssertOptionalSpriteAsset(Sprite icon)
        {
            if (icon == null)
            {
                return;
            }

            Assert.That(icon, Is.InstanceOf<Sprite>());
            Assert.That(EditorUtility.IsPersistent(icon), Is.True);
            Assert.That(AssetDatabase.Contains(icon), Is.True);
            string assetPath = AssetDatabase.GetAssetPath(icon);
            Assert.That(assetPath, Is.Not.Empty);
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(assetPath),
                Does.Contain(icon));
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }
    }
}
