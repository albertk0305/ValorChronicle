using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Bosses.Kragmor;

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

        [Test]
        public void Validate_DetectsDuplicateIdAcrossNewDefinitionKinds()
        {
            EffectDefinition effect =
                CreateDefinition<EffectDefinition>("shared_content_id");
            ResourceDefinition resource =
                CreateDefinition<ResourceDefinition>("shared_content_id");

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(
                    effects: new[] { effect },
                    resources: new[] { resource }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Duplicate content ID")), Is.True);
        }

        [Test]
        public void Validate_DetectsInvalidSkillKind()
        {
            SkillDefinition skill = CreateValidSkill(
                "skill_invalid_kind",
                SkillKind.Match);
            SetInt(skill, "skillKind", 99);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(skills: new[] { skill }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Skill kind is invalid")), Is.True);
        }

        [TestCase(SkillKind.Match)]
        [TestCase(SkillKind.Passive)]
        [TestCase(SkillKind.Active)]
        public void Validate_CharacterSkillIconIsOptional(SkillKind skillKind)
        {
            SkillDefinition skill = CreateValidSkill(
                $"skill_{skillKind.ToString().ToLowerInvariant()}",
                skillKind);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(skills: new[] { skill }));

            Assert.That(report.Issues.Any(issue =>
                issue.DefinitionId == skill.Id
                && issue.Message.Contains("Icon is missing")), Is.False);
        }

        [Test]
        public void Validate_BossActionStillWarnsWhenIconIsMissing()
        {
            SkillDefinition skill = CreateValidSkill(
                "boss_action_missing_icon",
                SkillKind.BossAction);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(skills: new[] { skill }));

            Assert.That(report.Issues.Any(issue =>
                issue.DefinitionId == skill.Id
                && issue.Severity == ValidationSeverity.Warning
                && issue.Message.Contains("Icon is missing")), Is.True);
        }

        [Test]
        public void Validate_EffectStillWarnsWhenIconIsMissing()
        {
            EffectDefinition effect =
                CreateDefinition<EffectDefinition>("effect_missing_icon");

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(effects: new[] { effect }));

            Assert.That(report.Issues.Any(issue =>
                issue.DefinitionId == effect.Id
                && issue.Severity == ValidationSeverity.Warning
                && issue.Message.Contains("Icon is missing")), Is.True);
        }

        [Test]
        public void Validate_DetectsDuplicateCharacterSkillReference()
        {
            SkillDefinition skill =
                CreateValidSkill("skill_duplicate_reference", SkillKind.Active);
            CharacterDefinition character =
                CreateValidCharacter("character_duplicate_reference");
            SetStringArray(
                character,
                "skillIds",
                skill.Id,
                skill.Id);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(
                    characters: new[] { character },
                    skills: new[] { skill }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Duplicate skill reference")), Is.True);
        }

        [Test]
        public void Validate_CharacterCannotReferenceBossAction()
        {
            SkillDefinition skill =
                CreateValidSkill("boss_action_test", SkillKind.BossAction);
            CharacterDefinition character =
                CreateValidCharacter("character_test");
            SetStringArray(character, "skillIds", skill.Id);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(
                    characters: new[] { character },
                    skills: new[] { skill }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("cannot reference BossAction")), Is.True);
        }

        [Test]
        public void Validate_BossActionListRejectsNonBossAction()
        {
            SkillDefinition skill =
                CreateValidSkill("active_test", SkillKind.Active);
            BossDefinition boss = CreateValidBoss("boss_test");
            SetStringArray(boss, "actionOrSkillIds", skill.Id);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(
                    bosses: new[] { boss },
                    skills: new[] { skill }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("must reference BossAction")), Is.True);
        }

        [Test]
        public void Validate_DetectsMissingBossActionReference()
        {
            BossDefinition boss = CreateValidBoss("boss_test");
            SetStringArray(
                boss,
                "actionOrSkillIds",
                "boss_action_not_registered");

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(bosses: new[] { boss }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Missing action or skill reference")),
                Is.True);
        }

        [Test]
        public void Validate_DetectsDuplicateBossActionReference()
        {
            SkillDefinition action =
                CreateValidSkill("boss_action_test", SkillKind.BossAction);
            BossDefinition boss = CreateValidBoss("boss_test");
            SetStringArray(
                boss,
                "actionOrSkillIds",
                action.Id,
                action.Id);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(
                    bosses: new[] { boss },
                    skills: new[] { action }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Duplicate action or skill reference")),
                Is.True);
        }

        [Test]
        public void Validate_NewMetadataMissing_ProducesWarningsOnly()
        {
            EffectDefinition effect =
                CreateDefinition<EffectDefinition>("effect_warning");
            ResourceDefinition resource =
                CreateDefinition<ResourceDefinition>("resource_warning");

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(
                    effects: new[] { effect },
                    resources: new[] { resource }));

            Assert.That(report.HasErrors, Is.False);
            Assert.That(report.Issues.Count(issue =>
                issue.Severity == ValidationSeverity.Warning),
                Is.EqualTo(6));
        }

        [Test]
        public void Validate_BossPresentationRequiresRegisteredBossAndDefaultSprite()
        {
            BossPresentationDefinition presentation =
                CreatePresentation("boss_missing", null);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(bossPresentations: new[] { presentation }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Missing boss reference")), Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("default sprite is missing")), Is.True);
        }

        [Test]
        public void Validate_DetectsDuplicateBossPresentationBossId()
        {
            BossDefinition boss = CreateValidBoss("boss_test");
            Sprite sprite = CreateSprite();
            BossPresentationDefinition first =
                CreatePresentation(boss.Id, sprite);
            BossPresentationDefinition second =
                CreatePresentation(boss.Id, sprite);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(
                    bosses: new[] { boss },
                    bossPresentations: new[] { first, second }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Duplicate boss presentation")), Is.True);
        }

        [Test]
        public void Validate_DetectsNullBossPresentation()
        {
            ValidationReport report = DataValidator.Validate(
                CreateDatabase(
                    bossPresentations:
                        new BossPresentationDefinition[] { null }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Null BossPresentationDefinition")),
                Is.True);
        }

        [Test]
        public void Validate_DetectsInvalidDuplicateAndNullBossVisualStates()
        {
            BossDefinition boss = CreateValidBoss("boss_test");
            Sprite sprite = CreateSprite();
            BossVisualStateEntry first =
                CreateVisualState("core_exposure", sprite);
            BossVisualStateEntry duplicate =
                CreateVisualState("core_exposure", null);
            BossVisualStateEntry empty = CreateVisualState(string.Empty, sprite);
            BossPresentationDefinition presentation =
                CreatePresentation(
                    boss.Id,
                    sprite,
                    first,
                    duplicate,
                    empty,
                    null);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(
                    bosses: new[] { boss },
                    bossPresentations: new[] { presentation }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Duplicate boss visual state ID")),
                Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Invalid boss visual state ID <empty>")),
                Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("visual state sprite is missing")),
                Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Null boss visual state")), Is.True);
        }

        [Test]
        public void Validate_ValidBossPresentation_HasNoErrors()
        {
            BossDefinition boss = CreateValidBoss("boss_test");
            SkillDefinition action =
                CreateValidSkill("boss_action_test", SkillKind.BossAction);
            SetStringArray(boss, "actionOrSkillIds", action.Id);
            Sprite sprite = CreateSprite();
            BossPresentationDefinition presentation =
                CreatePresentation(
                    boss.Id,
                    sprite,
                    CreateVisualState("core_exposure", sprite));

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(
                    bosses: new[] { boss },
                    skills: new[] { action },
                    bossPresentations: new[] { presentation }));

            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void Validate_MissingCharacterCombatConfig_IsWarningOnly()
        {
            CharacterDefinition character =
                CreateValidCharacter("character_without_config");

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(characters: new[] { character }));

            Assert.That(report.HasErrors, Is.False);
            Assert.That(report.Issues.Any(issue =>
                issue.Severity == ValidationSeverity.Warning
                && issue.Message.Contains("Combat config is missing")),
                Is.True);
        }

        [Test]
        public void Validate_InvalidCharacterCombatConfig_IsError()
        {
            CharacterDefinition character =
                CreateValidCharacter("character_invalid_config");
            MareaBluefangCombatConfig config =
                MareaBluefangTestConfig.Create(
                    activeCooldownTurns: -1);
            createdObjects.Add(config);
            SetPrivateField(character, "combatConfig", config);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(characters: new[] { character }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Combat config is invalid")),
                Is.True);
        }

        [Test]
        public void Validate_MissingBossCombatConfig_IsWarningOnly()
        {
            BossDefinition boss = CreateValidBoss("boss_without_config");

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(bosses: new[] { boss }));

            Assert.That(report.HasErrors, Is.False);
            Assert.That(report.Issues.Any(issue =>
                issue.Severity == ValidationSeverity.Warning
                && issue.Message.Contains("Combat config is missing")),
                Is.True);
        }

        [Test]
        public void Validate_InvalidBossCombatConfig_IsError()
        {
            BossDefinition boss = CreateValidBoss("boss_invalid_config");
            KragmorCombatConfig config = KragmorTestConfig.Create(
                rockshardRockCreationCount: -1);
            createdObjects.Add(config);
            SetPrivateField(boss, "combatConfig", config);

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(bosses: new[] { boss }));

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains("Combat config is invalid")),
                Is.True);
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

        private SkillDefinition CreateValidSkill(string id, SkillKind kind)
        {
            SkillDefinition definition =
                CreateDefinition<SkillDefinition>(id);
            SetString(definition, "displayNameKey", "skill.test.name");
            SetString(definition, "descriptionKey", "skill.test.description");
            SetInt(definition, "skillKind", (int)kind);
            return definition;
        }

        private BossPresentationDefinition CreatePresentation(
            string bossId,
            Sprite defaultSprite,
            params BossVisualStateEntry[] stateVisuals)
        {
            BossPresentationDefinition definition =
                ScriptableObject.CreateInstance<BossPresentationDefinition>();
            createdObjects.Add(definition);
            SetPrivateField(definition, "bossId", bossId);
            SetPrivateField(definition, "defaultSprite", defaultSprite);
            SetPrivateField(
                definition,
                "stateVisuals",
                stateVisuals ?? System.Array.Empty<BossVisualStateEntry>());
            return definition;
        }

        private static BossVisualStateEntry CreateVisualState(
            string id,
            Sprite sprite)
        {
            var entry = new BossVisualStateEntry();
            SetPrivateField(entry, "visualStateId", id);
            SetPrivateField(entry, "sprite", sprite);
            return entry;
        }

        private Sprite CreateSprite()
        {
            var texture = new Texture2D(1, 1);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                Vector2.zero);
            createdObjects.Add(sprite);
            createdObjects.Add(texture);
            return sprite;
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
            RelicDefinition[] relics = null,
            EffectDefinition[] effects = null,
            ResourceDefinition[] resources = null,
            BossPresentationDefinition[] bossPresentations = null)
        {
            DefinitionDatabase database = ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);

            var serializedObject = new SerializedObject(database);
            SetObjectArray(serializedObject.FindProperty("characters"), characters);
            SetObjectArray(serializedObject.FindProperty("bosses"), bosses);
            SetObjectArray(serializedObject.FindProperty("skills"), skills);
            SetObjectArray(serializedObject.FindProperty("effects"), effects);
            SetObjectArray(serializedObject.FindProperty("resources"), resources);
            SetObjectArray(serializedObject.FindProperty("relics"), relics);
            SetObjectArray(
                serializedObject.FindProperty("bossPresentations"),
                bossPresentations);
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

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            System.Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Field '{fieldName}' was not found.");
        }
    }
}
