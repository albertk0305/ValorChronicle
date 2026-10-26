using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodLookupDescriptionProviderTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void SkillDescriptionsUseResolvedConfigValues()
        {
            CharacterDefinition definition = Definition(Config());
            var provider = new BrinaAshwoodLookupDescriptionProvider();

            CharacterLookupDescriptionDefinition match3 =
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3);
            CharacterLookupDescriptionDefinition match4 =
                provider.ResolveSkill(
                    definition,
                    4,
                    CharacterLookupSkillType.Match4);
            CharacterLookupDescriptionDefinition match5 =
                provider.ResolveSkill(
                    definition,
                    6,
                    CharacterLookupSkillType.Match5);
            CharacterLookupDescriptionDefinition passive =
                provider.ResolveSkill(
                    definition,
                    3,
                    CharacterLookupSkillType.Passive);
            CharacterLookupDescriptionDefinition active =
                provider.ResolveSkill(
                    definition,
                    5,
                    CharacterLookupSkillType.Active);

            Assert.That(provider.CharacterId,
                Is.EqualTo(BrinaAshwoodRules.CharacterId));
            Assert.That(match3.Title.Key,
                Is.EqualTo("brina.skill.match3.name"));
            AssertArgument(match3, "coefficient", "130");
            AssertArgument(match3, "woodcut_gain", 1);
            AssertArgument(match4, "coefficient", "250");
            AssertArgument(match4, "woodcut_gain", 2);
            AssertArgument(match5, "coefficient", "400");
            AssertArgument(match5, "woodcut_gain", 3);
            AssertArgument(passive, "threshold", 10);
            AssertArgument(passive, "coefficient", "525");
            AssertArgument(active, "woodcut_multiplier", "200");
            AssertArgument(active, "duration", 1);
            AssertArgument(active, "cooldown", 7);
        }

        [Test]
        public void AwakeningDescriptionsExposeEveryChangedValue()
        {
            CharacterDefinition definition = Definition(Config());
            var provider = new BrinaAshwoodLookupDescriptionProvider();

            CharacterLookupDescriptionDefinition awakening1 =
                provider.ResolveAwakening(definition, 1);
            CharacterLookupDescriptionDefinition awakening2 =
                provider.ResolveAwakening(definition, 2);
            CharacterLookupDescriptionDefinition awakening3 =
                provider.ResolveAwakening(definition, 3);
            CharacterLookupDescriptionDefinition awakening4 =
                provider.ResolveAwakening(definition, 4);
            CharacterLookupDescriptionDefinition awakening5 =
                provider.ResolveAwakening(definition, 5);
            CharacterLookupDescriptionDefinition awakening6 =
                provider.ResolveAwakening(definition, 6);

            AssertArgument(awakening1, "multiplier_before", "100");
            AssertArgument(awakening1, "multiplier_after", "105");
            AssertArgument(awakening2, "multiplier_before", "100");
            AssertArgument(awakening2, "multiplier_after", "105");
            AssertArgument(awakening3, "coefficient_before", "500");
            AssertArgument(awakening3, "coefficient_after", "525");
            AssertArgument(awakening4, "match3_before", "130");
            AssertArgument(awakening4, "match3_after", "135");
            AssertArgument(awakening4, "match4_before", "240");
            AssertArgument(awakening4, "match4_after", "250");
            AssertArgument(awakening5, "cooldown_before", 8);
            AssertArgument(awakening5, "cooldown_after", 7);
            AssertArgument(awakening6, "first_gain_before", 0);
            AssertArgument(awakening6, "first_gain_after", 1);

            for (int stage = 1; stage <= 6; stage++)
            {
                CharacterLookupDescriptionDefinition description =
                    provider.ResolveAwakening(definition, stage);
                Assert.That(description.Title.Key,
                    Is.EqualTo("character.lookup.awakening.title"));
                Assert.That(description.Sections[0].Key,
                    Is.EqualTo($"brina.awakening.{stage}.description"));
                Assert.That(description.Title.Arguments["stage"],
                    Is.EqualTo(stage));
                Assert.That(description.Title.Arguments["name"],
                    Is.TypeOf<CharacterLookupLocalizedText>());
            }
        }

        [Test]
        public void ConfigChangesFlowIntoLookupArguments()
        {
            BrinaAshwoodCombatConfig config = Config();
            BrinaAshwoodTestConfig.SetDouble(
                config,
                "match3Coefficient",
                1.32d);
            BrinaAshwoodTestConfig.SetInt(
                config,
                "match3WoodcutGainAmount",
                4);
            BrinaAshwoodTestConfig.SetDouble(
                config,
                "activeWoodcutGainMultiplier",
                3d);
            BrinaAshwoodTestConfig.SetInt(
                config,
                "activeCooldownTurns",
                9);
            CharacterDefinition definition = Definition(config);
            var provider = new BrinaAshwoodLookupDescriptionProvider();

            CharacterLookupDescriptionDefinition match =
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3);
            CharacterLookupDescriptionDefinition active =
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Active);

            AssertArgument(match, "coefficient", "132");
            AssertArgument(match, "woodcut_gain", 4);
            AssertArgument(active, "woodcut_multiplier", "300");
            AssertArgument(active, "cooldown", 9);
        }

        [Test]
        public void InMemoryRuntimeRegistrationProjectsAllComponents()
        {
            var registration = new CharacterRuntimeRegistration(
                BrinaAshwoodRules.CharacterId,
                () => new BrinaAshwoodBuildModifier(),
                () => new BrinaAshwoodCombatProviderRegistrar(),
                () => new BrinaAshwoodLookupDescriptionProvider());
            var isolatedCatalog = new CharacterRuntimeRegistrationCatalog(
                new[] { registration });
            CharacterRuntimeRegistrationCatalog defaultCatalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();

            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(BrinaAshwoodCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<BrinaAshwoodBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<BrinaAshwoodCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<BrinaAshwoodLookupDescriptionProvider>());
            Assert.That(isolatedCatalog.Registrations.Count,
                Is.EqualTo(1));
            Assert.That(defaultCatalog.Registrations.Any(item =>
                item.CharacterId == BrinaAshwoodRules.CharacterId),
                Is.True);
        }

        [Test]
        public void ProviderRejectsWrongDefinitionConfigIdsAndStages()
        {
            CharacterDefinition definition = Definition(Config());
            var provider = new BrinaAshwoodLookupDescriptionProvider();

            Assert.Throws<ArgumentNullException>(() =>
                provider.ResolveSkill(
                    null,
                    0,
                    CharacterLookupSkillType.Match3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveSkill(
                    definition,
                    -1,
                    CharacterLookupSkillType.Match3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveAwakening(definition, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveAwakening(definition, 7));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    (CharacterLookupSkillType)999));

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = "wrong_character";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<ArgumentException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3));

            CharacterDefinition missingConfig = Definition(null);
            Assert.Throws<InvalidOperationException>(() =>
                provider.ResolveSkill(
                    missingConfig,
                    0,
                    CharacterLookupSkillType.Match3));
        }

        private BrinaAshwoodCombatConfig Config()
        {
            BrinaAshwoodCombatConfig config =
                BrinaAshwoodTestConfig.Create();
            createdObjects.Add(config);
            return config;
        }

        private CharacterDefinition Definition(
            CombatConfigDefinition config)
        {
            CharacterDefinition definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                BrinaAshwoodRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Grass;
            serialized.FindProperty("level1Hp").intValue = 1000;
            serialized.FindProperty("level1Attack").intValue = 1000;
            serialized.FindProperty("level100Hp").intValue = 2000;
            serialized.FindProperty("level100Attack").intValue = 2000;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void AssertArgument(
            CharacterLookupDescriptionDefinition description,
            string key,
            object expected)
        {
            Assert.That(
                description.Sections[0].Arguments.ContainsKey(key),
                Is.True,
                key);
            Assert.That(
                description.Sections[0].Arguments[key],
                Is.EqualTo(expected),
                key);
        }
    }
}
