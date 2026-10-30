using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.BaekHyeon;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonLookupDescriptionProviderTests
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
        public void SkillDescriptionsUseResolvedValuesAndStableKeys()
        {
            CharacterDefinition definition = Definition(Config());
            var provider = new BaekHyeonLookupDescriptionProvider();

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
                Is.EqualTo(BaekHyeonRules.CharacterId));
            Assert.That(match3.Title.Key,
                Is.EqualTo("baek_hyeon.skill.match3.name"));
            Assert.That(match4.Title.Key,
                Is.EqualTo("baek_hyeon.skill.match4.name"));
            Assert.That(match5.Title.Key,
                Is.EqualTo("baek_hyeon.skill.match5.name"));
            Assert.That(passive.Title.Key,
                Is.EqualTo("baek_hyeon.skill.passive.name"));
            Assert.That(active.Title.Key,
                Is.EqualTo("baek_hyeon.skill.active.name"));

            AssertArgument(match3, "base_coefficient", "70");
            AssertArgument(match3, "per_mark_coefficient", "3");
            AssertArgument(match3, "mark_gain", 1);
            AssertArgument(match3, "max_coefficient", "100");
            AssertArgument(match4, "base_coefficient", "135");
            AssertArgument(match4, "per_mark_coefficient", "5");
            AssertArgument(match4, "mark_gain", 2);
            AssertArgument(match4, "max_coefficient", "185");
            AssertArgument(match5, "base_coefficient", "200");
            AssertArgument(match5, "per_mark_coefficient", "8");
            AssertArgument(match5, "mark_gain", 3);
            AssertArgument(match5, "max_coefficient", "280");
            AssertArgument(passive,
                "additional_damage_increase_per_mark", "3.5");
            AssertArgument(passive,
                "additional_damage_increase_max", "35");
            AssertArgument(active, "base_coefficient", "280");
            AssertArgument(active, "per_mark_coefficient", "22");
            AssertArgument(active, "max_coefficient", "500");
            AssertArgument(active, "cooldown", 8);
        }

        [Test]
        public void AwakeningDescriptionsExposeEveryChangedValue()
        {
            CharacterDefinition definition = Definition(Config());
            var provider = new BaekHyeonLookupDescriptionProvider();

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
            AssertArgument(awakening3, "per_mark_before", "3");
            AssertArgument(awakening3, "per_mark_after", "3.5");
            AssertArgument(awakening3, "maximum_before", "30");
            AssertArgument(awakening3, "maximum_after", "35");
            AssertArgument(awakening4, "match3_before", "70");
            AssertArgument(awakening4, "match3_after", "75");
            AssertArgument(awakening4, "match4_before", "125");
            AssertArgument(awakening4, "match4_after", "135");
            AssertArgument(awakening5, "base_before", "250");
            AssertArgument(awakening5, "base_after", "280");
            AssertArgument(awakening5, "per_mark_before", "20");
            AssertArgument(awakening5, "per_mark_after", "22");
            AssertArgument(awakening6, "damage_increase", "8");
            AssertArgument(awakening6, "duration", 2);

            for (int stage = 1; stage <= 6; stage++)
            {
                CharacterLookupDescriptionDefinition description =
                    provider.ResolveAwakening(definition, stage);
                Assert.That(description.Title.Key,
                    Is.EqualTo("character.lookup.awakening.title"));
                Assert.That(description.Sections[0].Key,
                    Is.EqualTo(
                        $"baek_hyeon.awakening.{stage}.description"));
                Assert.That(description.Title.Arguments["stage"],
                    Is.EqualTo(stage));
                var name = (CharacterLookupLocalizedText)description
                    .Title.Arguments["name"];
                Assert.That(name.Key,
                    Is.EqualTo($"baek_hyeon.awakening.{stage}.name"));
            }
        }

        [Test]
        public void ConfigChangesFlowIntoLookupArguments()
        {
            BaekHyeonCombatConfig config = Config();
            BaekHyeonTestConfig.SetField(
                config,
                "match3BaseCoefficient",
                0.72d);
            BaekHyeonTestConfig.SetField(
                config,
                "match3MarkGainAmount",
                4);
            BaekHyeonTestConfig.SetField(
                config,
                "activeCooldownTurns",
                9);
            CharacterDefinition definition = Definition(config);
            var provider = new BaekHyeonLookupDescriptionProvider();

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

            AssertArgument(match, "base_coefficient", "72");
            AssertArgument(match, "mark_gain", 4);
            AssertArgument(active, "cooldown", 9);
        }

        [Test]
        public void ProviderRejectsWrongDefinitionConfigIdsAndStages()
        {
            CharacterDefinition definition = Definition(Config());
            var provider = new BaekHyeonLookupDescriptionProvider();

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

        private BaekHyeonCombatConfig Config()
        {
            BaekHyeonCombatConfig config = BaekHyeonTestConfig.Create();
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
                BaekHyeonRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Grass;
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
