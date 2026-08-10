using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class DataValidatorTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void Validate_DetectsDuplicateIds()
        {
            SkillDefinition first = CreateDefinition<SkillDefinition>("skill_duplicate");
            SkillDefinition second = CreateDefinition<SkillDefinition>("skill_duplicate");
            SetString(first, "displayNameKey", "skill.first");
            SetString(second, "displayNameKey", "skill.second");

            DefinitionDatabase database = CreateDatabase(
                skills: new[] { first, second });

            ValidationReport report = DataValidator.Validate(database);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(
                report.Issues.Any(issue => issue.Message.Contains("Duplicate")),
                Is.True);
        }

        [Test]
        public void Validate_DetectsInvalidCharacterStats()
        {
            CharacterDefinition character = CreateValidCharacter("character_invalid_stats");
            SetInt(character, "level1Hp", 0);
            DefinitionDatabase database = CreateDatabase(
                characters: new[] { character });

            ValidationReport report = DataValidator.Validate(database);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(
                report.Issues.Any(issue => issue.Message.Contains("Lv.1 HP")),
                Is.True);
        }

        [Test]
        public void Validate_DetectsMissingSkillReference()
        {
            CharacterDefinition character = CreateValidCharacter("character_missing_skill");
            SetStringArray(character, "skillIds", "skill_not_registered");
            DefinitionDatabase database = CreateDatabase(
                characters: new[] { character });

            ValidationReport report = DataValidator.Validate(database);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(
                report.Issues.Any(issue => issue.Message.Contains("Missing skill reference")),
                Is.True);
        }

        [Test]
        public void Validate_WarningOnly_DoesNotSetHasErrors()
        {
            SkillDefinition skill = CreateDefinition<SkillDefinition>("skill_warning_only");
            DefinitionDatabase database = CreateDatabase(skills: new[] { skill });

            ValidationReport report = DataValidator.Validate(database);

            Assert.That(report.HasErrors, Is.False);
            Assert.That(
                report.Issues.Any(issue => issue.Severity == ValidationSeverity.Warning),
                Is.True);
        }

        [Test]
        public void Validate_DetectsDuplicateBossDifficultyId()
        {
            BossDefinition boss = CreateValidBoss("boss_duplicate_difficulty");
            SetDifficultyStats(
                boss,
                new BossDifficultyStats("difficulty_normal", 100, 10d),
                new BossDifficultyStats("difficulty_normal", 200, 20d));

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(bosses: new[] { boss }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Duplicate difficulty ID")), Is.True);
        }

        [Test]
        public void Validate_DetectsEmptyBossDifficultyId()
        {
            BossDefinition boss = CreateValidBoss("boss_empty_difficulty");
            SetDifficultyStats(
                boss,
                new BossDifficultyStats(string.Empty, 100, 10d));

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(bosses: new[] { boss }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Invalid difficulty ID <empty>")),
                Is.True);
        }

        [Test]
        public void Validate_DetectsNonPositiveBossDifficultyMaxHp()
        {
            BossDefinition boss = CreateValidBoss("boss_invalid_hp");
            SetDifficultyStats(
                boss,
                new BossDifficultyStats("difficulty_normal", 0, 10d));

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(bosses: new[] { boss }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Difficulty Max HP")), Is.True);
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Validate_DetectsInvalidBossDifficultyAttack(double attack)
        {
            BossDefinition boss = CreateValidBoss("boss_invalid_attack");
            SetDifficultyStats(
                boss,
                new BossDifficultyStats(
                    "difficulty_normal",
                    100,
                    attack));

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(bosses: new[] { boss }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Difficulty ATK")), Is.True);
        }

        private CharacterDefinition CreateValidCharacter(string id)
        {
            CharacterDefinition definition = CreateDefinition<CharacterDefinition>(id);
            SetString(definition, "displayNameKey", "character.test");
            SetInt(definition, "level1Hp", 100);
            SetInt(definition, "level1Attack", 20);
            SetInt(definition, "level100Hp", 200);
            SetInt(definition, "level100Attack", 50);
            return definition;
        }

        private BossDefinition CreateValidBoss(string id)
        {
            BossDefinition definition = CreateDefinition<BossDefinition>(id);
            SetString(definition, "displayNameKey", "boss.test");
            SetInt(definition, "turnLimit", 25);
            return definition;
        }

        private TDefinition CreateDefinition<TDefinition>(string id)
            where TDefinition : GameDefinition
        {
            TDefinition definition = ScriptableObject.CreateInstance<TDefinition>();
            createdObjects.Add(definition);
            SetString(definition, "id", id);
            return definition;
        }

        private DefinitionDatabase CreateDatabase(
            CharacterDefinition[] characters = null,
            BossDefinition[] bosses = null,
            SkillDefinition[] skills = null,
            RelicDefinition[] relics = null)
        {
            DefinitionDatabase database = ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);

            var serializedObject = new SerializedObject(database);
            SetObjectArray(serializedObject.FindProperty("characters"), characters);
            SetObjectArray(serializedObject.FindProperty("bosses"), bosses);
            SetObjectArray(serializedObject.FindProperty("skills"), skills);
            SetObjectArray(serializedObject.FindProperty("relics"), relics);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return database;
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStringArray(
            Object target,
            string propertyName,
            params string[] values)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetDifficultyStats(
            BossDefinition boss,
            params BossDifficultyStats[] stats)
        {
            var serializedObject = new SerializedObject(boss);
            SerializedProperty property =
                serializedObject.FindProperty("difficultyStats");
            property.arraySize = stats.Length;
            for (int index = 0; index < stats.Length; index++)
            {
                SerializedProperty entry =
                    property.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("difficultyId").stringValue =
                    stats[index].DifficultyId;
                entry.FindPropertyRelative("maxHp").longValue =
                    stats[index].MaxHp;
                entry.FindPropertyRelative("attack").doubleValue =
                    stats[index].Attack;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray<TObject>(
            SerializedProperty property,
            TObject[] values)
            where TObject : Object
        {
            values ??= System.Array.Empty<TObject>();
            property.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
