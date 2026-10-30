using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Serena
{
    public sealed class SerenaCoralspearLookupDescriptionProviderTests
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
        public void SkillDescriptionsUseResolvedAwakeningValues()
        {
            CharacterDefinition definition = Definition();
            var provider = new SerenaCoralspearLookupDescriptionProvider();

            CharacterLookupDescriptionDefinition match4Before =
                provider.ResolveSkill(
                    definition,
                    awakening: 0,
                    CharacterLookupSkillType.Match4);
            CharacterLookupDescriptionDefinition match4After =
                provider.ResolveSkill(
                    definition,
                    awakening: 4,
                    CharacterLookupSkillType.Match4);
            CharacterLookupDescriptionDefinition passive =
                provider.ResolveSkill(
                    definition,
                    awakening: 3,
                    CharacterLookupSkillType.Passive);
            CharacterLookupDescriptionDefinition activeBefore =
                provider.ResolveSkill(
                    definition,
                    awakening: 4,
                    CharacterLookupSkillType.Active);
            CharacterLookupDescriptionDefinition activeAfter =
                provider.ResolveSkill(
                    definition,
                    awakening: 5,
                    CharacterLookupSkillType.Active);
            CharacterLookupDescriptionDefinition match5After =
                provider.ResolveSkill(
                    definition,
                    awakening: 6,
                    CharacterLookupSkillType.Match5);

            Assert.That(provider.CharacterId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(match4Before.Title.Key,
                Is.EqualTo("serena.skill.match4.name"));
            AssertArgument(match4Before, "base_coefficient", "135");
            AssertArgument(match4Before, "additional_coefficient", "90");
            AssertArgument(match4After, "base_coefficient", "150");
            AssertArgument(match4After, "additional_coefficient", "100");
            AssertArgument(passive, "per_water_coefficient", "85");
            AssertArgument(passive, "damage_increase", "10");
            AssertArgument(passive, "empowered_damage_increase", "80");
            AssertArgument(passive, "max_charges", 3);
            AssertArgument(activeBefore, "damage_increase", "35");
            AssertArgument(activeAfter, "damage_increase", "45");
            AssertArgument(activeAfter, "duration", 3);
            AssertArgument(activeAfter, "cooldown", 8);
            AssertArgument(match5After, "water_gain", 1);
            AssertArgument(match5After, "empowered_chase_gain", 1);
        }

        [Test]
        public void AwakeningDescriptionsExposeEveryChangedValue()
        {
            CharacterDefinition definition = Definition();
            var provider = new SerenaCoralspearLookupDescriptionProvider();

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
            AssertArgument(awakening2, "damage_before", "0");
            AssertArgument(awakening2, "damage_after", "10");
            AssertArgument(awakening3, "chase_before", "75");
            AssertArgument(awakening3, "chase_after", "85");
            AssertArgument(awakening4, "match3_before", "75");
            AssertArgument(awakening4, "match3_after", "85");
            AssertArgument(awakening4, "match4_base_before", "135");
            AssertArgument(awakening4, "match4_base_after", "150");
            AssertArgument(
                awakening4,
                "match4_additional_before",
                "90");
            AssertArgument(
                awakening4,
                "match4_additional_after",
                "100");
            AssertArgument(awakening5, "active_before", "35");
            AssertArgument(awakening5, "active_after", "45");
            AssertArgument(awakening6, "water_before", 0);
            AssertArgument(awakening6, "water_after", 1);

            for (int stage = 1; stage <= 6; stage++)
            {
                CharacterLookupDescriptionDefinition description =
                    provider.ResolveAwakening(definition, stage);
                Assert.That(description.Title.Key,
                    Is.EqualTo("character.lookup.awakening.title"));
                Assert.That(description.Sections[0].Key,
                    Is.EqualTo($"serena.awakening.{stage}.description"));
                Assert.That(description.Title.Arguments["stage"],
                    Is.EqualTo(stage));
                Assert.That(description.Title.Arguments["name"],
                    Is.TypeOf<CharacterLookupLocalizedText>());
            }
        }

        [Test]
        public void TestRuntimeRegistrationProjectsAllSerenaComponents()
        {
            var registration = new CharacterRuntimeRegistration(
                SerenaCoralspearRules.CharacterId,
                () => new SerenaCoralspearBuildModifier(),
                () => new SerenaCoralspearCombatProviderRegistrar(),
                () => new SerenaCoralspearLookupDescriptionProvider());
            var isolatedCatalog = new CharacterRuntimeRegistrationCatalog(
                new[] { registration });
            CharacterRuntimeRegistrationCatalog defaultCatalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();

            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(SerenaCoralspearCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<SerenaCoralspearBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<SerenaCoralspearCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<SerenaCoralspearLookupDescriptionProvider>());
            Assert.That(isolatedCatalog.Registrations.Count, Is.EqualTo(1));
            Assert.That(defaultCatalog.Registrations.Count, Is.EqualTo(8));
            Assert.That(defaultCatalog.Registrations.Any(item =>
                item.CharacterId == MareaBluefangRules.CharacterId),
                Is.True);
            Assert.That(defaultCatalog.Registrations.Any(item =>
                item.CharacterId == SerenaCoralspearRules.CharacterId),
                Is.True);
            Assert.That(defaultCatalog.Registrations.Any(item =>
                item.CharacterId == ShimotsukiMiyukiRules.CharacterId),
                Is.True);
        }

        [Test]
        public void ProviderRejectsWrongDefinitionAndInvalidStages()
        {
            CharacterDefinition definition = Definition();
            var provider = new SerenaCoralspearLookupDescriptionProvider();

            Assert.Throws<ArgumentNullException>(() =>
                provider.ResolveSkill(
                    null,
                    0,
                    CharacterLookupSkillType.Match3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveAwakening(definition, 0));

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = "wrong_character";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<ArgumentException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3));
        }

        private CharacterDefinition Definition()
        {
            SerenaCoralspearCombatConfig config =
                SerenaCoralspearTestConfig.Create();
            createdObjects.Add(config);
            CharacterDefinition definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                SerenaCoralspearRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Water;
            serialized.FindProperty("level1Hp").intValue = 870;
            serialized.FindProperty("level1Attack").intValue = 170;
            serialized.FindProperty("level100Hp").intValue = 3200;
            serialized.FindProperty("level100Attack").intValue = 980;
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
            Assert.That(description.Sections[0].Arguments.ContainsKey(key),
                Is.True,
                key);
            Assert.That(description.Sections[0].Arguments[key],
                Is.EqualTo(expected),
                key);
        }
    }
}
